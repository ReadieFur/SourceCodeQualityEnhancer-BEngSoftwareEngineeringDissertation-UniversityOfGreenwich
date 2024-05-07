#Requires -Version 7.0

# Define source and destination paths
$srcStandalone = ".\src\ReadieFur.SourceAnalyzer.Standalone\bin\Debug\net472"
$srcVSIX = ".\src\ReadieFur.SourceAnalyzer.VSIX\bin\Debug"
$srcReport = ".\report\Main.pdf"
$srcDir = ".\src"

$dstStandalone = ".\dist\Standalone"
$dstVSIX = ".\dist\VSIX"
$dstReport = ".\dist\Report.pdf"
$dstSubmission = ".\submission_files"

# Create directories if they don't exist
if (-not (Test-Path $dstStandalone)) { New-Item -ItemType Directory -Force -Path $dstStandalone }
if (-not (Test-Path $dstVSIX)) { New-Item -ItemType Directory -Force -Path $dstVSIX }
if (-not (Test-Path $dstSubmission)) { New-Item -ItemType Directory -Force -Path $dstSubmission }

function TryDelete
{
    param($path)
    try { Remove-Item -Path $path -Force -Recurse -ErrorAction Stop }
    catch
    {
        if ($_.Exception.GetType().FullName -eq 'System.Management.Automation.ItemNotFoundException')
        {
            Write-Host "$path is already empty."
        }
        else
        {
            Write-Error $_
        }
    }
}

# Clear existing contents of dist directory
TryDelete $dstStandalone\*
TryDelete $dstVSIX\*
TryDelete $dstReport

# Copy contents to destination directories
Copy-Item -Path $srcStandalone\* -Destination $dstStandalone -Recurse -Force
Copy-Item -Path $srcVSIX\* -Destination $dstVSIX -Recurse -Force
Copy-Item -Path $srcReport -Destination $dstReport -Force

# Compress the src directory into a single archive excluding .gitignore
Compress-Archive -Path $srcDir\* -DestinationPath "$dstSubmission\src.zip" -Force

# Copy Report.pdf to submission_files directory
Copy-Item -Path $dstReport -Destination "$dstSubmission\Report.pdf" -Force
