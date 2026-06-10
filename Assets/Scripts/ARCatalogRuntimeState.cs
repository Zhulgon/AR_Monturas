public static class ARCatalogRuntimeState
{
    public static bool HasSelection { get; private set; }
    public static int SelectedModelIndex { get; private set; }
    public static GlassesSize SelectedSize { get; private set; } = GlassesSize.M;
    public static int SelectedFrameMaterialIndex { get; private set; }
    public static int SelectedLensMaterialIndex { get; private set; }

    public static void SaveSelection(int modelIndex, GlassesSize size, int frameMaterialIndex, int lensMaterialIndex)
    {
        SelectedModelIndex = modelIndex;
        SelectedSize = size;
        SelectedFrameMaterialIndex = frameMaterialIndex;
        SelectedLensMaterialIndex = lensMaterialIndex;
        HasSelection = true;
    }
}

