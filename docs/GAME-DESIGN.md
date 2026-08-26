# Devside Fishing Idle — Game Design

> Document de référence. Toute mécanique implémentée doit se rattacher à une section ici ;
> toute décision de design prise en cours de route se consigne ici.

## Pitch

Un idle/incrémental mobile à thème pêche, au ton léger/absurde (référence d'ambiance :
How to Fish). On commence seul avec une canne pourrie ; on finit à la tête d'un empire de
la pêche industrielle qu'on ne fait plus qu'optimiser.

## Inspirations assumées

Principe : prendre ce qui a fait ses preuves dans les meilleurs du genre et le passer à
notre sauce. Mapping de ce qu'on emprunte :

| Source | Mécanique éprouvée | Chez nous |
|---|---|---|
| Cookie Clicker | Courbes de coût ×1.15, prestige, le clic qui devient marginal | Économie de base, prestige en √ |
| Egg Inc | **Silos = plafond hors-ligne choisi par le joueur** | La **cale** (`cargo_hold`) |
| Hooked Inc | Thème pêche mobile validé, équipage, bateau qui grossit | Pêcheurs, améliorations de bateau |
| Melvor Idle / Pokémon | Collection d'espèces à bonus permanents | Le **Poissodex** |
| Vampire Survivors | Runs courts à choix de reliques qui se combinent | Couche roguelike (phase 4/5) |
| How to Fish | Ton absurde, pêche « physique », armes improbables | Direction d'ambiance (plus tard) |
| Fishing Frenzy: Idle Hooked Inc | **Caméra quasi top-down portrait**, lignes à l'eau, tapis de transformation sur le pont, bassins, équipement d'équipage, cartes de collection | Cadrage caméra, chaîne visible sur le pont, futur système d'équipement |

Backlog d'emprunts à évaluer plus tard (non implémentés) : missions/quêtes journalières
(Egg Inc), boosts temporaires activables, événements datés, statistiques de fierté
(« poissons pêchés au total »).

## Direction visuelle : le diorama isométrique

Référence validée (type *Idle Business Empire Tycoon*) : **la scène 3D est l'écran
principal et matérialise la progression** — pas un jeu de menus. Chez nous : le bateau vu
en 3/4 isométrique sur l'eau, et l'état du jeu incarné physiquement :

| État de la simulation | Dans le diorama |
|---|---|
| Pêcheurs achetés (par tier) | Personnages qui pêchent sur le pont (plafond visuel par tier) |
| Ateliers de découpe/filetage | Postes physiques animés sur le pont |
| Remplissage de la cale | Caisses qui s'empilent à l'arrière |
| Extensions de cale | Le bateau grossit |
| Paliers de profondeur | L'eau fonce |
| Pêche manuelle | Tap sur l'eau → poisson qui jaillit + chiffre qui vole |

L'UI devient une surcouche : bandeau de stats en haut, barre à **5 onglets** en bas
(Bateau / Carte / Pêcher / Profil / Boutique — voir BUSINESS-PLAN.md) ouvrant des
panneaux, bandeau PRESTIGE quand disponible. La Carte montre l'archipel, les îles
verrouillées par la coque et le bateau en direct ; le Profil porte le Poissodex et
les statistiques (les équipements arrivent en v0.5) ; la Boutique est un teaser
tant que la monétisation n'est pas branchée.

Art v1 : packs low-poly **Quaternius** (bateaux pirates, personnages nommés, props,
poissons) et **Kenney** (props d'appoint), tous CC0 — voir CREDITS.md. `BoatView` charge
les modèles via `ArtLibrary` (Resources), convertit les matériaux vers URP, mesure et
normalise les échelles, et résout la hauteur du pont par raycast : changer de pack ne
touche ni à la logique ni au mapping ci-dessus. Chaque modèle garde un fallback primitive.
L'eau est un shader stylisé maison (`Devside/StylizedWater`) : dégradé de profondeur,
vaguelettes, écume autour de la coque. Étape suivante (plus tard) : animations
squelettiques des personnages (les modèles sont riggés) et upgrade éventuel vers Synty.

## L'archipel navigable (semi-open world)

Référence : *How to Fish* pour la sensation « je pars en mer », vue du dessus type
*Project Zomboid*. Ce n'est pas un vrai open world — c'est un **archipel** : quelques
îles posées sur un océan, et tout le reste est de l'eau à pêcher. La sensation de
liberté vient du déplacement, pas de la taille du monde.

- **Joystick virtuel** (bas-gauche) : le bateau se déplace librement sur l'océan,
  caméra top-down qui suit, l'eau défile (bruit du shader en coordonnées monde).
- **La profondeur est de la géographie** : le monde est découpé en **anneaux de zones**
  concentriques autour du point de départ (rayons 35/85/145, au-delà = zone 3).
  `state.currentZone` est écrit par la couche hôte d'après la position ; le Core ne fait
  que le lire (`Catching.DepthLevel`). Les espèces profondes se pêchent **là-bas**, pas
  via un menu.
- **La coque est un permis de naviguer** : `boat_hull` ne donne plus les espèces
  directement — `Catching.MaxNavigableZone` borne le rayon navigable. Franchir la
  frontière sans coque → on bute dessus, message « ta coque ne supporte pas ces eaux ».
  Acheter la coque = partir explorer (même économie, sensation de voyage).
- **On commence sur une barque, à côté d'une île toute petite** avec un ponton, une
  cabane et un marchand (décor en v1 ; le troc arrive en phase « carte & commerce »).
  Chaque zone a son île (silhouettes différentes), posée dans son anneau.
- **Le comptoir du marchand** (v0.4, durci au playtest) : la vente manuelle
  N'EXISTE qu'à quai chez le marchand — vendre depuis la mer enlevait tout
  l'intérêt de l'île. En mer, la cale se remplit ; il faut rentrer encaisser
  (bonus `merchantSellBonus`, +25 % en v1, injecté par la couche hôte dans
  `Economy.Sell` — le Core ne sait pas où est le bateau). La vente automatique
  (améliorations tardives) reste le confort de fin de partie.
- **Pilotage posé-glissé** (retour playtest) : plus de joystick visible — un doigt
  posé sur la scène et glissé dans n'importe quelle direction pilote le bateau
  (zone morte 40 px, pleine vitesse à ~190 px) ; un tap bref reste un lancer de
  ligne. Le joystick gênait les menus et l'écran.
- **Paliers de navire** : on COMMENCE sur une petite barque ; navire moyen au
  niveau 3 d'extension de cale, grand navire au niveau 8 — l'évolution se fait
  petit à petit. Modèles custom (`Art/Custom/Ships/`) prioritaires.
- Le bot de `PacingTests` simule la couche hôte : il navigue toujours aussi profond que
  sa coque l'autorise — les bornes de rythme couvrent donc aussi la géographie. Il ne
  visite pas le comptoir (bonus non modélisé : borne prudente, le vrai joueur paie le
  bonus en temps de trajet).

Étapes suivantes (annoncées) : **carte** qui révèle la prochaine île et accostage /
troc chez le marchand, puis **pêche active** (viser, lancer, ferrer, mouliner — la
pêche manuelle devient un vrai geste pendant que l'équipage continue en idle).

## Le principe directeur : le métier du joueur change

Le piège mortel d'un idle est la répétition. La parade n'est pas « plus de contenu », c'est
le **changement de verbe** : à chaque phase, ce que fait concrètement le joueur change, et
l'action manuelle de la phase précédente devient un poste automatisé qu'il gère.

| Phase | Verbe principal du joueur | Ce qui vient d'être automatisé | Nouveau système introduit |
|---|---|---|---|
| 1. Le pêcheur (0–15 min) | Tapoter pour pêcher, vendre | — | Canne, premières améliorations |
| 2. Le patron (15 min–2 h) | Acheter des pêcheurs, arbitrer | La pêche elle-même | Équipage, découpe manuelle du poisson (nouveau geste !) |
| 3. La criée (2–10 h) | Optimiser la chaîne de transformation | La découpe (ateliers) | Filets, prix de vente, vente auto |
| 4. L'armateur (10 h+) | Gérer le rendement global, agrandir | La vente | Bateau(x), zones de pêche, équipement d'équipage |
| 5. La dynastie (prestige) | Recommencer plus vite, collectionner | Tout | Arbre de prestige, collections |

Règle de design : **chaque système qui s'automatise doit avoir été un geste manuel d'abord**
(on apprécie l'automatisation parce qu'on a connu la corvée), et **chaque phase introduit un
geste manuel neuf** (le clic ne meurt jamais, il se déplace).

## Les quatre boucles imbriquées

1. **Boucle seconde (active)** : lancer → attraper → vendre. Tout le « juice » vit ici :
   chiffres qui volent, sons, poissons rares qui font sursauter.
2. **Boucle minute (progression)** : argent → canne/leurres/équipage → zones plus riches →
   plus d'argent. Coûts géométriques (~×1,15 par palier), revenus par étages.
3. **Boucle heure (automatisation)** : pêcheurs, ateliers de découpe, vente automatique,
   gains hors-ligne plafonnés. C'est ce qui fait rouvrir le jeu 10 fois par jour.
4. **Boucle prestige (semaines)** : reset volontaire contre des points permanents
   (formule en racine carrée de la richesse cumulée). C'est la réponse principale au
   « jeu fini en 20 h » : le contenu ne s'épuise pas, il se re-parcourt plus vite avec
   de nouveaux embranchements.

## Le problème des 20 heures : les leviers de longévité

Un incrémental ne se prolonge pas en ajoutant des heures de contenu artisanal, mais en
superposant des systèmes qui se relancent l'un l'autre :

- **Prestige à embranchements** : les points de prestige s'investissent dans un petit arbre
  (pêche, transformation, commerce) → chaque run se joue différemment.
- **Collections (« Poissodex »)** : chaque espèce capturée se consigne ; les rares/légendaires
  donnent des bonus permanents. Levier de rétention le moins cher à produire : une espèce
  = une ligne de données + un sprite. **Implémenté v1** (13 espèces sur 4 paliers de
  profondeur, bonus permanents, survit au prestige). Décision de design : la découverte
  d'espèces ne passe que par la pêche manuelle — le clic garde ainsi un rôle à vie, même
  quand toute la production est automatisée.
- **Zones/profondeurs** : débloquées par palier, chaque zone remélange espèces, prix et
  dangers. Même moteur, sensation de nouveauté.
- **RNG à ratio variable** : poissons rares, coffres, quêtes d'un PNJ — la machine à sous
  psychologique d'un idle. À doser avec parcimonie et sans payer pour relancer.
- **Événements datés** (plus tard) : poisson de saison, multiplicateurs de week-end.
- **Cadence de déblocage** : toujours 2 à 3 objectifs visibles à l'écran (« prochain
  déblocage à X »). Première heure : un déblocage toutes les 2 à 5 minutes, sans exception.

Objectif chiffré v1 : ~30 h pour « voir » tout le contenu d'un run, prestige rentable dès
la 6–8ᵉ heure, et un joueur qui prestige 3 fois avant d'avoir tout vu.

## Économie (implémentée dans `Core`)

- Ressources : `Money`, `RawFish` (poisson brut), `CutFish` (découpé), `Fillet` (filet).
  Chaîne de valeur : brut 1 → découpé 4 → filet 12 (les ratios de transformation font que
  transformer doit toujours battre vendre brut, sans rendre le brut inutile).
- Producteurs primaires (pêcheurs) et postes de transformation (mêmes maths, avec une
  ressource d'entrée) — voir `BalanceConfig.Default()` pour la table v1.
- Espèces & profondeur : 13 espèces sur 4 paliers, réservées par l'amélioration `boat_hull`
  (coût ×8 par palier → les espèces profondes sont mid/late game). Tirage pondéré
  déterministe (le roll est injecté par l'hôte), valeur de la prise multipliée par
  l'espèce, bonus permanent de production à chaque découverte.
- Coûts : `coût(n) = base × croissance^n`, achats groupés par somme géométrique.
- Hors-ligne — **la cale** : pas de vente auto en mer, le poisson s'accumule dans la cale
  et c'est sa capacité (`cargo_hold`, ×2 par niveau) qui plafonne le gain hors-ligne ;
  cale pleine = production stoppée. Deux styles de jeu assumés : le joueur idle investit
  dans la cale et revient la vider (future notification « votre cale est pleine ») ; le
  joueur actif l'ignore, car en ligne la vente auto vide le stock au fil de l'eau et la
  cale ne bride jamais le flux. Subtilité voulue : les ateliers compressent le stock
  (2 découpés → 1 filet), donc transformer augmente la capacité hors-ligne effective.
  `offlineCapSeconds` (72 h) n'est qu'un garde-fou de calcul, pas un levier de gameplay.
- Prestige : `points = ⌊√(richesse cumulée / 25 M)⌋`, +4 % de production par point (v2).

**Équilibrage v2** (leçon du premier playtest : tout le contenu consommé en minutes) :
- La canne progresse moins vite que son coût (×1,5 de pêche pour ×2,2 de prix) — le clic
  reste utile mais ne peut plus porter l'économie seul.
- Valeurs d'espèces compressées (×1 à ×60, au lieu de ×1 à ×5000) : la rareté fait le
  frisson et le Poissodex, pas des jackpots qui écrasent l'économie.
- Gros achats espacés : vente auto 15 k, profondeur 50 k → 600 k → 7,2 M (palier 1 en
  première grosse session, palier 3 en plusieurs jours), prestige à 25 M cumulés.

L'équilibrage se règle **uniquement** dans `BalanceConfig` et se vérifie avec le bot de
`PacingTests`, qui protège le rythme **dans les deux sens** : bornes basses (pas trop
lent : premier pêcheur < 2 min, progression réelle en 1 h) et bornes hautes (pas de
speedrun : pas de vente auto en 15 min, canne loin du max à 1 h, Poissodex incomplet à
2 h, pas de prestige la première heure) — jamais « au doigt mouillé » en jouant.

## Hors périmètre v1 (décisions à prendre plus tard)

- Monétisation : le plan complet (deux monnaies, pubs récompensées, IAP, profil
  et équipements, refonte 5 onglets, roadmap) est posé dans **BUSINESS-PLAN.md**
  — rien n'entre dans le code tant que la boucle n'est pas fun.
- Direction artistique (low-poly 3D vs 2D) — le DevHud suffit tant que l'économie n'est pas
  validée.
- Couche roguelike « sorties en mer » (runs courts à choix de leurres/reliques) — prévue
  comme système de phase 4/5, à prototyper une fois la base addictive.
- Notifications locales mobiles (« votre cale est pleine ») — le champ `holdFull`
  d'`OfflineResult` est prêt pour ça.
- Localisation : le Core ne contient aucun texte, donc la question est purement UI.
