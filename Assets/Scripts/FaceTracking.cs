using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class FaceTracking : MonoBehaviour
{
    private const string BackCanvasName = "FaceTrackingBackCanvas";
    private const string BackButtonName = "BtnReturnToCatalog";

    [Header("References")]
    [SerializeField] private ARFaceManager faceManager;
    [SerializeField] private GameObject[] glassesPrefabs;

    [Header("Placement")]
    [SerializeField] private Vector3 localPositionOffset = new Vector3(0f, -0.012f, 0.075f);
    [SerializeField] private Vector3 localRotationOffset = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 localScale = new Vector3(1.12f, 1.12f, 1.12f);

    [Header("Catalog Material Options")]
    [SerializeField] private Material[] frameMaterials;
    [SerializeField] private Material[] lensMaterials;
    [SerializeField] private bool applyCatalogMaterials = true;

    private readonly Dictionary<TrackableId, GameObject> spawnedByFace = new Dictionary<TrackableId, GameObject>();

    private void Awake()
    {
        if (faceManager == null)
        {
            faceManager = FindFirstObjectByType<ARFaceManager>();
        }
    }

    private void Start()
    {
        EnsureBackButtonUi();
    }

    private void OnEnable()
    {
        if (faceManager == null)
        {
            return;
        }

        faceManager.trackablesChanged.AddListener(OnFacesChanged);
        foreach (ARFace face in faceManager.trackables)
        {
            HideFaceVisuals(face);
            EnsureGlassesForFace(face);
        }
    }

    private void OnDisable()
    {
        if (faceManager != null)
        {
            faceManager.trackablesChanged.RemoveListener(OnFacesChanged);
        }
    }

    private void OnFacesChanged(ARTrackablesChangedEventArgs<ARFace> args)
    {
        for (int i = 0; i < args.added.Count; i++)
        {
            HideFaceVisuals(args.added[i]);
            EnsureGlassesForFace(args.added[i]);
        }

        for (int i = 0; i < args.updated.Count; i++)
        {
            HideFaceVisuals(args.updated[i]);
            EnsureGlassesForFace(args.updated[i]);
        }

        for (int i = 0; i < args.removed.Count; i++)
        {
            KeyValuePair<TrackableId, ARFace> removedPair = args.removed[i];
            ARFace removedFace = removedPair.Value;
            if (!spawnedByFace.TryGetValue(removedFace.trackableId, out GameObject spawned))
            {
                continue;
            }

            if (spawned != null)
            {
                Destroy(spawned);
            }

            spawnedByFace.Remove(removedFace.trackableId);
        }
    }

    private void EnsureGlassesForFace(ARFace face)
    {
        if (face == null || spawnedByFace.ContainsKey(face.trackableId))
        {
            return;
        }

        GameObject prefab = ResolveSelectedPrefab();
        if (prefab == null)
        {
            return;
        }

        GameObject spawned = Instantiate(prefab, face.transform, false);
        spawned.transform.localPosition = localPositionOffset;
        spawned.transform.localRotation = Quaternion.Euler(localRotationOffset);
        spawned.transform.localScale = localScale;

        if (ARCatalogRuntimeState.HasSelection)
        {
            GlassesVisualUtility.ApplySize(spawned, ARCatalogRuntimeState.SelectedSize);
        }

        if (applyCatalogMaterials)
        {
            Material frameMat = GetMaterial(frameMaterials, ARCatalogRuntimeState.SelectedFrameMaterialIndex);
            Material lensMat = GetMaterial(lensMaterials, ARCatalogRuntimeState.SelectedLensMaterialIndex);
            GlassesVisualUtility.ApplyMaterials(spawned, frameMat, lensMat);
        }

        spawnedByFace[face.trackableId] = spawned;
    }

    private static void HideFaceVisuals(ARFace face)
    {
        if (face == null)
        {
            return;
        }

        Renderer faceRenderer = face.GetComponent<Renderer>();
        ARFaceMeshVisualizer faceVisualizer = face.GetComponent<ARFaceMeshVisualizer>();
        if (faceVisualizer != null)
        {
            faceVisualizer.enabled = false;
        }

        if (faceRenderer != null)
        {
            faceRenderer.enabled = false;
        }

        Collider faceCollider = face.GetComponent<Collider>();
        if (faceCollider != null)
        {
            faceCollider.enabled = false;
        }
    }

    private GameObject ResolveSelectedPrefab()
    {
        if (glassesPrefabs == null || glassesPrefabs.Length == 0)
        {
            return null;
        }

        int index = 0;
        if (ARCatalogRuntimeState.HasSelection)
        {
            index = Mathf.Clamp(ARCatalogRuntimeState.SelectedModelIndex, 0, glassesPrefabs.Length - 1);
        }

        return glassesPrefabs[index];
    }

    private static Material GetMaterial(Material[] materials, int index)
    {
        if (materials == null || materials.Length == 0)
        {
            return null;
        }

        index = Mathf.Clamp(index, 0, materials.Length - 1);
        return materials[index];
    }

    private void EnsureBackButtonUi()
    {
        if (GameObject.Find(BackCanvasName) != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject(BackCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject buttonObject = new GameObject(BackButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(32f, -32f);
        buttonRect.sizeDelta = new Vector2(200f, 72f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.92f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.92f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 0.98f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(ReturnToCatalog);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textObject.GetComponent<Text>();
        buttonText.text = "VOLVER";
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontSize = 26;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        buttonText.raycastTarget = false;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static void ReturnToCatalog()
    {
        SceneManager.LoadScene("ARScene");
    }
}
