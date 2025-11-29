using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Monsters))]
public class RewardCalculator : MonoBehaviour
{
    [SerializeField] RewardConfig config;

    Monsters owner;
    EnemySituationEvaluator evaluator;
    EnemyWorkingMemory workingMemory;
    IEnemyBrain brain;

    float survivalTimer;
    float cumulativeReward;
    float damageDealtTotal;
    float damageTakenTotal;
    double startTime;
    bool episodeEnded;

    void Awake()
    {
        owner = GetComponent<Monsters>();
        evaluator = GetComponent<EnemySituationEvaluator>();
        workingMemory = GetComponent<EnemyWorkingMemory>();
    }

    void OnEnable()
    {
        Subscribe();
        startTime = Time.timeAsDouble;
        survivalTimer = 0f;
        cumulativeReward = 0f;
        damageDealtTotal = 0f;
        damageTakenTotal = 0f;
        episodeEnded = false;
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        if (episodeEnded || config == null)
        {
            return;
        }

        survivalTimer += Time.deltaTime;
        if (survivalTimer >= config.SurvivalTickInterval)
        {
            survivalTimer -= config.SurvivalTickInterval;
            AddReward(config.SurvivalTickReward);
            ApplyPositionalReward();
            ApplyObstructionPenalty();
        }
    }

    void Subscribe()
    {
        if (owner == null)
        {
            return;
        }

        owner.OnDamageDealt += HandleDamageDealt;
        owner.OnDamageTaken += HandleDamageTaken;
        owner.OnSpiritModeChanged += HandleSpiritModeChanged;
        owner.OnEnemyDeath += HandleEnemyDeath;
    }

    void Unsubscribe()
    {
        if (owner == null)
        {
            return;
        }

        owner.OnDamageDealt -= HandleDamageDealt;
        owner.OnDamageTaken -= HandleDamageTaken;
        owner.OnSpiritModeChanged -= HandleSpiritModeChanged;
        owner.OnEnemyDeath -= HandleEnemyDeath;
    }

    void HandleDamageDealt(float amount)
    {
        damageDealtTotal += amount;
        AddReward(amount * config.DamageDealtWeight);
        brain?.GiveReward(0f);
    }

    void HandleDamageTaken(float amount)
    {
        damageTakenTotal += amount;
        AddReward(amount * config.DamageTakenWeight);
    }

    void HandleSpiritModeChanged(bool isSpirit)
    {
        AddReward(isSpirit ? config.SpiritEnterReward : config.SpiritExitPenalty);
    }

    void HandleEnemyDeath()
    {
        if (episodeEnded)
        {
            return;
        }

        AddReward(config.DeathPenalty);
        CloseEpisode(false);
    }

    void ApplyPositionalReward()
    {
        SituationState state = GetLatestState();
        if (state.distanceToPlayer >= config.IdealDistanceMin && state.distanceToPlayer <= config.IdealDistanceMax)
        {
            AddReward(config.IdealDistanceReward);
        }
    }

    void ApplyObstructionPenalty()
    {
        SituationState state = GetLatestState();
        if (state.isObstructed)
        {
            AddReward(config.ObstructedPenalty);
        }
    }

    SituationState GetLatestState()
    {
        if (owner != null && owner.HAS_LATEST_STATE)
        {
            return owner.LATEST_STATE;
        }

        if (evaluator != null)
        {
            return evaluator.GetCurrentState(forceEvaluate: true);
        }

        return default;
    }

    void AddReward(float rawValue)
    {
        if (config == null || Mathf.Approximately(rawValue, 0f))
        {
            return;
        }

        float clamped = Mathf.Clamp(rawValue, -config.MaxRewardMagnitude, config.MaxRewardMagnitude);
        cumulativeReward += clamped;

        if (brain == null)
        {
            brain = owner?.BRAIN_INSTANCE;
        }

        brain?.GiveReward(clamped);
    }

    void CloseEpisode(bool survived)
    {
        episodeEnded = true;
        if (brain == null)
        {
            brain = owner?.BRAIN_INSTANCE;
        }

        EpisodeSummary summary = new EpisodeSummary
        {
            duration = Time.timeAsDouble - startTime,
            observations = workingMemory != null ? workingMemory.Entries.Count : 0,
            cumulativeReward = cumulativeReward,
            survived = survived,
            damageDealt = damageDealtTotal,
            damageTaken = damageTakenTotal
        };

        brain?.OnEpisodeEnd(summary);
    }
}
