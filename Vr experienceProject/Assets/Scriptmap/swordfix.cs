using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class swordfix : MonoBehaviour
{
    [Header("Grip")]
    [SerializeField]
    Transform gripAttachPoint;

    [SerializeField]
    bool applyRecommendedGrabSettings = true;

    [Header("Sword Model Axes")]
    [Tooltip("Local axis that points from the hilt toward the blade tip.")]
    [SerializeField]
    Vector3 bladeAxis = Vector3.up;

    [Tooltip("Local axis that points out of the cutting edge / front face of the blade.")]
    [SerializeField]
    Vector3 edgeAxis = Vector3.forward;

    [Header("Controller Target Axes")]
    [Tooltip("Controller-local axis the blade should align to when held. Up is usually the best choice for swords.")]
    [SerializeField]
    Vector3 controllerBladeAxis = Vector3.up;

    [Tooltip("Controller-local axis the sword edge should face when held. Forward is the usual choice.")]
    [SerializeField]
    Vector3 controllerEdgeAxis = Vector3.forward;

    XRGrabInteractable m_GrabInteractable;
    Rigidbody m_Rigidbody;

    void Reset()
    {
        CacheComponents();

        if (gripAttachPoint == null && m_GrabInteractable != null)
            gripAttachPoint = m_GrabInteractable.attachTransform;

        ApplyConfiguration();
    }

    void Awake()
    {
        CacheComponents();
        ApplyConfiguration();
    }

    void OnEnable()
    {
        CacheComponents();

        if (m_GrabInteractable != null)
            m_GrabInteractable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        if (m_GrabInteractable != null)
            m_GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnValidate()
    {
        CacheComponents();
        ApplyConfiguration();
    }

    [ContextMenu("Apply Sword Grip Setup")]
    public void ApplyConfiguration()
    {
        if (gripAttachPoint == null && m_GrabInteractable != null)
            gripAttachPoint = m_GrabInteractable.attachTransform;

        if (m_GrabInteractable != null && gripAttachPoint != null)
        {
            gripAttachPoint.localRotation = BuildAttachLocalRotation(
                bladeAxis,
                edgeAxis,
                controllerBladeAxis,
                controllerEdgeAxis);
            m_GrabInteractable.attachTransform = gripAttachPoint;
        }

        if (m_GrabInteractable == null || !applyRecommendedGrabSettings)
            return;

        m_GrabInteractable.useDynamicAttach = false;
        m_GrabInteractable.matchAttachPosition = false;
        m_GrabInteractable.matchAttachRotation = false;
        m_GrabInteractable.attachEaseInTime = 0f;
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (m_GrabInteractable == null || gripAttachPoint == null)
            return;

        var interactorAttach = args.interactorObject.GetAttachTransform(m_GrabInteractable);
        if (interactorAttach == null)
            return;

        var targetRotation = interactorAttach.rotation * Quaternion.Inverse(gripAttachPoint.localRotation);
        var targetPosition = interactorAttach.position - (targetRotation * gripAttachPoint.localPosition);

        if (m_Rigidbody != null)
        {
            m_Rigidbody.position = targetPosition;
            m_Rigidbody.rotation = targetRotation;
            m_Rigidbody.linearVelocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
        }
    }

    /// <summary>
    /// Returns the local pose the sword should use when it is parented directly
    /// under a controller hand transform instead of being grabbed through XRI.
    /// </summary>
    public bool TryGetHandLocalPose(out Vector3 localPosition, out Quaternion localRotation)
    {
        CacheComponents();

        if (gripAttachPoint == null && m_GrabInteractable != null)
            gripAttachPoint = m_GrabInteractable.attachTransform;

        if (gripAttachPoint == null)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            return false;
        }

        // Make the grip attach point line up with the hand origin/rotation.
        localRotation = Quaternion.Inverse(gripAttachPoint.localRotation);
        localPosition = -(localRotation * gripAttachPoint.localPosition);
        return true;
    }

    void CacheComponents()
    {
        if (m_GrabInteractable == null)
            m_GrabInteractable = GetComponent<XRGrabInteractable>();

        if (m_Rigidbody == null)
            m_Rigidbody = GetComponent<Rigidbody>();
    }

    static Quaternion BuildAttachLocalRotation(
        Vector3 swordBladeAxis,
        Vector3 swordEdgeAxis,
        Vector3 targetBladeAxis,
        Vector3 targetEdgeAxis)
    {
        var swordReference = BuildReferenceRotation(swordBladeAxis, swordEdgeAxis);
        var controllerReference = BuildReferenceRotation(targetBladeAxis, targetEdgeAxis);
        return swordReference * Quaternion.Inverse(controllerReference);
    }

    static Quaternion BuildReferenceRotation(Vector3 upAxis, Vector3 forwardAxis)
    {
        var up = NormalizeOrDefault(upAxis, Vector3.up);
        var forward = Vector3.ProjectOnPlane(forwardAxis, up);

        if (forward.sqrMagnitude < 0.0001f)
        {
            var fallback = Mathf.Abs(Vector3.Dot(up, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
            forward = Vector3.Cross(fallback, up);
        }

        forward.Normalize();
        return Quaternion.LookRotation(forward, up);
    }

    static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback)
    {
        if (value.sqrMagnitude < 0.0001f)
            return fallback;

        return value.normalized;
    }
}
