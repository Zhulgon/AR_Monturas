using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ARModelManipulator : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 0.25f;

    [Header("Scale")]
    public float pinchScaleSpeed = 0.0025f;
    public float minScaleMultiplier = 0.7f;
    public float maxScaleMultiplier = 1.4f;

    [Header("Editor")]
    public bool enableMouseInEditor = true;

    private Vector3 _baseScale = Vector3.one;
    private float _currentScaleMultiplier = 1f;

    public void CaptureCurrentAsBaseScale()
    {
        _baseScale = transform.localScale;
    }

    public void ResetPose()
    {
        transform.localRotation = Quaternion.identity;
        _currentScaleMultiplier = 1f;
        transform.localScale = _baseScale;
    }

    private void Awake()
    {
        CaptureCurrentAsBaseScale();
    }

    private void Update()
    {
        var handledByInputSystem = HandleInputSystem();
        if (!handledByInputSystem)
        {
            HandleLegacyInput();
        }
    }

    private bool HandleInputSystem()
    {
        var handledAny = false;

#if ENABLE_INPUT_SYSTEM
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var activeTouches = 0;
            var firstIndex = -1;
            var secondIndex = -1;

            for (var i = 0; i < touchscreen.touches.Count; i++)
            {
                var t = touchscreen.touches[i];
                if (!t.press.isPressed) continue;

                activeTouches++;
                if (firstIndex < 0) firstIndex = i;
                else if (secondIndex < 0) secondIndex = i;
            }

            if (activeTouches == 1 && firstIndex >= 0)
            {
                var first = touchscreen.touches[firstIndex];
                var delta = first.delta.ReadValue();
                var y = -delta.x * rotationSpeed;
                transform.Rotate(0f, y, 0f, Space.Self);
                handledAny = true;
            }

            if (activeTouches >= 2 && firstIndex >= 0 && secondIndex >= 0)
            {
                var first = touchscreen.touches[firstIndex];
                var second = touchscreen.touches[secondIndex];
                var p0 = first.position.ReadValue();
                var p1 = second.position.ReadValue();
                var d0 = first.delta.ReadValue();
                var d1 = second.delta.ReadValue();

                var prev0 = p0 - d0;
                var prev1 = p1 - d1;
                var prevDist = (prev0 - prev1).magnitude;
                var currentDist = (p0 - p1).magnitude;
                var delta = currentDist - prevDist;

                _currentScaleMultiplier = Mathf.Clamp(
                    _currentScaleMultiplier + delta * pinchScaleSpeed,
                    minScaleMultiplier,
                    maxScaleMultiplier);

                transform.localScale = _baseScale * _currentScaleMultiplier;
                handledAny = true;
            }
        }

        if (enableMouseInEditor && Application.isEditor && Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed)
            {
                var delta = Mouse.current.delta.ReadValue();
                if (Mathf.Abs(delta.x) > 0.0001f)
                {
                    transform.Rotate(0f, -delta.x * 0.08f, 0f, Space.Self);
                    handledAny = true;
                }
            }

            var scrollY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.0001f)
            {
                _currentScaleMultiplier = Mathf.Clamp(
                    _currentScaleMultiplier + scrollY * pinchScaleSpeed * 0.02f,
                    minScaleMultiplier,
                    maxScaleMultiplier);

                transform.localScale = _baseScale * _currentScaleMultiplier;
                handledAny = true;
            }
        }
#endif

        return handledAny;
    }

    private void HandleLegacyInput()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        HandleLegacyTouchRotation();
        HandleLegacyPinchZoom();
        HandleLegacyMouseRotation();
#endif
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    private void HandleLegacyTouchRotation()
    {
        if (Input.touchCount != 1) return;

        var t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Moved) return;

        var y = -t.deltaPosition.x * rotationSpeed;
        transform.Rotate(0f, y, 0f, Space.Self);
    }

    private void HandleLegacyPinchZoom()
    {
        if (Input.touchCount < 2) return;

        var t0 = Input.GetTouch(0);
        var t1 = Input.GetTouch(1);

        var prev0 = t0.position - t0.deltaPosition;
        var prev1 = t1.position - t1.deltaPosition;

        var prevDist = (prev0 - prev1).magnitude;
        var currentDist = (t0.position - t1.position).magnitude;
        var delta = currentDist - prevDist;

        _currentScaleMultiplier = Mathf.Clamp(
            _currentScaleMultiplier + delta * pinchScaleSpeed,
            minScaleMultiplier,
            maxScaleMultiplier);

        transform.localScale = _baseScale * _currentScaleMultiplier;
    }

    private void HandleLegacyMouseRotation()
    {
        if (!enableMouseInEditor) return;
        if (!Application.isEditor) return;
        if (!Input.GetMouseButton(0)) return;

        var x = Input.GetAxis("Mouse X");
        if (Mathf.Abs(x) < 0.0001f) return;

        transform.Rotate(0f, -x * 8f, 0f, Space.Self);
    }
#endif
}
