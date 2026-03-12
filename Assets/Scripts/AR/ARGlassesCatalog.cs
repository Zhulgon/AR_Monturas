using System.Collections.Generic;
using UnityEngine;

public class ARGlassesCatalog : MonoBehaviour
{
    [Header("Catalog")]
    public List<GameObject> glassesPrefabs = new();
    public Transform spawnRoot;
    public int initialIndex;

    [Header("Materials")]
    public Material frameMaterial;
    public Material lensMaterial;
    [Range(0f, 1f)] public float lensOpacity = 0.35f;

    [Header("Size")]
    public GlassesSize currentSize = GlassesSize.M;

    private readonly List<Renderer> _frameRenderers = new();
    private readonly List<Renderer> _lensRenderers = new();
    private GameObject _currentInstance;
    private int _currentIndex;

    public int Count => glassesPrefabs.Count;

    private void Start()
    {
        if (spawnRoot == null)
        {
            spawnRoot = transform;
        }

        if (glassesPrefabs.Count > 0)
        {
            Show(initialIndex);
        }
    }

    public void ShowNext()
    {
        if (glassesPrefabs.Count == 0) return;
        Show(_currentIndex + 1);
    }

    public void ShowPrevious()
    {
        if (glassesPrefabs.Count == 0) return;
        Show(_currentIndex - 1);
    }

    public void Show(int index)
    {
        if (glassesPrefabs.Count == 0) return;

        _currentIndex = Mathf.Clamp(index, 0, glassesPrefabs.Count - 1);
        if (index < 0) _currentIndex = glassesPrefabs.Count - 1;
        if (index >= glassesPrefabs.Count) _currentIndex = 0;

        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
        }

        _currentInstance = Instantiate(glassesPrefabs[_currentIndex], spawnRoot);
        _currentInstance.name = $"{glassesPrefabs[_currentIndex].name}_Instance";
        _currentInstance.transform.localPosition = Vector3.zero;
        _currentInstance.transform.localRotation = Quaternion.identity;

        CacheRenderers();
        ApplyMaterials();
        ApplySize(currentSize);

        var manipulator = _currentInstance.GetComponent<ARModelManipulator>();
        if (manipulator == null)
        {
            manipulator = _currentInstance.AddComponent<ARModelManipulator>();
        }
        manipulator.CaptureCurrentAsBaseScale();
    }

    public void ResetCurrentPose()
    {
        if (_currentInstance == null) return;
        var manipulator = _currentInstance.GetComponent<ARModelManipulator>();
        if (manipulator != null)
        {
            manipulator.ResetPose();
        }
    }

    public void SetSizeX() => ApplySize(GlassesSize.X);
    public void SetSizeS() => ApplySize(GlassesSize.S);
    public void SetSizeM() => ApplySize(GlassesSize.M);
    public void SetSizeL() => ApplySize(GlassesSize.L);
    public void SetSizeXL() => ApplySize(GlassesSize.XL);

    public void ApplySize(GlassesSize size)
    {
        currentSize = size;
        if (_currentInstance == null) return;

        var preset = _currentInstance.GetComponent<GlassesSizePreset>();
        if (preset == null)
        {
            preset = _currentInstance.AddComponent<GlassesSizePreset>();
            preset.CaptureCurrentAsM();
        }

        preset.size = size;
        preset.ApplySize();

        var manipulator = _currentInstance.GetComponent<ARModelManipulator>();
        if (manipulator != null)
        {
            manipulator.CaptureCurrentAsBaseScale();
            manipulator.ResetPose();
        }
    }

    private void CacheRenderers()
    {
        _frameRenderers.Clear();
        _lensRenderers.Clear();

        if (_currentInstance == null) return;

        var renderers = _currentInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var n = renderer.gameObject.name.ToLowerInvariant();
            if (n.Contains("lens") || n.Contains("lente"))
            {
                _lensRenderers.Add(renderer);
            }
            else
            {
                _frameRenderers.Add(renderer);
            }
        }
    }

    private void ApplyMaterials()
    {
        ApplyRendererMaterial(_frameRenderers, frameMaterial, 1f);
        ApplyRendererMaterial(_lensRenderers, lensMaterial, lensOpacity);
    }

    private static void ApplyRendererMaterial(List<Renderer> renderers, Material sourceMat, float alpha)
    {
        if (sourceMat == null) return;

        foreach (var renderer in renderers)
        {
            var mats = renderer.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                mats = new[] { sourceMat };
            }
            else
            {
                for (var i = 0; i < mats.Length; i++)
                {
                    mats[i] = sourceMat;
                }
            }

            renderer.sharedMaterials = mats;
            ApplyAlpha(renderer, alpha);
        }
    }

    private static void ApplyAlpha(Renderer renderer, float alpha)
    {
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);

        if (block == null) return;

        if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
        {
            var c = renderer.sharedMaterial.GetColor("_BaseColor");
            c.a = alpha;
            block.SetColor("_BaseColor", c);
        }
        else if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
        {
            var c = renderer.sharedMaterial.GetColor("_Color");
            c.a = alpha;
            block.SetColor("_Color", c);
        }

        renderer.SetPropertyBlock(block);
    }
}
