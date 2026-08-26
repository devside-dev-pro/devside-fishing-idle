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

L'UI devient une surcouche : bandeau de stats en haut, barre d'onglets en bas
(Équipage / Pêcher / Améliorer) ouvrant des panneaux, bandeau PRESTIGE quand disponible.

Art v1 : packs low-poly **Quaternius** (bateaux pirates, personnages nommés, props,
poissons) et **Kenney** (props d'appoint), tous CC0 — voir CREDITS.md. `BoatView` charge
les modèles via `ArtLibrary` (Resources), convertit les matériaux vers URP, mesure et
normalise les échelles, et résout la hauteur du pont par raycast : changer de pack ne
touche ni à la logique ni au mapping ci-dessus. Chaque modèle garde un fallback primitive.
L'eau est un shader stylisé maison (`Devside/StylizedWater`) : dégradé de profondeur,
vaguelettes, écume autour de la coque. Étape suivante (plus tard) : animations
squelettiques des personnages (les modèles sont riggés) et upgrade éventuel vers Synty.

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
- Prestige : `points = ⌊√(richesse cumulée / 1 M)⌋`, +2 % de production par point (v1).

L'équilibrage se règle **uniquement** dans `BalanceConfig` et se vérifie avec le bot de
`PacingTests` (première heure simulée) — jamais « au doigt mouillé » en jouant.

## Hors périmètre v1 (décisions à prendre plus tard)

- Monétisation (pub récompensée vs IAP) — rien dans le code tant que la boucle n'est pas fun.
- Direction artistique (low-poly 3D vs 2D) — le DevHud suffit tant que l'économie n'est pas
  validée.
- Couche roguelike « sorties en mer » (runs courts à choix de leurres/reliques) — prévue
  comme système de phase 4/5, à prototyper une fois la base addictive.
- Notifications locales mobiles (« votre cale est pleine ») — le champ `holdFull`
  d'`OfflineResult` est prêt pour ça.
- Localisation : le Core ne contient aucun texte, donc la question est purement UI.
