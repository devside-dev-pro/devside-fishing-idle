# Pipeline d'assets

Deux sources d'assets 3D, complémentaires :

1. **Packs gratuits** (Quaternius, Kenney, itch.io…) — la base du jeu : décor,
   PNJ, items. Voir « Packs tiers » ci-dessous.
2. **Génération IA sur mesure** (MCP Higgsfield) — pour ce qui n'existe dans
   aucun pack : les 13 espèces du Poissodex, props signature, navires. Les
   crédits sont limités : réserver l'IA à l'introuvable.

## Packs tiers (itch.io, Kenney, Quaternius…)

Mode d'emploi pour intégrer un pack téléchargé sans friction :

1. **Vérifier la licence avant de télécharger** (encart de la page itch.io) :
   - **CC0 / domaine public** → parfait, aucune condition.
   - **CC-BY / « credit required »** → OK, crédit obligatoire dans CREDITS.md.
   - **« Free for commercial use », sans redistribution des fichiers bruts** →
     OK pour le jeu (dépôt privé), à re-vérifier si le dépôt devient public.
   - **À écarter** : « personal use only », « non-commercial » — l'ambition
     Steam/mobile est un usage commercial.
2. **Consigner le pack** : une ligne dans le tableau de `CREDITS.md` au moment
   du dépôt (nom, auteur, licence, lien).
3. **Déposer** dans `Assets/Resources/Art/<NomDuPack>/` (nom court, sans
   espaces ni accents). Formats : FBX/OBJ importés nativement par Unity,
   GLB/GLTF via glTFast (déjà en place), `.unitypackage` → Assets ▸ Import
   Package… (ne pas commiter le `.unitypackage` lui-même). Textures à côté des
   modèles. Le LFS couvre déjà fbx/obj/glb/textures/audio (`.gitattributes`).
4. **Câblage par le code** : indiquer le nom du pack et ce qu'il contient (un
   screenshot du dossier importé suffit) → les chemins entrent dans
   `ArtLibrary` avec repli (`SpawnFirst`) : le jeu reste jouable si un modèle
   manque ou change de nom.

Priorités d'après la roadmap : items/potions/coffres (équipements v0.5),
décor d'îles (végétation, bâtiments portuaires), PNJ marchands/villageois,
et packs d'**icônes UI 2D** (style candy — chargées via `Resources/UI/Icons`).

# Pipeline d'assets custom (IA)

Recette validée pour produire les modèles 3D maison (poissons du Poissodex, props),
exécutable via le MCP Higgsfield. Première fournée (13 poissons + 5 props + 3
navires) livrée le 26/08/2026 — ids de jobs consignés dans le manifeste de session.

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
- `Art/Custom/Characters/` : personnages custom **déjà produits** (avec
  animations) et déposés par Mathieu — **ne pas les regénérer**. Les fichiers
  sont nommés librement (ex. `char_capitaine_x.glb`) : le code les retrouve par
  fragments de nom (`ArtLibrary.SpawnCustomCharacter` — capitaine / mousse ou
  marin / pecheur_pro / vieux). Certains existent en deux versions (T-pose brute
  et animée) : la version **animée** est préférée (composant d'animation détecté
  sur le prefab, indice « anim » dans le nom, malus « tpose »).
- À venir éventuellement : `Art/Custom/Ships/` (barque, chalutier_moyen,
  chalutier_grand — vues 3/4 déjà générées côté images).

Côté code, `ArtLibrary.CustomFish(id)` / `CustomProp(nom)` + `SpawnFirst`
(custom d'abord, pack CC0 en repli) : le jeu reste jouable sans aucun asset
custom. Orientation : les poissons Meshy générés de profil ont le nez en **+x**
(les poissons du pack l'ont en +z) — `BoatView.SpawnFishModel` fait la rotation.

## Kit d'icônes UI 2D (recette validée)

Les icônes de l'interface (`Assets/Resources/UI/Icons/*.png`, chargées par
`UiKit.Icon`) sont générées par **planches**, pas une par une : c'est dix fois
moins cher et le style reste cohérent d'une icône à l'autre.

1. **Planche** — `nano_banana_pro`, format 1:1, une grille **3×3** (ou 2×2)
   décrite explicitement dans le prompt, sujets listés « in reading order »,
   *plain flat light gray background*, marge généreuse entre les cellules, et le
   gabarit de style : *modern casual mobile game art style like a polished idle
   tycoon game, chunky rounded 3D shapes, glossy candy colors, saturated palette,
   soft gradient shading, clean subtle dark outline, soft drop shadow under each
   icon, no text, no labels, no frames around cells*.
2. **Découpe** — dans le sandbox (Pillow + numpy) : couleur de fond = médiane des
   quatre coins, alpha = `clip((distance − 28) / 25)` (le seuil coupe l'ombre
   portée sans ronger les icônes), découpe en cellules égales, `getbbox()` sur le
   canal alpha, recadrage carré centré avec 6 % de marge, sortie en 256×256.
3. **Livraison** — zip structuré `UI/Icons/<nom>.png`, à dézipper dans
   `Assets/Resources/`. Une icône absente n'est jamais une erreur : `UiKit.Icon`
   renvoie null et l'emplacement se masque.

Première fournée (27/08/2026) : 58 icônes en 7 planches — ressources et monnaies,
améliorations, équipement, coffres et gemmes, icônes système, et les 13 espèces du
Poissodex. Coût total : moins de 15 crédits.

## Contraintes d'environnement (sessions distantes)

- Le CDN Higgsfield (cloudfront) est **bloqué** par le proxy des sessions Claude :
  impossible d'y télécharger directement. Tout transit de fichiers passe par le
  sandbox Higgsfield (`sandbox_exec`, accès internet libre) ; la sortie se fait par
  upload en médiathèque (`media_upload` → PUT depuis le sandbox → `media_confirm`).
- Ne jamais relayer de binaire en base64 dans la conversation (corruption vécue).
- Le sandbox est éphémère (~10 s après chaque commande, bail 15 min en
  `background:true`) : chaîner chaque étape en une seule commande.
- Coût observé : 30 crédits par conversion 3D texturée, ~1,5 crédit par image.
