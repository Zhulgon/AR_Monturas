using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class ARCatalogAutoSetup
{
    // Cambia esta ruta si mueves la carpeta de exportacion fuera de la actual.
    private const string SourceFbxFolderAbsolute = @"C:/Users/Js/Desktop/Proyectos/AR_Monturas/exports/fbx/for_unity_canonical";
    private const string ModelsFolderAsset = "Assets/Models/Glasses";
    private const string PrefabsFolderAsset = "Assets/Prefabs/Glasses";
    private const string MaterialsFolderAsset = "Assets/Materials/Glasses";
    private const string ArScenePath = "Assets/Scenes/ARScene.unity";

    [MenuItem("Tools/Setup AR Catalog")]
    public static void SetupArCatalog()
    {
        EnsureFolder("Assets/Models");
        EnsureFolder(ModelsFolderAsset);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabsFolderAsset);
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialsFolderAsset);

        var importedModelPaths = ImportFbxModels();
        if (importedModelPaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Setup AR Catalog",
                "No se encontraron modelos FBX para configurar. " +
                "Revisa la ruta de origen o coloca FBX en Assets/Models/Glasses.",
                "OK");
            return;
        }

        var frameMaterial = CreateOrLoadFrameMaterial();
        var lensMaterial = CreateOrLoadLensMaterial();
        var prefabAssets = CreateOrUpdatePrefabs(importedModelPaths, frameMaterial, lensMaterial);

        SetupArScene(prefabAssets, frameMaterial, lensMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Setup AR Catalog",
            $"Setup completo.\n\nModelos: {importedModelPaths.Count}\nPrefabs: {prefabAssets.Count}\nEscena: {ArScenePath}",
            "OK");
    }

    [MenuItem("Tools/Fix UI Input Module")]
    public static void FixUiInputModule()
    {
        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("UI Input module fixed for current Input Handling.");
    }

    private static List<string> ImportFbxModels()
    {
        var modelAssetPaths = new List<string>();
        var destinationAbsolute = Path.Combine(Application.dataPath, "Models/Glasses");

        if (Directory.Exists(SourceFbxFolderAbsolute))
        {
            foreach (var sourceFile in Directory.GetFiles(SourceFbxFolderAbsolute, "*.fbx"))
            {
                var fileName = Path.GetFileName(sourceFile);
                var destinationFile = Path.Combine(destinationAbsolute, fileName);
                File.Copy(sourceFile, destinationFile, true);

                var assetPath = $"{ModelsFolderAsset}/{fileName}".Replace("\\", "/");
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer != null)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.SaveAndReimport();
                }

                modelAssetPaths.Add(assetPath);
            }
        }

        // Fallback: usar lo que ya exista en el proyecto.
        if (modelAssetPaths.Count == 0 && AssetDatabase.IsValidFolder(ModelsFolderAsset))
        {
            modelAssetPaths.AddRange(
                AssetDatabase.FindAssets("t:Model", new[] { ModelsFolderAsset })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.EndsWith(".fbx")));
        }

        modelAssetPaths.Sort();
        return modelAssetPaths;
    }

    private static Material CreateOrLoadFrameMaterial()
    {
        var path = $"{MaterialsFolderAsset}/M_Frame.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader);
        material.name = "M_Frame";
        SetMaterialColor(material, new Color(0.08f, 0.08f, 0.08f, 1f));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material CreateOrLoadLensMaterial()
    {
        var path = $"{MaterialsFolderAsset}/M_Lens.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader);
        material.name = "M_Lens";
        SetMaterialColor(material, new Color(0.65f, 0.82f, 1f, 0.35f));
        ConfigureTransparent(material);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static List<GameObject> CreateOrUpdatePrefabs(
        List<string> modelAssetPaths,
        Material frameMaterial,
        Material lensMaterial)
    {
        var result = new List<GameObject>();

        foreach (var modelPath in modelAssetPaths)
        {
            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (sourceModel == null) continue;

            var instance = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
            if (instance == null) continue;

            instance.name = sourceModel.name;
            AssignFrameAndLensMaterials(instance, frameMaterial, lensMaterial);

            var sizePreset = instance.GetComponent<GlassesSizePreset>();
            if (sizePreset == null)
            {
                sizePreset = instance.AddComponent<GlassesSizePreset>();
            }
            sizePreset.size = GlassesSize.M;
            sizePreset.baseScale = instance.transform.localScale;
            sizePreset.mMultiplier = 1f;

            var prefabPath = $"{PrefabsFolderAsset}/{sourceModel.name}.prefab";
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            if (prefabAsset != null)
            {
                result.Add(prefabAsset);
            }
        }

        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }

    private static void SetupArScene(List<GameObject> prefabs, Material frameMaterial, Material lensMaterial)
    {
        if (!File.Exists(ArScenePath))
        {
            EditorUtility.DisplayDialog("Setup AR Catalog", $"No existe la escena: {ArScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ArScenePath, OpenSceneMode.Single);
        var imageTarget = GameObject.Find("ImageTarget");
        if (imageTarget == null)
        {
            imageTarget = new GameObject("ImageTarget");
        }

        var mountRoot = FindOrCreateChild(imageTarget.transform, "MonturasRoot");
        var catalogRoot = FindOrCreateChild(imageTarget.transform, "ARCatalogManager");
        var catalog = catalogRoot.GetComponent<ARGlassesCatalog>();
        if (catalog == null)
        {
            catalog = catalogRoot.AddComponent<ARGlassesCatalog>();
        }

        catalog.spawnRoot = mountRoot.transform;
        catalog.frameMaterial = frameMaterial;
        catalog.lensMaterial = lensMaterial;
        catalog.glassesPrefabs = prefabs;
        catalog.initialIndex = 0;
        catalog.currentSize = GlassesSize.M;

        var sceneLoaderHost = GameObject.Find("UIManager");
        if (sceneLoaderHost == null)
        {
            sceneLoaderHost = new GameObject("UIManager");
        }
        var sceneLoader = sceneLoaderHost.GetComponent<SceneLoader>();
        if (sceneLoader == null)
        {
            sceneLoader = sceneLoaderHost.AddComponent<SceneLoader>();
        }

        SetupUi(catalog, sceneLoader);
        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupUi(ARGlassesCatalog catalog, SceneLoader sceneLoader)
    {
        var oldUi = GameObject.Find("ARUI");
        if (oldUi != null)
        {
            Object.DestroyImmediate(oldUi);
        }

        var canvasGo = new GameObject("ARUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = CreatePanel(canvasGo.transform, "BottomPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(900f, 250f));
        var row1 = CreatePanel(panel.transform, "MainRow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 42f), new Vector2(860f, 90f), false);
        var row2 = CreatePanel(panel.transform, "SizeRow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(860f, 76f), false);

        var prevBtn = CreateButton(row1.transform, "BtnPrev", "ANTERIOR", new Vector2(-310f, 0f), new Vector2(180f, 66f));
        var nextBtn = CreateButton(row1.transform, "BtnNext", "SIGUIENTE", new Vector2(-105f, 0f), new Vector2(180f, 66f));
        var resetBtn = CreateButton(row1.transform, "BtnReset", "RESETEAR", new Vector2(100f, 0f), new Vector2(180f, 66f));
        var backBtn = CreateButton(row1.transform, "BtnBack", "VOLVER", new Vector2(305f, 0f), new Vector2(180f, 66f));

        var xBtn = CreateButton(row2.transform, "BtnX", "X", new Vector2(-220f, 0f), new Vector2(90f, 56f));
        var sBtn = CreateButton(row2.transform, "BtnS", "S", new Vector2(-110f, 0f), new Vector2(90f, 56f));
        var mBtn = CreateButton(row2.transform, "BtnM", "M", new Vector2(0f, 0f), new Vector2(90f, 56f));
        var lBtn = CreateButton(row2.transform, "BtnL", "L", new Vector2(110f, 0f), new Vector2(90f, 56f));
        var xlBtn = CreateButton(row2.transform, "BtnXL", "XL", new Vector2(220f, 0f), new Vector2(90f, 56f));

        UnityEventTools.AddPersistentListener(prevBtn.onClick, catalog.ShowPrevious);
        UnityEventTools.AddPersistentListener(nextBtn.onClick, catalog.ShowNext);
        UnityEventTools.AddPersistentListener(resetBtn.onClick, catalog.ResetCurrentPose);
        UnityEventTools.AddPersistentListener(backBtn.onClick, sceneLoader.IrAInfo);

        UnityEventTools.AddPersistentListener(xBtn.onClick, catalog.SetSizeX);
        UnityEventTools.AddPersistentListener(sBtn.onClick, catalog.SetSizeS);
        UnityEventTools.AddPersistentListener(mBtn.onClick, catalog.SetSizeM);
        UnityEventTools.AddPersistentListener(lBtn.onClick, catalog.SetSizeL);
        UnityEventTools.AddPersistentListener(xlBtn.onClick, catalog.SetSizeXL);

        EditorUtility.SetDirty(canvasGo);
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.transform.SetAsLastSibling();
            eventSystem = go.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        var inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule != null)
        {
            Object.DestroyImmediate(standaloneModule);
        }
#else
        var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule == null)
        {
            standaloneModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    private static GameObject FindOrCreateChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null) return child.gameObject;

        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreatePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 size,
        bool withImage = true)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (withImage)
        {
            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.35f);
        }

        return go;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var bg = go.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.94f);

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.94f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 0.98f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);

        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<Text>();
        text.text = label;
        text.fontSize = 24;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        return button;
    }

    private static void AssignFrameAndLensMaterials(GameObject root, Material frame, Material lens)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var n = renderer.gameObject.name.ToLowerInvariant();
            var isLens = n.Contains("lens") || n.Contains("lente");
            var targetMat = isLens ? lens : frame;

            var mats = renderer.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                mats = new[] { targetMat };
            }
            else
            {
                for (var i = 0; i < mats.Length; i++)
                {
                    mats[i] = targetMat;
                }
            }
            renderer.sharedMaterials = mats;
        }
    }

    private static void ConfigureTransparent(Material material)
    {
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.renderQueue = (int)RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath)) return;

        var parts = assetPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
