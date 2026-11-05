# 3D model sources

Bundled models are not all under the same license. See each entry below; source URLs are retained for provenance.

## Kenney — Graveyard Kit

- Source: https://kenney.nl/assets/graveyard-kit
- License: CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/
- `hero.glb` — `character-keeper.glb`.
  - Idle with weapon: `holding-both`.
  - Shot: `holding-both-shoot`.
- `Textures/colormap.png` — texture atlas for the keeper.

## Quaternius — Shotgun

- Model page: https://poly.pizza/m/8Z4HaN1NyS
- Pack: Zombie Apocalypse Kit, https://quaternius.com/packs/zombieapocalypsekit.html
- License shown on the model page: CC0 1.0.
- `shotgun.glb` — `Shotgun`; static model, no animation clips.

## Denys Almaral — Polyart Zombies with Animations Free Pack

- Author: Denys Almaral.
- Official pack page: https://denysalmaral.com/post/free-zombie-animations-for-your-games/
- Official glTF demonstration: https://denysalmaral.com/gamedev/free-zombies/
- License: CC BY 4.0, https://creativecommons.org/licenses/by/4.0/; attribution is required.
- Attribution: “Polyart Zombies with Animations Free Pack” by Denys Almaral, from the official pack page above, licensed under CC BY 4.0.
- All ten local models are used in the game: `polyart/female_a.glb` through `polyart/female_e.glb`, and `polyart/male_a.glb` through `polyart/male_e.glb`.
- Every GLB contains one rigged model, its material and embedded `zcolors.png` texture, plus ten clips: `attack_left`, `attack_right`, `idle2`, `idle`, `idle_alert`, `running`, `slowWalk`, `walk`, `walk_agressive`, `walk_limp`.
- The official model glTF files and animation accessor data were merged directly into GLB. Root motion is preserved in the assets as in the source; no retargeting was performed.
- At runtime, a local duplicate of `walk` without the root position track plays as `walk_in_place`; the shared animation and its other tracks are unchanged.
- The source pack has no death animation. Death uses a procedural fall and the existing blood effect.
- `polyart/manifest.json` records clip durations and GLB sizes.
