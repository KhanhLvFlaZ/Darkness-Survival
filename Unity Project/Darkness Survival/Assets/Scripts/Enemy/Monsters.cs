using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemySituationEvaluator), typeof(EnemyWorkingMemory))]
public class Monsters : MonoBehaviour, IDamageable
{
    ///////////////////////
    //       Stats       //
    ///////////////////////

    Transform targetDestination;
    GameObject targetGameobject;
    Character targetCharacter;
    Rigidbody2D rigidbody2d;
    ColorChange colorChange;

    [Header("Stats")]

    [SerializeField] float maxHp = 4f;
    [SerializeField] float minHp = 4f;
    float hp;

    [SerializeField] float damage = 8f;
    [SerializeField] float speed = 2.5f * 0.25f;
    [SerializeField, Range(0.1f, 0.99f)] float maxSpeedRatioToPlayer = 0.85f;
    [SerializeField] int soulsReward = 30;

    // Changeable stats //

    float currentDamage;
    float damageMultiplier = 0.7f;
    float currentSpeed;

    //////////////////////

    Color defaultColor;
    private bool isSwappingSide = false;
    bool isSpirit;
    float swapTimer;

    // Settings //

    [Space]
    [Space]
    [Header("Settings")]
    [Space]

    [SerializeField] Color spiritColor;
    [SerializeField] float colorChangeTime = 1f;
    [SerializeField] float slowDownSpeed = 0.3f;
    [SerializeField] float sideSwapDelay = 4f;
    [SerializeField] ObjectsDetection objectsDetection;
    [SerializeField] float attackReloadTime = 1f;
    [SerializeField] float knockBackTime = 0.5f;

    SimpleFlash simpleFlash;

    bool isKnockedBack = false;
    float knockBackEndTime;

    //////////////
    /// Boss
    //////////////

    [SerializeField] StatusBar hpBar;
    [SerializeField] GameObject hpBarPrefab;
    [SerializeField] Vector3 hpBarOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] bool autoConfigureHpBar = true;
    [SerializeField] float hpBarVerticalPadding = 0.2f;
    [SerializeField] float hpBarWidthMultiplier = 1f;
    [SerializeField] float hpBarMinWidth = 0.75f;
    [SerializeField] float hpBarHeightMultiplier = 0.4f;
    [SerializeField] bool useSpriteTopForHpBar = false;
    [SerializeField] float hpBarStandardVerticalAdjustment = -0.3f;
    bool hasHpBar = false;
    Vector3 offset;

    [Header("AI")]
    [SerializeField] MonoBehaviour brainBehaviour;
    [SerializeField, Range(0f, 1f)] float brainSteerWeight = 0.5f;
    [SerializeField] float brainAttackRange = 1.35f;

    [Header("Ranged Combat")]
    [SerializeField] bool enableRangedAttack = false;
    [SerializeField] float rangedAttackRange = 6f;
    [SerializeField] Transform projectileSpawnPoint;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] float projectileSpawnForwardOffset = 0.65f;

    public event Action<float> OnDamageDealt;
    public event Action<float> OnDamageTaken;
    public event Action<bool> OnSpiritModeChanged;
    public event Action OnEnemyDeath;

    EnemySituationEvaluator situationEvaluator;
    EnemyWorkingMemory workingMemory;
    RewardCalculator rewardCalculator;
    AttackTimingOptimizer attackOptimizer;
    RangedCombatBehavior rangedCombat;
    MetricsTracker metricsTracker;
    IEnemyBrain brainInstance;
    Vector2 brainDesiredDirection;
    bool pendingAttackRequest;
    bool episodeFinalized;
    
    // ML-Agents training tracking
    float episodeStartTime;
    float damageDealtThisEpisode;
    float damageTakenThisEpisode;

    //////////////

    float timer;

    // Brain scheduling
    SituationState latestState;
    EnemyAction latestAction = EnemyAction.Idle;
    bool hasLatestState;
    bool hasLatestAction;
    bool hasLatestObservation;

    //private Vector2[] path;
    //private int targetIndex;

    ///////////////////////
    //      Methods      //
    ///////////////////////

    public float SPEED
    {
        get { return speed; }
        set { speed = value; }
    }

    public float CURRENT_SPEED
    {
        get { return currentSpeed; }
    }

    public bool IS_KNOCKED_BACK
    {
        get { return isKnockedBack; }
    }

    public float HP
    {
        get { return hp; }
        set { maxHp = hp; }
    }

    public float MAX_HP
    {
        get { return maxHp; }
    }

    public bool IS_SPIRIT
    {
        get { return isSpirit; }
    }

    public float ATTACK_COOLDOWN_REMAINING
    {
        get { return Mathf.Max(timer, 0f); }
    }

    public ObjectsDetection OBJECTS_DETECTION => objectsDetection;
    public IEnemyBrain BRAIN_INSTANCE => brainInstance;
    public EnemyWorkingMemory WORKING_MEMORY => workingMemory;
    public bool HAS_LATEST_STATE => hasLatestState;
    public SituationState LATEST_STATE => latestState;
    public EnemyAction LATEST_ACTION => latestAction;

    // - Awake 

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        defaultColor = GetComponent<SpriteRenderer>().color;
        colorChange = GetComponent<ColorChange>();
        simpleFlash = GetComponent<SimpleFlash>();
        situationEvaluator = GetComponent<EnemySituationEvaluator>();
        workingMemory = GetComponent<EnemyWorkingMemory>();
        rewardCalculator = GetComponent<RewardCalculator>();
        attackOptimizer = GetComponent<AttackTimingOptimizer>();
        rangedCombat = GetComponent<RangedCombatBehavior>();
        metricsTracker = GetComponent<MetricsTracker>();

        if (brainBehaviour is IEnemyBrain runtimeBrain)
        {
            brainInstance = runtimeBrain;
        }
        else
        {
            // Check for MonsterAgent (ML-Agents) - using reflection to avoid compile dependency
            var mlAgentComponent = GetComponent("MonsterAgent");
            if (mlAgentComponent != null && mlAgentComponent is IEnemyBrain mlAgent)
            {
                brainInstance = mlAgent;
            }
        }

        if (situationEvaluator != null)
        {
            situationEvaluator.StateUpdated += HandleStateUpdated;
        }
        
        // Subscribe to damage events for metrics tracking
        if (metricsTracker != null)
        {
            OnDamageDealt += HandleDamageDealt;
            OnDamageTaken += HandleDamageTaken;
        }
    }

    private void OnDestroy()
    {
        if (situationEvaluator != null)
        {
            situationEvaluator.StateUpdated -= HandleStateUpdated;
        }
        
        // Unsubscribe from damage events
        if (metricsTracker != null)
        {
            OnDamageDealt -= HandleDamageDealt;
            OnDamageTaken -= HandleDamageTaken;
        }
    }

    // - Start

    private void Start()
    {
        timer = -1f;
        swapTimer = 0f;
        isSpirit = false;
        currentDamage = damage;
        currentSpeed = speed;
        hp = UnityEngine.Random.Range(minHp, maxHp);
        maxHp = hp;

        if(hpBar == null && hpBarPrefab != null)
        {
            GameObject hpBarInstance = Instantiate(hpBarPrefab, transform.position + hpBarOffset, Quaternion.identity, null);
            hpBar = hpBarInstance.GetComponent<StatusBar>();

            if(hpBar != null)
            {
                hpBar.transform.SetParent(transform);
            }
        }

        if(hpBar != null)
        {
            hpBar.SetState(hp, maxHp);
            hasHpBar = true;
            ConfigureHpBar();
        }

        if (situationEvaluator != null)
        {
            latestState = situationEvaluator.GetCurrentState(forceEvaluate: true);
            hasLatestState = true;
        }
    }


    // - Set Target

    public void SetTarget(GameObject target)
    {
        targetGameobject = target;
        targetDestination = target.transform;

        PlayerMove playerMove = targetGameobject.GetComponent<PlayerMove>();
        if (playerMove != null)
        {
            float playerSpeed = Mathf.Max(playerMove.SPEED, 0.01f);
            float desiredSpeed = playerSpeed * speed;
            float maxAllowedSpeed = playerSpeed * maxSpeedRatioToPlayer;
            speed = Mathf.Min(desiredSpeed, maxAllowedSpeed);
            currentSpeed = speed;
        }
    }

    void ConfigureHpBar()
    {
        if (!hasHpBar || hpBar == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (!autoConfigureHpBar || spriteRenderer == null)
        {
            offset = hpBar.transform.position - transform.position;
            return;
        }

        Bounds spriteBounds = spriteRenderer.bounds;
        float verticalReference = useSpriteTopForHpBar
            ? spriteBounds.max.y - transform.position.y
            : spriteBounds.size.y * 0.5f;
        float verticalPadding = hpBarVerticalPadding + (useSpriteTopForHpBar ? 0f : hpBarStandardVerticalAdjustment);
        float calculatedWidth = Mathf.Max(spriteBounds.size.x, hpBarMinWidth);

        Vector3 autoOffset = new Vector3(0f, verticalReference + verticalPadding, 0f);
        offset = autoOffset + hpBarOffset;

        Transform hpBarTransform = hpBar.transform;
        hpBarTransform.position = transform.position + offset;

        float desiredWidth = Mathf.Max(calculatedWidth * hpBarWidthMultiplier, hpBarMinWidth);
        float desiredHeight = Mathf.Max(hpBarTransform.localScale.y * hpBarHeightMultiplier, 0.01f);
        hpBarTransform.localScale = new Vector3(desiredWidth, desiredHeight, hpBarTransform.localScale.z);
    }

    // - Updates 

    private void Update()
    {
        if (hasHpBar && hpBar != null)
        {
            hpBar.transform.position = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, transform.position.z);
        }

        UpdateBrain();
        TryRangedAttack();
        
        // Track positioning score for metrics
        if (metricsTracker != null && targetDestination != null)
        {
            float distance = Vector2.Distance(transform.position, targetDestination.position);
            bool inOptimalRange = metricsTracker.IsInOptimalRange(distance);
            metricsTracker.UpdatePositioningScore(inOptimalRange, Time.deltaTime);
        }

        if (timer > 0f)
        {
            timer -= Time.deltaTime;
        }

        //////////////////////////////////
        // Check and change spirit mode //
        //////////////////////////////////

        if (colorChange != null && objectsDetection != null)
        {
            if (objectsDetection.IsDetected() && !isSpirit)
            {
                isSpirit = true;
                SpiritSettings(isSpirit);
            }
            if (!objectsDetection.IsDetected() && isSpirit)
            {
                isSpirit = false;
                SpiritSettings(isSpirit);
            }
        }

        ////////////////////////////////


        ////////////////////////////////
        // Check and change look side //
        ////////////////////////////////

        bool isLeft = targetDestination.position.x < transform.position.x;
        bool needsToSwapSide = (isLeft && transform.localScale.x > 0f) || (!isLeft && transform.localScale.x < 0f);

        if (needsToSwapSide)
        {
            if (!isSwappingSide)
            {
                currentSpeed *= slowDownSpeed;
                isSwappingSide = true;
            }

            swapTimer += Time.deltaTime;
        }

        if (swapTimer >= sideSwapDelay)
        {
            currentSpeed = speed;
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            swapTimer = 0f;
            isSwappingSide = false;
        }

        ////////////////////////////////
    }

    private void FixedUpdate()
    {
        // Safety check for targetDestination
        if (targetDestination == null)
        {
            return;
        }
        
        Vector3 direction = (targetDestination.position - transform.position).normalized;

        if (!isKnockedBack)
        {
            Vector2 moveDirection = direction;
            
            // Apply ranged combat behavior if available
            if (rangedCombat != null && enableRangedAttack)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, targetDestination.position);
                
                // Priority 1: Too close - retreat
                if (rangedCombat.ShouldRetreat(distanceToPlayer))
                {
                    Vector2 retreatVector = rangedCombat.CalculateRetreatVector(targetDestination.position, transform.position);
                    moveDirection = retreatVector;
                }
                // Priority 2: Too far - advance toward player
                else if (rangedCombat.ShouldAdvance(distanceToPlayer))
                {
                    moveDirection = direction; // Move toward player
                }
                // Priority 3: In optimal range - strafe or maintain
                else
                {
                    // In optimal range, use strafe behavior if enabled
                    Vector2 strafeDir = rangedCombat.GetStrafeDirection(direction);
                    if (strafeDir.sqrMagnitude > 0.01f)
                    {
                        moveDirection = strafeDir;
                    }
                    else
                    {
                        // Maintain optimal distance - slight movement toward player
                        // to compensate for player movement
                        moveDirection = direction * 0.3f; // Slow approach to maintain distance
                    }
                }
            }
            
            if (brainInstance != null)
            {
                // Brain can override ranged combat behavior
                rigidbody2d.velocity = brainDesiredDirection * currentSpeed;
            }
            else
            {
                rigidbody2d.velocity = moveDirection * currentSpeed;
            }
        }
        else
        {
            if (Time.time >= knockBackEndTime)
            {
                isKnockedBack = false;
            }
        }
    }


    // - Collisions

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (enableRangedAttack)
        {
            return;
        }

        if (collision.gameObject == targetGameobject)
        {
            bool shouldAttack = brainInstance == null || pendingAttackRequest;
            if (shouldAttack)
            {
                Attack();
                if (brainInstance != null)
                {
                    pendingAttackRequest = false;
                }
            }
        }
    }

    // - Gameplay methods 

    private void Attack()
    {
        if(timer > 0f)
        {
            return;
        }

        // Check if attack should be blocked by AttackTimingOptimizer
        if (attackOptimizer != null && attackOptimizer.ShouldBlockAttack())
        {
            return;
        }

        // Register attack attempt for coordination
        if (attackOptimizer != null)
        {
            attackOptimizer.RegisterAttackAttempt();
        }

        if (enableRangedAttack)
        {
            if (FireProjectile())
            {
                timer = attackReloadTime;
            }
            return;
        }

        if (targetCharacter == null)
        {
            targetCharacter = targetGameobject.GetComponent<Character>();
        }

        if (targetCharacter != null)
        {
            float adjustedDamage = currentDamage * GameDifficultySettings.EnemyDamageMultiplier;
            targetCharacter.TakeDamage(adjustedDamage);
            OnDamageDealt?.Invoke(currentDamage);
            
            // Track attack hit for metrics
            if (metricsTracker != null)
            {
                metricsTracker.UpdateAttackAccuracy(true);
            }
        }
        else
        {
            // Track attack miss for metrics
            if (metricsTracker != null)
            {
                metricsTracker.UpdateAttackAccuracy(false);
            }
        }
        timer = attackReloadTime;
    }

    // - Take Damage and knockback
    public void TakeDamage(float damage, Vector2 knockBack = default)
    {
        if (knockBack != Vector2.zero)
        {
            rigidbody2d.AddForce(knockBack, ForceMode2D.Impulse);
            isKnockedBack = true;
            knockBackEndTime = Time.time + knockBackTime;
        }
        TakeDamage(damage);
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hasHpBar && hpBar != null) hpBar.SetState(hp, maxHp);
        OnDamageTaken?.Invoke(damage);

        // Damage message

        Vector3 messagePosition = new Vector3(
                   transform.position.x,
                   transform.position.y + 0.8f,
                   transform.position.z
                   );

        if (simpleFlash != null) simpleFlash.Flash();

        MessageSystem.instance.PostMessage(damage.ToString(), messagePosition);

        // Check if dead 

        if (hp <= 0)
        {
            targetGameobject.GetComponent<Level>().AddExperience(soulsReward);
            GetComponent<DropOnDestroy>().CheckDrop();
            if (!episodeFinalized)
            {
                episodeFinalized = true;
                
                // Record episode end for metrics and brain
                RecordEpisodeEnd(survived: false);
                
                OnEnemyDeath?.Invoke();
            }

            if(hasHpBar && hpBar != null) Destroy(hpBar.gameObject);
            Destroy(gameObject);
        }
    }


    // Method for objects colliding 

    private Coroutine activeCoroutine;

    public void SpiritSettings(bool isSpirit)
    {
        // If Coroutine is active, we need to stop it
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        // Check condition and change settings 

        if (isSpirit)
        {
            currentDamage *= damageMultiplier;
            activeCoroutine = StartCoroutine(colorChange.ChangeColor(spiritColor, colorChangeTime));
        }
        else
        {
            currentDamage = damage;
            activeCoroutine = StartCoroutine(colorChange.ChangeColor(defaultColor, colorChangeTime));
        }
        OnSpiritModeChanged?.Invoke(isSpirit);
    }

    void HandleStateUpdated(SituationState state)
    {
        latestState = state;
        hasLatestState = true;
        hasLatestObservation = false;
    }

    void UpdateBrain()
    {
        if (brainInstance == null || !hasLatestState)
        {
            return;
        }

        EnemyAction action = brainInstance.Decide(latestState, workingMemory);
        latestAction = action;
        hasLatestAction = true;

        Vector2 fallbackDirection = Vector2.zero;
        if (targetDestination != null)
        {
            Vector3 delta = targetDestination.position - transform.position;
            if (delta.sqrMagnitude > 0.0001f)
            {
                fallbackDirection = delta.normalized;
            }
        }

        Vector2 desiredDirection = action.moveDirection.sqrMagnitude > 0.0001f
            ? action.moveDirection.normalized
            : fallbackDirection;

        if (fallbackDirection == Vector2.zero)
        {
            brainDesiredDirection = desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection : Vector2.right;
        }
        else if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            brainDesiredDirection = fallbackDirection;
        }
        else
        {
            brainDesiredDirection = Vector2.Lerp(fallbackDirection, desiredDirection, brainSteerWeight).normalized;
        }

        if (action.requestSpiritMode != isSpirit)
        {
            SpiritSettings(action.requestSpiritMode);
        }

        pendingAttackRequest = action.attemptAttack;

        RecordObservation(0f);
    }

    /// <summary>
    /// Requirement 12.5: Continue recording observations even in heuristic mode.
    /// Stores observations in working memory for offline training.
    /// Ensures observation format matches ML requirements.
    /// </summary>
    void RecordObservation(float rewardDelta)
    {
        if (workingMemory == null || !hasLatestState)
        {
            return;
        }

        if (!hasLatestAction)
        {
            latestAction = EnemyAction.Idle;
            hasLatestAction = true;
        }

        if (Mathf.Approximately(rewardDelta, 0f) && hasLatestObservation)
        {
            return;
        }

        // Record observation regardless of whether using ML or heuristic mode
        // This enables offline training from heuristic demonstrations
        workingMemory.PushObservation(latestState, latestAction, rewardDelta);
        hasLatestObservation = true;
    }

    public void LogReward(float rewardDelta)
    {
        if (Mathf.Approximately(rewardDelta, 0f))
        {
            return;
        }

        RecordObservation(rewardDelta);
    }

    void TryRangedAttack()
    {
        if (!enableRangedAttack || targetDestination == null)
        {
            return;
        }

        if (timer > 0f)
        {
            return;
        }

        float sqrDistance = (targetDestination.position - transform.position).sqrMagnitude;
        float distance = Mathf.Sqrt(sqrDistance);
        
        // Allow attack within range, even while retreating (simultaneous actions)
        if (rangedCombat != null)
        {
            // Attack if within max engagement distance
            if (distance <= rangedCombat.MaxEngagementDistance)
            {
                Attack();
            }
        }
        else
        {
            // Fallback to basic range check
            if (sqrDistance <= rangedAttackRange * rangedAttackRange)
            {
                Attack();
            }
        }
    }

    bool FireProjectile()
    {
        if (projectilePrefab == null || targetDestination == null)
        {
            return false;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 targetPosition = targetDestination.position;
        Vector2 playerVelocity = Vector2.zero;
        
        // Use predictive aiming if ranged combat behavior is available
        if (rangedCombat != null)
        {
            // Get player velocity
            Rigidbody2D playerRb = targetDestination.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerVelocity = playerRb.velocity;
            }
            
            // Get projectile speed from the prefab
            float projectileSpeed = 10f; // Default
            DemonicSpikeProjectile spikeComponent = projectilePrefab.GetComponent<DemonicSpikeProjectile>();
            if (spikeComponent != null)
            {
                // Try to get speed via reflection or use default
                projectileSpeed = 10f; // Assuming default speed
            }
            
            // Calculate predictive aim point
            Vector2 predictedPosition = rangedCombat.CalculatePredictiveAimPoint(
                targetDestination.position, 
                playerVelocity, 
                projectileSpeed
            );
            
            targetPosition = predictedPosition;
        }
        
        Vector3 direction = (targetPosition - spawn.position);
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.right;
        }
        direction.Normalize();

        Vector3 spawnPosition = spawn.position + direction * projectileSpawnForwardOffset;

        GameObject projectileInstance = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        DemonicSpikeProjectile spike = projectileInstance.GetComponent<DemonicSpikeProjectile>();
        if (spike != null)
        {
            spike.Initialize(transform);
            spike.SetDamage(currentDamage);
            spike.SetDirection(direction.x, direction.y);
            
            // Track shot for accuracy rewards (will be updated when projectile hits/misses)
            if (rangedCombat != null)
            {
                float shotDifficulty = rangedCombat.CalculateShotDifficulty(targetDestination.position, playerVelocity);
                // Store shot info for later tracking
                StartCoroutine(TrackProjectileResult(spike, shotDifficulty));
            }
            else if (metricsTracker != null)
            {
                // Track projectile for basic accuracy metrics
                StartCoroutine(TrackProjectileAccuracy(spike));
            }
        }
        return true;
    }
    
    System.Collections.IEnumerator TrackProjectileResult(DemonicSpikeProjectile projectile, float shotDifficulty)
    {
        // Wait for projectile to either hit or be destroyed
        float timeout = 5f;
        float elapsed = 0f;
        bool hit = false;
        
        while (projectile != null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        // If projectile was destroyed quickly, it likely hit something
        if (elapsed < timeout && projectile == null)
        {
            hit = true;
        }
        
        // Record result
        if (rangedCombat != null)
        {
            rangedCombat.RecordShotResult(hit, shotDifficulty);
        }
        
        // Also track for metrics
        if (metricsTracker != null)
        {
            metricsTracker.UpdateAttackAccuracy(hit);
        }
    }
    
    System.Collections.IEnumerator TrackProjectileAccuracy(DemonicSpikeProjectile projectile)
    {
        // Wait for projectile to either hit or be destroyed
        float timeout = 5f;
        float elapsed = 0f;
        bool hit = false;
        
        while (projectile != null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        // If projectile was destroyed quickly, it likely hit something
        if (elapsed < timeout && projectile == null)
        {
            hit = true;
        }
        
        // Track for metrics
        if (metricsTracker != null)
        {
            metricsTracker.UpdateAttackAccuracy(hit);
        }
    }
    
    // Metrics tracking handlers
    private void HandleDamageDealt(float damage)
    {
        if (metricsTracker != null)
        {
            metricsTracker.UpdateDamageEfficiency(damage, 0f);
        }
    }
    
    private void HandleDamageTaken(float damage)
    {
        if (metricsTracker != null)
        {
            metricsTracker.UpdateDamageEfficiency(0f, damage);
        }
    }
    
    /// <summary>
    /// Record episode end and aggregate metrics.
    /// Implements Requirement 10.1
    /// </summary>
    private void RecordEpisodeEnd(bool survived)
    {
        // Calculate episode duration
        float duration = Time.time - (metricsTracker != null ? Time.time - metricsTracker.CurrentMetrics.totalTimeAlive : 0f);
        
        // Get cumulative reward from reward calculator
        float cumulativeReward = 0f;
        if (rewardCalculator != null)
        {
            cumulativeReward = rewardCalculator.GetCumulativeReward();
        }
        
        // Get damage stats from metrics tracker
        float damageDealt = 0f;
        float damageTaken = 0f;
        if (metricsTracker != null)
        {
            damageDealt = metricsTracker.CurrentMetrics.totalDamageDealt;
            damageTaken = metricsTracker.CurrentMetrics.totalDamageTaken;
        }
        
        // Create episode summary
        EpisodeSummary summary = new EpisodeSummary
        {
            duration = duration,
            observations = workingMemory != null ? workingMemory.GetObservationCount() : 0,
            cumulativeReward = cumulativeReward,
            survived = survived,
            damageDealt = damageDealt,
            damageTaken = damageTaken
        };
        
        // Record in metrics tracker
        if (metricsTracker != null)
        {
            metricsTracker.RecordEpisodeEnd(summary);
        }
        
        // Notify brain
        if (brainInstance != null)
        {
            brainInstance.OnEpisodeEnd(summary);
        }
    }
    
    /// <summary>
    /// Execute an action from ML-Agents policy.
    /// Called by MonsterAgent to apply ML decisions.
    /// </summary>
    public void ExecuteMLAction(EnemyAction action)
    {
        latestAction = action;
        
        // Apply movement
        if (action.moveDirection.sqrMagnitude > 0.01f)
        {
            Vector2 movement = action.moveDirection * currentSpeed;
            rigidbody2d.velocity = movement;
        }
        
        // Handle attack attempt
        if (action.attemptAttack && timer <= 0f)
        {
            AttemptAttack();
        }
        
        // Handle spirit mode request (if applicable)
        if (action.requestSpiritMode && !isSpirit)
        {
            SpiritSettings(true);
        }
    }
    
    /// <summary>
    /// Reset monster state for training episode.
    /// Called by MonsterAgent at episode start.
    /// </summary>
    public void ResetForTraining()
    {
        // Reset HP
        hp = maxHp;
        if (hpBar != null)
        {
            hpBar.SetState(hp, maxHp);
        }
        
        // Reset velocity
        if (rigidbody2d != null)
        {
            rigidbody2d.velocity = Vector2.zero;
        }
        
        // Reset timers
        timer = -1f;
        swapTimer = 0f;
        
        // Reset spirit mode
        if (isSpirit)
        {
            SpiritSettings(false);
        }
        
        // Reset knockback
        isKnockedBack = false;
        
        // Reset state tracking
        hasLatestState = false;
        damageDealtThisEpisode = 0f;
        damageTakenThisEpisode = 0f;
        episodeStartTime = Time.time;
    }
    
    /// <summary>
    /// Check if monster is dead.
    /// Used by MonsterAgent to detect episode end.
    /// </summary>
    public bool IsDead()
    {
        return hp <= 0f;
    }
    
    private void AttemptAttack()
    {
        if (targetCharacter != null)
        {
            // Melee attack
            targetCharacter.TakeDamage(currentDamage);
            OnDamageDealt?.Invoke(currentDamage);
            timer = attackReloadTime;
            
            // Track attack for metrics
            if (metricsTracker != null)
            {
                metricsTracker.UpdateAttackAccuracy(true);
            }
        }
        else if (enableRangedAttack && projectilePrefab != null)
        {
            // Ranged attack
            if (FireProjectile())
            {
                timer = attackReloadTime;
            }
        }
    }
}
