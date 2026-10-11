<#
.SYNOPSIS>
    Envoie un fichier de commandes au jeu, depuis un processus SANS FENETRE.

.DESCRIPTION>
    A utiliser via Run-Hidden.ps1 : une fenetre PowerShell visible vole le focus
    clavier et l'injection n'atteint jamais le jeu. Voir README.md.

.EXAMPLE>
    .\agentTools\Run-Hidden.ps1 -Script .\agentTools\Send-Commands.ps1 `
        -Arguments "-CommandFile .\agentTools\scenarios\poc-reference.txt"
#>
param(
    [Parameter(Mandatory = $true)][string]$CommandFile,
    [int]$EnterCount = 0,
    [int]$DelayMs = 700
)

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $here 'ReleveGameTools.psm1') -Force

$process = Wait-ReleveGameWindow
if ($null -eq $process)
{
    exit 1
}

# EnterCount valide une entree de menu (« Nouvelle partie ») avant le scenario.
for ($i = 0; $i -lt $EnterCount; $i++)
{
    Send-ReleveGameEnter -Count 1 -DelayMs $DelayMs
}

Send-ReleveGameCommands -CommandFile $CommandFile -DelayMs $DelayMs
