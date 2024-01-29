<#
.SYNOPSIS
    Merges markdown files into a single file.

.DESCRIPTION
    This script merges markdown files into a single file. It requires an index file describing the source markdown file as well as other resource directories to copy.

    Example input json file content:
    {
        "files": [
            "./../markdown/file1.md",
            "./../markdown/file2.md",
            "./../markdown/file3.md"
        ],
        "assets": [
            "./../markdown/images"
        ],
        "style": "@media print { .break-page { page-break-before: always;} }",
        "pageBreak": "<div class='break-page'></div>"
    }

.PARAMETER Input
    The file path to the index JSON file.

.PARAMETER Output
    The file path for the merged markdown file.

.INPUTS
    None. You cannot pipe objects to this script.

.OUTPUTS
    A merged markdown file

.EXAMPLE
    PS> .\merge-markdown.ps1 -i merge-markdown-input.json -o merged.md

.LINK
    No Links
#>
param(
    [Parameter()]
    [Alias("i")]
    [string] $Index = "$PSScriptRoot\merge-markdown-input.json",

    
    [Parameter()]
    [Alias("o")]
    [string] $Output = "$PSScriptRoot\out\readme.md"
)

if ([string]::IsNullOrEmpty($Index)) {
    throw "Index file path was null or empty."
}

if ([string]::IsNullOrEmpty($Output)) {
    throw "Output file path was null or empty."
}

if (-not (Test-Path $Index -PathType Leaf)) {
    throw "Index file '$Index' does not exist."
}


$outputDir = Split-Path -Parent $Output 
if (-not (Test-Path $outputDir -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDir
}

$indexDir = Split-Path -Parent $Index 
$jsonObject = Get-Content $Index | ConvertFrom-Json
$titlePagePath = $jsonObject.title
$filesPaths = $jsonObject.files
$assetDirs = $jsonObject.assets;

if (-not ($assetDirs -eq $null)) {
    foreach ($dir in $assetDirs) {
        if ([System.IO.Path]::IsPathRooted($dir)) {
            $fullPath = $dir
        } else {
            $fullPath = Join-Path $indexDir $dir
        }
        if (Test-Path -PathType Container -Path $fullPath) {
            Copy-Item -Path $fullPath -Container -Recurse -Force -Destination $outputDir
        }
    }
}

$pageBreak = ""
if (-not ([string]::IsNullOrEmpty($jsonObject.pageBreak))) {
    $pageBreak =  "`n`n" + $jsonObject.pageBreak + "`n`n"
}

$mergedConent = ""
if (-not ([string]::IsNullOrEmpty($jsonObject.style))) {
    $mergedConent = "<style>`n" + $jsonObject.style + "`n</style>`n`n"
}


$titlePageConent = ""
if ([System.IO.Path]::IsPathRooted($titlePagePath)) {
    $titlePageFullPath = $titlePagePath
} else {
    $titlePageFullPath = Join-Path $indexDir $titlePagePath
}

if (Test-Path -PathType Leaf -Path $titlePageFullPath) {
    $titlePageConent = (Get-Content -Path $titlePageFullPath -Raw) + $pageBreak
}

$tableOfContents = "# Table of Contents`n"
$filesWritten = 0
$titlesWritten = 0
foreach ($file in $filesPaths) {
    if ($filesWritten -gt 0) {
        $mergedConent += $pageBreak
    }

    if ([System.IO.Path]::IsPathRooted($file)) {
        $fullPath = $file
    } else {
        $fullPath = Join-Path $indexDir $file 
    }    

    if (Test-Path -PathType Leaf -Path $fullPath) {
        $currentContent = (Get-Content -Path $fullPath -Raw) -replace "\([a-zA-Z0-9.\-/\\]+\.md#", "(#";
        if (($currentContent -match "^# {0,1}([a-zA-Z0-9\-\(\)][ a-zA-Z0-9\-\(\)]*[a-zA-Z0-9\-\(\)])") -and 
            ($null -ne $Matches) -and 
            ($null -ne $Matches.1)) {
            $currentTitle = $Matches.1
            $currentTitleLink = "#" + (($currentTitle.ToLower() -replace " ", "-") -replace "[\(\)]", "")
            $titlesWritten += 1;
            $tableOfContents += "- [$currentTitle]($currentTitleLink)`n"
        }
        $mergedConent += $currentContent
        $filesWritten += 1
    }
}

$mergedConent = $titlePageConent + $tableOfContents + $pageBreak + $mergedConent
Out-File -FilePath $Output -Force -InputObject $mergedConent



