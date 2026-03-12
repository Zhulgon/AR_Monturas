using UnityEngine;

public enum GlassesSize
{
    X,
    S,
    M,
    L,
    XL
}

[ExecuteAlways]
public class GlassesSizePreset : MonoBehaviour
{
    [Header("Size Preset")]
    public GlassesSize size = GlassesSize.M;

    [Header("Base Scale (M)")]
    public Vector3 baseScale = Vector3.one;

    [Header("Multipliers")]
    public float xMultiplier = 1.10f;
    public float sMultiplier = 0.95f;
    public float mMultiplier = 1.00f;
    public float lMultiplier = 1.05f;
    public float xlMultiplier = 1.10f;

    public bool autoApplyInEditor = true;

    private void Start()
    {
        ApplySize();
    }

    private void OnValidate()
    {
        if (autoApplyInEditor)
        {
            ApplySize();
        }
    }

    [ContextMenu("Capture Current Scale As M")]
    public void CaptureCurrentAsM()
    {
        baseScale = transform.localScale;
        ApplySize();
    }

    [ContextMenu("Apply Size")]
    public void ApplySize()
    {
        transform.localScale = baseScale * GetMultiplier(size);
    }

    private float GetMultiplier(GlassesSize currentSize)
    {
        return currentSize switch
        {
            GlassesSize.X => xMultiplier,
            GlassesSize.S => sMultiplier,
            GlassesSize.M => mMultiplier,
            GlassesSize.L => lMultiplier,
            GlassesSize.XL => xlMultiplier,
            _ => 1.0f
        };
    }
}
