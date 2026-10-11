<#
.SYNOPSIS
    Outils d'automatisation pour piloter la fenetre reelle du jeu Releve.

.DESCRIPTION
    Petit module PowerShell sans dependance, hors build (aucun .cs ici) : il lance des
    commandes clavier dans la fenetre raylib, capture l'ecran et verifie les etats de
    fleche par mesure pixelique. Voir README.md pour la recette et les pieges connus.

    Les 3 regles qui font marcher ces outils :
      1. toute commande doit etre envoyee depuis un processus SANS FENETRE, sinon
         l'injection clavier part dans le terminal appelé et le jeu ne recoit rien ;
      2. les caracteres passent par SendKeys (SendInput / keybd_event / PostMessage
         n'ont PAS atteint la saisie du jeu pour les caracteres dans cette
         configuration) ; l'Entree passe en revanche par keybd_event MAINTENU, car
         le panneau la lit via IsKeyPressed, qui exige un front sur l'etat clavier :
         un appui-relache instantane (SendKeys.SendWait '{ENTER}') tombant entre
         deux images 60 Hz ne laisse aucun front et est perdu par intermittence ;
      3. les fichiers de commandes sont lus en UTF8, sans quoi les accents arrivent
         corrompus et le moteur repond « Vous ne voyez pas cela ici. ».
#>

Set-StrictMode -Version 2.0

if (-not ('ReleveNative' -as [type]))
{
    $source = @'
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class ReleveNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
'@
    Add-Type -TypeDefinition $source
}

if (-not ('ReleveEnum' -as [type]))
{
    $enumSource = @'
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class ReleveEnum
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    public static string[] WindowsOf(int processId)
    {
        var found = new System.Collections.Generic.List<string>();
        EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
        {
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid == processId)
            {
                var title = new StringBuilder(256);
                GetWindowText(hWnd, title, 256);
                found.Add(hWnd.ToString() + "|" + IsWindowVisible(hWnd) + "|" + title.ToString());
            }
            return true;
        }, IntPtr.Zero);
        return found.ToArray();
    }
}
'@
    Add-Type -TypeDefinition $enumSource
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$script:GameProcessName = 'Releve.Game'

# Bordure WS_CAPTION de la fenetre : le client raylib est 1280x720, la capture
# PrintWindow fait 1296x759. Ces valeurs etalonnent les mesures pixeliques.
$script:WindowBorderX = 8
$script:WindowBorderY = 39

# Couleurs de la palette du jeu (src/Releve.Game/Ui/Palette.cs), en ecart
# bleu-vert : Prompt (ouverte) 46, DimText (bloquee) 22, Border 32. La bordure
# des panneaux tombe dans la meme bande que DimText : les zones de Get-ReleveArrowStates
# l'excluent (elle longe le bas du panneau « Deplacements possibles », y ~123).

<#
.SYNOPSIS>
    Echappe les caracteres que SendKeys interprete comme des modificateurs.
#>
function Protect-SendKeysText
{
    param([string]$Text)

    $escaped = $Text
    foreach ($pair in @(
        @{ Raw = '{'; Safe = '{{' },
        @{ Raw = '}'; Safe = '}}' },
        @{ Raw = '('; Safe = '{(' },
        @{ Raw = ')'; Safe = '{)}' },
        @{ Raw = '['; Safe = '{[' },
        @{ Raw = ']'; Safe = '{]}' },
        @{ Raw = '+'; Safe = '{+}' },
        @{ Raw = '^'; Safe = '{^}' },
        @{ Raw = '%'; Safe = '{%}' },
        @{ Raw = '~'; Safe = '{~}' }
    ))
    {
        $escaped = $escaped.Replace($pair.Raw, $pair.Safe)
    }

    return $escaped
}

<#
.SYNOPSIS>
    Retourne le processus du jeu, ou $null.
#>
function Get-ReleveGameProcess
{
    Get-Process -Name $script:GameProcessName -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

<#
.SYNOPSIS>
    Attend que la fenetre du jeu existe (handle non nul).
.OUTPUTS>
    Le processus, ou rien en cas de timeout.
#>
function Wait-ReleveGameWindow
{
    [CmdletBinding()]
    param([int]$TimeoutSeconds = 90)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        $process = Get-ReleveGameProcess
        if ($null -ne $process -and $process.MainWindowHandle -ne 0)
        {
            return $process
        }

        Start-Sleep -Milliseconds 400
    }

    Write-Error "Fenetre du jeu introuvable apres $TimeoutSeconds s."
    return $null
}

<#
.SYNOPSIS>
    Arrete le jeu (necessaire avant un rebuild : Releve.Game.exe est verrouille).
#>
function Stop-ReleveGame
{
    $process = Get-ReleveGameProcess
    if ($null -ne $process)
    {
        Stop-Process -Id $process.Id -Force
        Start-Sleep -Milliseconds 800
        Write-Output "Jeu arrete (pid $($process.Id))."
    }
    else
    {
        Write-Output "Aucun processus jeu en cours."
    }
}

<#
.SYNOPSIS>
    Donne le focus clavier a la fenetre du jeu.

.DESCRIPTION>
    SetForegroundWindow seul echoue des qu'un autre processus est au premier plan ;
    AttachThreadInput autorise la bascule. Sans ca, l'injection clavier part ailleurs.
#>
function Set-ReleveGameFocus
{
    $process = Get-ReleveGameProcess
    if ($null -eq $process -or $process.MainWindowHandle -eq 0)
    {
        throw "Fenetre du jeu introuvable."
    }

    $hwnd = $process.MainWindowHandle

    # Deja au premier plan : rien a faire (evite de manipuler la fenetre pour rien).
    if ([ReleveNative]::GetForegroundWindow() -eq $hwnd)
    {
        return $true
    }

    $foreground = [ReleveNative]::GetForegroundWindow()
    $foregroundPid = 0
    $foregroundThread = [ReleveNative]::GetWindowThreadProcessId($foreground, [ref]$foregroundPid)
    $currentThread = [ReleveNative]::GetCurrentThreadId()

    $attached = $false
    if ($foregroundThread -ne $currentThread)
    {
        $attached = [ReleveNative]::AttachThreadInput($currentThread, $foregroundThread, $true)
    }

    [ReleveNative]::BringWindowToTop($hwnd) | Out-Null
    if ([ReleveNative]::IsIconic($hwnd))
    {
        # Seul cas ou ShowWindow est appele : restaurer une fenêtre minimisée.
        # Montrer une fenêtre déjà visible (SW_SHOW) l'a déjà rendue invisible.
        [ReleveNative]::ShowWindow($hwnd, 9) | Out-Null  # SW_RESTORE
    }

    [ReleveNative]::SetForegroundWindow($hwnd) | Out-Null
    [ReleveNative]::SetActiveWindow($hwnd) | Out-Null

    if ($attached)
    {
        [ReleveNative]::AttachThreadInput($currentThread, $foregroundThread, $false) | Out-Null
    }

    Start-Sleep -Milliseconds 120
    return ([ReleveNative]::GetForegroundWindow() -eq $hwnd)
}

<#
.SYNOPSIS>
    Attend que la fenetre du jeu ait reellement le focus clavier (retente).

.DESCRIPTION>
    Un appelant visible peut reprendre le premier plan entre deux frappes : sans
    cette attente, des caracteres ou des Entree se perdent et la commande soumise
    devient « prends badgeinventaire » — le moteur repond alors « Vous ne voyez pas
    cela ici. ». Chaque retour $true garantit l'etat juste avant l'envoi suivant.
#>
function Wait-ReleveGameFocus
{
    [CmdletBinding()]
    param([int]$TimeoutSeconds = 5)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        if (Set-ReleveGameFocus) { return $true }
        Start-Sleep -Milliseconds 120
    }

    return $false
}

<#
.SYNOPSIS>
    Diagnostique les fenetres du jeu et la fenetre qui a le focus clavier.
#>
function Get-ReleveGameWindowInfo
{
    $process = Get-ReleveGameProcess
    if ($null -eq $process)
    {
        Write-Output "Aucun processus jeu."
        return
    }

    Write-Output "pid=$($process.Id) MainWindowHandle=$($process.MainWindowHandle) titre=$($process.MainWindowTitle)"
    foreach ($line in [ReleveEnum]::WindowsOf($process.Id))
    {
        $parts = $line.Split('|')
        Write-Output "  hWnd=$($parts[0]) visible=$($parts[1]) titre='$($parts[2])'"
    }

    $foreground = [ReleveNative]::GetForegroundWindow()
    $foregroundPid = 0
    [ReleveNative]::GetWindowThreadProcessId($foreground, [ref]$foregroundPid) | Out-Null
    Write-Output "foreground hWnd=$foreground pid=$foregroundPid"
}

<#
.SYNOPSIS>
    Maintient une touche enfoncée le temps d'au moins une image raylib.

.DESCRIPTION>
    Ui/ConsolePanel.cs lit les touches avec Raylib.IsKeyPressed, qui detecte un
    front montant sur l'etat clavier. Un couple keydown/keyup instantane (ce que
    fait SendKeys.SendWait '{ENTER}') qui tombe entierement entre deux images a
    60 Hz ne laisse aucun front : la touche est silencieusement perdue, deux
    commandes se retrouvent collees sur la meme ligne et le moteur repond
    « Vous ne voyez pas cela ici. ». Maintenir la touche ~60 ms (3 a 4 images)
    garantit la detection.

    Reserve aux touches d'etat (Entree, Backspace...) : les caracteres, eux,
    passent par SendKeys et la file WM_CHAR de raylib.
#>
function Invoke-ReleveKeyPulse
{
    param(
        [Parameter(Mandatory = $true)][byte]$VirtualKey,
        [int]$HoldMs = 60
    )

    # dwFlags : 0 = keydown, 2 = KEYEVENTF_KEYUP.
    [ReleveNative]::keybd_event($VirtualKey, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $HoldMs
    [ReleveNative]::keybd_event($VirtualKey, 0, 2, [UIntPtr]::Zero)
}

<#
.SYNOPSIS>
    Vide la ligne de saisie en cours (utile apres une commande mal validee).
#>
function Clear-ReleveGameInput
{
    [CmdletBinding()]
    param([int]$Backspaces = 90, [int]$DelayMs = 20)

    $focused = Wait-ReleveGameFocus
    if (-not $focused)
    {
        Write-Warning "Focus clavier non obtenu : la ligne de saisie n'est peut-etre pas videe."
    }

    for ($i = 0; $i -lt $Backspaces; $i++)
    {
        Invoke-ReleveKeyPulse -VirtualKey 0x08 -HoldMs 40
        Start-Sleep -Milliseconds $DelayMs
    }

    Write-Output "Ligne de saisie videe."
}

<#
.SYNOPSIS>
    Envoie de simples frappes Entree (utile pour valider une entree de menu).
#>
function Send-ReleveGameEnter
{
    [CmdletBinding()]
    param([int]$Count = 1, [int]$HoldMs = 60, [int]$DelayMs = 400)

    Set-ReleveGameFocus | Out-Null
    for ($i = 0; $i -lt $Count; $i++)
    {
        Wait-ReleveGameFocus | Out-Null
        Invoke-ReleveKeyPulse -VirtualKey 0x0D -HoldMs $HoldMs
        Start-Sleep -Milliseconds $DelayMs
    }

    Write-Output "Entree envoyee x$Count."
}

<#
.SYNOPSIS>
    Envoie une liste de commandes au jeu, une par une, validees par Entree.

.PARAMETER CommandFile>
    Fichier texte (UTF8) : une commande par ligne, lignes vides ignorees.
.PARAMETER Commands>
    Commandes passees directement.
.PARAMETER DelayMs>
    Attente apres chaque validation (le rendu raylib tourne a 60 images/s).

.EXAMPLE>
    Send-ReleveGameCommands -CommandFile agentTools/scenarios/poc-reference.txt
#>
function Send-ReleveGameCommands
{
    [CmdletBinding(DefaultParameterSetName = 'File')]
    param(
        [Parameter(Mandatory = $true, ParameterSetName = 'File')]
        [string]$CommandFile,

        [Parameter(Mandatory = $true, ParameterSetName = 'List')]
        [string[]]$Commands,

        [Parameter(ParameterSetName = 'File')]
        [Parameter(ParameterSetName = 'List')]
        [int]$DelayMs = 700
    )

    if ($PSCmdlet.ParameterSetName -eq 'File')
    {
        if (-not (Test-Path -LiteralPath $CommandFile))
        {
            throw "Fichier de commandes introuvable : $CommandFile"
        }

        # UTF8 explicite : Get-Content lit en ANSI par defaut sous PowerShell 5.1
        # et les caracteres accentues arriveraient corrompus au jeu.
        $Commands = Get-Content -LiteralPath $CommandFile -Encoding UTF8 | Where-Object { $_.Trim() -ne '' }
    }

    $focused = Wait-ReleveGameFocus
    if (-not $focused)
    {
        Write-Warning "Focus clavier non obtenu : l'injection peut ne pas atteindre le jeu."
    }

    foreach ($command in $Commands)
    {
        # Un caractere a la fois : la file de caracteres de raylib n'absorbe que
        # 16 codepoints par image (MAX_CHAR_PRESSED_QUEUE), un envoi en bloc perd
        # la fin des commandes longues (« utilise badge sur lecteur » = 24 signes).
        $protected = Protect-SendKeysText $command
        foreach ($character in $protected.ToCharArray())
        {
            Wait-ReleveGameFocus | Out-Null
            [System.Windows.Forms.SendKeys]::SendWait($character)
            Start-Sleep -Milliseconds 30
        }

        Start-Sleep -Milliseconds 150
        Wait-ReleveGameFocus | Out-Null
        # Entree tenue 80 ms : un SendKeys.SendWait instantane est perdu des que
        # le couple keydown/keyup tombe entre deux images 60 Hz.
        Invoke-ReleveKeyPulse -VirtualKey 0x0D -HoldMs 80
        Start-Sleep -Milliseconds $DelayMs
        Write-Output "Envoye : $command"
    }

    Write-Output "Commandes envoyees."
}

<#
.SYNOPSIS>
    Capture la fenetre du jeu seule, 1:1 (PrintWindow).

.DESCRIPTION>
    A preferer a une capture plein ecran pour les mesures pixeliques : les
    coordonnees ne dependent pas de la position de la fenetre sur le bureau.
    La fenetre rendue fait 1280x720 cote client ; la capture inclut la bordure
    WS_CAPTION (+8 en x, +39 en y), ce que ArrowStates compense.

.PARAMETER Path>
    Chemin du PNG a ecrire.
#>
function Save-ReleveGameWindowShot
{
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$Path)

    $process = Get-ReleveGameProcess
    if ($null -eq $process -or $process.MainWindowHandle -eq 0)
    {
        throw "Fenetre du jeu introuvable."
    }

    $rect = New-Object 'ReleveNative+RECT'
    [ReleveNative]::GetWindowRect($process.MainWindowHandle, [ref]$rect) | Out-Null
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top

    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $hdc = $graphics.GetHdc()
    try
    {
        [ReleveNative]::PrintWindow($process.MainWindowHandle, $hdc, 0) | Out-Null
    }
    finally
    {
        $graphics.ReleaseHdc($hdc)
    }

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()

    Write-Output "Capture fenetre : $Path (${width}x${height})."
}

<#
.SYNOPSIS>
    Capture l'ecran entier (pour un apercu visuel rapide, pas pour mesurer).
#>
function Save-ScreenShot
{
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$Path)

    $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bitmap = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()

    Write-Output "Capture ecran : $Path."
}

<#
.SYNOPSIS>
    Verifie les etats de fleche dans une capture faite par Save-ReleveGameWindowShot.

.DESCRIPTION>
    Absent  : rien n'est dessine.
    Bloquee : fleche en DimText.
    Ouverte : fleche en Prompt.

    La detection est pixelique (aucune sonde dans le jeu) : chaque cellule du
    panneau « Deplacements possibles » est scannee et classee selon l'ecart
    bleu-vert des deux couleurs de la palette.

.EXAMPLE>
    Save-ReleveGameWindowShot -Path out.png
    Get-ReleveArrowStates -PngFile out.png
#>
function Get-ReleveArrowStates
{
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$PngFile)

    $bitmap = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $PngFile))
    try
    {
        # Zones cote client, deduites des constantes de ArrowsPanel.cs et
        # Layout.cs (textX = 970, rowY = 36, CellWidth = 100, RowHeight = 34,
        # ArrowSize = 18, Fonts.LineHeight = 26) :
        #   Nord : glyphe x 1120..1138, y 36..54 (libelle « Nord » a x 1148)
        #   Ouest/Sud/Est : glyphes y 70..88, x 970 / 1070 / 1170
        # Chaque zone couvre glyphe + libelle, sans chevaucher la bordure
        # basse du panneau (y ~123) qui serait comptee comme « bloquee ».
        $cells = [ordered]@{
            'North' = @(1100, 34, 1220, 64)
            'West'  = @(965, 68, 1060, 96)
            'South' = @(1065, 68, 1160, 96)
            'East'  = @(1165, 68, 1275, 96)
        }

        foreach ($name in $cells.Keys)
        {
            $zone = $cells[$name]
            $x1 = $zone[0] + $script:WindowBorderX
            $x2 = $zone[2] + $script:WindowBorderX
            $y1 = $zone[1] + $script:WindowBorderY
            $y2 = $zone[3] + $script:WindowBorderY

            $open = 0
            $dim = 0
            for ($y = $y1; $y -lt $y2; $y++)
            {
                for ($x = $x1; $x -lt $x2; $x++)
                {
                    $pixel = $bitmap.GetPixel($x, $y)
                    $blueGreen = $pixel.B - $pixel.G
                    if ($blueGreen -gt 38) { $open++ }
                    elseif ($blueGreen -gt 15) { $dim++ }
                }
            }

            $state = 'Absent'
            if ($open -gt 40) { $state = 'Open' }
            elseif ($dim -gt 40) { $state = 'Blocked' }

            [PSCustomObject]@{
                Direction = $name
                State     = $state
                OpenPixels = $open
                DimPixels  = $dim
            }
        }
    }
    finally
    {
        $bitmap.Dispose()
    }
}

Export-ModuleMember -Function @(
    'Get-ReleveGameProcess'
    'Wait-ReleveGameWindow'
    'Stop-ReleveGame'
    'Wait-ReleveGameFocus'
    'Set-ReleveGameFocus'
    'Get-ReleveGameWindowInfo'
        'Send-ReleveGameEnter'
        'Clear-ReleveGameInput'
    'Send-ReleveGameCommands'
    'Save-ReleveGameWindowShot'
    'Save-ScreenShot'
    'Get-ReleveArrowStates'
)
