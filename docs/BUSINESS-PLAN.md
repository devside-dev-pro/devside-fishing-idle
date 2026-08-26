# Devside Fishing Idle — Business plan (v1, ~80 %)

> Document de référence monétisation/UX/roadmap. Règle absolue : **rien de tout
> ceci n'entre dans le code tant que la boucle de jeu n'est pas prouvée fun** —
> mais tout ce qu'on code dès maintenant doit laisser la place à ces systèmes
> (d'où ce plan posé tôt). Complète GAME-DESIGN.md (le design du jeu) ;
> ici : comment le jeu vit économiquement.

## 1. Modèle : free-to-play, pubs récompensées d'abord, IAP ensuite

Référence marché (repérages Sensor Tower faits en session) : les idle/tycoon de
pêche vivent longtemps et monétisent surtout par **pubs récompensées opt-in +
petits IAP de confort**. Hooked Inc (~10 ans de vie) et Egg Inc restent les
modèles. À l'inverse, les clickers secs meurent vite. Notre positionnement :
**idle profond + navigation** (notre différenciateur), monétisation 100 %
non-punitive.

Trois principes non négociables :

1. **Jamais de pub forcée** (pas d'interstitiels). Toute pub est un choix du
   joueur contre une récompense. La rétention vaut plus que l'eCPM.
2. **Le temps s'achète, le contenu se joue.** Aucun IAP ne débloque une espèce,
   une île ou un système : ils s'obtiennent en jouant. On vend de la vitesse,
   du confort et du cosmétique.
3. **L'économie du jeu reste dans le Core pur et testée** : chaque nouveau
   levier (perles, équipements) entre comme fonctions pures + bornes dans
   PacingTests, comme le reste.

## 2. Les deux monnaies

| | Pièces (existant) | **Perles** (nouveau, premium) |
|---|---|---|
| Rôle | Économie de production | Monnaie rare trans-prestige |
| Se gagne | Vente du poisson | Découvertes Poissodex, quêtes du jour, succès, coffres flottants, pubs récompensées |
| S'achète | Non | IAP |
| Se dépense | Pêcheurs, ateliers, améliorations bateau | Boosts temporaires, coffres d'équipement, cosmétiques, finitions instantanées |
| Prestige | Remise à zéro | **Survit** (comme le Poissodex) |

Le joueur gratuit gagne des perles en flux lent mais réel (~15-25/jour actif) :
il touche à tout, plus lentement. Thème marin assumé : des perles, pas des gemmes.

## 3. Placements de pubs récompensées (par rentabilité attendue)

1. **Doubler les gains hors-ligne** au retour dans le jeu (le placement n° 1 de
   tout idle — Egg Inc). S'affiche avec le résumé « Pendant votre absence ».
2. **Coup de filet** : ×2 revenus pendant 4 h (relançable, se cumule à 8 h max).
3. **Coffre flottant** : épave/bouteille visible sur l'océan pendant la
   navigation → pub pour l'ouvrir (équipement ou perles). Lie la monétisation à
   notre force : l'open world. Apparition ~20-30 min de jeu.
4. **Vent dans les voiles** : +50 % vitesse de navigation 10 min (utile dès que
   les îles s'éloignent).
5. **La perle du jour** : petite dose quotidienne de perles au comptoir du
   marchand (fait revenir + fait entrer dans la boutique).

Cap global ~12 pubs/jour. SDK : Unity Ads / LevelPlay pour démarrer (le plus
simple depuis Unity), AppLovin MAX si les volumes le justifient plus tard.

## 4. Catalogue IAP (petit, lisible)

- **Pack du débutant** (2,99 €, une fois) : perles + 1 coffre rare + skin de
  canne. Affiché après le premier prestige, pas avant.
- **Perles** : 4,99 / 9,99 / 19,99 / 49,99 €.
- **Permis de pêche doré** (9,99 €, permanent) : +50 % revenus, vente auto
  active hors-ligne, et les boosts « pub » s'activent gratuitement (élégant :
  pas de « remove ads» — les pubs étant opt-in, le Permis les remplace).
- **Cosmétiques** : skins bateau/capitaine/canne (perles ou €). Zéro stat.

Objectif de mix à maturité : ~60-70 % pubs / 30-40 % IAP (classique du genre).

## 5. Profil & équipements (le système à coder — phase v0.5)

Nouvel onglet **Profil** : le capitaine, son niveau (XP par poissons pêchés,
découvertes, quêtes), et **4 slots d'équipement** :

| Slot | Exemples de bonus |
|---|---|
| Canne | Vitesse/valeur de pêche manuelle, chance de double prise |
| Leurre | Chance d'espèces rares, bonus par zone de profondeur |
| Tenue | Capacité de cale, gains hors-ligne |
| Amulette | Vitesse du bateau, prix de vente |

- Raretés commun → rare → épique → légendaire ; obtention par coffres (pêche,
  coffres flottants, perles) ; **fusion de doublons** pour monter en niveau
  (boucle de collection type Hooked Inc, s'ajoute au Poissodex).
- Côté Core : `Equipment.cs` pur (slots, raretés, bonus typés branchés dans
  Multipliers), sérialisé dans GameState, testé, **bornes PacingTests mises à
  jour** pour empêcher un stacking qui recasserait le rythme (leçon du premier
  playtest).

## 6. Refonte UX : 5 onglets (phase v0.4)

Barre du bas → 5 entrées, le centre reste l'action :

`[ Bateau ] [ Carte ] [ 🎣 PÊCHER ] [ Profil ] [ Boutique ]`

- **Bateau** : équipage + ateliers + améliorations (fusion des 2 panneaux
  actuels).
- **Carte** : l'archipel, îles découvertes, prochaine destination, frontières
  de zones (l'accostage/troc du marchand vit ici).
- **Pêcher** : le bouton central mis en avant ; deviendra la pêche active.
- **Profil** : capitaine, équipements, Poissodex, statistiques, prestige.
- **Boutique** : perles, boosts, coffres, cosmétiques, IAP, la perle du jour.
- Le dézoom caméra (fait) + la jauge de cale restent au-dessus de la scène 3D.

## 7. KPIs et jalons de décision

Avant tout euro d'acquisition : **1 000 installs organiques/soft launch, D1 >
30 %, D7 > 10 %, session moyenne > 6 min**. Si D1 < 25 % : on retravaille la
première heure (cadence de déblocage), pas le marketing. Analytics : Firebase
(gratuit) — events : première vente, premier pêcheur, premier prestige, entrée
par zone, pubs vues, temps de session.

## 8. Roadmap (chaque version = des PR petites et testées)

| Version | Contenu | Monétisation |
|---|---|---|
| v0.3 (fait) | Archipel navigable, assets custom, dézoom | — |
| v0.4 | 5 onglets, carte, accostage + troc marchand | — |
| v0.5 | Pêche active (viser/ferrer/mouliner), profil + équipements, coffres | Fondations (perles en jeu, aucune pub) |
| v0.6 | Quêtes du jour, succès, coffres flottants | Pubs récompensées (Unity Ads) |
| v0.7 | Notifications locales (« cale pleine »), analytics | IAP + Permis doré |
| v0.8 | Beta fermée Android (Play internal testing), équilibrage cohortes | Mix observé |
| v1.0 | Soft launch (1-2 pays), puis global | Optimisation placements |

## 9. Les 20 % restants (à trancher plus tard, avec des données)

- Prix exacts des IAP et taux de change perles (à calibrer en beta).
- Abonnement hebdo vs achat permanent pour le Permis doré.
- Événements datés (poisson de saison) et passes d'événement — après v1.0.
- iOS : après validation Android (coût Apple + ATT).
- Steam (l'ambition How to Fish) : version premium sans pubs ni IAP, même Core
  — décision après le succès mobile.
