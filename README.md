# Devside Fishing Idle

Jeu mobile idle/incrémental à thème pêche, développé sous **Unity 6 (C#)**.
On commence en pêchant soi-même à la ligne ; on finit à la tête d'un empire de la pêche
qu'on ne fait plus qu'optimiser. Le design complet est dans
[docs/GAME-DESIGN.md](docs/GAME-DESIGN.md).

## Prérequis

- **Unity Hub** + une version **Unity 6 LTS** (série `6000.x`) — licence Personal gratuite
- **git-lfs** : `git lfs install` (une fois par machine, avant d'ajouter le premier asset binaire)
- IDE : JetBrains Rider, ou VS Code + extension C# Dev Kit

## Premier lancement

1. Cloner le repo, puis Unity Hub → **Add project from disk** → sélectionner le dossier.
2. Ouvrir avec la version Unity 6 installée. Si elle diffère de
   `ProjectSettings/ProjectVersion.txt`, accepter la mise à niveau proposée.
3. Au premier lancement, Unity génère `Library/` (ignoré par git) ainsi que les
   `ProjectSettings/*.asset` et les fichiers `.meta` manquants : **committer ces fichiers
   générés** (les `.meta` portent les références entre assets, ils font partie du projet).
4. Vérifier dans Edit → Project Settings → Editor que *Asset Serialization* est sur
   **Force Text** (c'est le défaut).
5. Créer la scène de jeu : File → New Scene, l'enregistrer dans `Assets/Scenes/Main.unity`,
   créer un GameObject vide nommé `Game`, lui ajouter les composants **GameBootstrap** et
   **GameUi**, puis appuyer sur Play. Tout se construit tout seul au démarrage : le
   diorama 3D du bateau (`BoatView`, ajouté automatiquement) et l'UI mobile par-dessus —
   aucun câblage de scène. On pêche en tapant sur l'eau ou via le bouton central.
   Pour le bon rendu, mettre la vue Game en portrait : menu déroulant d'aspect en haut de
   la vue Game → **+** → 1080×1920.
   (`DevHud` reste disponible comme HUD de secours ; `GameUi` le désactive s'il est présent.)

## Tests

Window → General → **Test Runner** → onglet *EditMode* → Run All.

Tout le gameplay vit dans `Assets/Scripts/Core` en C# pur et est couvert par ces tests —
y compris `PacingTests`, un bot qui joue la première heure en accéléré et sert de
harnais d'équilibrage.

## Architecture

| Dossier | Rôle |
|---|---|
| `Assets/Scripts/Core` | Moteur pur, **zéro dépendance Unity** (imposé par asmdef `noEngineReferences`) : simulation, économie, progression hors-ligne, prestige, équilibrage |
| `Assets/Scripts/Game` | Couche Unity : bootstrap, sauvegarde JSON, HUD de développement |
| `Assets/Tests/EditMode` | Tests NUnit du Core |
| `docs/` | Game design et décisions |

Les conventions de code et les règles du projet sont dans [AGENTS.md](AGENTS.md).
