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
    IEnemyBrain brainInstance;
    Vector2 brainDesiredDirection;
    bool pendingAttackRequest;
    bool episodeFinalized;

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

        if (brainBehaviour is IEnemyBrain runtimeBrain)
        {
            brainInstance = runtimeBrain;
        }

        if (situationEvaluator != null)
        {
            situationEvaluator.StateUpdated += HandleStateUpdated;
        }
    }

    private void OnDestroy()
    {
        if (situationEvaluator != null)
        {
            situationEvaluator.StateUpdated -= HandleStateUpdated;
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


        if (timer > 0f)
        {
            timer -= Time.deltaTime;
        }

        //////////////////////////////////
        // Check and change spirit mode //
        //////////////////////////////////

        if (colorChange != null)
        {
            if (objectsDetection != null)
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
            else
            {
                Debug.Log(" Error :: Monsters -> ObjectTrigger can not be found! Object Detection fail.");
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
        Vector3 direction = (targetDestination.position - transform.position).normalized;

        if (!isKnockedBack)
        {
            if (brainInstance != null)
            {
                rigidbody2d.velocity = brainDesiredDirection * currentSpeed;
            }
            else
            {
                rigidbody2d.velocity = direction * currentSpeed;
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
            targetCharacter.TakeDamage(currentDamage);
            OnDamageDealt?.Invoke(currentDamage);
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
        if (sqrDistance <= rangedAttackRange * rangedAttackRange)
        {
            Attack();
        }
    }

    bool FireProjectile()
    {
        if (projectilePrefab == null || targetDestination == null)
        {
            return false;
        }

        Transform spawn = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 direction = (targetDestination.position - spawn.position);
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
        }
        return true;
    }
}
