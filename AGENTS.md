# Projet : Devside Fishing Idle (Unity)

Jeu mobile idle/incrémental à thème pêche. Unity 6 LTS, C#, cible Android/iOS en portrait.
Le game design de référence est `docs/GAME-DESIGN.md` — le lire avant de toucher aux mécaniques.

## Règles d'architecture

- `Assets/Scripts/Core` est un **moteur pur** : l'asmdef a `noEngineReferences: true`,
  donc aucun `using UnityEngine`, aucun accès disque, aucun accès à l'horloge, aucun RNG
  caché (le temps, les timestamps et les tirages aléatoires — rolls — sont passés en
  paramètre par la couche hôte) et
  **aucun texte affichable** (uniquement des ids stables type `fisherman_t1`).
- Toute mécanique de jeu entre dans Core sous forme de **fonctions pures accompagnées de
  tests NUnit** dans `Assets/Tests/EditMode`. Pas de mécanique dans un MonoBehaviour.
- Tout l'équilibrage (coûts, taux, courbes) vit dans `BalanceConfig` et nulle part ailleurs.
- `Assets/Scripts/Game` est la couche Unity : MonoBehaviours fins qui branchent Core sur la
  scène, sauvegarde, UI. La vraie UI (`GameUi`, bâtie par code via `UiKit`) ne contient
  **aucun libellé en dur** : tout texte affichable vient de `GameTheme` (couche thème), et
  `ThemeTests` vérifie que chaque id de `BalanceConfig.Default()` y a son libellé.
  Exception assumée : `DevHud` est un outil de dev jetable, ses libellés en dur sont tolérés.
- `PacingTests` est le harnais d'équilibrage : un bot joue la première heure en accéléré.
  Après tout changement de `BalanceConfig`, vérifier que ses assertions tiennent toujours.

## Règles git / Unity

- Les fichiers `.meta` se **committent toujours** ; `Library/`, `Temp/`, `*.csproj` jamais.
- Assets binaires (textures, sons, modèles) → Git LFS (déjà configuré via `.gitattributes`).
- Scènes et prefabs en sérialisation texte (Force Text, défaut du projet).

## Validation avant commit

- **Compiler, toujours** : `tools/compile-check/check.sh` (prérequis `mono-mcs`). Le script
  compile Core + Game + tests contre des stubs d'UnityEngine et de NUnit. Il n'exécute
  rien, mais une erreur de compilation empêche Unity de lancer quoi que ce soit — la
  découvrir ici coûte dix secondes, la découvrir chez le testeur coûte un aller-retour.
  Si le script signale un membre absent des stubs alors qu'il existe vraiment dans Unity,
  compléter le stub (`tools/compile-check/UnityStub.cs`) — jamais contorsionner le jeu
  pour satisfaire l'outil.
- Les tests EditMode doivent passer (Unity → Test Runner). Dans un environnement sans
  éditeur Unity (sessions distantes), ils ne peuvent pas s'exécuter : garder les
  changements de Core autonomes et déterministes, et signaler dans la PR que les tests
  doivent être lancés localement.
