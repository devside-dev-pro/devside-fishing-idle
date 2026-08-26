# Pipeline d'assets custom (IA)

Recette validée pour produire les modèles 3D maison (poissons du Poissodex, props),
exécutable via le MCP Higgsfield. Première fournée (13 poissons + 5 props) livrée
le 26/08/2026 — ids de jobs consignés dans le manifeste de session.

## La recette, étape par étape

1. **Image de référence** — modèle `nano_banana_2`, format 1:1 (poissons/props) ou
   9:16 (personnages). Gabarit de prompt validé (l'ancre de style est la sardine) :
   sujet unique, **profil parfait face à droite** (poissons) ou vue 3/4 surélevée
   (props/bateaux) ou T-pose (personnages), *plain light-gray seamless background,
   soft subtle ground shadow, low-poly stylized in the style of Quaternius asset
   packs, chunky simplified rounded geometry, flat saturated colors, clean uniform
   thin dark outline, flat shading no gradients, matte, no text, no watermark*.
2. **Image → 3D** — modèle `image_to_3d` (Meshy classique, **jamais** meshy_v7) :
   `should_texture: true`, pas de rigging pour poissons/props,
   `target_polycount: 2500` (poissons) / `3000` (props), `symmetry_mode: auto`.
   NB : le filtre de sécurité peut produire des faux positifs (ex. baril de
   poissons) — relancer avec `enable_safety_checker: false`.
3. **Réduction des textures** — Meshy sort du JPEG 2048 (~3 Mo par GLB). Dans le
   sandbox Higgsfield : `npm i -g @gltf-transform/cli@3` (la v4 exige Node ≥ 20.10,
   le sandbox est en 20.9) puis `gltf-transform resize in.glb out.glb --width 1024
   --height 1024`. **Piège** : le nom de sortie doit finir en `.glb`, sinon le CLI
   écrit un `.gltf` JSON + fichiers externes qui s'écrasent entre eux.
4. **QC** — poissons < 3000 tris, 1 matériau, 1 texture embarquée ≤ 1024, en-tête
   GLB valide (magic `glTF`), aucun buffer/image externe. Résultat attendu :
   ~250-400 Ko par modèle.
5. **Livraison** — zip structuré `Custom/{Fish,Props}/<id>.glb` uploadé en
   médiathèque Higgsfield (fichier général → URL permanente), à dézipper dans
   `Assets/Resources/Art/` (import Unity via com.unity.cloud.gltfast).

## Conventions de nommage (chargées par le code)

- `Art/Custom/Fish/<speciesId>.glb` — les 13 ids du Core : sardine, mackerel,
  sea_bass, sunfish, tuna, swordfish, moonfish, ghost_eel, anglerfish,
  giant_squid, abyssal_shark, kraken_spawn, leviathan.
- `Art/Custom/Props/` : fish_barrel, crate, cutting_station, fillet_station,
  fishing_rod.
- À venir : `Art/Custom/Characters/` (mousse, pecheur_pro, vieux_loup, capitaine —
  T-poses déjà générées), `Art/Custom/Ships/` (barque, chalutier_moyen,
  chalutier_grand — vues 3/4 déjà générées).

Côté code, `ArtLibrary.CustomFish(id)` / `CustomProp(nom)` + `SpawnFirst`
(custom d'abord, pack CC0 en repli) : le jeu reste jouable sans aucun asset
custom. Orientation : les poissons Meshy générés de profil ont le nez en **+x**
(les poissons du pack l'ont en +z) — `BoatView.SpawnFishModel` fait la rotation.

## Contraintes d'environnement (sessions distantes)

- Le CDN Higgsfield (cloudfront) est **bloqué** par le proxy des sessions Claude :
  impossible d'y télécharger directement. Tout transit de fichiers passe par le
  sandbox Higgsfield (`sandbox_exec`, accès internet libre) ; la sortie se fait par
  upload en médiathèque (`media_upload` → PUT depuis le sandbox → `media_confirm`).
- Ne jamais relayer de binaire en base64 dans la conversation (corruption vécue).
- Le sandbox est éphémère (~10 s après chaque commande, bail 15 min en
  `background:true`) : chaîner chaque étape en une seule commande.
- Coût observé : 30 crédits par conversion 3D texturée, ~1,5 crédit par image.
