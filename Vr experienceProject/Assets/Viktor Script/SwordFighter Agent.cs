using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SwordFighterAgent : Agent
{
    [Header("Physics Setup")]
    public ConfigurableJoint swordJoint;
    public Rigidbody swordRb;

    [Header("Targets")]
    public Transform playerTransform;
    public Transform playerSwordTransform;

    [Header("Stats")]
    public Health myHealth;
    public Health playerHealth;

    private Quaternion initialRotation;

    public override void Initialize()
    {
        // Sla de begin-rotatie op van de joint
        initialRotation = swordJoint.transform.localRotation;

        // Koppel de Health events
        myHealth.OnDeath.AddListener(() =>
        {
            AddReward(-1.0f);
            EndEpisode();
        });

        playerHealth.OnDeath.AddListener(() =>
        {
            AddReward(1.0f);
            EndEpisode();
        });
    }

    public override void OnEpisodeBegin()
    {
        // Reset gezondheid en posities aan het begin van elk potje
        myHealth.ResetHealth();
        playerHealth.ResetHealth();

        // Zet het zwaard stil
        swordRb.linearVelocity = Vector3.zero;
        swordRb.angularVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Eigen positie en rotatie van het zwaard (3 + 4 floats)
        sensor.AddObservation(swordJoint.transform.localPosition);
        sensor.AddObservation(swordJoint.transform.localRotation);

        // 2. Positie van de VR-speler (3 floats)
        sensor.AddObservation(transform.InverseTransformPoint(playerTransform.position));

        // 3. Positie van het zwaard van de speler (3 floats)
        // Cruciaal voor pareren!
        sensor.AddObservation(transform.InverseTransformPoint(playerSwordTransform.position));

        // 4. Eigen HP en Speler HP (2 floats)
        sensor.AddObservation(myHealth.currentHealth / 100f);
        sensor.AddObservation(playerHealth.currentHealth / 100f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // De AI stuurt 3 getallen aan voor de rotatie van het zwaard
        float rotX = actions.ContinuousActions[0] * 180f;
        float rotY = actions.ContinuousActions[1] * 180f;
        float rotZ = actions.ContinuousActions[2] * 180f;

        // Pas de rotatie aan op de Configurable Joint
        swordJoint.targetRotation = Quaternion.Euler(rotX, rotY, rotZ);

        // Kleine straf per stap om de AI te dwingen sneller te winnen
        AddReward(-0.0005f);

        // Bonus beloning: als het zwaard van de agent dichtbij de speler is
        float dist = Vector3.Distance(swordRb.position, playerTransform.position);
        if (dist < 1.5f)
        {
            AddReward(0.001f); // "Aanmoediging" om dichtbij te blijven
        }
    }

    // Voor het testen met je toetsenbord (zonder VR of Training)
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical"); // W/S
        continuousActionsOut[1] = Input.GetAxis("Horizontal"); // A/D
        continuousActionsOut[2] = Input.GetKey(KeyCode.Q) ? 1f : 0f;
    }
}