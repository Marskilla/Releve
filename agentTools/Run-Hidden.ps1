<#
.SYNOPSIS>
    Execute un script dans un processus sans fenetre (indispensable pour injecter
    du clavier dans le jeu : un terminal visible vole le focus).

.EXAMPLE>
    .\agentTools\Run-Hidden.ps1 -Script .\agentTools\Send-Commands.ps1 `
        -Arguments "-CommandFile .\agentTools\scenarios\poc-reference.txt -DelayMs 900"
#>
param(
    [Parameter(Mandatory = $true)][string]$Script,
    [string]$Arguments = '',
    [int]$TimeoutSeconds = 300
)

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = 'powershell.exe'
$startInfo.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$Script`" $Arguments"
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardOutput = $true

$process = [System.Diagnostics.Process]::Start($startInfo)
$output = $process.StandardOutput.ReadToEnd()

if (-not $process.WaitForExit($TimeoutSeconds * 1000))
{
    Write-Output 'TIMEOUT'
    exit 1
}

Write-Output "code=$($process.ExitCode)"
if ($output) { Write-Output $output }
