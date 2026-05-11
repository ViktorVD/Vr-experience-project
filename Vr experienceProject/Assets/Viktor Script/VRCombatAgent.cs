using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

public class VRCombatAgent : Agent
{
    [Header("Targets")]
    public Transform swordTarget;
    public Transform shieldTarget;
    
    [Header("Physics & Refs")]
    public VRPhysicalWeapon physicalSword;
    public VRPhysicalWeapon physicalShield;
    public VRCombatAgent opponent;
    public Rigidbody agentRb;
    public Health myHealth;
    
    [Header("Settings")]
    public float moveSpeed = 2.2f;
    public bool isAttacking = false;
    public bool isBlocking = false;
    private int currentBlockDir = 0; 
    
    private float animTimer = 0f;
    private int currentAttackType = 0; 
    private int lastAttackType = 0;
    private bool hitRegisteredThisAttack = false;
    
    [HideInInspector] public bool isStunned = false;
    [HideInInspector] public bool isCombatStunned = false;
    private float stunTimer = 0f;

    // Cooldown tussen acties (Anti-spam)
    private float globalActionCooldown = 0f;

    private Vector3 swordIdlePos = new Vector3(0.5f, 0.9f, 0.6f);
    private Vector3 shieldIdlePos = new Vector3(-0.5f, 0.9f, 0.6f);
    private Vector3 initialPos;
    private Quaternion initialRot;

    private bool attack1Req = false;
    private bool attack2Req = false;
    private bool attack3Req = false;

    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
        initialPos = transform.localPosition;
        initialRot = transform.localRotation;
    }

    void Update()
    {
        if (Academy.Instance.IsCommunicatorOn) return; 
        var ms = Mouse.current;
        var kb = Keyboard.current;
        if (ms != null) {
            if (ms.leftButton.wasPressedThisFrame) attack1Req = true;
            if (ms.rightButton.wasPressedThisFrame) attack2Req = true;
        }
        if (kb != null && kb.qKey.wasPressedThisFrame) attack3Req = true;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = initialPos;
        transform.localRotation = initialRot;
        Vector3 dir = (opponent.transform.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

        if (agentRb != null) {
            agentRb.linearVelocity = Vector3.zero;
            agentRb.angularVelocity = Vector3.zero;
        }
        
        isStunned = false;
        isCombatStunned = false;
        stunTimer = 0f;
        globalActionCooldown = 0f;
        isAttacking = false;
        isBlocking = false;
        currentBlockDir = 0;
        animTimer = 0f;
        
        swordTarget.localPosition = swordIdlePos;
        shieldTarget.localPosition = shieldIdlePos;
        physicalSword.ResetWeapon();
        physicalShield.ResetWeapon();
        if(myHealth != null) myHealth.ResetHealth();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float dist = Vector3.Distance(transform.position, opponent.transform.position);
        sensor.AddObservation(dist / 10f); 
        sensor.AddObservation(transform.InverseTransformDirection((opponent.transform.position - transform.position).normalized));
        sensor.AddObservation(transform.InverseTransformPoint(opponent.physicalSword.transform.position));
        sensor.AddObservation(transform.InverseTransformPoint(opponent.physicalShield.transform.position));
        
        sensor.AddObservation(opponent.isAttacking ? 1f : 0f);
        sensor.AddObservation(opponent.isBlocking ? 1f : 0f);
        sensor.AddObservation((float)opponent.currentBlockDir / 3f);
        
        sensor.AddObservation(isAttacking ? 1f : 0f);
        sensor.AddObservation(isBlocking ? 1f : 0f);
        sensor.AddObservation(globalActionCooldown > 0 ? 1f : 0f);
        
        sensor.AddObservation(myHealth.currentHealth / 100f);
        sensor.AddObservation(opponent.myHealth.currentHealth / 100f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (myHealth.currentHealth <= 0) return;

        // Timers updaten
        if (stunTimer > 0)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0) { isStunned = false; isCombatStunned = false; }
        }
        if (globalActionCooldown > 0) globalActionCooldown -= Time.fixedDeltaTime;

        if (isStunned) return;

        int moveIdx = actions.DiscreteActions[0];
        int attackIdx = actions.DiscreteActions[1];
        int blockIdx = actions.DiscreteActions[2];
        int rotIdx = actions.DiscreteActions[3];

        float speed = moveSpeed;
        if (isAttacking) speed = 0f; 
        if (blockIdx > 0) speed *= 0.4f;

        Vector3 moveDir = Vector3.zero;
        if (moveIdx == 1) moveDir = transform.forward;
        else if (moveIdx == 2) moveDir = -transform.forward;
        else if (moveIdx == 3) moveDir = -transform.right;
        else if (moveIdx == 4) moveDir = transform.right;

        if (agentRb != null && !agentRb.isKinematic) agentRb.linearVelocity = moveDir * speed;
        else transform.position += moveDir * speed * Time.fixedDeltaTime;

        if (rotIdx == 1) transform.Rotate(Vector3.up, -140f * Time.fixedDeltaTime);
        else if (rotIdx == 2) transform.Rotate(Vector3.up, 140f * Time.fixedDeltaTime);

        // ACTIE LOGICA (Slaan of Blokkeren)
        bool canAction = !isCombatStunned && !isAttacking && globalActionCooldown <= 0;

        if (canAction)
        {
            // 2. BLOKKEREN
            currentBlockDir = blockIdx;
            isBlocking = (blockIdx > 0);
            
            Vector3 blockPos = shieldIdlePos;
            if (blockIdx == 1) blockPos = new Vector3(0f, 1.2f, 0.7f); 
            else if (blockIdx == 2) blockPos = new Vector3(-0.6f, 1.2f, 0.6f); 
            else if (blockIdx == 3) blockPos = new Vector3(0.6f, 1.2f, 0.6f); 
            
            if (isBlocking) {
                AddReward(-0.001f);
                shieldTarget.localPosition = Vector3.MoveTowards(shieldTarget.localPosition, blockPos, Time.fixedDeltaTime * 15f);
            } else {
                shieldTarget.localPosition = Vector3.MoveTowards(shieldTarget.localPosition, shieldIdlePos, Time.fixedDeltaTime * 20f);
            }

            // 3. AANVALLEN (Alleen als we NIET blokkeren)
            if (!isBlocking && attackIdx > 0)
            {
                float dist = Vector3.Distance(transform.position, opponent.transform.position);
                if (dist < 4.0f) 
                {
                    if (attackIdx == lastAttackType) AddReward(-0.02f);
                    else AddReward(0.01f);
                    
                    lastAttackType = attackIdx;
                    currentAttackType = attackIdx;
                    isAttacking = true;
                    animTimer = 0f;
                    hitRegisteredThisAttack = false;
                }
            }
        }
        else
        {
            // Geen actie mogelijk (Stun of Cooldown of al bezig)
            if (!isAttacking) {
                isBlocking = false;
                currentBlockDir = 0;
                shieldTarget.localPosition = Vector3.MoveTowards(shieldTarget.localPosition, shieldIdlePos, Time.fixedDeltaTime * 20f);
            }
        }

        UpdateTacticalAnimations();

        // --- TACTISCHE REWARDS (De Sweet Spot) ---
        float currentDist = Vector3.Distance(transform.position, opponent.transform.position);
        
        if (currentDist < 0.8f) 
        {
            AddReward(-0.005f); // Straf voor 'face-hugging'
        }
        else if (currentDist >= 1.2f && currentDist <= 2.3f)
        {
            AddReward(0.004f); // Sweet Spot: Nu goed afgestemd op de zwaard-lengte
        }
        else if (currentDist > 6f)
        {
            AddReward(-0.01f); 
        }

        // --- EVADE REWARD ---
        if (opponent.isAttacking && moveIdx == 2)
        {
            AddReward(0.005f); 
        }

        Vector3 toOpponent = (opponent.transform.position - transform.position).normalized;
        if (Vector3.Dot(transform.forward, toOpponent) > 0.85f) AddReward(0.005f);
        
        if (StepCount > 5000) { EndEpisode(); opponent.EndEpisode(); }
        if (myHealth.currentHealth <= 0) {
            AddReward(-1.0f); opponent.AddReward(1.0f);
            EndEpisode(); opponent.EndEpisode();
        }
    }

    void UpdateTacticalAnimations()
    {
        if (!isAttacking)
        {
            swordTarget.localPosition = Vector3.MoveTowards(swordTarget.localPosition, swordIdlePos, Time.fixedDeltaTime * 20f);
            return;
        }

        animTimer += Time.fixedDeltaTime;
        float duration = 0.7f; 
        float t = animTimer / duration;

        if (currentAttackType == 1) // OVERHEAD
        {
            float y = (t < 0.6f) ? Mathf.Lerp(0.9f, 2.1f, t/0.6f) : Mathf.Lerp(2.1f, 0.4f, (t-0.6f)/0.4f);
            float z = (t < 0.6f) ? Mathf.Lerp(0.6f, 0.3f, t/0.6f) : Mathf.Lerp(0.3f, 2.0f, (t-0.6f)/0.4f); // Naar 2.0m
            swordTarget.localPosition = new Vector3(0.1f, y, z);
            Debug.Log("Overhead Pos: " + swordTarget.localPosition);
        }
        else if (currentAttackType == 2) // SIDE SWING
        {
            float x = (t < 0.5f) ? Mathf.Lerp(0.5f, 1.2f, t/0.5f) : Mathf.Lerp(1.2f, -1.2f, (t-0.5f)/0.5f);
            float z = (t < 0.5f) ? Mathf.Lerp(0.6f, 0.4f, t/0.5f) : Mathf.Sin((t-0.5f) * Mathf.PI) * 1.0f + 1.0f; // Diepere boog
            swordTarget.localPosition = new Vector3(x, 1.1f, z + 0.4f); // Meer reach
            Debug.Log("Side Swing Pos: " + swordTarget.localPosition);
        }
        else if (currentAttackType == 3) // STAB
        {
            float z = (t < 0.4f) ? Mathf.Lerp(0.6f, 0.1f, t/0.4f) : Mathf.Lerp(0.1f, 2.6f, (t-0.4f)/0.6f); // Naar 2.6m
            swordTarget.localPosition = new Vector3(0.2f, 1.0f, z);
            Debug.Log("Stab Pos: " + swordTarget.localPosition);
        }

        if (t >= 1.0f)
        {
            if (!hitRegisteredThisAttack) AddReward(-0.02f); // Lagere straf voor missen
            isAttacking = false;
            globalActionCooldown = 0.7f; // Wacht langer na een zwaai
        }
    }

    public void RegisterHitOnOpponent(float damage)
    {
        hitRegisteredThisAttack = true;
        AddReward(0.8f); 
        if (opponent.myHealth != null) 
        {
            opponent.myHealth.TakeDamage(damage);
            if (opponent.myHealth.currentHealth <= 0)
            {
                AddReward(1.0f);
                opponent.AddReward(-1.0f);
                EndEpisode();
                opponent.EndEpisode();
            }
        }
        isAttacking = false;
        globalActionCooldown = 0.7f; 
    }

    public void ApplyCombatStun(float duration)
    {
        isCombatStunned = true;
        stunTimer = duration;
        isAttacking = false;
        isBlocking = false;
        globalActionCooldown = duration;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (kb == null || ms == null) return;
        if (kb.wKey.isPressed) d[0] = 1;
        else if (kb.sKey.isPressed) d[0] = 2;
        if (attack1Req) { d[1] = 1; attack1Req = false; }
        else if (attack2Req) { d[1] = 2; attack2Req = false; }
        else if (attack3Req) { d[1] = 3; attack3Req = false; }
        if (kb.spaceKey.isPressed) d[2] = 1; 
        else if (kb.xKey.isPressed) d[2] = 2; 
        else if (kb.cKey.isPressed) d[2] = 3; 
        if (kb.aKey.isPressed) d[3] = 1;
        else if (kb.dKey.isPressed) d[3] = 2;
    }
}
