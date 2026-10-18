# Jeu "Relève" — Implémentation du PoC V2

> Document d’implémentation — version 1.0 (finale)
> **Prérequis** : PoC V1 livré (compte rendu : `Docs/POC_V1/CompteRenduPOCV0.1.md`, plan : `Docs/POC_V1/ImplementationPOCV0.1.md`), architecture technique V0.3 en place.
> À fournir à l’agent avec : l’architecture technique (V0.3), la note d’intention (V5), le document univers/scénario (V0.7, §5.1 à §5.3), le document PoC V1 et son compte rendu.
> Les mentions « **[code]** » signalent un constat établi par lecture du code du dépôt ; l’agent le **re-vérifie** avant d’agir et signale tout écart.

## 1. But et périmètre

### 1.1 But
Consolider le moteur et l’interface sur le **même contenu de lore que le PoC V1** (chambre, couloir, accueil, rue provisoire) : corriger les défauts constatés au test du V1, enrichir l’interface et le vocabulaire, et introduire des composants réutilisables (visualiseur de texte).

### 1.2 Hors périmètre (impératif)
- Aucun nouveau lieu, objet de lore, puzzle ni élément d’histoire. La rue reste provisoire.
- **Pas de migration .NET** : le projet reste sur .NET 8.
- Pas de déclenchement du visualiseur depuis le moteur (lecture d’une lettre, par exemple), pas de molette de souris, pas de défilement de l’inventaire à la souris, pas de scène de fin, pas de support Linux/macOS.
- Toute évolution d’architecture non listée au §3 : la **signaler**, ne pas la faire.
- Ne pas modifier : `.kilo/`, `_ContentSources/`, les images de lieux, les fichiers audio, `assets/menu/menuBackground.png`, `assets/texts/help.txt`. Aucune commande git d’écriture (règle d’`AGENTS.md`) : l’utilisateur commite lui-même.

### 1.3 Critère de fin
La liste fermée des lots A à C, le scénario de référence V2 (§7.2) rejoué dans le jeu réel, les validations humaines du §7.3 demandées à l’utilisateur, build et tests au vert, documentation mise à jour (§8).

## 2. Règle d’interaction (amende la note d’intention V5, §2)

Tout doit rester **faisable au clavier**. La souris n’est admise que pour des exceptions explicites, toutes doublées d’un équivalent clavier :
1. Deux boutons cliquables (haut / bas) pour faire défiler l’historique de la console (équivalent : Page↑ / Page↓).
2. Le bouton « Fermer » du visualiseur de texte (équivalent : Échap ou Entrée).

Le curseur de la souris est visible. Cette règle prime sur la V5 pour le PoC V2 ; l’utilisateur amende la note d’intention de son côté.

## 3. Évolutions d’architecture autorisées (liste fermée)

1. **Mots de sortie** : `Exit` reçoit une liste optionnelle `Words` ; `CommandParser.Parse` reçoit en paramètre **optionnel** la table des mots de sortie de la pièce courante (les appels existants et les tests restent valides) (§4, A3).
2. **Action `Read`** et nouveaux verbes dans `Vocabulary` (§4, A3).
3. **Correction du plantage `Go` sans direction** dans `GameEngine.Execute` (§4, A5).
4. **Lignes typées dans la console**, boutons de défilement cliquables, gestion de la souris limitée au §2 (couche Game).
5. **Visualiseur de texte modal** et état « aide » de l’application (couche Game).
6. **Sauvegarde sur changement d’état** et sauvegarde de sécurité à la sortie (couche Game).
7. **Seconde taille de police** pour le titre du menu (`Fonts`).
8. **Réécriture du panneau des flèches** (couche Game) et recalibrage de `Get-ReleveArrowStates` dans `agentTools/`.
9. **Image du lieu étirée** à toute la zone utile de la vue (couche Game) : amende l’architecture §9.2 et §9.3.
10. **Menu à trois entrées** (couche Game) : `MenuScreen` renvoie un choix typé.

Le moteur (projet Engine) ne change que pour 1, 2, 3 et les corrections du §4. Tout autre besoin : signaler.

## 4. Lot A — Corrections moteur et contenu

Tous les textes de cette section sont **provisoires**.

### A1. Écart d’alias « prends badge » (documentation seulement)
**[code]** `GameEngine.Scope()` ne renvoie que les objets visibles ; `CommandParser.Resolve` teste l’égalité exacte d’un alias, puis la présence de tous les mots de la saisie dans un alias. Le badge caché est donc absent de la portée, et « badge » correspond partiellement à l’alias « lecteur badge » du lecteur, visible.
- Décision : **accepter le comportement** (« Vous ne pouvez pas prendre cela. » ne révèle rien). Aucun changement de moteur.
- Corriger : architecture §6.6 et §15 (point 6, dont la solution proposée serait sans effet), étape 3 du scénario de référence, nom et commentaire du test `Badge_Is_Reachable_Only_After_Opening_The_Wardrobe`.

### A2. Corbeille
`BedroomBin` : si `GarbageBag` est dans `Bedroom` : « Une corbeille. Elle ne contient qu’un sac poubelle vide. » ; sinon : « Une corbeille, vide. »

### A3. Vocabulaire, mots de sortie et réponses

**Verbes ajoutés** (`Vocabulary.Verbs`) :
| Action | Formes ajoutées |
|---|---|
| Talk | interroge, interroger, demande, demander |
| Use | pousse, pousser, actionne, actionner |
| Read (nouveau) | lis, lire, lisez |
| Go (verbes de déplacement) | sors, sortir, monte, monter, descends, descendre, entre, entrer, rentre, rentrer, retourne, retourner |

**Mots de sortie** (`Exit.Words`, normalisés comme les alias). Ils franchissent la sortie comme la direction cardinale correspondante, avec les mêmes règles de blocage et les mêmes messages.

| Lieu | Sortie | Mots |
|---|---|---|
| Chambre | Est | sors, sortir |
| Couloir | Ouest | retourne, retourner, chambre |
| Couloir | Nord | monte, monter, escalier |
| Accueil | Sud | descends, descendre |
| Accueil | Ouest | sors, sortir, rue |
| Accueil | Nord | monte, monter, ascenseur |
| Rue | Est | entre, entrer, rentre, rentrer, hôpital |

**Règle de reconnaissance** (dans `CommandParser.Parse`, après la normalisation et le test de direction seule) :
- Elle ne s’applique que si le verbe est de la classe `Go` (anciens et nouveaux verbes).
- Les mots qui suivent le verbe, prépositions ignorées, doivent être tous des mots de sortie de la pièce, avec au moins un mot de sortie ; le **verbe lui-même** compte comme mot de sortie s’il figure dans la liste (« sors », « monte » seuls). Exemples : « sors », « va dans la rue », « monte l’escalier », « rentre dans l’hôpital ».
- Pour un verbe de `Go`, les mots de sortie sont **prioritaires** sur les alias d’objets : **[code]** « va rue » résolvait aujourd’hui « rue » comme alias de la porte vitrée. Pour tout autre verbe, seuls les alias d’objets comptent : « regarde escalier » reste un `Look`.
- Un verbe de déplacement **nouveau** sans mot de sortie correspondant (« sors » dans le couloir) répond « Vous ne pouvez pas aller par là. ». « va » et « aller » seuls gardent leur comportement actuel (« Précisez l’objet de la commande. »).
- Chaque liste de mots est unique dans une pièce, non vide, ne contient aucune direction cardinale ; `WorldConsistencyTests` le vérifie.
- Remarque **[code]** : toute commande dont le premier mot est une direction est un déplacement (« est badge » va à l’est). Comportement inchangé, couvert par un test.

**`Read`** : `GameEngine.DefaultBehaviour` ne le gère pas aujourd’hui. Défaut : « Il n’y a rien à lire ici. » ; sur `Display` et `BedroomMachines`, même texte que `Look` (via `Handle` des pièces).

**`Use` sans instrument** : « utilise badge » (cible dans l’inventaire, non gérée par la pièce) répond « Précisez sur quoi l’utiliser. » au lieu de « Rien ne se passe. ».

**« Ouvre » sur les portes** (`Handle` des pièces) :
| Objet | Condition | Message |
|---|---|---|
| GlassDoor | Terminal ≠ Activated | « La porte vitrée de l’entrée reste fermée. » |
| GlassDoor | Terminal = Activated | « La porte vitrée est déverrouillée. Le passage est libre vers l’ouest. » |
| StairsDoor | StairsReader = Locked | « La porte de l’escalier est fermée. Le lecteur de badge affiche un rouge fixe. » |
| StairsDoor | StairsReader = Unlocked | « La porte de l’escalier est déverrouillée. » |
| TechDoor | toujours | « La porte du local technique est fermée. Le lecteur de badge affiche un rouge fixe. » |
| Elevator | toujours | « Les portes de l’ascenseur restent closes. Le lecteur de badge affiche un rouge fixe. » |

### A4. `StateChanged` fiable
**[code]** Dans `GameEngine`, `Take`, `Drop`, `Open`/`Close` et le déplacement renvoient déjà `Changed`. Reste à vérifier les `Handle` des trois pièces (déverrouillages, fiche patient, ouverture de l’armoire) : toute modification d’état doit renvoyer `Changed`. S’il en manque, le corriger (c’est un défaut) et le signaler. Le point C1 en dépend.

### A5. Plantage « va <objet> »
**[code]** `GameEngine.Execute` appelle `Move(command.Direction!.Value)` pour toute commande `Go`. Une commande `Go` avec une cible objet et sans direction (« va porte », « aller lit ») lève une exception qui n’est interceptée nulle part.
- Correction : une commande `Go` sans direction répond « Vous ne pouvez pas aller par là. » (`Messages.CannotGo`) sans exception.
- Test moteur : « va porte » (chambre), « aller lit », « va ascenseur » (accueil, sans mot de sortie correspondant à ce verbe seul) ne lèvent aucune exception et répondent ce message. Mettre à jour l’architecture §7.3, qui décrit déjà ce cas comme défensif.

## 5. Lot B — Corrections d’interface (retours de playtest)

### B1. Touche Retour arrière
**[code]** `ConsolePanel.ReadCharacters` teste `KeyboardKey.Back`, qui est la touche « retour » d’Android (code 4) ; la touche du clavier est `KeyboardKey.Backspace`. Suppr (`KeyboardKey.Delete`) fonctionne.
- Corriger en `Backspace` (vérifier dans la cheatsheet raylib et l’énumération de Raylib-cs).
- Retour arrière et Suppr se **répètent** lorsque la touche est maintenue (comme ←/→, via `IsKeyPressedRepeat`).
- L’automatisation de `agentTools/` ne peut pas valider cette touche comme un vrai clavier : validation **humaine** (§7.3). Documenter la limite dans `agentTools/README.md` et l’architecture §16.

### B2. Console : couleurs et boutons de défilement
- **Deux types de ligne** : *saisie du joueur* (écho « > commande » et ligne en cours de saisie, y compris son invite) et *réponse du jeu* (description de lieu, messages, fiche patient). **[code]** `ConsolePanel` stocke aujourd’hui des `string` dans `_lines` ; chaque ligne d’historique mémorise un type (les lignes issues du retour à la ligne d’un même message gardent son type). `Append` reçoit le type ; `GameApp.Submit` ajoute l’écho en type *saisie*, les réponses du moteur et `Start()` en type *réponse*. Deux couleurs distinctes dans `Ui/Palette.cs`, contraste suffisant sur le fond de la console (valeurs provisoires, à valider à l’écran). Le moteur n’est pas modifié.
- **Deux boutons cliquables** (haut, bas) dessinés comme **formes géométriques** (la police n’a pas les glyphes ▲ ▼), dans une colonne d’environ 28 px à l’extrémité droite de la console : haut en haut de la zone d’historique, bas en bas de la zone d’historique (au-dessus de la ligne de saisie). La largeur de retour à la ligne de la console est réduite de cette colonne pour que le texte ne passe jamais dessous.
- Un clic fait défiler de 3 lignes (provisoire) ; maintenir le clic répète. Un bouton est grisé quand le défilement est impossible dans son sens ; retour visuel au survol et au clic. Vérifier les appels souris de raylib dans la cheatsheet (position, bouton enfoncé, collision point/rectangle).
- Page↑ / Page↓ restent actifs (**[code]** déjà fonctionnels). Le défilement de l’inventaire reste au clavier (Ctrl+↑/↓). Un nouveau message ramène toujours l’historique en bas (comportement actuel conservé).

### B3. Panneau des flèches de déplacement
**[code]** `ArrowsPanel.DrawArrowShape` appelle `Raylib.DrawTriangle` avec un ordre de sommets **horaire** pour Nord, Sud et Ouest. Raylib exige l’ordre anti-horaire et ne dessine pas les autres : il ne reste que les fûts (les « traits » constatés). Seule la pointe Est est dans le bon sens. Les libellés sont par ailleurs placés en grille, toujours à droite de la flèche, et la couleur « possible » (`Palette.Prompt`) est trop proche de la couleur « bloquée ».

Nouvelle disposition, en **croix**, centrée horizontalement dans le panneau, sous le titre (conservé) ; trois rangées d’environ 30 px (hauteur disponible : 132 px) :
```text
         ▲ Nord
  Ouest ◄        ► Est
         ▼ Sud
```
- Rangée 1 : flèche Nord puis libellé « Nord », l’ensemble centré. Rangée 2 : libellé « Ouest » puis sa flèche (le libellé est à gauche de la flèche) ; au centre, un espace libre ; à droite, la flèche Est puis le libellé « Est ». Rangée 3 : flèche Sud puis libellé « Sud », centrés.
- Les flèches sont des polygones à pointe et fût réels (environ 20 px), **dessinés avec un ordre de sommets valide** pour la primitive utilisée ; elles ne s’appuient pas sur des glyphes.
- `Absent` : ni flèche ni libellé, l’emplacement reste réservé. `Open` : flèche **pleine**, couleur vive, libellé normal. `Blocked` : flèche **creuse** (contour seul), gris sombre nettement plus bas en luminosité, libellé atténué. La distinction ne repose pas que sur la couleur.
- Couleurs : constantes de `Ui/Palette.cs` distinctes de `Palette.Prompt`, valeurs provisoires à valider à l’écran.
- Les états restent calculés par `GameEngine.GetView` ; aucun changement de moteur.
- **Recalibrer `Get-ReleveArrowStates`** (`agentTools/`) sur la nouvelle géométrie, pour reconnaître aussi flèche pleine / creuse, et mettre à jour `README.md`.

### B4. Image du lieu étirée à toute la zone
**[code]** `RoomViewPanel.DrawScaledCentered` calcule une échelle uniforme (`MathF.Min`) et centre l’image dans `Layout.ViewBounds`. La zone utile faisant 940 × 460 px et les images étant en ratio 2:1, deux bandes d’environ 10 px restent vides à gauche et à droite.
- L’image est **étirée pour remplir exactement `ViewBounds`**, proportions non conservées (déformation horizontale d’environ 2 %, acceptée). Les constantes de `Ui/Layout.cs` ne changent pas.
- Le cadre (`DrawRectangleLinesEx`) est redessiné **après** l’image pour rester visible.
- Le rectangle de remplacement (image absente) reste inchangé.
- Mettre à jour l’architecture §9.2 et §9.3, qui décrivent encore la conservation des proportions.

## 6. Lot C — Nouvelles fonctionnalités

### C1. Sauvegarde automatique
- **[code]** `GameApp.Run` ne sauvegarde qu’à la sortie normale de la boucle, et rien n’intercepte une exception.
- Après toute commande dont le résultat signale `StateChanged` (donc aussi tout déplacement), `GameApp.Submit` sauvegarde l’état. Uniquement si une partie est en cours.
- La sauvegarde de fermeture est conservée, déplacée dans un bloc `finally` pour couvrir aussi une sortie sur exception. **Ne pas** avaler les exceptions.
- Le moteur n’est pas modifié (voir A4).

### C2. Menu avec image de fond
- Fichier : `src/Releve.Game/assets/menu/menuBackground.png` (respecter la casse), résolu via `Ui/Assets.cs` (nouveau chemin). **Fourni par l’utilisateur, ne pas le remplacer.**
- Chargée après `InitWindow`, déchargée à la fermeture. Étirée pour remplir toute la fenêtre (proportions non conservées), dessinée avant le titre et les entrées. Si le fichier est absent : fond actuel (`Palette.Background`), sans erreur.
- **[code]** `Fonts` n’a qu’une taille (`Size` = 20, `LineHeight` constants) et un seul `Font`. Ajouter une seconde police plus grande (environ 64 px) pour le titre, avec ses propres méthodes de mesure et de dessin, sans changer l’API existante. Le jeu de codepoints du titre peut se limiter à l’ASCII et au Latin-1.
- Le titre « Relève » s’affiche en grand, centré en haut. Les entrées, dans l’ordre : **Continuer, Nouvelle partie, Aide**, écrites sur l’image, aux positions actuelles (`y = 300 + 60 × index`, à ajuster pour l’équilibre visuel). « Continuer » reste grisé sans sauvegarde valide ; « Aide » est toujours accessible.
- **[code]** `MenuScreen` bascule aujourd’hui entre deux entrées (`Math.Abs(_selected - 1)` pour Haut **et** Bas) et renvoie un `int?` (−1 nouvelle partie, 0 continuer). À réécrire pour trois entrées : navigation cyclique Haut/Bas, `Reset(canContinue)` conserve la sélection par défaut actuelle (Continuer si sauvegarde, sinon Nouvelle partie), retour typé (`Continue`, `NewGame`, `Help`). Valider « Continuer » grisé ne fait rien (comportement actuel). `GameApp.UpdateMenu` n’appelle plus `StartNewGame` par défaut : « Aide » n’entre pas en jeu, ne change pas la musique et ne modifie pas `_playing`.
- Lisibilité du texte sur l’image : ombre ou contour du texte d’abord ; la sélection garde son rectangle ; un voile discret derrière les entrées seulement si cela ne suffit pas. Signaler le choix.

### C3. Visualiseur de texte (composant réutilisable)
Composant de la couche Game (`Ui/TextViewer.cs`), **superposé** à l’écran courant.
- **Paramètres** : rectangle d’affichage (plein écran pour l’aide ; en jeu, une fenêtre partielle), titre optionnel, texte à afficher.
- **Opaque et modal** : il remplit son rectangle d’une couleur pleine et masque complètement ce qu’il recouvre. **[code]** Dans `GameApp`, tant qu’il est ouvert : ni `UpdateMenu`, ni `UpdateGame` (donc ni `ConsolePanel.TryReadCommand` ni `InventoryPanel.Update`) ne sont appelés ; la scène sous-jacente est dessinée puis recouverte ; la file de caractères (`GetCharPressed`) est vidée à chaque image tant qu’il est ouvert, pour qu’aucune saisie ne fuie dans la console à la fermeture. La musique continue.
- **Texte** : retour à la ligne automatique à la largeur du rectangle (mesurée avec `Fonts.MeasureWidth`), mots plus larges que la zone coupés, lignes vides conservées.
- **Défilement** : Page↑ / Page↓ (une page moins une ligne), ↑ / ↓ (une ligne, avec répétition), Début / Fin. Indicateurs « il reste du texte au-dessus / en dessous » dessinés comme formes géométriques (provisoire).
- **Fermeture** : un bouton « Fermer » cliquable et l’équivalent clavier Échap ou Entrée. Le visualiseur ne traite aucune entrée dans la frame où il est créé (l’Entrée qui l’ouvre depuis le menu ne doit pas le fermer).
- API suggérée : `Open(rectangle, titre, texte)`, `Update()` (renvoie vrai à la fermeture), `Draw()`.
- La partie « rectangle partiel » ne peut pas être déclenchée en jeu dans ce PoC (hors périmètre) : la vérifier par une **vérification temporaire non committée**, et le signaler.

### C4. Entrée « Aide »
Valider « Aide » ouvre le visualiseur en plein écran, titre « Aide », avec le contenu de `src/Releve.Game/assets/texts/help.txt` (UTF-8, **fourni par l’utilisateur**, actuellement du lorem ipsum), résolu via `Ui/Assets.cs` (nouveau chemin). Fichier absent : le visualiseur affiche « Texte d’aide introuvable. » (provisoire). Vérifier que `assets/texts/` et `assets/menu/` sont bien copiés dans le dossier de sortie et de publication, et que tous les caractères du fichier sont couverts par la police (`Fonts.BuildCodepoints` : ASCII, Latin-1, Latin Extended-A, ponctuation listée) ; sinon, le signaler. Échap ou Entrée ramène au menu avec la même sélection (« Aide »).

## 7. Tests et validation

### 7.1 Tests moteur
- A2 : corbeille selon l’emplacement du sac.
- A3 : nouveaux verbes ; mots de sortie (un test par ligne du tableau, avec blocage) ; priorité des mots de sortie pour les verbes `Go` et des alias pour les autres (« regarde escalier » = `Look`) ; « sors » dans le couloir (sans mot correspondant) ; `Read` ; `Use` sans instrument ; réponses à « ouvre ».
- A4 : `StateChanged` sur les actions et `Handle` concernés. A5 : aucune exception pour « va porte », « aller lit ».
- A1 : test renommé, explication corrigée.
- `WorldConsistencyTests` : mots de sortie non vides, normalisés comme les alias, uniques par pièce, sans direction cardinale. Les tests génériques restent verts.

### 7.2 Scénario de référence V2
Scénario V1 (§4.1 du PoC V1), avec l’étape 3 corrigée (refus « Vous ne pouvez pas prendre cela. »), plus, dans le jeu réel :
1. Chambre : « regarde corbeille » (avec sac), « prends sac », « regarde corbeille » (vide) ; « va porte » → « Vous ne pouvez pas aller par là. » sans plantage.
2. « utilise badge » → « Précisez sur quoi l’utiliser. ».
3. « sors » depuis la chambre (lecteur déverrouillé) → couloir ; « monte » → bloquée puis, escalier débloqué, → accueil.
4. « lis afficheur » → la date.
5. Fiche patient, puis « sors » → rue ; « entre » → accueil.
6. Flèches : disposition en croix, flèches pleines / creuses correctes dans les quatre lieux (capture et mesure).
7. Couleurs : saisie et réponses en couleurs distinctes (capture).
8. Image du lieu : sur une capture de la chambre, du couloir et de l’accueil, plus aucune bande vide entre l’image et le cadre.
9. Menu : image, titre, trois entrées ; Aide s’ouvre, défile, se ferme ; retour au menu.
10. Sauvegarde : après « ouvre armoire » (sans changement de pièce), arrêt brutal du processus, relance, « Continuer » : armoire ouverte.

### 7.3 Validations humaines (à demander à l’utilisateur, non automatisables)
Retour arrière et répétition de touche ; clics sur les boutons de défilement de la console ; clic sur « Fermer » du visualiseur ; lisibilité des couleurs, des flèches et du menu ; aspect de l’image étirée.

## 8. Critères d’acceptation et documentation
- Scénario V2 de bout en bout dans le jeu réel ; scénario V1 non régressé.
- Build et tests au vert ; accents corrects partout (architecture §2). Les caractères corrompus rencontrés dans des commentaires (par exemple « Â§ » dans `GameEngine.cs`) sont signalés, pas corrigés au passage.
- Mises à jour : architecture (V0.4 : §6.6, §7.3, §9.2, §9.3, §15, §16), `README.md` (section Sauvegarde, liste des assets), `AGENTS.md` (contrat d’assets avec `menu/` et `texts/`, mention du PoC V2, limite de l’automatisation clavier), `agentTools/` recalibré et documenté. Les chemins des documents déplacés (`Docs/POC_V1/`, `Docs/POC_V2/`) sont mis à jour dans `AGENTS.md`, `README.md` et l’architecture.
- Compte rendu du PoC V2 rédigé dans `Docs/POC_V2/ImplementationPOC2V0.1.md`.
- Le compte rendu distingue ce qui est vérifié dans le jeu réel, ce qui ne l’est que par les tests et ce qui attend l’utilisateur (§7.3).

## 9. Consignes pour l’agent
1. Jalon 1 : lot A (moteur et tests).
2. Jalon 2 : B1, B2, B3, B4 et C1.
3. Jalon 3 : C2, C3, C4.
4. Jalon 4 : scénario V2 dans le jeu réel, documentation, récapitulatif.
5. Vérifier chaque appel raylib (souris, répétition de touche, primitives de dessin, collision point/rectangle) dans la cheatsheet et les exemples officiels, pas de mémoire.
6. Respecter le §3 ; signaler tout besoin hors liste, toute incohérence avec le lore ou l’architecture, et tout constat **[code]** qui s’avérerait inexact.

## 10. Points à valider par l’utilisateur
1. Décision A1 (accepter le comportement).
2. Textes provisoires du lot A et de C3/C4.
3. Mots de sortie (tableau A3).
4. Couleurs (console, flèches), taille et pas de défilement des boutons.
5. Indicateurs du visualiseur ; Échap ou Entrée pour fermer.
6. Rendu de l’image du lieu étirée (déformation d’environ 2 %) et du menu (lisibilité du texte sur l’image).

## 11. Pistes pour la suite (hors périmètre, à titre de mémoire)
Déclenchement du visualiseur depuis le moteur (lettre des parents, crédits), vrai texte d’aide, molette de souris, dialogues plus riches, variantes d’image selon l’état d’un objet.