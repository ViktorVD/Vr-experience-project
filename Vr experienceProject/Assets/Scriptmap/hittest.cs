using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class hittest : MonoBehaviour
{
    [Header("Haptics")]
    [Tooltip("Assign the HapticImpulsePlayer on the hand/controller that should vibrate.")]
    [SerializeField] private HapticImpulsePlayer hapticImpulsePlayer;

    [SerializeField, Range(0f, 1f)]
    private float hapticAmplitude = 0.5f;

    [SerializeField]
    private float hapticDuration = 0.08f;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("raakt, binnen box");

        if (hapticImpulsePlayer != null)
            hapticImpulsePlayer.SendHapticImpulse(hapticAmplitude, hapticDuration);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("raakt, buiten box");

    }
}
