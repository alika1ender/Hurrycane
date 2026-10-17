# Hurrycane interior asset

- `Hurrycane_Interior.fbx`: furnished two-storey interior plus basement, 307 visual meshes, approximately 338k triangles; 38 separate pickup roots and bedroom/bathroom/fridge door pivots.
- `Hurrycane_Interior_Colliders.fbx`: 453 simple collision meshes including a smooth stair ramp. Disable renderers on these meshes and add static collision components. Place both FBX roots at the same transform.
- Metre scale; upper floor is 3.3 m above ground; basement is 3.2 m below ground. Use `MARKER_PlayerStart` for player placement. Suggested capsule: 1.8 m tall, 0.3 m radius, 0.25 m step offset.
- Basic material colors are embedded. Recreate lighting and use materials appropriate to your Unity render pipeline. Blender preview lights/cameras are not exported.
- Pickups/doors are prepared as separate transforms; gameplay scripts and Unity scene wiring are not included. Door collision meshes represent the saved open position and need to follow doors if animated.
- Editable source, rebuild scripts, previews and detailed handoff notes are in the workspace's `art/house_interior` folder, alongside the Unity project.

Original procedural geometry created with OpenAI Codex assistance, 16 October 2026. The initial procedural pass used no downloaded assets; subsequent downloaded sources are recorded below. Mesh labels originate from Blender's bundled default font. See the competition disclosure log before submission.

Basement: descend from the north end of the ground-floor stair hall. Includes supply shelves, workbench and 24 new resource roots (water, food, first aid, batteries, tools and repair tape). Geometry checks passed for walking routes and staircase headroom; Unity controller playtesting remains pending.


Texture update: five Poly Haven CC0 texture sets and preserved UVs are included. External color/normal/roughness maps are in Textures/. Check URP material assignment, import normals as Normal Maps and convert roughness to smoothness during Unity integration. See art/house_interior/texture-manifest.json for mappings and source links.


Downloaded props: Kenney Food Kit and Quaternius Survival / Ultimate House Interior packs (CC0-1.0). 61 curated placements/replacements are included; 38 pickup roots remain. Kenney palette texture is in Textures/. Detailed source mapping: art/house_interior/prop-manifest.json.


Cabinet update: 19 doors and 16 drawers are now movable and the FBX includes a combined open/close preview take. Use CabinetRigSetup with cabinet-interactions.json and optional cabinet-collision-links.json to configure independent CabinetOpenable controls. Connect Toggle() to your input/raycast system. See art/house_interior/CABINET_CONTROLS.md; Unity scene/input wiring remains pending.


Stored supplies: 15 of the 38 pickups are now placed inside cabinets/drawers/wardrobe, with five parented to moving drawer controls. stored-supplies.json records the layout. Detach pickups from their storage parent when collecting them.


Visual upgrade (16–17 October 2026): six additional CC0 texture sets, baked color variations, pleated curtains, softer upholstery/bedding, electrical plates and 31 further pack prop placements/replacements. Source manifests: beauty-textures.json and diverse-props.json under art/house_interior. Preview lighting is Blender-only; recreate/tune lighting and URP materials in Unity. All 38 pickups and 35 cabinet controls are preserved and FBX motion checks passed.


TV/KayKit update (17 October 2026): Animimo Modern TV replaces the placeholder; eight KayKit Furniture Bits placements improve seating, lamps, study chair and frame. CC0 source/license records in Licenses/. See art/house_interior/tv-asset.json and kaykit-selection.json. Current visual FBX: 314 meshes, 352,680 triangles, 94 materials; 465 collision proxies. All 38 pickups and 35 working cabinet controls preserved and export checks passed.

Map audit update: see `art/house_interior/MAP_AUDIT.md` for the current repaired map, Unity exploration scene and remaining game systems. Earlier notes describe prior stages.
