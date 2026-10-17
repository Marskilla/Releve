# Jeu "Relève" — Implémentation du PoC V2

> Document d’implémentation — version 0.3
> **Prérequis** : PoC V1 livré (« CompteRenduPOCV0.1.md »), architecture technique V0.3 en place.
> À fournir à l’agent avec : l’architecture technique (V0.3), la note d’intention (V5), le document univers/scénario (V0.7, §5.1 à §5.3), le document PoC V1 et son compte rendu.

## 1. But et périmètre

### 1.1 But
Consolider le moteur et l’interface sur le **même contenu de lore que le PoC V1** (chambre, couloir, accueil, rue provisoire) : corriger les défauts constatés au test du V1, enrichir l’interface et le vocabulaire, et introduire des composants réutilisables par la suite (visualiseur de texte).

### 1.2 Hors périmètre (impératif)
- Aucun nouveau lieu, objet de lore, puzzle ni élément d’histoire. La rue reste provisoire.
- **Pas de migration .NET** : le projet reste sur .NET 8.
- Pas de déclenchement du visualiseur depuis le moteur (lecture d’une lettre, par exemple), pas de molette de souris, pas de défilement de l’inventaire à la souris, pas de scène de fin, pas de support Linux/macOS.
- Toute évolution d’architecture non listée au §3 : la **signaler**, ne pas la faire.

### 1.3 Critère de fin
La liste fermée des lots A à C, le scénario de référence V2 (§7.2) rejoué dans le jeu réel, les validations humaines du §7.3 demandées à l’utilisateur, build et tests au vert, documentation mise à jour (§8).

## 2. Règle d’interaction (amende la note d’intention V5, §2)

Tout doit rester **faisable au clavier**. La souris n’est admise que pour des exceptions explicites, toutes doublées d’un équivalent clavier :
1. Deux boutons cliquables (haut / bas) pour faire défiler l’historique de la console (équivalent : Page↑ / Page↓).
2. Le bouton « Fermer » du visualiseur de texte (équivalent : Échap ou Entrée).

Le curseur de la souris est visible. Cette règle prime sur la V5 pour le PoC V2 ; l’utilisateur amende la note d’intention de son côté.

## 3. Évolutions d’architecture autorisées (liste fermée)

1. **Mots de sortie** : `Exit` reçoit une liste optionnelle `Words` (§4, A3).
2. **Action `Read`** : nouveau membre de `GameAction`, nouveaux verbes (§4, A3).
3. **Lignes typées dans la console** (couche Game).
4. **Visualiseur de texte modal** et état « aide » de l’application (couche Game).
5. **Sauvegarde sur changement d’état ou de pièce** (couche Game).
6. **Gestion de la souris** dans la couche Game, limitée aux exceptions du §2.
7. **Seconde taille de police** pour le titre du menu.
8. **Réécriture du panneau des flèches** (couche Game) et recalibrage de `Get-ReleveArrowStates` dans `agentTools/`.
9. **Image du lieu étirée** à toute la zone de la vue, proportions non conservées (couche Game) : amende l’architecture §9.2 et §9.3.

Le moteur (projet Engine) ne change que pour 1 et 2 et pour les corrections du §4. Tout autre besoin : signaler.

## 4. Lot A — Corrections moteur et contenu

Tous les textes de cette section sont **provisoires**.

### A1. Écart d’alias « prends badge » (documentation seulement)
Diagnostic : le badge, invisible armoire fermée, est **déjà exclu de la portée** (architecture §6.6). C’est « badge » qui correspond partiellement à l’alias « lecteur badge » du lecteur, visible.
- **Vérifier d’abord dans le code** que ce diagnostic est exact. S’il ne l’est pas, signaler avant de continuer.
- Décision proposée : **accepter le comportement** (« Vous ne pouvez pas prendre cela. » ne révèle rien). Aucun changement de moteur.
- Corriger : architecture §6.6 et §15 (point 6), étape 3 du scénario de référence, nom et commentaire du test `Badge_Is_Reachable_Only_After_Opening_The_Wardrobe`.

### A2. Corbeille
`BedroomBin` : si `GarbageBag` est dans `Bedroom` : « Une corbeille. Elle ne contient qu’un sac poubelle vide. » ; sinon : « Une corbeille, vide. »

### A3. Vocabulaire et réponses

**Verbes ajoutés** (table du §6.3 de l’architecture) :
| Action | Formes ajoutées |
|---|---|
| Talk | interroge, interroger, demande, demander |
| Use | pousse, pousser, actionne, actionner |
| Read (nouveau) | lis, lire, lisez |

**Mots de sortie** (`Exit.Words`) : ils franchissent la sortie comme la direction cardinale correspondante, avec les mêmes règles de blocage et les mêmes messages.

| Lieu | Sortie | Mots |
|---|---|---|
| Chambre | Est | sors, sortir |
| Couloir | Ouest | retourne, retourner, chambre |
| Couloir | Nord | monte, monter, escalier |
| Accueil | Sud | descends, descendre |
| Accueil | Ouest | sors, sortir, rue |
| Accueil | Nord | monte, monter, ascenseur |
| Rue | Est | entre, entrer, rentre, rentrer, hôpital |

Règle de reconnaissance (après normalisation) : la commande est un mot de sortie seul, **ou** composée uniquement de mots de sortie et de verbes de déplacement (va, vas, aller, allez, avance, avancer), avec au moins un mot de sortie, les prépositions étant ignorées. Exemples : « sors », « va dans la rue », « monte l’escalier », « rentre dans l’hôpital ». Un alias d’objet n’est **jamais** masqué par un mot de sortie : « regarde escalier » reste un `Look`. Chaque liste de mots est unique dans une pièce et ne contient aucune direction cardinale ; `WorldConsistencyTests` le vérifie.

**`Read`** : défaut « Il n’y a rien à lire ici. » ; sur `Display` et `BedroomMachines`, même texte que `Look`.

**`Use` sans instrument** : « utilise badge » (cible dans l’inventaire, non gérée par la pièce) répond « Précisez sur quoi l’utiliser. »

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
Le point C1 (sauvegarde) s’appuie sur `CommandResult.StateChanged`. Vérifier que `Take`, `Drop`, `Open`, `Close` et les comportements `Handle` du contenu le positionnent bien dès qu’ils modifient l’état. S’il manque quelque part : le corriger (c’est un défaut) et le signaler.

## 5. Lot B — Corrections d’interface (retours de playtest)

### B1. Touche Retour arrière
Défaut constaté : Retour arrière ne fonctionne pas à la saisie (Suppr fonctionne).
- Reproduire, identifier la cause, corriger. Signaler la cause.
- Retour arrière, Suppr et ←/→ se **répètent** lorsque la touche est maintenue (vérifier dans la cheatsheet l’API de répétition de raylib).
- L’automatisation (`agentTools/`) injecte des touches par `keybd_event` : ce n’est pas un vrai clavier. Elle n’a pas détecté ce défaut. La validation finale est donc **humaine** (§7.3) ; documenter cette limite dans `agentTools/README.md` et l’architecture §16.

### B2. Console : couleurs et boutons de défilement
- **Deux types de ligne** : *saisie du joueur* (écho « > commande » et ligne en cours de saisie) et *réponse du jeu* (description de lieu, messages, fiche patient). L’historique mémorise un type par ligne ; deux couleurs distinctes dans `Ui/Palette.cs`, avec un contraste suffisant sur le fond de la console. Valeurs provisoires, à valider à l’écran. Le moteur n’est pas modifié.
- **Deux boutons cliquables** (▲ haut, ▼ bas) dans la zone de la console, côté droit, sans masquer le texte. Un clic fait défiler de 3 lignes (provisoire) ; maintenir le clic répète. Un bouton est grisé quand le défilement est impossible dans son sens. Retour visuel au survol et au clic.
- Page↑ / Page↓ restent actifs (déjà fonctionnels). Le défilement de l’inventaire reste au clavier (Ctrl+↑/↓).

### B3. Panneau des flèches de déplacement
Défauts constatés : Nord, Ouest, Sud s’affichent comme de simples traits ; libellés mal alignés et dans le mauvais ordre ; contraste insuffisant entre « bloquée » et « possible ».

Nouvelle disposition, en **croix**, dans la zone de 320 × 132 px sous le titre « Déplacements possibles : » (trois rangées compactes) :
```text
        ▲ Nord
  Ouest ◄      ► Est
        ▼ Sud
```
- Rangée 1 : Nord, centré ; rangée 2 : le libellé « Ouest » à gauche, suivi de sa flèche vers l’ouest ; au centre, un espace ; à droite, la flèche vers l’est suivie du libellé « Est » ; rangée 3 : Sud, centré.
- Chaque flèche a un fût et une pointe réels, orientés selon la direction (environ 20 px).
- `Absent` : ni flèche ni libellé, l’emplacement reste réservé. `Open` : flèche **pleine**, couleur vive, libellé normal. `Blocked` : flèche **creuse** (contour seul), gris sombre nettement distinct, libellé atténué. La distinction ne repose pas que sur la couleur.
- Les couleurs sont des constantes de `Ui/Palette.cs` (valeurs provisoires, à valider à l’écran).
- Les états restent calculés par `GameEngine.GetView` ; aucun changement de moteur.
- **Recalibrer `Get-ReleveArrowStates`** (`agentTools/`) sur la nouvelle géométrie, pour reconnaître aussi flèche pleine / creuse, et mettre à jour `README.md`.

### B4. Image du lieu étirée à toute la zone
Défaut constaté : l’image du lieu est mise à l’échelle en conservant ses proportions et centrée ; la zone utile de la vue faisant 940 × 460 px (960 × 480 moins la marge `Padding` de 10 px) et les images étant en ratio 2:1, deux bandes vides d’environ 10 px restent visibles à gauche et à droite.
- L’image est **étirée pour remplir exactement la zone utile** (l’intérieur du cadre, marges `Padding` conservées), proportions non conservées. La déformation horizontale est d’environ 2 %, acceptée.
- Les constantes de `Ui/Layout.cs` ne changent pas.
- Le rectangle de remplacement (image absente) reste inchangé et remplit déjà la zone.
- Mettre à jour l’architecture §9.2 et §9.3, qui décrivent encore la conservation des proportions.

## 6. Lot C — Nouvelles fonctionnalités

### C1. Sauvegarde automatique
En plus de la sauvegarde à la fermeture, `GameApp` sauvegarde après toute commande dont le résultat signale un changement d’état (`StateChanged`) **ou** qui a changé `CurrentRoom`. Uniquement si une partie est en cours. Aucune exception ne doit remonter de la couche de sauvegarde (déjà garanti). Le moteur n’est pas modifié (voir A4).

### C2. Menu avec image de fond
- Fichier : `src/Releve.Game/assets/menu/menuBackground.png` (respecter la casse). **Fourni par l’utilisateur, ne pas le remplacer.**
- L’image est étirée pour remplir toute la fenêtre (proportions non conservées). Chargée une fois, déchargée à la fermeture. Si le fichier est absent : fond actuel, sans erreur.
- Le titre « Relève » s’affiche en grand, centré dans le haut de l’écran ; il nécessite une seconde taille de la police DejaVu (agrandir la taille de 20 px la rendrait floue).
- Les entrées, dans l’ordre : **Continuer, Nouvelle partie, Aide**, écrites sur l’image. Navigation clavier inchangée (haut/bas, Entrée). « Continuer » reste grisé sans sauvegarde valide ; « Aide » est toujours accessible.
- Lisibilité : ombre ou contour du texte d’abord ; un voile discret derrière les entrées seulement si cela ne suffit pas. Signaler le choix.

### C3. Visualiseur de texte (composant réutilisable)
Composant de la couche Game (`Ui/TextViewer.cs`), **superposé** à l’écran courant.
- **Paramètres** : rectangle d’affichage (plein écran pour l’aide ; en jeu, une fenêtre partielle), titre optionnel, texte à afficher.
- **Opaque et modal** : il masque complètement ce qu’il recouvre ; tant qu’il est ouvert, la console, les commandes et le menu en arrière-plan sont inactifs et ne reçoivent aucune saisie. La musique continue.
- **Texte** : retour à la ligne automatique à la largeur du rectangle (mesurée avec la police), mots trop longs coupés, lignes vides conservées.
- **Défilement** : Page↑ / Page↓ (une page), ↑ / ↓ (une ligne), Début / Fin. Des indicateurs ▲ / ▼ signalent qu’il reste du texte au-dessus / en dessous (provisoire).
- **Fermeture** : un bouton « Fermer » cliquable, et l’équivalent clavier Échap ou Entrée. La touche utilisée pour ouvrir (Entrée dans le menu) ne doit pas fermer immédiatement le visualiseur : traiter la touche sur front, une frame après l’ouverture, et vider la file de caractères à l’ouverture et à la fermeture.
- La partie « rectangle partiel » ne peut être déclenchée qu’en jeu, hors périmètre : la vérifier par une **vérification temporaire non committée**, et le signaler.

### C4. Entrée « Aide »
Valider « Aide » ouvre le visualiseur en plein écran, titre « Aide », avec le contenu de `src/Releve.Game/assets/texts/help.txt` (UTF-8, **fourni par l’utilisateur**, actuellement du lorem ipsum). Fichier absent : le visualiseur affiche « Texte d’aide introuvable. » (provisoire). Vérifier que les assets `texts/` sont bien copiés dans le dossier de sortie et de publication, et que tous les caractères du fichier sont couverts par la police (§9.5 de l’architecture) ; sinon, le signaler.

## 7. Tests et validation

### 7.1 Tests moteur
- A2 : corbeille selon l’emplacement du sac. A3 : nouveaux verbes ; mots de sortie (un test par ligne du tableau, avec blocage, plus la règle « un alias d’objet n’est jamais masqué ») ; `Read` ; `Use` sans instrument ; réponses à « ouvre ». A4 : `StateChanged` sur les actions concernées.
- A1 : test renommé, explication corrigée.
- `WorldConsistencyTests` : mots de sortie non vides, normalisés comme les alias, uniques par pièce, sans direction cardinale. Les tests génériques restent verts.

### 7.2 Scénario de référence V2
Scénario V1 (§4.1 du PoC V1), avec l’étape 3 corrigée (refus « Vous ne pouvez pas prendre cela. »), plus, dans le jeu réel :
1. Chambre : « regarde corbeille » (avec sac), « prends sac », « regarde corbeille » (vide).
2. « utilise badge » → « Précisez sur quoi l’utiliser. ».
3. « sors » depuis la chambre (lecteur déverrouillé) → couloir ; « monte » → bloquée puis, escalier débloqué, → accueil.
4. « lis afficheur » → la date.
5. Fiche patient, puis « sors » → rue ; « entre » → accueil.
6. Flèches : disposition en croix, flèches pleines / creuses correctes dans les quatre lieux (capture et mesure).
7. Couleurs : saisie et réponses en couleurs distinctes (capture).
8. Image du lieu : sur une capture de la chambre, du couloir et de l’accueil, plus aucune bande vide entre l’image et le cadre, à gauche comme à droite.
9. Menu : image, titre, trois entrées ; Aide s’ouvre, défile, se ferme ; retour au menu.
10. Sauvegarde : après « ouvre armoire » (sans changement de pièce), arrêt brutal du processus, relance, « Continuer » : armoire ouverte.

### 7.3 Validations humaines (à demander à l’utilisateur, non automatisables)
Retour arrière et répétition de touche ; clics sur les boutons ▲ / ▼ de la console ; clic sur « Fermer » du visualiseur ; lisibilité des couleurs, des flèches et du menu ; aspect de l’image étirée.

## 8. Critères d’acceptation et documentation
- Scénario V2 de bout en bout dans le jeu réel ; scénario V1 non régressé.
- Build et tests au vert ; accents corrects partout (architecture §2).
- Architecture mise à jour (V0.4, dont §9.2 et §9.3 pour l’image étirée), compte rendu du PoC V2 rédigé, `agentTools/` recalibré et documenté.
- Le compte rendu distingue ce qui est vérifié dans le jeu réel, ce qui ne l’est que par les tests et ce qui attend l’utilisateur (§7.3).

## 9. Consignes pour l’agent
1. Jalon 1 : lot A (moteur et tests).
2. Jalon 2 : B1, B2, B3, B4 et C1.
3. Jalon 3 : C2, C3, C4.
4. Jalon 4 : scénario V2 dans le jeu réel, documentation, récapitulatif.
5. Vérifier chaque appel raylib (souris, répétition de touche, collision point/rectangle) dans la cheatsheet et les exemples officiels, pas de mémoire.
6. Respecter le §3 ; signaler tout besoin hors liste, toute incohérence avec le lore ou l’architecture. Ne pas modifier les assets fournis (image, texte d’aide, audio).

## 10. Points à valider par l’utilisateur
1. Décision A1 (accepter le comportement).
2. Textes provisoires du lot A et de C3/C4.
3. Mots de sortie (tableau A3).
4. Couleurs (console, flèches), taille et pas de défilement des boutons.
5. Indicateurs ▲ / ▼ du visualiseur ; Échap ou Entrée pour fermer.
6. Rendu de l’image du lieu étirée (déformation d’environ 2 %).

## 11. Pistes pour la suite (hors périmètre, à titre de mémoire)
Déclenchement du visualiseur depuis le moteur (lettre des parents, crédits), vrai texte d’aide, molette de souris, dialogues plus riches, variantes d’image selon l’état d’un objet.