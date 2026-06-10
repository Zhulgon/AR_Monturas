using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectManipulator : MonoBehaviour
{
    [Header("References")]
    public GameObject ARObject;
    [SerializeField] private Camera aRCamera;
    [SerializeField] private ARScene arScene;

    [Header("Config")]
    [SerializeField] private string tagARObjects = "ARObject";
    [SerializeField] private bool useActiveModelFromScene = true;
    [SerializeField] private bool requireRaycastSelection = false;
    [SerializeField] private float speedMovement = 4f;
    [SerializeField] private float speedRotation = 5f;
    [SerializeField] private float scaleStep = 0.08f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 2.2f;
    [SerializeField] private float rotationTolerance = 1.5f;
    [SerializeField] private float movementScreenFactor = 0.0001f;
    [SerializeField] private float pinchTolerance = 6f;

    private bool isARObjectSelected;
    private Vector2 initialTouchPos;
    private float previousPinchDistance;
    private Vector2 previousPinchVector;
    private bool pinchInitialized;

    private void Awake()
    {
        if (aRCamera == null)
        {
            aRCamera = Camera.main;
        }

        if (arScene == null)
        {
            arScene = FindFirstObjectByType<ARScene>();
        }
    }

    private void Update()
    {
        if (useActiveModelFromScene && arScene != null)
        {
            ARObject = arScene.CurrentModel;
        }

        if (Input.touchCount == 0)
        {
            isARObjectSelected = false;
            pinchInitialized = false;
            return;
        }

        if (ARObject == null)
        {
            return;
        }

        Touch touchOne = Input.GetTouch(0);
        if (IsTouchOverUI(touchOne))
        {
            return;
        }

        if (Input.touchCount == 1)
        {
            if (touchOne.phase == TouchPhase.Began)
            {
                initialTouchPos = touchOne.position;
            }

            if (requireRaycastSelection && touchOne.phase == TouchPhase.Began)
            {
                isARObjectSelected = CheckTouchOnARObject(initialTouchPos);
            }
            else if (!requireRaycastSelection)
            {
                isARObjectSelected = true;
            }

            if (touchOne.phase == TouchPhase.Moved && isARObjectSelected && ARObject != null)
            {
                Vector2 diffPos = (touchOne.position - initialTouchPos) * movementScreenFactor;
                float y = ARObject.transform.rotation.eulerAngles.y - (diffPos.x * speedMovement * 100f);
                ARObject.transform.rotation = Quaternion.Euler(0f, y, 0f);
                initialTouchPos = touchOne.position;
            }

            return;
        }

        Touch touchTwo = Input.GetTouch(1);
        if (IsTouchOverUI(touchTwo))
        {
            return;
        }

        if (requireRaycastSelection && !isARObjectSelected && touchOne.phase == TouchPhase.Began)
        {
            isARObjectSelected = CheckTouchOnARObject(touchOne.position);
        }
        else if (!requireRaycastSelection)
        {
            isARObjectSelected = true;
        }

        if (!isARObjectSelected || ARObject == null)
        {
            return;
        }

        if (!pinchInitialized || touchOne.phase == TouchPhase.Began || touchTwo.phase == TouchPhase.Began)
        {
            previousPinchVector = touchTwo.position - touchOne.position;
            previousPinchDistance = Vector2.Distance(touchTwo.position, touchOne.position);
            pinchInitialized = true;
            return;
        }

        if (touchOne.phase == TouchPhase.Moved || touchTwo.phase == TouchPhase.Moved)
        {
            Vector2 currentPinchVector = touchTwo.position - touchOne.position;
            float currentPinchDistance = Vector2.Distance(touchTwo.position, touchOne.position);

            float pinchDelta = currentPinchDistance - previousPinchDistance;
            if (Mathf.Abs(pinchDelta) > pinchTolerance)
            {
                float dir = Mathf.Sign(pinchDelta);
                Vector3 currentScale = ARObject.transform.localScale;
                Vector3 targetScale = currentScale + (Vector3.one * dir * scaleStep);
                targetScale.x = Mathf.Clamp(targetScale.x, minScale, maxScale);
                targetScale.y = Mathf.Clamp(targetScale.y, minScale, maxScale);
                targetScale.z = Mathf.Clamp(targetScale.z, minScale, maxScale);
                ARObject.transform.localScale = Vector3.Lerp(currentScale, targetScale, 0.35f);
            }

            float angle = Vector2.SignedAngle(previousPinchVector, currentPinchVector);
            if (Mathf.Abs(angle) > rotationTolerance)
            {
                float y = ARObject.transform.rotation.eulerAngles.y - Mathf.Sign(angle) * speedRotation;
                ARObject.transform.rotation = Quaternion.Euler(0f, y, 0f);
            }

            previousPinchDistance = currentPinchDistance;
            previousPinchVector = currentPinchVector;
        }
    }

    private static bool IsTouchOverUI(Touch touch)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }

    private bool CheckTouchOnARObject(Vector2 touchPosition)
    {
        if (aRCamera == null)
        {
            aRCamera = Camera.main;
            if (aRCamera == null)
            {
                return false;
            }
        }

        Ray ray = aRCamera.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hitARObject))
        {
            if (hitARObject.collider != null && hitARObject.collider.CompareTag(tagARObjects))
            {
                ARObject = hitARObject.transform.gameObject;
                return true;
            }
        }

        return false;
    }
}
