# Hurrycane

First-person hurricane preparation game prototype.

## Open the project

1. Install Git LFS (`git lfs install`) before cloning or pulling.
2. Clone https://github.com/alika1ender/Hurrycane.git, or fetch and pull `main` in an existing clone.
3. Run `git lfs pull` inside the repository to download the models and textures.
4. Open this folder in Unity Hub using Unity **6000.5.3f1**. Let Unity restore packages and import assets.
5. Open `Assets/Scenes/HouseMap.unity` and press Play.

Controls: WASD to move, mouse to look, Shift to sprint, E to open/close doors and drawers.

The scene includes the two-storey house, basement, furnished rooms, materials, supplies, and 31 doors/drawers. Cabinet fronts use single hinged leaves. Kitchen curtains have been removed and countertop props repositioned.

## Assets and validation

Imported models, textures, material definitions, and source attribution are in `Assets/Art/HouseInterior`. License information is in its `Licenses` folder. `HouseMap-interaction-test-report.txt` records the latest successful 31-object interaction test.

The Unity scene is ready to use; rebuilding is optional through **Hurrycane > Rebuild House Map**. Keep `.meta` files with their assets. Unity-generated Library, Temp, and other local caches are excluded from version control.
