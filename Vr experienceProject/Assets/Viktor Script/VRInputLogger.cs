using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRInputLogger : MonoBehaviour
{
    // Houdt bij welke knop vorige frame was ingedrukt om spam in de console te voorkomen
    private Dictionary<InputDevice, Dictionary<InputFeatureUsage<bool>, bool>> previousStates = new Dictionary<InputDevice, Dictionary<InputFeatureUsage<bool>, bool>>();
    
    private List<InputFeatureUsage<bool>> featuresToTest;

    void Start()
    {
        // Lijst met alle standaard VR knoppen die we willen controleren
        featuresToTest = new List<InputFeatureUsage<bool>>
        {
            CommonUsages.triggerButton,
            CommonUsages.gripButton,
            CommonUsages.primaryButton,
            CommonUsages.secondaryButton,
            CommonUsages.menuButton,
            CommonUsages.primary2DAxisClick // Joystick klik
        };
    }

    void Update()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        foreach (var device in devices)
        {
            if (!previousStates.ContainsKey(device))
            {
                previousStates[device] = new Dictionary<InputFeatureUsage<bool>, bool>();
                foreach (var feature in featuresToTest)
                {
                    previousStates[device][feature] = false;
                }
            }

            foreach (var feature in featuresToTest)
            {
                if (device.TryGetFeatureValue(feature, out bool currentValue))
                {
                    bool previousValue = previousStates[device][feature];
                    
                    // Alleen loggen op het moment dat je hem indrukt (niet ingedrukt houden)
                    if (currentValue && !previousValue)
                    {
                        Debug.Log($"[VR LOGGER] Controller: {device.name} -> KNOP: {feature.name} ingedrukt!");
                    }
                    
                    previousStates[device][feature] = currentValue;
                }
            }
        }
    }
}
