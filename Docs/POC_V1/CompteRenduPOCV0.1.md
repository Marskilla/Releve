# Jeu « Relève » — Compte rendu du PoC

> Compte rendu — version 0.1
> **État : PoC v1.0 terminé.** Le contenu décrit par « ImplementationPOCV0.1.md » est livré, jouable de bout en bout dans le jeu réel, build et tests au vert.
> Documents liés : `ImplementationPOCV0.1.md` (le plan, ses §4 et §5 servent de référence aux tableaux ci-dessous), `ArchitectureTechniqueV0.3.md` (l’architecture après les changements de ce PoC), `NoteIntentionJeuAventureV5.md` (comportement), `UniversScenarioLoreJeuAventureV0.7.md` (lieux et lore), `AGENTS.md` (règles de travail du dépôt).

## 1. Résultat

Le PoC demande de remplacer le monde d’amorçage technique par le contenu réel des trois premiers lieux du lore plus une rue provisoire, et de le valider de bout en bout. C’est fait :

- **Monde livré** : 4 pièces (`Bedroom`, `TechCorridor`, `HospitalLobby`, `Street`), 22 objets, chambre de réveil comme départ.
- **Monde d’amorçage entièrement supprimé** : classes, objets, enums, tests (critère d’acceptation §5 du document d’implémentation).
- **Scénario de référence §4.1** exécuté dans le jeu réel, commande par commande, avec les réponses attendues à chaque étape.
- **Tests** : 83 tests verts (79 à l’issue de l’initialisation), dont 7 propres au contenu du PoC.
- **Deux défauts d’architecture** recensés à la version précédente corrigés au passage (menu et sauvegarde).
- **Outillage d’automatisation** ajouté hors build (`agentTools/`), qui a servi aux vérifications du jeu réel et reste utilisable pour la suite.

## 2. Travail réalisé

### 2.1 Contenu du monde (moteur, `src/Releve.Engine/Content/`)

| Fichier | Contenu |
|---|---|
| `Rooms/Bedroom.cs` | Chambre de réveil, pièce de départ. Annonce unique (`GameEvent.WakeAnnouncementDone`), sortie Est conditionnée par le lecteur de badge, armoire fermée au départ. |
| `Rooms/TechCorridor.cs` | Couloir technique. Ouest vers la chambre, Nord conditionné par le lecteur de l’escalier, Est sans destination (fausse piste). |
| `Rooms/HospitalLobby.cs` | Accueil. Sud vers le couloir, Ouest conditionné par la borne (fiche patient), Nord sans destination (ascenseur), Est absente. |
| `Rooms/Street.cs` | Rue provisoire : Est vers l’accueil uniquement. Aucun objet, aucune valeur de lore. |
| `Objects/BedroomObjects.cs` | 9 objets : lit, machines, armoire, vêtements d’enfant, badge, lecteur, porte, corbeille, sac poubelle. |
| `Objects/TechCorridorObjects.cs` | 4 objets : porte et lecteur de l’escalier, porte et lecteur du local technique. |
| `Objects/HospitalLobbyObjects.cs` | 9 objets : androïde, borne, afficheur, fauteuils et brancards, porte vitrée, plantes, ascenseur, lecteur d’ascenseur, bouton. |
| `GameWorld.cs` | Point de construction unique : 4 pièces, départ sur `Bedroom`, 22 objets assemblés depuis les trois fichiers d’objets. Succède à `BootstrapWorld.cs` (supprimé). |

Les comportements propres aux pièces vivent dans `Handle`, comme prévu par l’architecture §5.6 : badges sur lecteurs, borne et fiche patient, réponse de l’androïde, refus du lecteur d’ascenseur, bouton sans effet. Les identifiants ajoutés dans `World/Ids.cs` sont exactement ceux du §2 du document d’implémentation (plus `WakeAnnouncementDone` dans `GameEvent`).

### 2.2 Suppression du monde d’amorçage

- Supprimés : `Content/BootstrapWorld.cs`, `Rooms/SandboxA.cs`, `Rooms/SandboxB.cs`, `Objects/SandboxObjects.cs` et `tests/…/SandboxScenarioTests.cs`.
- `StartRoom` positionné sur `Bedroom`.
- Les tests génériques (analyseur, sauvegarde, comportements) qui s’appuyaient sur le monde d’amorçage utilisent désormais `TestWorld`, le monde de test indépendant du contenu, comme le prévoit l’architecture §11.
- `WorldConsistencyTests` mis à jour : 4 pièces, 22 objets, départ sur `Bedroom`, et listes explicites des membres réservés au monde de test.

### 2.3 Tests

| Fichier | Rôle |
|---|---|
| `PocContentTests.cs` (nouveau, 7 tests) | Sorties sans destination toujours bloquées, existence des retours, ambiguïtés du couloir (« lecteur » et « porte »), armoire/badge, fiche patient à l’identique, scénario de référence complet du réveil à la rue, sauvegarde puis rechargement (pièce, inventaire, états, événements, annonce non rejouée). |
| `WorldConsistencyTests.cs` (mis à jour) | Cohérence du monde livré **et** du monde de test, plus les compteurs (4 pièces, 22 objets) et le départ. |
| `NormalizationTests.cs`, `ParserTests.cs`, `EngineBehaviourTests.cs`, `SaveTests.cs` | Inchangés dans leur contenu, rebasés là où c’était nécessaire sur `TestWorld`. |

Total : **83 tests, tous verts** (`dotnet test`).

### 2.4 Corrections d’architecture assumées

Le document d’implémentation interdit de modifier l’architecture sans signaler le besoin ; aucun changement de structure n’a été nécessaire. En revanche, deux défauts recensés dans la version précédente de l’architecture ont été corrigés pendant la mise au point, car ils gênaient la validation dans le jeu réel :

1. **Navigation du menu neutralisée** — `GameApp.Run` appelait `MenuScreen.Reset(...)` à chaque image. La sélection est maintenant réinitialisée une seule fois, à l’entrée dans l’état menu (drapeau `_menuEntered`). Flèches Haut/Bas de nouveau fonctionnelles.
2. **Fichier de sauvegarde relu en permanence** — `CanContinue()` déclenchait une lecture et un déchiffrement complets plusieurs fois par image. Le résultat est mémorisé au démarrage (`_canContinue`) et rafraîchi à la sortie d’une partie.

Ces corrections sont décrites dans « ArchitectureTechniqueV0.3.md » §15 (qui passe de « à corriger » à « corrigés »).

### 2.5 Outillage d’automatisation (hors build, `agentTools/`)

Pour vérifier le scénario dans le jeu réel sans clic humain, un dossier d’automatisation a été ajouté. Il ne fait **pas partie du build** (aucun `.cs`, `dotnet build` et `dotnet test` inchangés) :

| Fichier | Rôle |
|---|---|
| `ReleveGameTools.psm1` | Module PowerShell : focus clavier, envoi de commandes, captures, mesure des états de flèche. |
| `Run-Hidden.ps1` | Exécute un script dans un processus sans fenêtre (un terminal visible vole le focus). |
| `Send-Commands.ps1` | Envoi d’un fichier de commandes, avec validation d’entrées de menu (`-EnterCount`). |
| `scenarios/poc-reference.txt` | Les 14 commandes du scénario §4.1, une par ligne. |
| `scenarios/quit-and-save.txt` | `quitter`, pour exercer la sauvegarde à la fermeture. |
| `README.md` | Recette et pièges d’injection clavier. |

Deux problèmes d’automatisation, découverts et résolus pendant la vérification, sont documentés dans le module et le `README` :

1. **La touche Entrée était perdue par intermittence.** `Ui/ConsolePanel.cs` lit Entrée et Retour arrière avec `Raylib.IsKeyPressed`, qui détecte un front montant sur l’état clavier. Un appui-relâché instantané (`SendKeys.SendWait '{ENTER}'`) qui tombe entièrement entre deux images 60 Hz ne laisse aucun front : la touche est silencieusement ignorée et deux commandes se retrouvent collées sur la même ligne, le moteur répondant « Vous ne voyez pas cela ici. ». La touche est maintenant **maintenue 60 à 80 ms** via `keybd_event` (`Invoke-ReleveKeyPulse`). Les caractères, eux, continuent de passer par `SendKeys` (la file de caractères de raylib est limitée à 16 codepoints par image : envoi d’un caractère à la fois).
2. **Les zones de mesure des flèches étaient décalées d’une ligne.** `Get-ReleveArrowStates` mesurait des zones qui ne correspondaient pas à la grille réelle de `Ui/ArrowsPanel.cs` (une flèche « Est » unique se lisait « Nord »). Les zones sont maintenant déduites des constantes de mise en page (`textX` 970, `rowY` 36, `CellWidth` 100, `RowHeight` 34, `ArrowSize` 18, `Fonts.LineHeight` 26) et excluent la bordure du panneau, dont la couleur tombe dans la bande « bloquée ». Vérifié : affichage et mesure concordent dans les quatre lieux.

### 2.6 Documentation

- « ArchitectureTechniqueV0.3.md » : reflet du code livré après le PoC (contenu, tests, corrections, outillage).
- `AGENTS.md` : section « Agent tools », correction de la note d’injection clavier, note sur les images de pièces désormais vivantes.
- Le présent compte rendu.

## 3. Vérification dans le jeu réel

Le scénario §4.1 a été joué dans le jeu lancé, depuis une nouvelle partie, via l’automatisation. Les 14 commandes ont toutes été validées séparément (aucune collision de ligne) et ont produit la réponse attendue :

| # | Commande | Réponse observée |
|---|---|---|
| 1 | `regarde lit` | « Un lit médicalisé. » |
| 2 | `est` | « La porte est solidement fermée et le lecteur de badge affiche un rouge fixe. » |
| 3 | `prends badge` | refus (armoire fermée — voir l’écart du §4) |
| 4 | `ouvre armoire` | « Vous ouvrez l’armoire. » |
| 5 | `prends badge` | « Pris. » |
| 6 | `utilise badge sur lecteur` | « Vous passez le badge devant le lecteur. Le voyant passe au vert et la porte se déverrouille. » — flèche Est : `Blocked` → `Open` |
| 7 | `est` | description du couloir |
| 8 | `utilise badge sur lecteur` | « Précisez : le lecteur de l’escalier ou le lecteur du local technique. » |
| 9 | `utilise badge sur lecteur escalier` | « Le voyant passe au vert : la porte de l’escalier se déverrouille. » — flèche Nord : `Open` |
| 10 | `nord` | description de l’accueil |
| 11 | `ouest` | « La porte vitrée de l’entrée reste fermée. » |
| 12 | `parle à l’androïde` | « L’androïde s’anime : « Veuillez passer votre badge sur la borne pour sortir. » » |
| 13 | `passe le badge sur la borne` | fiche patient affichée en 8 lignes à l’identique du §3.3, puis « La porte vitrée de l’entrée se déverrouille. » — flèche Ouest : `Open` |
| 14 | `ouest` | « Une rue vide, gagnée par la végétation. (Lieu provisoire : fin du PoC.) » |

**Les trois états de flèche dans les quatre lieux** (vérifiés par capture et mesure pixelique, concordance avec les sorties déclarées) :

| Lieu | Nord | Ouest | Sud | Est |
|---|---|---|---|---|
| Chambre | absente | absente | absente | bloquée → **ouverte** (étape 6) |
| Couloir | bloquée | ouverte | absente | bloquée (sans destination) |
| Accueil | bloquée (sans destination) | bloquée → **ouverte** (étape 13) | ouverte | absente |
| Rue | absente | absente | absente | ouverte |

**Sauvegarde et reprise** : `quitter` ferme le jeu et écrit `Documents/Releve/save.dat`. Au relancement, « Continuer » est présélectionné ; validé, il restaure la partie à l’identique — hall d’accueil, « le badge » à l’inventaire, mêmes états de flèches. La reprise depuis un fichier est aussi couverte par les tests (`PocContentTests`, `SaveTests`).

**Textes et accents** : la fiche patient, les guillemets, l’apostrophe typographique et les accents s’affichent correctement dans la console (caractères précomposés, police DejaVu Sans). Aucun accent décomposé dans le contenu livré.

## 4. Écart constaté par rapport au document d’implémentation

Un seul écart, déjà signalé pendant les travaux et désormais **documenté dans un test** (`PocContentTests.Badge_Is_Reachable_Only_After_Opening_The_Wardrobe`) plutôt que corrigé silencieusement :

- Attendu (§4.1, étape 3) : « prends badge » avant « ouvre armoire » doit répondre « objet inconnu ».
- Observé : le badge est bien invisible tant que l’armoire est fermée, mais l’alias « badge » correspond partiellement à « le lecteur de badge », présent dans la même pièce. Le moteur résout donc le lecteur et refuse la prise : « Vous ne pouvez pas prendre cela. »

La cause est la règle de résolution partielle de l’analyseur (§6.6 de l’architecture) : un objet invisible reste partiellement identifiable par ses alias. **Point à trancher** : corriger le moteur (un objet invisible ne devrait pas entrer dans les candidats) ou accepter le comportement. Le choix appartient à l’utilisateur ; il est rappelé au §15 de l’architecture V0.3.

## 5. Critères d’acceptation (document d’implémentation §5)

| Critère | État | Preuve |
|---|---|---|
| Le scénario de référence se déroule de bout en bout dans le jeu réel | atteint | §3 ci-dessus, 14/14 commandes |
| Les trois états de flèche sont corrects dans les quatre lieux | atteint | §3, tableau par lieu, mesure pixelique |
| Les textes accentués s’affichent correctement, fiche patient et guillemets compris | atteint | §3, captures du jeu réel |
| Le monde d’amorçage a entièrement disparu (code, objets, tests, enums) | atteint | §2.2 ; les enums de test restent, par conception (§11 de l’architecture) |
| Build et tests au vert | atteint | `dotnet test` : 83/83 |

## 6. Ce qui reste approximatif, points à valider

1. **Textes provisoires** des objets (hors voix de réveil, date et fiche patient, qui viennent du lore) : à relire à l’oreille.
2. **Formulation des messages d’accompagnement** (déverrouillages, refus de badge, réponse de l’androïde) : à confirmer.
3. **La rue provisoire** n’a ni lore ni image (`Street` sans PNG → rectangle de remplacement affichant le titre, comportement prévu par l’architecture §9.2). À remplacer quand le §5.4 du lore sera rédigé.
4. **L’écart d’alias du §4** (résolution partielle) : arbitrage moteur ou contenu.
5. **Page d’aide** pleine page prévue par la note d’intention : toujours non implémentée (hors PoC, rappelé au §14/§15 de l’architecture).
6. **Icônes d’inventaire et formes des flèches** : encore des formes de remplacement, en attente des assets définitifs.
7. **Dossier de sauvegarde** `Documents/Releve/save.dat` : à revoir (dossier de données d’application plus conventionnel, Linux/macOS).

## 7. Pour rejouer la vérification

```powershell
# 1. lancer le jeu
dotnet run --project src/Releve.Game

# 2. menu sans sauvegarde : supprimer Documents/Releve/save.dat, puis relancer

# 3. depuis un processus SANS FENETRE (Run-Hidden.ps1 s’en charge) :
.\agentTools\Run-Hidden.ps1 -Script .\agentTools\Send-Commands.ps1 `
    -Arguments "-CommandFile .\agentTools\scenarios\poc-reference.txt -DelayMs 700"

# 4. vérifier sans quitter le jeu
#    Save-ReleveGameWindowShot -Path .\shot.png ; Get-ReleveArrowStates -PngFile .\shot.png
```

La recette complète et les pièges d’injection clavier sont dans `agentTools/README.md`.
