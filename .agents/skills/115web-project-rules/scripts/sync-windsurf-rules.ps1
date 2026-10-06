[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepositoryRoot))
{
    $RepositoryRoot = [System.IO.Path]::GetFullPath(
        (Join-Path $PSScriptRoot '..\..\..\..'))
}
else
{
    $RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
}

$sourceRoot = Join-Path $RepositoryRoot '.windsurf'
$referenceRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..\references'))

if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container))
{
    throw "Windsurf source directory was not found: $sourceRoot"
}

$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)

function Remove-GeneratedMarkdown
{
    param(
        [Parameter(Mandatory)]
        [string]$DirectoryPath
    )

    if (-not (Test-Path -LiteralPath $DirectoryPath -PathType Container))
    {
        return
    }

    Get-ChildItem -LiteralPath $DirectoryPath -Filter '*.md' -File |
        ForEach-Object {
            $firstLine = Get-Content -LiteralPath $_.FullName -TotalCount 1
            if ($firstLine -like '<!-- Generated from .windsurf/* by scripts/sync-windsurf-rules.ps1. -->')
            {
                Remove-Item -LiteralPath $_.FullName
            }
        }
}

function Convert-WindsurfMarkdown
{
    param(
        [Parameter(Mandatory)]
        [string]$SourcePath,

        [Parameter(Mandatory)]
        [string]$DestinationPath
    )

    $content = [System.IO.File]::ReadAllText($SourcePath)
    $content = $content -replace "^\uFEFF", ''

    if ($content -match '(?s)\A---\r?\n.*?\r?\n---\r?\n')
    {
        $content = $content.Substring($Matches[0].Length)
    }

    $content = $content -replace '(?m)^\s*// turbo\s*\r?\n', ''
    $content = $content.Replace('.windsurf/rules/', 'references/rules/')
    $content = $content.Replace('/code-standards-workflow', '$115web-project-rules')
    $content = $content.Replace('/code-audit-workflow', '$115web-project-rules')
    $content = $content.TrimStart("`r", "`n")

    $sourceRelativePath = [System.IO.Path]::GetRelativePath(
        $RepositoryRoot,
        $SourcePath).Replace('\', '/')
    $header = "<!-- Generated from $sourceRelativePath by scripts/sync-windsurf-rules.ps1. -->`n`n"
    $destinationDirectory = Split-Path -Parent $DestinationPath
    [System.IO.Directory]::CreateDirectory($destinationDirectory) | Out-Null
    [System.IO.File]::WriteAllText(
        $DestinationPath,
        $header + $content.TrimEnd() + "`n",
        $utf8WithoutBom)
}

$mappings = @(
    @{ Source = 'rules'; Destination = 'rules' },
    @{ Source = 'workflows'; Destination = 'workflows' }
)

$convertedCount = 0

foreach ($mapping in $mappings)
{
    $sourceDirectory = Join-Path $sourceRoot $mapping.Source
    $destinationDirectory = Join-Path $referenceRoot $mapping.Destination
    Remove-GeneratedMarkdown -DirectoryPath $destinationDirectory

    if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container))
    {
        continue
    }

    Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.md' -File |
        Sort-Object Name |
        ForEach-Object {
            $destinationPath = Join-Path $destinationDirectory $_.Name
            Convert-WindsurfMarkdown -SourcePath $_.FullName -DestinationPath $destinationPath
            $convertedCount++
        }
}

Write-Output "Converted $convertedCount Windsurf markdown files into $referenceRoot"
