using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class VRHandEquipmentActivator : MonoBehaviour
{
    [Header("Hand Transforms")]
    [Tooltip("Transform for the left controller/hand. The shield will be parented here.")]
    [SerializeField]
    Transform leftHandTransform;

    [Tooltip("Transform for the right controller/hand. The sword will be parented here.")]
    [SerializeField]
    Transform rightHandTransform;

    [Header("Input Actions")]
    [Tooltip("Optional. Bind this to a left-hand-only trigger button action. If left empty, the script creates one automatically.")]
    [SerializeField]
    [FormerlySerializedAs("leftPrimaryButtonAction")]
    InputActionReference leftTriggerButtonAction;

    [Tooltip("Optional. Bind this to a right-hand-only trigger button action. If left empty, the script creates one automatically.")]
    [SerializeField]
    [FormerlySerializedAs("rightPrimaryButtonAction")]
    InputActionReference rightTriggerButtonAction;

    [Header("Shield")]
    [Tooltip("Existing shield object in the scene. If this is empty, Shield Prefab will be instantiated once and reused.")]
    [SerializeField]
    GameObject shieldObject;

    [Tooltip("Optional prefab used only when no scene shield object is assigned.")]
    [SerializeField]
    GameObject shieldPrefab;

    [Tooltip("Shield position relative to the left hand.")]
    [SerializeField]
    Vector3 shieldLocalPosition;

    [Tooltip("Shield rotation relative to the left hand.")]
    [SerializeField]
    Vector3 shieldLocalEulerAngles;

    [Header("Sword")]
    [Tooltip("Existing sword object in the scene. If this is empty, Sword Prefab will be instantiated once and reused.")]
    [SerializeField]
    GameObject swordObject;

    [Tooltip("Optional prefab used only when no scene sword object is assigned.")]
    [SerializeField]
    GameObject swordPrefab;

    [Tooltip("Sword position relative to the right hand.")]
    [SerializeField]
    Vector3 swordLocalPosition;

    [Tooltip("Sword rotation relative to the right hand.")]
    [SerializeField]
    Vector3 swordLocalEulerAngles;

    InputAction m_LeftResolvedAction;
    InputAction m_RightResolvedAction;
    bool m_LeftActionEnabledByThisScript;
    bool m_RightActionEnabledByThisScript;
    bool m_LeftActionCreatedAtRuntime;
    bool m_RightActionCreatedAtRuntime;

    void Awake()
    {
        shieldObject = PrepareItem(
            shieldObject,
            shieldPrefab,
            leftHandTransform,
            shieldLocalPosition,
            shieldLocalEulerAngles,
            "Shield");

        swordObject = PrepareItem(
            swordObject,
            swordPrefab,
            rightHandTransform,
            swordLocalPosition,
            swordLocalEulerAngles,
            "Sword");

        SetItemVisible(shieldObject, false);
        SetItemVisible(swordObject, false);
    }

    void OnEnable()
    {
        ResolveInputActions();

        SubscribeAction(m_LeftResolvedAction, OnLeftTriggerButtonChanged, ref m_LeftActionEnabledByThisScript);
        SubscribeAction(m_RightResolvedAction, OnRightTriggerButtonChanged, ref m_RightActionEnabledByThisScript);

        RefreshCurrentState();
    }

    void OnDisable()
    {
        UnsubscribeAction(m_LeftResolvedAction, OnLeftTriggerButtonChanged, m_LeftActionEnabledByThisScript);
        UnsubscribeAction(m_RightResolvedAction, OnRightTriggerButtonChanged, m_RightActionEnabledByThisScript);

        m_LeftActionEnabledByThisScript = false;
        m_RightActionEnabledByThisScript = false;

        DisposeRuntimeActions();

        // Always hide both items when this manager is disabled so state stays predictable.
        SetItemVisible(shieldObject, false);
        SetItemVisible(swordObject, false);
    }

    void LateUpdate()
    {
        // Keep the equipment snapped to the hands every frame in case the hand hierarchy is animated or re-centered.
        SnapItemToHand(shieldObject, leftHandTransform, shieldLocalPosition, shieldLocalEulerAngles);
        SnapItemToHand(swordObject, rightHandTransform, swordLocalPosition, swordLocalEulerAngles);
    }

    void OnLeftTriggerButtonChanged(InputAction.CallbackContext context)
    {
        // Performed means the button is being held, canceled means it was released.
        SetItemVisible(shieldObject, context.ReadValueAsButton());
    }

    void OnRightTriggerButtonChanged(InputAction.CallbackContext context)
    {
        // Performed means the button is being held, canceled means it was released.
        SetItemVisible(swordObject, context.ReadValueAsButton());
    }

    void RefreshCurrentState()
    {
        var leftPressed = m_LeftResolvedAction != null && m_LeftResolvedAction.IsPressed();
        var rightPressed = m_RightResolvedAction != null && m_RightResolvedAction.IsPressed();

        SetItemVisible(shieldObject, leftPressed);
        SetItemVisible(swordObject, rightPressed);
    }

    void ResolveInputActions()
    {
        DisposeRuntimeActions();

        m_LeftResolvedAction = ResolveAction(
            leftTriggerButtonAction,
            "<XRController>{LeftHand}/{TriggerButton}",
            "Left Trigger Button");

        m_RightResolvedAction = ResolveAction(
            rightTriggerButtonAction,
            "<XRController>{RightHand}/{TriggerButton}",
            "Right Trigger Button");

        // If both inspector fields point to the same action, both hands will react together.
        // Fall back to explicit per-hand bindings so the setup still behaves correctly.
        if (m_LeftResolvedAction != null && m_LeftResolvedAction == m_RightResolvedAction)
        {
            Debug.LogWarning(
                $"{nameof(VRHandEquipmentActivator)} on {name} has the same Input Action assigned to both hands. " +
                "Using built-in left/right trigger button bindings instead.",
                this);

            DisposeRuntimeActions();

            m_LeftResolvedAction = CreateRuntimeAction("<XRController>{LeftHand}/{TriggerButton}", "Left Trigger Button");
            m_RightResolvedAction = CreateRuntimeAction("<XRController>{RightHand}/{TriggerButton}", "Right Trigger Button");
            m_LeftActionCreatedAtRuntime = true;
            m_RightActionCreatedAtRuntime = true;
        }
    }

    InputAction ResolveAction(InputActionReference actionReference, string bindingPath, string actionName)
    {
        if (actionReference != null && actionReference.action != null)
            return actionReference.action;

        var runtimeAction = CreateRuntimeAction(bindingPath, actionName);
        if (bindingPath.Contains("{LeftHand}"))
            m_LeftActionCreatedAtRuntime = true;
        else
            m_RightActionCreatedAtRuntime = true;

        return runtimeAction;
    }

    static InputAction CreateRuntimeAction(string bindingPath, string actionName)
    {
        return new InputAction(
            name: actionName,
            type: InputActionType.Button,
            binding: bindingPath);
    }

    void DisposeRuntimeActions()
    {
        if (m_LeftActionCreatedAtRuntime && m_LeftResolvedAction != null)
        {
            m_LeftResolvedAction.Dispose();
            m_LeftResolvedAction = null;
            m_LeftActionCreatedAtRuntime = false;
        }

        if (m_RightActionCreatedAtRuntime && m_RightResolvedAction != null)
        {
            m_RightResolvedAction.Dispose();
            m_RightResolvedAction = null;
            m_RightActionCreatedAtRuntime = false;
        }

        if (!m_LeftActionCreatedAtRuntime)
            m_LeftResolvedAction = leftTriggerButtonAction != null ? leftTriggerButtonAction.action : null;

        if (!m_RightActionCreatedAtRuntime)
            m_RightResolvedAction = rightTriggerButtonAction != null ? rightTriggerButtonAction.action : null;
    }

    static void SubscribeAction(
        InputAction action,
        System.Action<InputAction.CallbackContext> callback,
        ref bool enabledByThisScript)
    {
        if (action == null)
            return;

        if (!action.enabled)
        {
            action.Enable();
            enabledByThisScript = true;
        }

        action.performed += callback;
        action.canceled += callback;
    }

    static void UnsubscribeAction(
        InputAction action,
        System.Action<InputAction.CallbackContext> callback,
        bool disableIfEnabledHere)
    {
        if (action == null)
            return;

        action.performed -= callback;
        action.canceled -= callback;

        if (disableIfEnabledHere)
            action.Disable();
    }

    GameObject PrepareItem(
        GameObject existingObject,
        GameObject prefab,
        Transform handTransform,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        string itemName)
    {
        if (handTransform == null)
        {
            Debug.LogWarning($"{nameof(VRHandEquipmentActivator)} on {name} is missing the hand transform for {itemName}.", this);
            return existingObject;
        }

        var item = existingObject;

        // Only instantiate once if no scene object has been assigned.
        if (item == null && prefab != null)
            item = Instantiate(prefab, handTransform);

        if (item == null)
        {
            Debug.LogWarning($"{nameof(VRHandEquipmentActivator)} on {name} has no {itemName} object assigned.", this);
            return null;
        }

        SnapItemToHand(item, handTransform, localPosition, localEulerAngles);
        ConfigurePhysicsForAttachment(item);
        return item;
    }

    static void SnapItemToHand(
        GameObject item,
        Transform handTransform,
        Vector3 localPosition,
        Vector3 localEulerAngles)
    {
        if (item == null || handTransform == null)
            return;

        if (item.transform.parent != handTransform)
            item.transform.SetParent(handTransform, false);

        // If the sword has a grip-fix component, use its calibrated grip pose so
        // the weapon appears as if it was grabbed normally.
        if (item.TryGetComponent<swordfix>(out var gripFix) && gripFix.TryGetHandLocalPose(out var gripLocalPosition, out var gripLocalRotation))
        {
            item.transform.localPosition = gripLocalPosition;
            item.transform.localRotation = gripLocalRotation;
            return;
        }

        item.transform.localPosition = localPosition;
        item.transform.localRotation = Quaternion.Euler(localEulerAngles);
    }

    static void SetItemVisible(GameObject item, bool isVisible)
    {
        if (item != null && item.activeSelf != isVisible)
            item.SetActive(isVisible);
    }

    static void ConfigurePhysicsForAttachment(GameObject item)
    {
        if (item == null || !item.TryGetComponent<Rigidbody>(out var rb))
            return;

        // Kinematic rigidbodies stay attached to the controller transform instead of fighting physics.
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}
