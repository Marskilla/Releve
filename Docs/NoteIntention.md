# Jeu "Relève" - Note d’intention — Jeu d’aventure textuel et graphique

> Version 6 — amende la version 5 : règle d’interaction à la souris (§2) et entrées du menu (§10).

## 1. Concept général

Le projet est un jeu d’aventure en vue fixe, reposant sur une représentation graphique des lieux et sur des interactions principalement réalisées au moyen de commandes textuelles.

Le joueur évolue dans un monde constitué de lieux distincts, organisés spatialement. Chaque lieu correspond à une position identifiable dans le monde du jeu et possède sa propre représentation graphique, ses objets, ses personnages et ses possibilités d’interaction.

Le gameplay repose sur l’exploration, l’observation, la collecte et l’utilisation d’objets, les déplacements et la résolution de situations par l’intermédiaire d’un analyseur de commandes textuelles.

L’objectif est de donner au joueur une représentation suffisamment précise de son environnement pour qu’il puisse déterminer lui-même quelles actions sont pertinentes, sans transformer le jeu en une succession d’actions prédéterminées accessibles uniquement par menus.

## 2. Interface graphique

L’écran de jeu est organisé en deux grandes zones horizontales.

La partie supérieure occupe environ les deux tiers de la hauteur de l’écran. Elle est elle-même divisée verticalement.

La partie gauche, représentant environ les trois quarts de la largeur de cette zone, est consacrée à la **représentation graphique du lieu courant**. Cette zone constitue le principal moyen de visualisation de l’environnement. Le lieu est représenté par une illustration fixe montrant les éléments pertinents du décor, les objets présents et, le cas échéant, les personnages ou autres éléments interactifs.

La partie droite, plus étroite, représente environ un quart de la largeur de la zone supérieure. Elle est divisée horizontalement en deux zones.

La zone supérieure de cette colonne est consacrée aux **déplacements**. Sa hauteur est réduite au strict nécessaire afin de maximiser l’espace disponible pour l’inventaire. Elle présente les directions ou accès existant depuis la position du joueur. Les directions sont représentées graphiquement, par exemple au moyen de flèches correspondant aux directions cardinales.

La zone de déplacement est dynamique. Elle ne représente pas simplement les connexions théoriques de la carte : elle reflète l’état courant du monde. Chaque direction possède l’un des états suivants :

- **inexistante** : aucune connexion n’existe dans cette direction ; rien n’est affiché ;
- **existante mais actuellement impossible** : une connexion existe, mais elle est temporairement inaccessible dans l’état courant du monde (porte fermée, verrou, obstacle, personnage bloquant un passage, événement non déclenché). La direction est affichée sous forme de flèche grisée ;
- **existante et possible** : la connexion est accessible. La direction est affichée sous forme de flèche active.

Lorsque le joueur modifie l’état du monde, l’état d’affichage de la direction concernée change en conséquence. Une flèche « Est » grisée à cause d’une porte fermée devient active lorsque le joueur ouvre cette porte. À l’inverse, une action peut rendre impossible un déplacement précédemment disponible, dont la flèche devient alors grisée.

La flèche grisée informe le joueur qu’un accès existe sans lui dire comment le débloquer. Elle ne s’applique pas aux accès volontairement dissimulés (passage secret, par exemple) : tant que le joueur ne les a pas découverts, ils sont traités comme inexistants et rien n’est affiché.

La zone inférieure de cette colonne est consacrée à **l’inventaire**. Elle occupe la majeure partie de la colonne afin de permettre l’affichage d’un nombre important d’objets. Chaque objet peut être représenté par une icône et un libellé. Lorsque le nombre d’objets dépasse l’espace disponible, l’inventaire dispose de contrôles de défilement vertical permettant de parcourir la liste.

La partie inférieure de l’écran, sur toute sa largeur, constitue la **console d’interaction textuelle**. Sa hauteur est volontairement limitée afin de préserver l’espace consacré à la représentation graphique et à l’inventaire, tout en permettant l’affichage de plusieurs échanges récents.

Cette zone est à la fois le moyen par lequel le jeu communique avec le joueur et celui par lequel le joueur lui transmet ses commandes. Elle fonctionne comme un flux conversationnel et conserve un historique des échanges.

Lors de l’entrée dans un lieu, le moteur affiche une courte description textuelle destinée à compléter la représentation graphique. Cette description ne cherche pas à reproduire ce qui est déjà clairement visible à l’écran, mais à fournir les informations qui ne peuvent pas être représentées visuellement ou qui nécessitent une précision supplémentaire.

Le moteur affiche ensuite une invite de saisie. La commande saisie est ajoutée à l’historique, puis interprétée par l’analyseur syntaxique. La réponse du moteur est ajoutée à son tour à l’historique, puis une nouvelle invite est affichée.

Le fonctionnement général de la console est donc de la forme :

```text
┌──────────────────────────────────────┬────────────┐
│                                      │Déplacements│
│                                      ├────────────┤
│                                      │            │
│          Représentation              │ Inventaire │
│             du lieu                  │            │
│                                      │    ↕       │
├──────────────────────────────────────┴────────────┤
│ Console d’interaction                       ↕     │
│ Description du lieu                               │
│ > commande du joueur                              │
│ Réponse du jeu                                    │
│ > commande du joueur                              │
│ Réponse du jeu                                    │
│ > _                                               │
└───────────────────────────────────────────────────┘
```

La zone de texte dispose de contrôles de défilement vertical permettant de parcourir l’historique lorsque celui-ci dépasse l’espace visible. L’historique constitue une partie permanente du contexte de jeu et doit pouvoir être relu par le joueur.

La représentation graphique, la navigation, l’inventaire et la console sont donc complémentaires :

- la représentation graphique montre **où se trouve le joueur et ce qu’il peut observer** ;
- la zone de déplacement montre **quelles sorties existent et lesquelles sont effectivement praticables à cet instant** ;
- l’inventaire montre **ce que le joueur possède** ;
- la console indique **ce que le jeu lui communique, ce qu’il demande et comment le monde réagit à ses actions**.

**Règle d’interaction.** Le jeu reste intégralement jouable au clavier : toutes les interactions de jeu, y compris les déplacements, passent par la saisie de commandes textuelles (par exemple « Est » ou « Va vers l’est »). La souris n’est admise que pour quelques exceptions explicites, destinées à faciliter les premiers instants d’un joueur habitué à la souris, et chacune d’elles possède un équivalent clavier :

1. les deux boutons de défilement de l’historique de la console (équivalent : Page↑ / Page↓) ;
2. le bouton « Fermer » du visualiseur de texte (équivalent : Échap ou Entrée) ;
3. les entrées du menu d’accueil (Continuer, Nouvelle partie, Aide) : le survol d’une entrée la sélectionne et un clic gauche la valide (équivalent : flèches Haut / Bas et Entrée) ;
4. les indicateurs de défilement du visualiseur de texte : un clic fait défiler le texte de trois lignes (équivalent : Page↑ / Page↓, flèches Haut / Bas, Début / Fin).

Le curseur de la souris est visible. Aucune interaction de jeu proprement dite (déplacements, objets, inventaire, personnages) ne se fait à la souris. Les flèches de la zone de déplacement sont un affichage d’information sur l’état des directions, pas des éléments de commande.

Une tentative de déplacement dans une direction existante mais impossible n’est pas exécutée et produit une réponse textuelle qui guide le joueur en décrivant ce qui bloque le passage, sans lui dire comment le débloquer. Par exemple :

```text
> Est
La porte est solidement fermée et le lecteur de badge affiche un rouge fixe.
```

### Mockup de référence

Le mockup bitmap sert de référence visuelle pour l’organisation générale, les proportions relatives des différentes zones et le principe de fonctionnement de l’interface.

[Mockup bitmap de l’interface](MockupReference.png)

Les proportions générales retenues sont approximativement :

- **zone supérieure : ~2/3 de la hauteur de l’écran** ;
- **vue du lieu : ~3/4 de la largeur de la zone supérieure** ;
- **colonne latérale : ~1/4 de la largeur de la zone supérieure** ;
- **déplacements : faible hauteur, limitée au strict nécessaire** ;
- **inventaire : majeure partie de la hauteur de la colonne latérale** ;
- **console textuelle : ~1/3 de la hauteur de l’écran**, avec défilement vertical de l’historique.

Ces proportions constituent une base de conception et pourront être adaptées en fonction de la résolution cible et de la lisibilité effective de l’interface.

## 3. Représentation du monde

Le monde est constitué d’un ensemble de cases ou de lieux interconnectés.

Chaque case possède des coordonnées ou un identifiant permettant de déterminer sa position dans le monde. Les déplacements du joueur consistent à passer d’une case à une autre selon les connexions disponibles.

Une case peut être associée à plusieurs directions ou sorties : nord, sud, est, ouest, ainsi qu’éventuellement à des accès particuliers tels qu’une porte, un escalier, un passage secret ou un déplacement conditionnel.

La représentation graphique affichée à l’écran correspond à la case actuellement occupée par le joueur.

La localisation est une composante réelle du gameplay et pas uniquement un changement d’écran. Le changement de case peut modifier les objets visibles, les personnages présents, les possibilités d’interaction et les déplacements disponibles.

Une distinction doit être faite entre **sortie existante** et **sortie accessible**. Une connexion peut exister dans la définition du monde tout en étant temporairement inaccessible à cause de l’état d’un élément : porte fermée, verrou, obstacle, personnage bloquant un passage, événement non déclenché, etc. Cette distinction est directement visible du joueur dans l’interface : une sortie existante mais inaccessible est affichée grisée, une sortie accessible est affichée active. Seules les connexions volontairement dissimulées (passage secret) échappent à cette règle tant qu’elles n’ont pas été découvertes.

## 4. Analyseur de commandes

Le joueur interagit avec le jeu au moyen de commandes textuelles.

Le système d’analyse syntaxique a pour fonction d’interpréter une commande et d’en extraire une intention exploitable par le moteur de jeu.

L’analyseur n’a pas vocation à comprendre parfaitement le langage naturel. Il repose sur un vocabulaire contrôlé et sur un ensemble de structures syntaxiques permettant de reconnaître différentes formulations correspondant à une même action.

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

Cette séparation entre la formulation saisie par le joueur et l’action réellement exécutée permet d’ajouter progressivement des synonymes et des variantes sans modifier la logique du jeu.

L’analyseur doit également être capable d’identifier les éléments du contexte courant. Une commande faisant référence à « la clé » ne doit pouvoir être exécutée que si une clé pertinente est accessible dans la situation actuelle ou dans l’inventaire.

## 5. Interactions

Les interactions sont définies par la combinaison d’une action, d’un ou plusieurs objets et du contexte dans lequel l’action est effectuée.

Les principales catégories d’actions peuvent comprendre : examiner un élément, prendre ou déposer un objet, utiliser un objet, combiner plusieurs objets, ouvrir ou fermer un élément, activer ou désactiver un mécanisme, parler à un personnage, interroger un personnage, se déplacer ou réaliser une action spécifique au contexte.

Le moteur vérifie les conditions nécessaires à l’exécution d’une action avant de modifier l’état du jeu.

Une action peut produire une réponse textuelle, modifier la représentation graphique, modifier l’inventaire, modifier l’état d’un objet ou d’un personnage, déclencher un événement ou modifier les possibilités d’interaction futures.

## 6. Objets et inventaire

Les objets constituent l’un des éléments centraux du gameplay.

Un objet peut être présent dans un lieu, détenu par le joueur, détenu par un personnage ou associé à un élément du décor.

L’inventaire permet au joueur de conserver les objets récupérés et de les réutiliser ultérieurement.

Les objets peuvent posséder différents états et propriétés. Ils peuvent notamment être utilisables, combinables, déplaçables ou associés à certaines actions.

L’utilisation d’un objet est contextualisée. Une même action peut avoir un résultat différent selon le lieu, la cible ou l’état actuel du monde.

L’inventaire constitue donc une représentation de l’état des objets possédés par le joueur et non une simple liste décorative.

## 7. Personnages

Les personnages présents dans le monde sont considérés comme des entités interactives au même titre que les objets.

Ils possèdent un état propre et peuvent être associés à différentes réponses ou comportements selon la progression du joueur.

Les interactions avec les personnages passent principalement par les commandes textuelles. Le joueur peut notamment leur parler, les interroger ou leur demander quelque chose.

Les dialogues et comportements peuvent dépendre de l’état global du jeu, des objets possédés par le joueur, des actions précédemment effectuées et de la localisation actuelle.

## 8. État du monde

Le monde du jeu est piloté par un état global permettant de conserver les conséquences des actions du joueur.

Cet état peut notamment contenir la position du joueur, l’état et la localisation des objets, l’inventaire, la position et l’état des personnages, l’état des accès et des mécanismes, les événements déjà déclenchés et les conditions de progression.

Les actions du joueur modifient cet état.

La progression ne doit donc pas être uniquement basée sur une succession de scènes ou d’écrans. Elle doit émerger de la modification progressive de l’état du monde et des nouvelles interactions rendues possibles par ces modifications.

## 9. Principe de gameplay

Le gameplay repose sur une boucle simple :

1. observer le lieu ;
2. identifier les éléments intéressants ;
3. formuler une hypothèse sur leur utilité ;
4. tenter une action au moyen d’une commande textuelle ;
5. observer la réponse du système ;
6. modifier sa stratégie en fonction du résultat ;
7. explorer un autre lieu ou réutiliser les informations et objets découverts.

Le joueur doit pouvoir expérimenter avec le système et obtenir une réponse cohérente à ses actions.

L’objectif n’est donc pas uniquement de trouver la commande attendue par le jeu, mais de permettre au joueur de comprendre progressivement les règles du monde et de les exploiter pour progresser. Les flèches de déplacement grisées y contribuent : elles signalent qu’une progression est possible sans en indiquer le moyen.

## 10. Éléments divers

Le jeu disposera au lancement d'un menu permettant :

- De charger une sauvegarde unique (sauvegarde automatique à la fermeture de la dernière partie), avec un libellé "Continuer" qui sera grisé si pas de nouvelle sauvegarde, 
- De commencer une nouvelle partie 
- D'afficher une aide textuelle pleine page et scrollable.

Les entrées du menu se sélectionnent au clavier ou à la souris (voir la règle d’interaction, §2).

Les seuls éléments sonores du jeu seront deux bandes son (fichiers mp3 ou ogg) tournant en boucle : une pour le menu/ aide, une pour le jeu.