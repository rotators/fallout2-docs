[CmdletBinding()]
param(
    [string]$Root
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Root)) {
    $scriptDirectory = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $Root = (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

$rootPath = (Resolve-Path -LiteralPath $Root).Path
$sitePath = Join-Path $rootPath '_site'
$contentPagesPath = Join-Path $rootPath 'content/pages'
$errors = New-Object System.Collections.Generic.List[string]

function Add-ValidationError {
    param([string]$Message)
    $script:errors.Add($Message)
}

function Get-FrontMatter {
    param([string]$Path)

    $metadata = [ordered]@{}
    $lines = Get-Content -LiteralPath $Path
    if ($lines.Count -eq 0 -or $lines[0] -ne '---') {
        return $metadata
    }

    for ($index = 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -eq '---') {
            break
        }

        $separator = $lines[$index].IndexOf(':')
        if ($separator -le 0) {
            continue
        }

        $key = $lines[$index].Substring(0, $separator).Trim()
        $value = $lines[$index].Substring($separator + 1).Trim().Trim('"')
        $metadata[$key] = $value
    }

    return $metadata
}

function Get-LocalReferenceTarget {
    param(
        [string]$PagePath,
        [string]$Reference
    )

    if ($Reference -match '^[a-z][a-z0-9+.-]*:' -or $Reference.StartsWith('//')) {
        return $null
    }

    $hashIndex = $Reference.IndexOf('#')
    $pathAndQuery = if ($hashIndex -ge 0) { $Reference.Substring(0, $hashIndex) } else { $Reference }
    $fragment = if ($hashIndex -ge 0) { $Reference.Substring($hashIndex + 1) } else { '' }
    $queryIndex = $pathAndQuery.IndexOf('?')
    $referencePath = if ($queryIndex -ge 0) { $pathAndQuery.Substring(0, $queryIndex) } else { $pathAndQuery }

    if ($referencePath.Length -eq 0) {
        $targetPath = $PagePath
    } elseif ($referencePath.StartsWith('/')) {
        $targetPath = Join-Path $sitePath $referencePath.TrimStart('/').Replace('/', [IO.Path]::DirectorySeparatorChar)
    } else {
        $pageDirectory = Split-Path -Parent $PagePath
        $targetPath = Join-Path $pageDirectory $referencePath.Replace('/', [IO.Path]::DirectorySeparatorChar)
    }

    try {
        $targetPath = [Uri]::UnescapeDataString($targetPath)
    } catch {
        return @{
            Path = $targetPath
            Fragment = $fragment
        }
    }

    return @{
        Path = $targetPath
        Fragment = $fragment
    }
}

function Get-PageIds {
    param([string]$Path)

    $ids = New-Object System.Collections.Generic.HashSet[string]
    $html = Get-Content -LiteralPath $Path -Raw
    foreach ($match in [regex]::Matches($html, '(?i)\s(?:id|name)="([^"]+)"')) {
        [void]$ids.Add($match.Groups[1].Value)
    }

    return $ids
}

if (-not (Test-Path -LiteralPath $sitePath -PathType Container)) {
    Add-ValidationError "Missing generated site directory: $sitePath"
} else {
    if (-not (Test-Path -LiteralPath (Join-Path $sitePath 'style.css') -PathType Leaf)) {
        Add-ValidationError 'Generated site is missing style.css.'
    }

    $rootHtmlFiles = Get-ChildItem -LiteralPath $rootPath -Filter '*.html' -File
    foreach ($file in $rootHtmlFiles) {
        Add-ValidationError "Root-level generated HTML remains: $($file.Name)"
    }

    $pageMetadata = @()
    foreach ($page in Get-ChildItem -LiteralPath $contentPagesPath -Filter '*.md' -File) {
        $metadata = Get-FrontMatter -Path $page.FullName
        if (-not $metadata.Contains('output')) {
            Add-ValidationError "Missing output frontmatter: $($page.FullName)"
            continue
        }

        $outputPath = Join-Path $sitePath $metadata['output']
        $pageMetadata += [pscustomobject]@{
            Source = $page.FullName
            Output = $outputPath
            Metadata = $metadata
        }

        if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
            Add-ValidationError "Missing generated page for $($page.Name): $($metadata['output'])"
        }
    }

    foreach ($entry in $pageMetadata) {
        if (-not (Test-Path -LiteralPath $entry.Output -PathType Leaf)) {
            continue
        }

        $html = Get-Content -LiteralPath $entry.Output -Raw
        if ($entry.Metadata.Contains('width') -and $entry.Metadata['width'] -eq 'full' -and $html -notmatch '<div class="page-full">') {
            Add-ValidationError "Full-width page is missing page-full class: $($entry.Metadata['output'])"
        }

        if ($entry.Metadata.Contains('toc') -and $entry.Metadata['toc'] -eq 'auto' -and $html -notmatch '<nav class="floating-toc"') {
            Add-ValidationError "Auto-TOC page is missing floating TOC: $($entry.Metadata['output'])"
        }

        if ($html -match '{{[^}]+}}') {
            Add-ValidationError "Unresolved template token in generated page: $($entry.Metadata['output'])"
        }
    }

    $indexPath = Join-Path $sitePath 'index.html'
    if ((Test-Path -LiteralPath $indexPath -PathType Leaf) -and (Get-Content -LiteralPath $indexPath -Raw) -notmatch '<nav class="floating-toc"') {
        Add-ValidationError 'index.html is missing its floating table of contents.'
    }

    $idCache = @{}
    foreach ($page in Get-ChildItem -LiteralPath $sitePath -Filter '*.html' -File -Recurse) {
        $html = Get-Content -LiteralPath $page.FullName -Raw
        foreach ($match in [regex]::Matches($html, '(?i)\s(?:href|src)="([^"]+)"')) {
            $reference = $match.Groups[1].Value
            $target = Get-LocalReferenceTarget -PagePath $page.FullName -Reference $reference
            if ($null -eq $target) {
                continue
            }

            $targetPath = [IO.Path]::GetFullPath($target.Path)
            if (-not $targetPath.StartsWith($sitePath, [StringComparison]::OrdinalIgnoreCase)) {
                Add-ValidationError "Reference escapes _site from $($page.Name): $reference"
                continue
            }

            if (-not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
                Add-ValidationError "Missing local reference from $($page.Name): $reference"
                continue
            }

            if ($target.Fragment.Length -gt 0 -and [IO.Path]::GetExtension($targetPath).Equals('.html', [StringComparison]::OrdinalIgnoreCase)) {
                if (-not $idCache.ContainsKey($targetPath)) {
                    $idCache[$targetPath] = Get-PageIds -Path $targetPath
                }

                $fragment = [Uri]::UnescapeDataString($target.Fragment)
                if (-not $idCache[$targetPath].Contains($fragment)) {
                    Add-ValidationError "Missing anchor from $($page.Name): $reference"
                }
            }
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Validation failed with $($errors.Count) issue(s):"
    foreach ($errorMessage in $errors) {
        Write-Host " - $errorMessage"
    }
    exit 1
}

Write-Host 'Validation passed.'
