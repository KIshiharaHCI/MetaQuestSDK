using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ContactPainter : MonoBehaviour
{
    [Header("Brush")]
    public float strength = 0.01f; // 每次接触的位移强度（米）
    public EditableMesh.BrushMode mode = EditableMesh.BrushMode.Push;

    SphereCollider _sphere;

    void Awake()
    {
        _sphere = GetComponent<SphereCollider>();
        _sphere.isTrigger = true;

        // 物理触发器需要Kinematic刚体
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerStay(Collider other)
    {
        // 只对带有 EditableMesh 的对象生效
        if (!other.TryGetComponent<EditableMesh>(out var editable)) return;

        // 以画笔球心作为笔触中心；半径来自 SphereCollider（考虑缩放）
        float radiusWorld = _sphere.radius * Mathf.Max(
            transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z)
        );

        editable.ApplyBrushWorld(transform.position, radiusWorld, strength, mode);
    }
}