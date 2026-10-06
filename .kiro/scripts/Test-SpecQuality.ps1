[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Spec
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RequirementMarker = "$([char]0x9700)$([char]0x6C42)"
$DesignMarker = "$([char]0x8BBE)$([char]0x8BA1)"
$SectionPrefix = [string][char]0x00A7

function Add-Failure {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[string]]$Failures,

        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $Failures.Add($Message)
}

function Resolve-SpecPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SpecValue
    )

    if (Test-Path -LiteralPath $SpecValue) {
        return (Resolve-Path -LiteralPath $SpecValue).Path
    }

    $kiroRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
    $candidate = Join-Path $kiroRoot "specs\$SpecValue"
    if (Test-Path -LiteralPath $candidate) {
        return (Resolve-Path -LiteralPath $candidate).Path
    }

    throw "Spec path not found: $SpecValue"
}

function Split-Refs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value -split "," |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_.Length -gt 0 }
}

function Read-Utf8Text {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Get-RequirementIds {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string[]]$Lines
    )

    $ids = [System.Collections.Generic.HashSet[string]]::new()
    $currentRequirement = $null
    $requirementHeadingPattern = "^###\s+$([regex]::Escape($RequirementMarker))\s+([0-9]+[A-Za-z]?)"

    foreach ($line in $Lines) {
        if ($line -match $requirementHeadingPattern) {
            $currentRequirement = $Matches[1]
            continue
        }

        if ($null -ne $currentRequirement -and $line -match "^\s*([0-9]+)\.\s+(THE|WHEN|IF|WHILE|WHERE)\b") {
            [void]$ids.Add("$currentRequirement.$($Matches[1])")
        }
    }

    return ,$ids
}

function Get-DesignHeadings {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string[]]$Lines
    )

    $headings = [System.Collections.Generic.HashSet[string]]::new()

    foreach ($line in $Lines) {
        if ($line -match "^#{2,4}\s+(.+?)\s*$") {
            [void]$headings.Add($Matches[1].Trim())
        }
    }

    return ,$headings
}

$specPath = Resolve-SpecPath -SpecValue $Spec
$requiredFiles = @("requirements.md", "design.md", "tasks.md", ".config.kiro")
$failures = [System.Collections.Generic.List[string]]::new()

foreach ($fileName in $requiredFiles) {
    $path = Join-Path $specPath $fileName
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Failure $failures "Missing required file: $fileName"
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

$requirementsText = Read-Utf8Text -Path (Join-Path $specPath "requirements.md")
$designText = Read-Utf8Text -Path (Join-Path $specPath "design.md")
$tasksText = Read-Utf8Text -Path (Join-Path $specPath "tasks.md")

$requirementsLines = $requirementsText -split "\r?\n"
$designLines = $designText -split "\r?\n"

$requirementIds = Get-RequirementIds -Lines $requirementsLines
$designHeadings = Get-DesignHeadings -Lines $designLines

if ($requirementIds.Count -eq 0) {
    Add-Failure $failures "No requirement acceptance criteria found"
}

if ($designHeadings.Count -eq 0) {
    Add-Failure $failures "No design headings found"
}

$taskRequirementRefs = [System.Collections.Generic.HashSet[string]]::new()
$taskRequirementPattern = "_$([regex]::Escape($RequirementMarker)):\s*([^_]+)_"
foreach ($match in [regex]::Matches($tasksText, $taskRequirementPattern)) {
    foreach ($ref in Split-Refs -Value $match.Groups[1].Value) {
        if ($ref -notmatch "^[0-9]+[A-Za-z]?\.[0-9]+$") {
            Add-Failure $failures "Invalid requirement ref format: $ref"
            continue
        }

        if (-not $requirementIds.Contains($ref)) {
            Add-Failure $failures "Task references missing requirement: $ref"
            continue
        }

        [void]$taskRequirementRefs.Add($ref)
    }
}

if ($taskRequirementRefs.Count -eq 0) {
    Add-Failure $failures "No task requirement refs found"
}

foreach ($id in $requirementIds) {
    if (-not $taskRequirementRefs.Contains($id)) {
        Add-Failure $failures "Requirement is not covered by any task: $id"
    }
}

$taskDesignRefCount = 0
$taskDesignPattern = "_$([regex]::Escape($DesignMarker)):\s*([^_]+)_"
foreach ($match in [regex]::Matches($tasksText, $taskDesignPattern)) {
    foreach ($ref in Split-Refs -Value $match.Groups[1].Value) {
        $taskDesignRefCount++

        if (-not $ref.StartsWith($SectionPrefix, [System.StringComparison]::Ordinal)) {
            Add-Failure $failures "Design ref must start with ${SectionPrefix}: $ref"
            continue
        }

        $heading = $ref.Substring(1)
        if (-not $designHeadings.Contains($heading)) {
            Add-Failure $failures "Task references missing design heading: $ref"
        }
    }
}

if ($taskDesignRefCount -eq 0) {
    Add-Failure $failures "No task design refs found"
}

if ($failures.Count -gt 0) {
    Write-Host "Spec quality check failed for $specPath"
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "Spec quality check passed for $specPath"
Write-Host "Requirements: $($requirementIds.Count)"
Write-Host "Task requirement refs: $($taskRequirementRefs.Count)"
Write-Host "Design headings: $($designHeadings.Count)"
