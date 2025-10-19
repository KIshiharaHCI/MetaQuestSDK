using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PainterMove : MonoBehaviour
{
    [Header("Mouse Movement")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask draggableLayers = Physics.DefaultRaycastLayers;

    private bool _isDragging;
    private float _dragDistance;
    private Vector3 _dragOffset;

    /// <summary>
    /// Moves the attached GameObject to the requested world position.
    /// </summary>
    public void MoveTo(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    void Awake()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        if (interactionCamera == null)
        {
            Debug.LogWarning($"{nameof(PainterMove)} on {name} has no interaction camera assigned.");
        }
    }

    void Update()
    {
        if (interactionCamera == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            BeginDrag();
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }

        if (_isDragging)
        {
            Drag();
        }
    }

    private void BeginDrag()
    {
        var ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, draggableLayers, QueryTriggerInteraction.Collide))
        {
            return;
        }

        if (hit.transform != transform && !transform.IsChildOf(hit.transform) && !hit.transform.IsChildOf(transform))
        {
            return;
        }

        _isDragging = true;
        _dragDistance = hit.distance;
        _dragOffset = transform.position - hit.point;
    }

    private void Drag()
    {
        var ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
        var target = ray.origin + ray.direction * _dragDistance + _dragOffset;
        MoveTo(target);
    }
}
