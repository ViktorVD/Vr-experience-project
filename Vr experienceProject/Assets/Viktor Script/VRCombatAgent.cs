using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class VRCombatAgent : Agent
{
    [Header("Targets (AI bestuurt deze)")]
    public Transform swordTarget;
    public Transform shieldTarget;
    
    [Header("Fysieke Wapens")]
    public VRPhysicalWeapon physicalSword;
    public VRPhysicalWeapon physicalShield;

    [Header("Tegenstander & Referenties")]
    public VRCombatAgent opponent;
    public Rigidbody agentRb;
    public Health myHealth;
    
    [Header("Instellingen")]
    public float moveSpeed = 4f;
    public float reachRadius = 1.2f; 
    
    private Vector3 startPos;
    private Quaternion startRot;
    
    [HideInInspector] public bool isStunned = false;
    private float stunTimer = 0f;

    public override void Initialize()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
        
        if (myHealth != null)
        {
            myHealth.OnDeath.AddListener(() =>
            {
                AddReward(-1.0f);
                opponent.AddReward(1.0f);
                EndEpisode();
                opponent.EndEpisode();
            });
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset Agent
        transform.localPosition = startPos;
        transform.localRotation = startRot;
        agentRb.linearVelocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
        
        isStunned = false;
        stunTimer = 0f;
        
        // Reset Targets relatief aan body
        swordTarget.localPosition = new Vector3(0.4f, 1f, 0.5f);
        shieldTarget.localPosition = new Vector3(-0.4f, 1f, 0.5f);
        
        // Reset Fysica
        physicalSword.ResetWeapon();
        physicalShield.ResetWeapon();
        
        if(myHealth != null) myHealth.ResetHealth();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Relatieve positie en rotatie van tegenstander (7)
        Vector3 dirToOpponent = opponent.transform.position - transform.position;
        sensor.AddObservation(transform.InverseTransformDirection(dirToOpponent));
        sensor.AddObservation(Quaternion.Inverse(transform.rotation) * opponent.transform.rotation);

        // 2. Eigen wapen targets (posities relatief tot eigen body) (6)
        sensor.AddObservation(swordTarget.localPosition);
        sensor.AddObservation(shieldTarget.localPosition);

        // 3. Tegenstander wapen posities (relatief) (6)
        sensor.AddObservation(transform.InverseTransformPoint(opponent.physicalSword.transform.position));
        sensor.AddObservation(transform.InverseTransformPoint(opponent.physicalShield.transform.position));

        // 4. Stun status & Health (2)
        sensor.AddObservation(isStunned ? 1f : 0f);
        if(myHealth != null) sensor.AddObservation(myHealth.currentHealth / 100f);
        else sensor.AddObservation(1f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isStunned)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0) isStunned = false;
            return; 
        }

        var continuousActions = actions.ContinuousActions;

        // --- 1. Locomotion (3D) ---
        // Actie 0: Forward/Backward, Actie 1: Strafe Left/Right
        Vector3 moveDir = transform.forward * continuousActions[0] + transform.right * continuousActions[1];
        agentRb.linearVelocity = moveDir * moveSpeed;
        
        // Actie 2: Rotatie (om as)
        transform.Rotate(Vector3.up, continuousActions[2] * 150f * Time.fixedDeltaTime);

        // --- 2. Wapen Controle ---
        // Zwaard: Actie 3,4,5 (X,Y,Z offset)
        Vector3 swordInput = new Vector3(continuousActions[3], continuousActions[4], continuousActions[5]);
        swordTarget.localPosition = Vector3.Lerp(swordTarget.localPosition, swordTarget.localPosition + swordInput, Time.fixedDeltaTime * 10f);
        swordTarget.localPosition = Vector3.ClampMagnitude(swordTarget.localPosition, reachRadius);

        // Schild: Actie 6,7,8 (X,Y,Z offset)
        Vector3 shieldInput = new Vector3(continuousActions[6], continuousActions[7], continuousActions[8]);
        shieldTarget.localPosition = Vector3.Lerp(shieldTarget.localPosition, shieldTarget.localPosition + shieldInput, Time.fixedDeltaTime * 10f);
        shieldTarget.localPosition = Vector3.ClampMagnitude(shieldTarget.localPosition, reachRadius);

        // --- 3. Rewards ---
        // Kleine straf voor tijd (stimuleert snelle combat)
        AddReward(-0.0005f);
    }

    public void RegisterHitOnOpponent(float damage)
    {
        AddReward(0.3f); // Beloning voor rake klap
        if (opponent.myHealth != null) opponent.myHealth.TakeDamage(damage);
    }

    public void RegisterDamageReceived(float damage)
    {
        AddReward(-0.1f); // Straf voor geraakt worden
        ApplyStun(0.5f);
    }

    public void RegisterSuccessfulBlock()
    {
        AddReward(0.2f); // Beloning voor goed verdedigen
    }

    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
        agentRb.linearVelocity = Vector3.zero;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
        continuousActionsOut[2] = Input.GetAxis("Mouse X");
    }
}
