using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class gladiator : Agent
{
    [Header("References")]
    public gladiator opponent;
    public GladiatorArena arena;
    public Rigidbody agentRb;

    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float turnSpeed = 540f;

    [Header("Combat")]
    public float maxHealth = 100f;
    public float attackDamage = 20f;
    public float attackRange = 1.8f;
    [Range(-1f, 1f)] public float attackFacingDot = 0.55f;
    [Range(-1f, 1f)] public float defendFacingDot = 0.10f;
    public float attackWindup = 0.15f;
    public float attackRecover = 0.35f;
    public float attackCooldown = 0.70f;
    public float defendDuration = 0.30f;
    public float defendCooldown = 0.60f;

    [Header("Visual Feedback")]
    public Renderer[] visualRenderers;
    public Color attackTint = Color.green;
    public Color defendTint = Color.blue;
    public Color hitTint = Color.red;
    [Min(0f)] public float hitFlashDuration = 0.18f;
    [Min(0f)] public float visualBlendSpeed = 18f;

    [Header("Episode")]
    public float episodeLengthSeconds = 25f;
    public float observationScale = 6f;

    [Header("Reward Shaping")]
    public float stepPenalty = -0.0010f;
    public float movementEnergyPenalty = -0.0002f;
    public float closeDistanceReward = 0.0010f;
    public float driftAwayPenalty = -0.0007f;
    public float preferredRangeReward = 0.0015f;
    public float facingReward = 0.0010f;
    public float missedAttackPenalty = -0.0200f;
    public float blockedAttackPenalty = -0.0100f;
    public float successfulHitReward = 0.3500f;
    public float successfulBlockReward = 0.1500f;
    public float hitPenalty = -0.2500f;
    public float winReward = 1.0000f;
    public float losePenalty = -1.0000f;

    private Vector3 _fallbackStartPosition;
    private Quaternion _fallbackStartRotation;
    private float _currentHealth;
    private float _attackTimer;
    private float _attackCooldownTimer;
    private float _defendTimer;
    private float _defendCooldownTimer;
    private float _episodeTimer;
    private float _lastDistanceToOpponent;
    private float _hitFlashTimer;
    private bool _attackResolved;
    private bool _isAttacking;
    private bool _isDefending;
    private Vector3 _lastMoveDirection;
    private MaterialPropertyBlock _propertyBlock;
    private Color _currentTint = Color.white;

    public bool IsAttacking => _isAttacking;
    public bool IsDefending => _isDefending;
    public bool IsMoving => _lastMoveDirection.sqrMagnitude > 0.0001f;
    public bool IsIdle => !_isAttacking && !_isDefending && !IsMoving;
    public float HealthNormalized => maxHealth <= 0f ? 0f : _currentHealth / maxHealth;

    public override void Initialize()
    {
        CacheVisuals();

        if (agentRb == null)
        {
            agentRb = GetComponent<Rigidbody>();
        }

        _fallbackStartPosition = transform.position;
        _fallbackStartRotation = transform.rotation;
        _currentHealth = maxHealth;
        _lastDistanceToOpponent = GetPlanarDistanceToOpponent();
        _currentTint = Color.white;
        ClearVisualTint();
    }

    public override void OnEpisodeBegin()
    {
        ResetAgentPose();
        ResetCombatState();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (opponent == null)
        {
            for (int i = 0; i < 19; i++)
            {
                sensor.AddObservation(0f);
            }
            return;
        }

        Vector3 ownArenaPosition = GetArenaRelativePosition(transform.position);
        Vector3 opponentArenaPosition = GetArenaRelativePosition(opponent.transform.position);
        Vector3 toOpponent = GetPlanarDirectionToOpponent();
        float distanceToOpponent = toOpponent.magnitude;
        Vector3 directionToOpponent = distanceToOpponent > 0.001f ? toOpponent / distanceToOpponent : Vector3.zero;
        Vector3 localDirectionToOpponent = transform.InverseTransformDirection(directionToOpponent);
        float arenaRadius = GetArenaRadius();

        // Flat arena positions (x,z) normalized to arena radius.
        sensor.AddObservation(ownArenaPosition.x / arenaRadius);
        sensor.AddObservation(ownArenaPosition.z / arenaRadius);
        sensor.AddObservation(opponentArenaPosition.x / arenaRadius);
        sensor.AddObservation(opponentArenaPosition.z / arenaRadius);

        // Relative direction and distance to the opponent.
        sensor.AddObservation(localDirectionToOpponent.x);
        sensor.AddObservation(localDirectionToOpponent.z);
        sensor.AddObservation(Mathf.Clamp01(distanceToOpponent / (attackRange * 3f)));

        // Opponent state requested in the prompt.
        sensor.AddObservation(opponent.IsAttacking ? 1f : 0f);
        sensor.AddObservation(opponent.IsDefending ? 1f : 0f);
        sensor.AddObservation(opponent.IsMoving ? 1f : 0f);
        sensor.AddObservation(opponent.IsIdle ? 1f : 0f);

        // Helpful extras for more stable combat learning.
        sensor.AddObservation(_isAttacking ? 1f : 0f);
        sensor.AddObservation(_isDefending ? 1f : 0f);
        sensor.AddObservation(IsMoving ? 1f : 0f);
        sensor.AddObservation(Vector3.Dot(transform.forward, directionToOpponent));
        sensor.AddObservation(HealthNormalized);
        sensor.AddObservation(opponent.HealthNormalized);
        sensor.AddObservation(Mathf.Clamp01(_attackCooldownTimer / Mathf.Max(attackCooldown, 0.0001f)));
        sensor.AddObservation(Mathf.Clamp01(_defendCooldownTimer / Mathf.Max(defendCooldown, 0.0001f)));
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // Combat branch: 0 = none, 1 = attack, 2 = defend.
        if (!CanStartAttack())
        {
            actionMask.SetActionEnabled(2, 1, false);
        }

        if (!CanStartDefend())
        {
            actionMask.SetActionEnabled(2, 2, false);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (opponent == null)
        {
            return;
        }

        TickState(Time.fixedDeltaTime);

        var discreteActions = actions.DiscreteActions;
        int forwardBranch = discreteActions[0];
        int strafeBranch = discreteActions[1];
        int combatBranch = discreteActions[2];

        Vector3 moveDirection = GetMoveDirection(forwardBranch, strafeBranch);
        MoveAgent(moveDirection);
        HandleCombatAction(combatBranch);
        AddShapedRewards(moveDirection);
        UpdateVisualState(Time.fixedDeltaTime);

        _lastMoveDirection = moveDirection;
        _lastDistanceToOpponent = GetPlanarDistanceToOpponent();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        discreteActions[0] = 0;
        discreteActions[1] = 0;
        discreteActions[2] = 0;

        if (Input.GetKey(KeyCode.W))
        {
            discreteActions[0] = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActions[0] = 2;
        }

        if (Input.GetKey(KeyCode.A))
        {
            discreteActions[1] = 1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActions[1] = 2;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            discreteActions[2] = 1;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            discreteActions[2] = 2;
        }
    }

    public void ReceiveHit(gladiator attacker, float damage)
    {
        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        AddReward(hitPenalty);
        TriggerHitFlash();

        if (_currentHealth > 0f)
        {
            return;
        }

        AddReward(losePenalty);

        if (attacker != null)
        {
            attacker.AddReward(winReward);
            attacker.EndEpisode();
        }

        EndEpisode();
    }

    private void ResetAgentPose()
    {
        Vector3 position = _fallbackStartPosition;
        Quaternion rotation = _fallbackStartRotation;

        if (arena != null && arena.TryGetSpawnPose(this, out Vector3 arenaPosition, out Quaternion arenaRotation))
        {
            position = arenaPosition;
            rotation = arenaRotation;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (agentRb != null)
        {
            agentRb.linearVelocity = Vector3.zero;
            agentRb.angularVelocity = Vector3.zero;
        }
    }

    private void ResetCombatState()
    {
        _currentHealth = maxHealth;
        _attackTimer = 0f;
        _attackCooldownTimer = 0f;
        _defendTimer = 0f;
        _defendCooldownTimer = 0f;
        _episodeTimer = 0f;
        _attackResolved = false;
        _isAttacking = false;
        _isDefending = false;
        _lastMoveDirection = Vector3.zero;
        _lastDistanceToOpponent = GetPlanarDistanceToOpponent();
        _hitFlashTimer = 0f;
        _currentTint = Color.white;
        ClearVisualTint();
    }

    private void TickState(float deltaTime)
    {
        _episodeTimer += deltaTime;
        _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - deltaTime);
        _defendCooldownTimer = Mathf.Max(0f, _defendCooldownTimer - deltaTime);
        _hitFlashTimer = Mathf.Max(0f, _hitFlashTimer - deltaTime);

        if (_isAttacking)
        {
            _attackTimer -= deltaTime;

            float attackElapsed = (attackWindup + attackRecover) - _attackTimer;
            if (!_attackResolved && attackElapsed >= attackWindup)
            {
                ResolveAttack();
            }

            if (_attackTimer <= 0f)
            {
                _isAttacking = false;
                _attackResolved = false;
            }
        }

        if (_isDefending)
        {
            _defendTimer -= deltaTime;
            if (_defendTimer <= 0f)
            {
                _isDefending = false;
            }
        }

        if (_episodeTimer >= episodeLengthSeconds)
        {
            EndEpisode();
            if (opponent != null)
            {
                opponent.EndEpisode();
            }
        }
    }

    private void MoveAgent(Vector3 moveDirection)
    {
        Vector3 planarVelocity = moveDirection * moveSpeed;

        if (agentRb != null)
        {
            Vector3 verticalVelocity = Vector3.up * agentRb.linearVelocity.y;
            agentRb.linearVelocity = planarVelocity + verticalVelocity;
        }
        else
        {
            transform.position += planarVelocity * Time.fixedDeltaTime;
        }

        RotateAgent(moveDirection);
    }

    private void RotateAgent(Vector3 moveDirection)
    {
        Vector3 desiredForward = Vector3.zero;

        if (_isAttacking || _isDefending)
        {
            desiredForward = GetPlanarDirectionToOpponent();
        }
        else if (moveDirection.sqrMagnitude > 0.001f)
        {
            desiredForward = moveDirection;
        }

        if (desiredForward.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(desiredForward.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
    }

    private void HandleCombatAction(int combatBranch)
    {
        if (combatBranch == 1)
        {
            if (CanStartAttack())
            {
                StartAttack();
            }
            else
            {
                AddReward(missedAttackPenalty * 0.25f);
            }
        }
        else if (combatBranch == 2)
        {
            if (CanStartDefend())
            {
                StartDefend();
            }
            else
            {
                AddReward(missedAttackPenalty * 0.10f);
            }
        }
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _isDefending = false;
        _attackResolved = false;
        _attackTimer = attackWindup + attackRecover;
        _attackCooldownTimer = attackCooldown;
    }

    private void StartDefend()
    {
        _isDefending = true;
        _defendTimer = defendDuration;
        _defendCooldownTimer = defendCooldown;
    }

    private void ResolveAttack()
    {
        _attackResolved = true;

        Vector3 toOpponent = GetPlanarDirectionToOpponent();
        float distanceToOpponent = toOpponent.magnitude;
        Vector3 directionToOpponent = distanceToOpponent > 0.001f ? toOpponent / distanceToOpponent : Vector3.zero;
        float facingDot = Vector3.Dot(transform.forward, directionToOpponent);

        if (distanceToOpponent > attackRange || facingDot < attackFacingDot)
        {
            AddReward(missedAttackPenalty);
            return;
        }

        if (opponent.TryBlock(this))
        {
            AddReward(blockedAttackPenalty);
            return;
        }

        AddReward(successfulHitReward);
        opponent.ReceiveHit(this, attackDamage);
    }

    private bool TryBlock(gladiator attacker)
    {
        if (!_isDefending)
        {
            return false;
        }

        Vector3 toAttacker = attacker.transform.position - transform.position;
        Vector3 planarToAttacker = Vector3.ProjectOnPlane(toAttacker, Vector3.up);

        if (planarToAttacker.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float blockDot = Vector3.Dot(transform.forward, planarToAttacker.normalized);
        if (blockDot < defendFacingDot)
        {
            return false;
        }

        AddReward(successfulBlockReward);
        return true;
    }

    private void AddShapedRewards(Vector3 moveDirection)
    {
        AddReward(stepPenalty);
        AddReward(moveDirection.sqrMagnitude * movementEnergyPenalty);

        Vector3 toOpponent = GetPlanarDirectionToOpponent();
        float distanceToOpponent = toOpponent.magnitude;

        if (distanceToOpponent > 0.001f)
        {
            Vector3 directionToOpponent = toOpponent / distanceToOpponent;
            float facingDot = Mathf.Max(0f, Vector3.Dot(transform.forward, directionToOpponent));
            AddReward(facingDot * facingReward);

            float preferredDistance = attackRange * 0.65f;
            float distanceError = Mathf.Abs(distanceToOpponent - preferredDistance);
            float distanceScore = 1f - Mathf.Clamp01(distanceError / Mathf.Max(attackRange * 0.75f, 0.0001f));
            AddReward(distanceScore * preferredRangeReward);

            float distanceDelta = _lastDistanceToOpponent - distanceToOpponent;
            float normalizedDelta = Mathf.Clamp01(Mathf.Abs(distanceDelta) / Mathf.Max(attackRange, 0.0001f));

            if (distanceDelta > 0.001f)
            {
                AddReward(normalizedDelta * closeDistanceReward * 10f);
            }
            else if (distanceDelta < -0.001f)
            {
                AddReward(normalizedDelta * driftAwayPenalty * 10f);
            }
        }
    }

    private Vector3 GetMoveDirection(int forwardBranch, int strafeBranch)
    {
        Vector3 moveDirection = Vector3.zero;
        Vector3 arenaForward = arena != null ? arena.Forward : Vector3.forward;
        Vector3 arenaRight = arena != null ? arena.Right : Vector3.right;

        if (forwardBranch == 1)
        {
            moveDirection += arenaForward;
        }
        else if (forwardBranch == 2)
        {
            moveDirection -= arenaForward;
        }

        if (strafeBranch == 1)
        {
            moveDirection -= arenaRight;
        }
        else if (strafeBranch == 2)
        {
            moveDirection += arenaRight;
        }

        return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
    }

    private bool CanStartAttack()
    {
        return !_isAttacking && !_isDefending && _attackCooldownTimer <= 0f;
    }

    private bool CanStartDefend()
    {
        return !_isAttacking && !_isDefending && _defendCooldownTimer <= 0f;
    }

    private Vector3 GetPlanarDirectionToOpponent()
    {
        if (opponent == null)
        {
            return Vector3.zero;
        }

        return Vector3.ProjectOnPlane(opponent.transform.position - transform.position, Vector3.up);
    }

    private float GetPlanarDistanceToOpponent()
    {
        return GetPlanarDirectionToOpponent().magnitude;
    }

    private Vector3 GetArenaRelativePosition(Vector3 worldPosition)
    {
        if (arena == null)
        {
            return worldPosition;
        }

        return arena.transform.InverseTransformPoint(worldPosition);
    }

    private float GetArenaRadius()
    {
        return Mathf.Max(observationScale, 0.001f);
    }

    private void CacheVisuals()
    {
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        if (visualRenderers == null || visualRenderers.Length == 0)
        {
            visualRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    private void TriggerHitFlash()
    {
        _hitFlashTimer = hitFlashDuration;
        _currentTint = hitTint;
        ApplyVisualTint(hitTint);
    }

    private void UpdateVisualState(float deltaTime)
    {
        Color targetTint = Color.white;

        if (_hitFlashTimer > 0f)
        {
            targetTint = hitTint;
        }
        else if (_isAttacking)
        {
            targetTint = attackTint;
        }
        else if (_isDefending)
        {
            targetTint = defendTint;
        }

        if (targetTint == Color.white)
        {
            _currentTint = Color.white;
            ClearVisualTint();
            return;
        }

        float blendFactor = visualBlendSpeed <= 0f ? 1f : 1f - Mathf.Exp(-visualBlendSpeed * deltaTime);
        _currentTint = Color.Lerp(_currentTint, targetTint, blendFactor);
        ApplyVisualTint(_currentTint);
    }

    private void ApplyVisualTint(Color tint)
    {
        if (visualRenderers == null)
        {
            return;
        }

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Renderer renderer = visualRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", tint);
            _propertyBlock.SetColor("_Color", tint);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private void ClearVisualTint()
    {
        if (visualRenderers == null)
        {
            return;
        }

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Renderer renderer = visualRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.SetPropertyBlock(null);
        }
    }
}

public class GladiatorArena : MonoBehaviour
{
    [Header("Observation Setup")]
    public float observationScale = 6f;

    [Header("Spawn Points")]
    public Transform fighterASpawn;
    public Transform fighterBSpawn;

    [Header("Fighters")]
    public gladiator fighterA;
    public gladiator fighterB;

    public Vector3 Forward => transform.forward;
    public Vector3 Right => transform.right;

    void Awake()
    {
        if (fighterA != null)
        {
            fighterA.arena = this;
            fighterA.observationScale = observationScale;
        }

        if (fighterB != null)
        {
            fighterB.arena = this;
            fighterB.observationScale = observationScale;
        }

        if (fighterA != null && fighterB != null)
        {
            if (fighterA.opponent == null)
            {
                fighterA.opponent = fighterB;
            }

            if (fighterB.opponent == null)
            {
                fighterB.opponent = fighterA;
            }
        }
    }

    public bool TryGetSpawnPose(gladiator fighter, out Vector3 position, out Quaternion rotation)
    {
        Transform spawn = null;

        if (fighter == fighterA)
        {
            spawn = fighterASpawn;
        }
        else if (fighter == fighterB)
        {
            spawn = fighterBSpawn;
        }

        if (spawn == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = spawn.position;
        rotation = spawn.rotation;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (fighterASpawn != null)
        {
            Gizmos.DrawSphere(fighterASpawn.position, 0.15f);
        }

        if (fighterBSpawn != null)
        {
            Gizmos.DrawSphere(fighterBSpawn.position, 0.15f);
        }
    }
}
