using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Rebuildable, isolated map scene with openable doors/drawers. Never overwrites SampleScene.</summary>
[InitializeOnLoad]
public static class HouseMapBuilder
{
    const string Art = "Assets/Art/HouseInterior/";
    const string ScenePath = "Assets/Scenes/HouseMap.unity";
    static string Request => Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".codex-map-build.request");
    [Serializable] class Data { public Mat[] materials; public Lamp[] lights; public Hinge[] hinges; public Pickup[] pickups; }
    [Serializable] class Mat { public string name, base_map, normal_map, roughness_map, emission_map; public float[] color, emission; public float roughness, metallic, emission_strength, normal_strength = 1; }
    [Serializable] class Lamp { public string name; public float[] position, color; public float energy; }
    [Serializable] class Hinge { public string name; public float open_angle_unity_deg; public bool starts_open; }
    [Serializable] class Pickup { public string name, kind; }
    static HouseMapBuilder() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        try { File.Delete(Request); } catch (IOException) { return; }
        try { Build(); } catch (Exception e) { Debug.LogException(e); File.WriteAllText("HouseMap-build-error.txt", e.ToString()); }
    }
    // Blender stores linear colors; Unity color properties expect sRGB-space values.
    static Color ColorOf(float[] value) => new Color(value[0], value[1], value[2], value.Length > 3 ? value[3] : 1).gamma;
    static Texture2D Texture(string path, bool normal = false, bool linear = false)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (normal && importer.textureType != TextureImporterType.NormalMap) { importer.textureType = TextureImporterType.NormalMap; changed = true; }
            if (!normal && importer.sRGBTexture == linear) { importer.sRGBTexture = !linear; changed = true; }
            if (importer.maxTextureSize > 2048) { importer.maxTextureSize = 2048; changed = true; }
            if (changed) importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static readonly Color AmbientSky = new Color(.62f, .60f, .57f), AmbientEquator = new Color(.46f, .46f, .46f), AmbientGround = new Color(.30f, .29f, .28f);
    const float Exposure = .4f;
    /// <summary>Uniform warm-grey room reflection so glossy surfaces and mirrors do not reflect black.</summary>
    static Cubemap InteriorReflection()
    {
        string path = Art + "InteriorReflection.cubemap";
        var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
        if (!cube) { cube = new Cubemap(16, TextureFormat.RGBA32, false); AssetDatabase.CreateAsset(cube, path); }
        foreach (CubemapFace face in Enum.GetValues(typeof(CubemapFace)))
        {
            if (face == CubemapFace.Unknown) continue;
            var c = new Color(.52f, .51f, .50f) * (face == CubemapFace.PositiveY ? 1.15f : face == CubemapFace.NegativeY ? .7f : 1); c.a = 1;
            cube.SetPixels(Enumerable.Repeat(c, 16 * 16).ToArray(), face);
        }
        cube.Apply(); EditorUtility.SetDirty(cube);
        return cube;
    }
    /// <summary>Neutral tonemapping (closest to Blender AgX), gentle bloom and exposure matched to the Blender renders.</summary>
    static VolumeProfile LookProfile()
    {
        string path = Art + "HouseMapLook.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
        if (!profile.TryGet<Tonemapping>(out var tone)) tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);
        if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(.12f); bloom.threshold.Override(1.2f);
        if (!profile.TryGet<ColorAdjustments>(out var color)) color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(Exposure);
        EditorUtility.SetDirty(profile);
        return profile;
    }
    [MenuItem("Hurrycane/Rebuild House Map")]
    public static void Build()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Hurrycane_Interior.fbx");
        var collisionSource = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Hurrycane_Interior_Colliders.fbx");
        if (!source || !collisionSource) throw new InvalidOperationException("House FBX assets are not imported yet.");
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(Art + "map-runtime.json"));
        Directory.CreateDirectory(Art + "Materials");
        var materials = new Dictionary<string, Material>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) throw new InvalidOperationException("URP Lit shader unavailable.");
        foreach (var item in data.materials)
        {
            string path = Art + "Materials/" + item.name.Replace('/', '_') + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            var colorMap = Texture(item.base_map);
            mat.SetTexture("_BaseMap", colorMap);
            mat.SetColor("_BaseColor", colorMap ? Color.white : ColorOf(item.color));
            mat.SetFloat("_Metallic", item.metallic);
            mat.SetFloat("_Smoothness", Mathf.Clamp01(1 - item.roughness));
            var normal = Texture(item.normal_map, true);
            mat.SetTexture("_BumpMap", normal); mat.SetFloat("_BumpScale", item.normal_strength);
            if (normal) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
            string packedPath = Art + "Textures/Packed/" + item.name.Replace('/', '_') + "_MetalSmooth.png";
            var packed = Texture(packedPath, false, true);
            mat.SetTexture("_MetallicGlossMap", packed);
            if (packed) { mat.EnableKeyword("_METALLICSPECGLOSSMAP"); mat.SetFloat("_Smoothness", 1); }
            else mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            if (item.emission_strength > 0)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", Texture(item.emission_map));
                mat.SetColor("_EmissionColor", ColorOf(item.emission) * Mathf.Min(item.emission_strength, 1.8f));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else { mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black); }
            if (item.name.Contains("Curtain") || item.name.Contains("Fabric")) mat.SetFloat("_Cull", 0);
            materials[item.name] = mat; EditorUtility.SetDirty(mat);
        }
        File.WriteAllText("HouseMap-scene-diagnostic.txt", string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(i => { var s = SceneManager.GetSceneAt(i); return s.name + " | " + s.path + " | " + string.Join(",", s.GetRootGameObjects().Select(r => r.name)); })));
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid())
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var candidate = SceneManager.GetSceneAt(i);
                var roots = candidate.GetRootGameObjects();
                if (roots.Length == 2 && roots.Any(r => r.name == "HouseInterior") && roots.Any(r => r.name == "HouseCollisions")) { scene = candidate; break; }
            }
        if (scene.IsValid() && scene.isLoaded)
        {
            foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
        }
        else scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        if (string.IsNullOrEmpty(scene.path)) scene.name = "HouseMap_Generated";
        SceneManager.SetActiveScene(scene);
        var house = (GameObject)PrefabUtility.InstantiatePrefab(source, scene); house.name = "HouseInterior";
        var collisions = (GameObject)PrefabUtility.InstantiatePrefab(collisionSource, scene); collisions.name = "HouseCollisions";
        PrefabUtility.UnpackPrefabInstance(house, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        PrefabUtility.UnpackPrefabInstance(collisions, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var names = new Dictionary<string, Transform>();
        foreach (Transform t in house.GetComponentsInChildren<Transform>(true)) names[t.name] = t;
        Vector3 origin = names["MARKER_CoordOrigin"].position;
        Vector3 ax = names["MARKER_CoordX"].position - origin, ay = names["MARKER_CoordY"].position - origin, az = names["MARKER_CoordZ"].position - origin;
        Vector3 World(float x, float y, float z) => origin + ax * x + ay * y + az * z;
        foreach (var renderer in house.GetComponentsInChildren<Renderer>(true))
        {
            var slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++) if (slots[i] && materials.TryGetValue(slots[i].name, out var replacement)) slots[i] = replacement;
            renderer.sharedMaterials = slots;
        }
        foreach (var animator in house.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (var animation in house.GetComponentsInChildren<Animation>(true)) { animation.Stop(); animation.enabled = false; }
        var proxyNames = new Dictionary<string, Transform>();
        foreach (var filter in collisions.GetComponentsInChildren<MeshFilter>(true))
        {
            proxyNames[filter.name] = filter.transform;
            var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh;
            if (filter.TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;
        }
        // Openable doors/drawers. Link moving collision proxies while everything is still in the exported pose.
        var links = JsonUtility.FromJson<CollisionLinks>(File.ReadAllText(Art + "cabinet-collision-links.json"));
        int linked = 0;
        foreach (var link in links.items)
            if (proxyNames.TryGetValue(link.collider, out var proxy) && names.TryGetValue(link.controller, out var pivotOf)) { proxy.SetParent(pivotOf, true); linked++; }
        if (linked != links.items.Length) throw new InvalidOperationException("Linked " + linked + " of " + links.items.Length + " moving collision proxies.");
        // Room doors are saved open in Blender; every other control is saved closed.
        var startsOpen = new HashSet<string>(data.hinges.Where(h => h.starts_open).Select(h => h.name));
        var motion = JsonUtility.FromJson<Motion>(File.ReadAllText(Art + "motion-reference.json"));
        foreach (var item in motion.items)
        {
            var pivot = names[item.name];
            // Rigid closed->open motion evaluated in Blender, mapped through the exported coordinate markers.
            Vector3 W(Vector3 p) => World(p.x, p.y, p.z);
            Vector3 c0 = W(item.closed[0]), o0 = W(item.opened[0]);
            var from = Quaternion.LookRotation(W(item.closed[1]) - c0, W(item.closed[2]) - c0);
            var to = Quaternion.LookRotation(W(item.opened[1]) - o0, W(item.opened[2]) - o0);
            Quaternion delta = to * Quaternion.Inverse(from); Vector3 shift = o0 - delta * c0;
            bool open = startsOpen.Contains(item.name);
            Vector3 p0 = pivot.position; Quaternion r0 = pivot.rotation;
            Vector3 closedPos = open ? Quaternion.Inverse(delta) * (p0 - shift) : p0;
            Quaternion closedRot = open ? Quaternion.Inverse(delta) * r0 : r0;
            Vector3 openPos = open ? p0 : delta * p0 + shift;
            Quaternion openRot = open ? r0 : delta * r0;
            var parent = pivot.parent; var parentInverse = Quaternion.Inverse(parent.rotation);
            var control = pivot.gameObject.AddComponent<CabinetOpenable>();
            control.Configure(item.drawer, item.name.StartsWith("UF_"), parent.InverseTransformPoint(closedPos), parentInverse * closedRot,
                parent.InverseTransformPoint(openPos), parentInverse * openRot, open);
            var body = pivot.gameObject.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
        }
        var stash = names["Basement_ShelterStash"];
        foreach (var filter in stash.GetComponentsInChildren<MeshFilter>()) filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        // Ambient fill plus room lights; no exterior directional light leaking through the house.
        // Stand-in for Blender's bounced light (EEVEE GI off near-white walls) and its AgX view transform.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = AmbientSky;
        RenderSettings.ambientEquatorColor = AmbientEquator;
        RenderSettings.ambientGroundColor = AmbientGround;
        RenderSettings.skybox = null;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = InteriorReflection();
        RenderSettings.reflectionIntensity = .45f;
        var look = new GameObject("BlenderLookVolume").AddComponent<Volume>();
        look.isGlobal = true; look.priority = 10; look.sharedProfile = LookProfile();
        foreach (var item in data.lights)
        {
            // Keep one fixture per room and the principal window fill; decorative extra lamps use emission.
            if (!(item.name.StartsWith("Light_") || item.name.StartsWith("BF_Light") || item.name.StartsWith("BasementLight") || item.name.Contains("Window"))) continue;
            var go = new GameObject(item.name); var light = go.AddComponent<Light>();
            go.transform.position = World(item.position[0], item.position[1], item.position[2] - .14f);
            light.type = LightType.Point; light.color = ColorOf(item.color);
            light.intensity = Mathf.Clamp(item.energy / 150, .6f, 4); light.range = item.position[2] < 0 ? 7 : 5.5f;
            light.shadows = new [] {"Light_Living", "Light_Kitchen", "Light_Stair", "Light_BasementAisle"}.Contains(item.name) ? LightShadows.Soft : LightShadows.None;
            var lightData = light.GetUniversalAdditionalLightData();
            var serializedLight = new SerializedObject(lightData);
            serializedLight.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
            serializedLight.ApplyModifiedPropertiesWithoutUndo();
            light.shadowBias = .03f; light.shadowNormalBias = .15f;
        }
        var player = new GameObject("MapExplorer"); var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = .3f; cc.center = new Vector3(0, .9f, 0); cc.stepOffset = .25f; cc.slopeLimit = 45; cc.skinWidth = .025f;
        player.transform.position = names["MARKER_PlayerStart"].position + Vector3.up * .07f;
        player.transform.rotation = Quaternion.LookRotation((World(7, 5, 0) - World(7, 1.2f, 0)).normalized, Vector3.up);
        var cameraObject = new GameObject("FirstPersonCamera"); cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(player.transform, false); cameraObject.transform.localPosition = Vector3.up * 1.68f;
        var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .04f; camera.farClipPlane = 65; camera.fieldOfView = 75;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .17f, .22f); cameraObject.AddComponent<AudioListener>();
        var cameraData = camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing = true; cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var explorer = player.AddComponent<MapExplorer>(); explorer.viewCamera = camera; explorer.spawnPoint = player.transform.position;
        player.AddComponent<MapPlaytest>();
        player.AddComponent<HouseInteractionTest>().reference = AssetDatabase.LoadAssetAtPath<TextAsset>(Art + "motion-reference.json");
        Physics.SyncTransforms();
        int missing = house.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Count(m => !m || m.shader != shader);
        if (missing > 0) throw new InvalidOperationException("Missing/invalid materials: " + missing);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var template = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
        if (template.IsValid() && template.isLoaded && !template.isDirty) EditorSceneManager.CloseScene(template, true);
        var builds = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList(); builds.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = builds.ToArray();
        AssetDatabase.SaveAssets();
        if (File.Exists("HouseMap-build-error.txt")) File.Delete("HouseMap-build-error.txt");
        File.WriteAllText("HouseMap-build-report.txt", "Built " + ScenePath + "\nMaterials: " + materials.Count + "\nStatic supply props: " + data.pickups.Length + "\nOpenable doors/drawers: " + motion.items.Length + "\nMoving collision proxies: " + linked + "\nCollision proxies: " + proxyNames.Count + "\nCoordinate axes: " + ax + ", " + ay + ", " + az + "\nSpawn: " + player.transform.position + "\n");
        Debug.Log("HOUSE_MAP_BUILD_OK: saved HouseMap with materials, lights, collision and " + motion.items.Length + " openable doors/drawers.");
    }
    [Serializable] class Motion { public MotionItem[] items; }
    [Serializable] class MotionItem { public string name; public bool drawer; public Vector3[] closed, opened; }
    [Serializable] class CollisionLinks { public CollisionLink[] items; }
    [Serializable] class CollisionLink { public string collider, controller; }
}
