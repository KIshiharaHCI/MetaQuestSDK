using UnityEngine;

/// <summary>
/// Enables/disables SourceCreator based on VR trigger input.
/// SourceCreator handles the actual spawning automatically.
/// Uses Meta Quest SDK (Oculus Integration) for input.
/// </summary>
[RequireComponent(typeof(SourceCreator))]
public class VRPainter : MonoBehaviour
{
    [Header("VR Controller")]
    [Tooltip("The controller GameObject to track. Assign RightControllerInHandAnchor or LeftControllerInHandAnchor.")]
    [SerializeField] private Transform controllerTransform;

    [Header("VR Input")]
    [Tooltip("Which controller to use: RTouch = Right, LTouch = Left")]
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Tooltip("Trigger threshold (0-1) to activate painting")]
    [SerializeField] private float triggerThreshold = 0.1f;

    private SourceCreator _sourceCreator;

    void Awake()
    {
        _sourceCreator = GetComponent<SourceCreator>();
        if (_sourceCreator == null)
        {
            Debug.LogError($"VRPainter on {name} requires SourceCreator component!");
        }

        // Disable auto-spawning until trigger is pressed
        if (_sourceCreator != null)
        {
            _sourceCreator.enabled = false;
        }
    }

    void Update()
    {
        // Get trigger value (0.0 - 1.0)
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);

        // Fallback for desktop testing without VR headset
        if (!OVRInput.IsControllerConnected(controller))
        {
            _sourceCreator.enabled = Input.GetKey(KeyCode.Space);
            return;
        }

        // Enable SourceCreator only when trigger is pressed enough
        _sourceCreator.enabled = (triggerValue >= triggerThreshold);
    }

    void OnValidate()
    {
        // If controllerTransform is assigned, position this GameObject there
        if (controllerTransform != null && transform.parent != controllerTransform)
        {
            Debug.LogWarning($"VRPainter should be attached directly to the controller GameObject. Currently assigned to {name} but controllerTransform is {controllerTransform.name}");
        }
    }
}
