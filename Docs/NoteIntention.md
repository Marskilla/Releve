# Jeu "Relève" - Note d'intention — Jeu d'aventure textuel et graphique

> Version 7
> Historique : V7 — alignement sur le POC V4 (raccourcis clavier, souris, plein écran, fenêtres modales, options, défilement d'image, sauvegarde) et ajout d'une section sur les évolutions prévues de la bêta 0.5 (§11, à préciser). Cette note fait référence pour l'interface et le comportement du jeu.

## 1. Concept général

Le projet est un jeu d'aventure en vue fixe, reposant sur une représentation graphique des lieux et sur des interactions principalement réalisées au moyen de commandes textuelles.

Le joueur évolue dans un monde constitué de lieux distincts, organisés spatialement. Chaque lieu correspond à une position identifiable dans le monde du jeu et possède sa propre représentation graphique, ses objets, ses personnages et ses possibilités d'interaction.

Le gameplay repose sur l'exploration, l'observation, la collecte et l'utilisation d'objets, les déplacements et la résolution de situations par l'intermédiaire d'un analyseur de commandes textuelles.

L'objectif est de donner au joueur une représentation suffisamment précise de son environnement pour qu'il puisse déterminer lui-même quelles actions sont pertinentes, sans transformer le jeu en une succession d'actions prédéterminées accessibles uniquement par menus.

## 2. Interface graphique

### 2.1 Organisation de l'écran

L'écran de jeu est organisé en deux grandes zones horizontales. L'interface a une taille logique fixe de 1280 x 720 pixels : en mode fenêtré, la fenêtre a cette taille ; en plein écran (§2.7), l'interface est mise à l'échelle sans déformation.

La partie supérieure occupe environ les deux tiers de la hauteur de l'écran. Elle est elle-même divisée verticalement.

La partie gauche, représentant environ les trois quarts de la largeur de cette zone, est consacrée à la **représentation graphique du lieu courant** (§2.2).

La partie droite, plus étroite, représente environ un quart de la largeur de la zone supérieure. Elle est divisée horizontalement en deux zones : les **déplacements** (§2.3), en haut, dont la hauteur est réduite au strict nécessaire afin de maximiser l'espace disponible pour l'**inventaire** (§2.4), en dessous.

La partie inférieure de l'écran, sur toute sa largeur, constitue la **console d'interaction textuelle** (§2.5). Sa hauteur est volontairement limitée afin de préserver l'espace consacré à la représentation graphique et à l'inventaire, tout en permettant l'affichage de plusieurs échanges récents.

```text
┌──────────────────────────────────────┬────────────┐
│                                      │Déplacements│
│                                      ├────────────┤
│                                      │            │
│          Représentation              │ Inventaire │
│             du lieu                  │            │
│                                      │    ↕       │
├──────────────────────────────────────┴────────────┤
│ Console d'interaction                       ↕     │
│ Description du lieu                               │
│ > commande du joueur                              │
│ Réponse du jeu                                    │
│ > commande du joueur                              │
│ Réponse du jeu                                    │
│ > _                                               │
└───────────────────────────────────────────────────┘
```

La représentation graphique, la navigation, l'inventaire et la console sont complémentaires :

- la représentation graphique montre **où se trouve le joueur et ce qu'il peut observer** ;
- la zone de déplacement montre **quelles sorties existent et lesquelles sont effectivement praticables à cet instant** ;
- l'inventaire montre **ce que le joueur possède** ;
- la console indique **ce que le jeu lui communique, ce qu'il demande et comment le monde réagit à ses actions**.

### Mockup de référence

Le mockup bitmap sert de référence visuelle pour l'organisation générale, les proportions relatives des différentes zones et le principe de fonctionnement de l'interface.

[Mockup bitmap de l'interface](MockupReference.png)

Les proportions générales retenues sont approximativement :

- **zone supérieure : ~2/3 de la hauteur de l'écran** ;
- **vue du lieu : ~3/4 de la largeur de la zone supérieure** ;
- **colonne latérale : ~1/4 de la largeur de la zone supérieure** ;
- **déplacements : faible hauteur, limitée au strict nécessaire** ;
- **inventaire : majeure partie de la hauteur de la colonne latérale** ;
- **console textuelle : ~1/3 de la hauteur de l'écran**, avec défilement vertical de l'historique.

Ces proportions constituent une base de conception et pourront être adaptées en fonction de la résolution cible et de la lisibilité effective de l'interface.

### 2.2 La vue du lieu

Le lieu est représenté par une illustration fixe montrant les éléments pertinents du décor, les objets présents et, le cas échéant, les personnages ou autres éléments interactifs. L'illustration est étirée pour remplir toute la zone de la vue.

- **Évolution selon l'état du monde** : certaines parties de l'illustration changent lorsque le joueur agit sur le monde (par exemple une armoire qui s'ouvre, un voyant de lecteur qui passe au vert). Une zone rectangulaire de l'image est alors remplacée par la même zone d'une image alternative du lieu. L'affichage se déduit de l'état du monde : si la condition cesse d'être vraie, l'image de base revient.
- **Défilement lors des déplacements** : lorsque le joueur change de lieu, l'image du lieu quitté et celle du lieu d'arrivée défilent dans le sens du déplacement (par exemple, en allant vers l'est, la nouvelle image entre par la droite). Le défilement dure moins d'une demi-seconde, assez pour être perçu sans ralentir le joueur. Il n'y a pas de défilement au chargement d'une partie.
- **Vues secondaires** : une commande peut afficher une image complémentaire dans une fenêtre dédiée (§2.8), par exemple l'aperçu d'une pièce dans laquelle le joueur ne peut pas se rendre.

### 2.3 Les déplacements

La zone de déplacement présente les directions ou accès existant depuis la position du joueur, sous forme de **croix** : Nord en haut, Ouest à gauche, Est à droite, Sud en bas. Chaque direction est représentée par une pointe de flèche accompagnée de son libellé. Les flèches sont des indicateurs, jamais des éléments de commande.

La zone de déplacement est dynamique. Elle ne représente pas simplement les connexions théoriques de la carte : elle reflète l'état courant du monde. Chaque direction possède l'un des états suivants :

- **inexistante** : aucune connexion n'existe dans cette direction ; rien n'est affiché ;
- **existante mais actuellement impossible** : une connexion existe, mais elle est temporairement inaccessible dans l'état courant du monde (porte fermée, verrou, obstacle, personnage bloquant un passage, événement non déclenché). La flèche est **creuse et sombre**, et son libellé est atténué ;
- **existante et possible** : la connexion est accessible. La flèche est **pleine**, de couleur vive.

La distinction ne repose pas que sur la couleur : elle repose aussi sur la forme (pleine ou creuse).

Lorsque le joueur modifie l'état du monde, l'état d'affichage de la direction concernée change en conséquence. Une flèche « Est » creuse à cause d'une porte fermée devient pleine lorsque le joueur ouvre cette porte. À l'inverse, une action peut rendre impossible un déplacement précédemment disponible, dont la flèche devient alors creuse.

La flèche creuse informe le joueur qu'un accès existe sans lui dire comment le débloquer. Elle ne s'applique pas aux accès volontairement dissimulés (passage secret, par exemple) : tant que le joueur ne les a pas découverts, ils sont traités comme inexistants et rien n'est affiché.

### 2.4 L'inventaire

L'inventaire occupe la majeure partie de la colonne latérale afin de permettre l'affichage d'un nombre important d'objets. Chaque objet est représenté par une icône et un libellé. Lorsque le nombre d'objets dépasse l'espace disponible, la liste défile verticalement (Ctrl + flèche haut / bas).

### 2.5 La console d'interaction textuelle

La console est à la fois le moyen par lequel le jeu communique avec le joueur et celui par lequel le joueur lui transmet ses commandes. Elle fonctionne comme un flux conversationnel et conserve un historique des échanges, qui doit pouvoir être relu par le joueur.

- Lors de l'entrée dans un lieu, le moteur affiche une courte description textuelle destinée à compléter la représentation graphique. Cette description ne cherche pas à reproduire ce qui est déjà clairement visible à l'écran, mais à fournir les informations qui ne peuvent pas être représentées visuellement ou qui nécessitent une précision supplémentaire. Une description plus détaillée est disponible à la demande du joueur (« regarde », « observe »).
- Le moteur affiche ensuite une invite de saisie. La commande saisie est ajoutée à l'historique, puis interprétée par l'analyseur syntaxique. La réponse du moteur est ajoutée à son tour à l'historique, puis une nouvelle invite est affichée.
- Les commandes du joueur et les réponses du jeu sont affichées dans **deux couleurs distinctes**.
- L'historique défile verticalement (Page haut / Page bas, et deux boutons cliquables) ; un nouveau message ramène toujours l'affichage en bas.
- Les commandes déjà envoyées peuvent être rappelées dans la ligne de saisie (Maj + flèche haut / bas) sans être validées.

Le fonctionnement général de la console est donc de la forme :

```text
> commande du joueur
Réponse du jeu
> commande du joueur
Réponse du jeu
> _
```

Une tentative de déplacement dans une direction existante mais impossible n'est pas exécutée et produit une réponse textuelle qui guide le joueur en décrivant ce qui bloque le passage, sans lui dire comment le débloquer. Par exemple :

```text
> Est
La porte est solidement fermée et le lecteur de badge affiche un rouge fixe.
```

### 2.6 Interaction clavier et souris

**Principe : tout est faisable au clavier.** La saisie de commandes textuelles est le moyen d'action principal, y compris pour les déplacements (par exemple « Est » ou « Va vers l'est »). Les flèches de la zone de déplacement sont un affichage d'information, pas des éléments de commande.

Raccourcis clavier :

| Touches | Action |
|---|---|
| Entrée | valider la commande saisie |
| Flèches haut / bas / gauche / droite (seules) | déplacement (nord / sud / ouest / est), **uniquement lorsque la ligne de saisie est vide** ; sinon, gauche / droite déplacent le curseur dans la saisie |
| Retour arrière / Suppr | effacer des caractères (avec répétition) |
| Début / Fin | aller au début / à la fin de la ligne de saisie |
| Maj + flèche haut / bas | rappeler la commande précédente / suivante |
| Ctrl + flèche haut / bas | faire défiler l'inventaire |
| Page haut / Page bas | faire défiler l'historique de la console (et les fenêtres de texte) |
| Alt + Entrée | basculer entre le mode fenêtré et le plein écran |
| Échap ou Entrée | fermer une fenêtre de texte ou d'image ; Échap valide l'annulation d'une fenêtre à choix |

**La souris est facultative.** Elle n'est admise que pour des exceptions explicites, toutes doublées d'un équivalent clavier :

1. les boutons de défilement de la console (équivalent : Page haut / Page bas) ;
2. les entrées du menu : le survol les sélectionne, un clic gauche les valide (équivalent : flèches haut / bas et Entrée) ;
3. les fenêtres modales : boutons, cases à cocher et indicateurs de défilement des fenêtres de texte (un clic fait défiler de trois lignes) (équivalent : touches décrites au §2.8).

Le curseur de la souris est visible.

### 2.7 Plein écran

Le jeu peut être basculé en plein écran par la combinaison Alt + Entrée, ou par l'option « Plein écran » du menu Options. Le plein écran conserve les proportions de l'interface : l'interface logique de 1280 x 720 est mise à l'échelle et centrée, avec des bandes noires si le rapport de l'écran est différent. Les images sont étirées en conséquence et la souris reste alignée sur l'interface.

### 2.8 Fenêtres modales

Des fenêtres s'affichent par-dessus l'écran courant. Elles sont **opaques et modales** : tant qu'une fenêtre est ouverte, le reste du jeu n'est pas accessible. La musique continue.

- **Visualiseur de texte** : affiche un texte défilant, avec retour à la ligne automatique. Plein écran pour l'aide du menu ; environ 60 % de la largeur et de la hauteur de l'écran en jeu. Il peut proposer un ou plusieurs boutons de choix (par exemple, la fiche patient de l'accueil propose « J'accepte de sortir » et « Je ne suis pas prêt à sortir »). Clavier : flèches gauche / droite ou Tab pour choisir un bouton, Entrée pour valider, Échap pour annuler ; flèches haut / bas, Page haut / Page bas, Début / Fin pour faire défiler.
- **Visualiseur d'image** : fenêtre carrée au centre de l'écran (60 % de la hauteur) affichant une image associée à une commande (par exemple, ce que l'on voit à travers un hublot), avec un bouton « Fermer » (ou Échap, ou Entrée).
- **Fenêtre d'options** : accessible depuis le menu (§10).

Ces fenêtres sont déclenchées par le moteur à la suite d'une commande du joueur ; le choix du joueur revient au moteur, qui peut modifier l'état du monde (par exemple, valider la sortie de l'hôpital).

## 3. Représentation du monde

Le monde est constitué d'un ensemble de cases ou de lieux interconnectés.

Chaque case possède des coordonnées ou un identifiant permettant de déterminer sa position dans le monde. Les déplacements du joueur consistent à passer d'une case à une autre selon les connexions disponibles.

Une case peut être associée à plusieurs directions ou sorties : nord, sud, est, ouest, ainsi qu'éventuellement à des accès particuliers tels qu'une porte, un escalier, un passage secret ou un déplacement conditionnel.

La représentation graphique affichée à l'écran correspond à la case actuellement occupée par le joueur.

La localisation est une composante réelle du gameplay et pas uniquement un changement d'écran. Le changement de case peut modifier les objets visibles, les personnages présents, les possibilités d'interaction et les déplacements disponibles.

Une distinction doit être faite entre **sortie existante** et **sortie accessible**. Une connexion peut exister dans la définition du monde tout en étant temporairement inaccessible à cause de l'état d'un élément : porte fermée, verrou, obstacle, personnage bloquant un passage, événement non déclenché, etc. Cette distinction est directement visible du joueur dans l'interface : une sortie existante mais inaccessible est affichée par une flèche creuse, une sortie accessible par une flèche pleine. Seules les connexions volontairement dissimulées (passage secret) échappent à cette règle tant qu'elles n'ont pas été découvertes.

## 4. Analyseur de commandes

Le joueur interagit avec le jeu au moyen de commandes textuelles.

Le système d'analyse syntaxique a pour fonction d'interpréter une commande et d'en extraire une intention exploitable par le moteur de jeu.

L'analyseur n'a pas vocation à comprendre parfaitement le langage naturel. Il repose sur un vocabulaire contrôlé et sur un ensemble de structures syntaxiques permettant de reconnaître différentes formulations correspondant à une même action. Les majuscules, les accents, les articles et la ponctuation ne comptent pas.

Par exemple, plusieurs formulations peuvent être interprétées comme une même intention :

```text
prendre la clé
ramasser la clé
prends la clé
```

Le moteur transforme ces formulations en une représentation interne normalisée, par exemple :

```text
TAKE -> KEY
```

Cette séparation entre la formulation saisie par le joueur et l'action réellement exécutée permet d'ajouter progressivement des synonymes et des variantes sans modifier la logique du jeu.

L'analyseur doit également être capable d'identifier les éléments du contexte courant. Une commande faisant référence à « la clé » ne doit pouvoir être exécutée que si une clé pertinente est accessible dans la situation actuelle ou dans l'inventaire. Lorsque deux objets présents portent un nom proche, le jeu demande au joueur de préciser lequel.

Particularités du vocabulaire :

- **Directions** : une direction seule (« est », « n ») est un déplacement ; une saisie plus longue qui commence par une direction n'en est pas un.
- **Mots de sortie** : certaines sorties peuvent être franchies sans donner de direction (« sors », « monte l'escalier », « va dans la rue »).
- **Désigner le lieu** : « regarde » ou « observe » seuls, ou accompagnés d'un mot désignant le lieu (« regarde autour de moi »), décrivent le lieu en détail.
- **Saisies incomprises** : le jeu répond par des messages variés, parfois humoristiques, sans jamais répéter deux fois de suite le même.
- **Grossièretés** : une grossièreté détectée n'importe où dans la saisie déclenche une réponse réprobatrice et amusante ; la commande n'est alors pas exécutée. Il n'y a pas de pénalité.

## 5. Interactions

Les interactions sont définies par la combinaison d'une action, d'un ou plusieurs objets et du contexte dans lequel l'action est effectuée.

Les principales catégories d'actions peuvent comprendre : examiner un élément, lire un élément, prendre ou déposer un objet, utiliser un objet, combiner plusieurs objets, ouvrir ou fermer un élément, activer ou désactiver un mécanisme, parler à un personnage, interroger un personnage, se déplacer ou réaliser une action spécifique au contexte.

Le moteur vérifie les conditions nécessaires à l'exécution d'une action avant de modifier l'état du jeu.

Une action peut produire une réponse textuelle, modifier la représentation graphique, modifier l'inventaire, modifier l'état d'un objet ou d'un personnage, déclencher un événement, ouvrir une fenêtre (§2.8) ou modifier les possibilités d'interaction futures.

## 6. Objets et inventaire

Les objets constituent l'un des éléments centraux du gameplay.

Un objet peut être présent dans un lieu, détenu par le joueur, détenu par un personnage ou associé à un élément du décor.

L'inventaire permet au joueur de conserver les objets récupérés et de les réutiliser ultérieurement.

Les objets peuvent posséder différents états et propriétés. Ils peuvent notamment être utilisables, combinables, déplaçables ou associés à certaines actions.

L'utilisation d'un objet est contextualisée. Une même action peut avoir un résultat différent selon le lieu, la cible ou l'état actuel du monde.

L'inventaire constitue donc une représentation de l'état des objets possédés par le joueur et non une simple liste décorative.

## 7. Personnages

Les personnages présents dans le monde sont considérés comme des entités interactives au même titre que les objets.

Ils possèdent un état propre et peuvent être associés à différentes réponses ou comportements selon la progression du joueur.

Les interactions avec les personnages passent principalement par les commandes textuelles. Le joueur peut notamment leur parler, les interroger ou leur demander quelque chose.

Les dialogues et comportements peuvent dépendre de l'état global du jeu, des objets possédés par le joueur, des actions précédemment effectuées et de la localisation actuelle.

## 8. État du monde

Le monde du jeu est piloté par un état global permettant de conserver les conséquences des actions du joueur.

Cet état peut notamment contenir la position du joueur, l'état et la localisation des objets, l'inventaire, la position et l'état des personnages, l'état des accès et des mécanismes, les événements déjà déclenchés et les conditions de progression.

Les actions du joueur modifient cet état.

La progression ne doit donc pas être uniquement basée sur une succession de scènes ou d'écrans. Elle doit émerger de la modification progressive de l'état du monde et des nouvelles interactions rendues possibles par ces modifications.

## 9. Principe de gameplay

Le gameplay repose sur une boucle simple :

1. observer le lieu ;
2. identifier les éléments intéressants ;
3. formuler une hypothèse sur leur utilité ;
4. tenter une action au moyen d'une commande textuelle ;
5. observer la réponse du système ;
6. modifier sa stratégie en fonction du résultat ;
7. explorer un autre lieu ou réutiliser les informations et objets découverts.

Le joueur doit pouvoir expérimenter avec le système et obtenir une réponse cohérente à ses actions.

L'objectif n'est donc pas uniquement de trouver la commande attendue par le jeu, mais de permettre au joueur de comprendre progressivement les règles du monde et de les exploiter pour progresser. Les flèches de déplacement creuses y contribuent : elles signalent qu'une progression est possible sans en indiquer le moyen.

**Pas d'échec forcé** : le joueur ne peut pas mourir et ne peut pas être contraint de recommencer une partie. Une commande qui échoue ne coûte rien.

## 10. Éléments divers

### Menu

Le jeu dispose au lancement d'un menu à quatre entrées, avec une image de fond, le titre du jeu en grand et la version du jeu affichée discrètement en bas à droite :

- **Continuer** : charge la sauvegarde existante ; grisé s'il n'y a pas de sauvegarde valide ;
- **Nouvelle partie** : fondu au noir, puis fondu d'ouverture sur l'interface de jeu ;
- **Options** : ouvre une fenêtre modale avec les réglages suivants, mémorisés d'un lancement à l'autre :
  - une case à cocher **Musique** (activer / désactiver) ;
  - une case à cocher **Plein écran** (activer / désactiver) ;
  - un bouton **Fermer** pour revenir au menu ;
- **Aide** : affiche une aide textuelle pleine page et défilable.

Le menu et la fenêtre d'options se pilotent au clavier (flèches, Tab, Espace, Entrée, Échap) comme à la souris.

### Sauvegarde

La sauvegarde est unique et automatique : elle est écrite après toute action qui modifie l'état du monde (déplacement compris) et à la fermeture du jeu. Elle ne contient pas l'historique de la console ni les réglages, qui sont mémorisés séparément dans un fichier de réglages.

### Sons

Les seuls éléments sonores du jeu sont deux bandes son (fichiers mp3 ou ogg) tournant en boucle : une pour le menu et l'aide, une pour le jeu. La musique peut être désactivée dans les options.

### Distribution

Le jeu est publié sous la forme d'un exécutable autonome pour Windows, sans fenêtre de console, avec une icône, et pourra être proposé via un installeur.

## 11. Évolutions prévues (bêta 0.5 — à préciser)

Cette section recense les évolutions annoncées pour la bêta 0.5. Leur détail reste à définir avant implémentation.

- **Quitter** : plusieurs commandes de sortie (« quitter », « fin », « stop », etc. et leurs équivalents anglais), reconnues **seulement si elles sont seules** dans la saisie. Quitter en cours de partie ramène au menu principal, qui reçoit une entrée **Quitter** en dernière position, après « Aide » ; cette entrée ferme l'application.
- **Volume** : dans les options, entre « Musique » et « Plein écran », un réglage du volume de 0 à 100 %, utilisable au clavier et à la souris, mémorisé avec les autres réglages (100 % par défaut).
- **Langue** : après « Plein écran », une option de langue du jeu limitée à l'anglais et au français, ce qui implique une internationalisation complète (textes, vocabulaire, aide, et vérification de la saisie au clavier AZERTY / QWERTY).
- **Gestion des erreurs** : en version publiée uniquement, un plantage écrit un fichier « lasterror.log » (date, heure, message, pile d'appels, etc.) dans le dossier des sauvegardes et des réglages.
- **Flèches de déplacement** : alignement des symboles des flèches Nord et Sud entre eux (libellé à gauche du symbole) et réduction de l'espacement entre les flèches Est et Ouest.
- **Contenu** : ajout d'une ou deux pièces et ajustements du périmètre existant, après une reprise du lore.