using UnityEngine;

public static class GlassesVisualUtility
{
    public static void ApplySize(GameObject model, GlassesSize size)
    {
        if (model == null)
        {
            return;
        }

        GlassesSizePreset sizePreset = model.GetComponent<GlassesSizePreset>();
        if (sizePreset == null)
        {
            return;
        }

        sizePreset.size = size;
        sizePreset.ApplySize();
    }

    public static void ApplyMaterials(GameObject model, Material frameMaterial, Material lensMaterial)
    {
        if (model == null)
        {
            return;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererRef = renderers[i];
            if (rendererRef == null)
            {
                continue;
            }

            bool isLens = IsLensRenderer(rendererRef.name);
            Material targetMaterial = isLens ? lensMaterial : frameMaterial;
            if (targetMaterial == null)
            {
                continue;
            }

            Material[] mats = rendererRef.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                mats[m] = targetMaterial;
            }

            rendererRef.sharedMaterials = mats;
        }
    }

    private static bool IsLensRenderer(string rendererName)
    {
        if (string.IsNullOrEmpty(rendererName))
        {
            return false;
        }

        string lower = rendererName.ToLowerInvariant();
        return lower.Contains("lens") || lower.Contains("lente") || lower.Contains("glass");
    }
}

