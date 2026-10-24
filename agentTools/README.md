# agentTools

Outils d'automatisation pour piloter **la fenêtre réelle du jeu** (raylib) : commandes
clavier, captures d'écran, vérification des états de flèche par mesure pixelique.

Ces outils ne font **pas partie du build** : aucun fichier `.cs` ici, rien à compiler,
`dotnet build` et `dotnet test` restent strictement inchangés.

## Limites d'automatisation (validation humaine requise)

- **Souris** : survol/clic sur les entrées du menu, les indicateurs de défileur (console/visualiseur), le bouton « Fermer » — ne peuvent pas être pilotés par `agentTools/` (pas d'injection souris fiable).
- **Fondu (fade)** : transitions noir/ouverture (durées 0,6 s / 0,8 s) — impossible à chronométrer ni valider sans observation visuelle.
- **Répétition de touche maintenue** (`Retour arrière`, `Suppr`, flèches directionnelles) : `IsKeyPressedRepeat` est implémenté dans le moteur, mais `agentTools/` ne peut pas le valider comme un vrai clavier.
- **`Wait-ReleveGameWindow`** : la recette de rejeu du compte rendu du PoC V2 appelle cette fonction, mais elle n'est **pas listée** dans le tableau des fonctions ci-dessus (cf. §8.3 du PoC V3). Ne pas s'y fier avant de l'avoir testée au regard de la contrainte d'injection sans fenêtre (piège 1).

## Prérequis

- Windows, PowerShell 5.1 (celui de VS Code convient).
- Le jeu lancé : `dotnet run --project src/Releve.Game` (depuis un terminal dédié,
  pas depuis celui qui enverra les commandes).

## Recette

```powershell
# 1. le jeu tourne déjà ; on attend que sa fenêtre existe
Import-Module .\agentTools\ReleveGameTools.psm1
Wait-ReleveGameWindow

# 2. envoi du scénario de référence (document PoC §4.1), une commande par ligne
Send-ReleveGameCommands -CommandFile .\agentTools\scenarios\poc-reference.txt

# 3. vérifications visuelles sans quitter le jeu
Save-ReleveGameWindowShot -Path .\shot.png          # capture 1:1 de la fenêtre
Get-ReleveArrowStates -PngFile .\shot.png           # North/West/South/East : Absent/Blocked/Open

# 4. rejouer le scénario depuis le début
Stop-ReleveGame
# (relancer dotnet run, puis Wait-ReleveGameWindow de nouveau)
```

`scenarios/quit-and-save.txt` contient `quitter` : il déclenche la sauvegarde
automatique à la fermeture (utile pour tester le menu « Continuer » au relancement).

Pour repartir d'un menu sans sauvegarde, supprimer `Documents/Releve/save.dat`.

## Fonctions

| Fonction | Rôle |
|---|---|
| `Wait-ReleveGameWindow` | attend l'ouverture de la fenêtre (handle non nul) |
| `Set-ReleveGameFocus` | donne le focus clavier au jeu (renvoie `True`/`False`) |
| `Wait-ReleveGameFocus` | idem, avec attente bornée (à appeler avant chaque frappe) |
| `Send-ReleveGameCommands` | envoie un fichier UTF8 (ou `-Commands`) commande par commande |
| `Send-ReleveGameEnter` | frappes `Entrée` seules, touches maintenues (valider une entrée de menu) |
| `Clear-ReleveGameInput` | vide la ligne de saisie (après une commande mal validée) |
| `Save-ReleveGameWindowShot` | capture `PrintWindow` 1:1 (mesures fiables) |
| `Save-ScreenShot` | capture plein écran (aperçu rapide) |
| `Get-ReleveArrowStates` | états de flèche par mesure pixelique (aucune sonde dans le jeu) |
| `Get-ReleveGameWindowInfo` | diagnostic : fenêtres du processus, focus clavier |
| `Stop-ReleveGame` | arrête le jeu (obligatoire avant un rebuild : `Releve.Game.exe` est verrouillé) |

## Les cinq pièges qui coûtent une session chacun

1. **Ne jamais envoyer de commandes depuis un terminal visible.** Dès qu'un processus
   PowerShell fenêtré est au premier plan, l'injection clavier part dans ce terminal et
   le jeu ne reçoit rien — symptôme : aucune saisie n'apparaît alors que le script
   annonce « envoyé ». Solution : exécuter le script dans un processus *sans fenêtre*
   (`Start-Process powershell -WindowStyle Hidden`, `CreateNoWindow = $true`, ou un
   job). `Set-ReleveGameFocus` suppose déjà qu'il tourne dans ce contexte.

2. **Pour les caractères, `SendKeys` est le seul moyen d'injection qui atteint la
   saisie du jeu.** `SendInput` (`KEYEVENTF_UNICODE` ou `wVk`) et `PostMessage(WM_CHAR)`
   ont échoué ici : les caractères n'arrivent pas dans le champ de la console.
   `SetForegroundWindow` seul échoue aussi (Windows réserve la bascule de premier
   plan) — d'où `AttachThreadInput`.

3. **Pour `Entrée` (et `Retour arrière`), il faut au contraire `keybd_event` *maintenu*.**
   `Ui/ConsolePanel.cs` lit ces touches avec `Raylib.IsKeyPressed`, qui détecte un front
   montant sur l'état clavier. Un couple keydown/keyup instantané
   (`SendKeys.SendWait '{ENTER}'`) qui tombe entièrement entre deux images 60 Hz ne
   laisse aucun front : la touche est perdue par intermittence, deux commandes se
   retrouvent collées sur la même ligne et le moteur répond « Vous ne voyez pas cela
   ici. ». Le module maintient donc la touche ~60-80 ms via `keybd_event`
   (`Invoke-ReleveKeyPulse`). Ce n'est pas une contradiction du piège 2 : les
   caractères, eux, passent par la file `WM_CHAR` de raylib, les touches d'état par
   l'état clavier.

   **Limite d'automatisation (validation humaine requise)** : ni `SendKeys` ni
   `keybd_event` ne peuvent simuler un appui sur la touche `Retour arrière` d'un vrai
   clavier de manière indiscernable d'une frappe humaine. La répétition de touche
   (touche maintenue) pour `Retour arrière` et `Suppr` ne peut être validée
   automatiquement : voir §7.3 du document d'implémentation PoC V2.

4. **Les fichiers de commandes doivent être en UTF-8.** `Get-Content` lit en ANSI par
   défaut sous PowerShell 5.1 : « parle à l'androïde » arrive alors corrompu et le
   moteur répond « Vous ne voyez pas cela ici. ». Le module force `-Encoding UTF8`.

5. **Ne pas forcer le focus par un clic souris synthétique.** Un clic dans la fenêtre a
   déjà conduit à une fenêtre devenue invisible (`ShowWindow(5)` sur une fenêtre
   minimisée) : le jeu tournait toujours sans plus rien afficher. Le focus clavier
   Windows (`AttachThreadInput` + `SetForegroundWindow`) suffit.

## Notes de calibration

- Le client raylib fait 1280×720 ; la bordure `WS_CAPTION` décale la capture de +8 en x
  et +39 en y. `Get-ReleveArrowStates` compense ce décalage ; si la fenêtre change de
  style (plein écran, bordure différente), recalibrer `$script:WindowBorderX/Y`.
- Les couleurs attendues viennent de `src/Releve.Game/Ui/Palette.cs` : flèche ouverte =
  `ArrowOpen` (écart bleu-vert vif), flèche bloquée = `ArrowBlocked` (gris sombre,
  luminance nettement inférieure). La `Border` des panneaux (66,88,120 → écart 32)
  tombe dans la bande « bloquée » : les zones excluent la bordure basse du panneau
  « Déplacements possibles » (y ~ 123).
- Les zones de `Get-ReleveArrowStates` sont déduites des constantes de
  `Ui/ArrowsPanel.cs` / `Ui/Layout.cs` (nouvelle disposition en croix, lot B3) :
  textX = 970, ligne 1 (Nord) y = 36, ligne 2 (Ouest/Est) y = 70, ligne 3 (Sud)
  y = 104, `Fonts.LineHeight` = 26. Nord et Sud sur la ligne centrale (x ~ 1111 +
  libelle), Ouest sur la gauche (libelle + fleche), Est sur la droite (fleche +
  libelle). Si la mise en page du panneau change, recalibrer ces zones.
- Attendre ~700 ms entre deux commandes (`-DelayMs`) : la file de caractères de raylib
  est limitée à 16 codes par image.
