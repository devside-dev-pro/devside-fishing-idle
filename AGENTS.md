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
  scène, sauvegarde, UI. Exception assumée : `DevHud` est un outil de dev jetable, ses
  libellés en dur sont tolérés ; la vraie UI passera par une couche thème dédiée.
- `PacingTests` est le harnais d'équilibrage : un bot joue la première heure en accéléré.
  Après tout changement de `BalanceConfig`, vérifier que ses assertions tiennent toujours.

## Règles git / Unity

- Les fichiers `.meta` se **committent toujours** ; `Library/`, `Temp/`, `*.csproj` jamais.
- Assets binaires (textures, sons, modèles) → Git LFS (déjà configuré via `.gitattributes`).
- Scènes et prefabs en sérialisation texte (Force Text, défaut du projet).

## Validation avant commit

- Les tests EditMode doivent passer (Unity → Test Runner). Dans un environnement sans
  éditeur Unity (sessions distantes), les tests ne peuvent pas s'exécuter : dans ce cas,
  garder les changements de Core autonomes et déterministes, relire le diff avec soin, et
  signaler dans la PR que les tests doivent être lancés localement.
