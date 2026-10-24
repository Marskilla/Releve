$content = Get-Content 'D:\MesDocuments\_MesDocuments\Programmes\Releve\Docs\ArchitectureTechnique.md' -Raw -Encoding UTF8

# Replace Ctrl+ with Shift+ in the console description
$content = $content -replace 'Rappel de l\'historique des commandes** : `Ctrl \+ Haut` / `Ctrl \+ Bas`', 'Rappel de l\'historique des commandes** : `Shift + Haut` / `Shift + Bas`'

# Replace in section 9.4
$content = $content -replace 'Ctrl \+ Haut / Ctrl \+ Bas : défilement de l\'inventaire \*\*et\*\* rappel de l\'historique de la console', 'Ctrl + Haut / Ctrl + Bas : défilement de l\'inventaire.
- **Shift + Haut / Shift + Bas** : rappel de l\'historique de la console (commandes saisies précédemment).'

Set-Content 'D:\MesDocuments\_MesDocuments\Programmes\Releve\Docs\ArchitectureTechnique.md' -Value $content -Encoding UTF8
Write-Host 'Done'