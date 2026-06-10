using UnityEngine;

public class ARScene : MonoBehaviour
{
    [SerializeField] private GameObject startModel;
    [Header("Optional Materials")]
    [SerializeField] private Material[] frameMaterials;
    [SerializeField] private Material[] lensMaterials;

    private int modelsCount;
    private int indexCurrentModel;
    private GlassesSize currentSize = GlassesSize.M;
    private int frameMaterialIndex;
    private int lensMaterialIndex;

    public int CurrentIndex => indexCurrentModel;
    public GameObject CurrentModel => modelsCount > 0 ? transform.GetChild(indexCurrentModel).gameObject : null;

    private void Start()
    {
        modelsCount = transform.childCount;
        if (modelsCount == 0)
        {
            indexCurrentModel = 0;
            return;
        }

        int desiredIndex = 0;
        if (ARCatalogRuntimeState.HasSelection)
        {
            desiredIndex = Mathf.Clamp(ARCatalogRuntimeState.SelectedModelIndex, 0, modelsCount - 1);
            currentSize = ARCatalogRuntimeState.SelectedSize;
            frameMaterialIndex = ARCatalogRuntimeState.SelectedFrameMaterialIndex;
            lensMaterialIndex = ARCatalogRuntimeState.SelectedLensMaterialIndex;
        }
        else if (startModel != null)
        {
            desiredIndex = Mathf.Clamp(startModel.transform.GetSiblingIndex(), 0, modelsCount - 1);
        }

        SetActiveModel(desiredIndex);
    }

    public void ChangeARModel(int index)
    {
        int newIndex = indexCurrentModel + index;
        if (newIndex < 0)
        {
            newIndex = modelsCount - 1;
        }
        else if (newIndex > modelsCount - 1)
        {
            newIndex = 0;
        }

        SetActiveModel(newIndex);
    }

    public void NextModel()
    {
        ChangeARModel(1);
    }

    public void PreviousModel()
    {
        ChangeARModel(-1);
    }

    public void SetSizeByIndex(int sizeIndex)
    {
        if (sizeIndex < 0 || sizeIndex > (int)GlassesSize.XL)
        {
            return;
        }

        currentSize = (GlassesSize)sizeIndex;
        GlassesVisualUtility.ApplySize(CurrentModel, currentSize);
        SaveCurrentSelection();
    }

    public void SetSizeS() => SetSizeByIndex((int)GlassesSize.S);
    public void SetSizeM() => SetSizeByIndex((int)GlassesSize.M);
    public void SetSizeL() => SetSizeByIndex((int)GlassesSize.L);
    public void SetSizeXL() => SetSizeByIndex((int)GlassesSize.XL);
    public void SetSizeX() => SetSizeByIndex((int)GlassesSize.X);

    public void NextFrameMaterial()
    {
        if (frameMaterials == null || frameMaterials.Length == 0)
        {
            return;
        }

        frameMaterialIndex = (frameMaterialIndex + 1) % frameMaterials.Length;
        ApplyCurrentMaterials();
    }

    public void PreviousFrameMaterial()
    {
        if (frameMaterials == null || frameMaterials.Length == 0)
        {
            return;
        }

        frameMaterialIndex = (frameMaterialIndex - 1 + frameMaterials.Length) % frameMaterials.Length;
        ApplyCurrentMaterials();
    }

    public void NextLensMaterial()
    {
        if (lensMaterials == null || lensMaterials.Length == 0)
        {
            return;
        }

        lensMaterialIndex = (lensMaterialIndex + 1) % lensMaterials.Length;
        ApplyCurrentMaterials();
    }

    public void PreviousLensMaterial()
    {
        if (lensMaterials == null || lensMaterials.Length == 0)
        {
            return;
        }

        lensMaterialIndex = (lensMaterialIndex - 1 + lensMaterials.Length) % lensMaterials.Length;
        ApplyCurrentMaterials();
    }

    public void SaveCurrentSelection()
    {
        ARCatalogRuntimeState.SaveSelection(indexCurrentModel, currentSize, frameMaterialIndex, lensMaterialIndex);
    }

    private void SetActiveModel(int newIndex)
    {
        for (int i = 0; i < modelsCount; i++)
        {
            bool isActive = i == newIndex;
            Transform child = transform.GetChild(i);
            if (child.gameObject.activeSelf != isActive)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        indexCurrentModel = newIndex;
        GlassesVisualUtility.ApplySize(CurrentModel, currentSize);
        ApplyCurrentMaterials();
        SaveCurrentSelection();
    }

    private void ApplyCurrentMaterials()
    {
        Material frameMat = (frameMaterials != null && frameMaterials.Length > 0)
            ? frameMaterials[Mathf.Clamp(frameMaterialIndex, 0, frameMaterials.Length - 1)]
            : null;
        Material lensMat = (lensMaterials != null && lensMaterials.Length > 0)
            ? lensMaterials[Mathf.Clamp(lensMaterialIndex, 0, lensMaterials.Length - 1)]
            : null;

        GlassesVisualUtility.ApplyMaterials(CurrentModel, frameMat, lensMat);
        SaveCurrentSelection();
    }
}
