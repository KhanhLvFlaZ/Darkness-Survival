using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(25)]
[RequireComponent(typeof(Monsters))]
public class EnemySituationEvaluator : MonoBehaviour
{
    [Serializable]
    public struct TargetDetectionSettings
    {
        public bool enabled;
        public float radius;
        public LayerMask layerMask;
        [Range(0, 16)] public int maxTargets;
    }

    [Serializable]
    public struct TargetInfo
    {
        public Vector2 position;
        public float distance;
        public string tag;
    }

    [Serializable]
    public struct SituationSnapshot
    {
        public double timestamp;
        public Vector2 enemyPosition;
        public Vector2 enemyVelocity;
        public float enemyHp;
        public float enemyMaxHp;
        public float enemyHpRatio;
        public float baseSpeed;
        public float currentSpeed;
        public bool isSpirit;
        public bool isKnockedBack;
        public float attackCooldownRemaining;
        public bool isObstructed;
        public Vector2 playerPosition;
        public float playerHp;
        public float playerMaxHp;
        public float playerHpRatio;
        public float distanceToPlayer;
        public TargetInfo[] nearbyTargets;
    }

    [Serializable]
    public struct SituationTensor
    {
        public SituationTensor(float[] values, int targetSlotCount)
        {
            this.values = values;
            this.targetSlotCount = targetSlotCount;
        }

        public float[] values;
        public int targetSlotCount;
        public int Length => values?.Length ?? 0;
    }

    [SerializeField] float evaluationInterval = 0.1f;
    [SerializeField] TargetDetectionSettings targetDetection;
    [SerializeField] MonoBehaviour[] extraSensors;

    const int BaseFeatureCount = 20;

    readonly List<IEnemySituationSensor> sensorProviders = new List<IEnemySituationSensor>();

    Monsters owner;
    Rigidbody2D ownerBody;
    ObjectsDetection objectsDetection;
    Transform playerTransform;
    Character playerCharacter;
    Collider2D[] targetBuffer = Array.Empty<Collider2D>();
    float evaluationTimer;
    int targetSlotCount;

    public SituationSnapshot LatestSnapshot { get; private set; }
    public SituationTensor LatestTensor { get; private set; }
    public SituationState LatestState { get; private set; }

    public event Action<SituationSnapshot> SnapshotUpdated;
    public event Action<SituationTensor> TensorUpdated;
    public event Action<SituationState> StateUpdated;

    private void Awake()
    {
        owner = GetComponent<Monsters>();
        ownerBody = GetComponent<Rigidbody2D>();
        objectsDetection = owner.OBJECTS_DETECTION;
        CachePlayerReferences();
        CacheSensorProviders();
        AllocateTargetBuffer();
        evaluationTimer = Mathf.Max(evaluationInterval, 0.02f);
    }

    private void Update()
    {
        evaluationTimer -= Time.deltaTime;
        if (evaluationTimer <= 0f)
        {
            evaluationTimer = Mathf.Max(evaluationInterval, 0.02f);
            EvaluateNow();
        }
    }

    public SituationSnapshot EvaluateNow()
    {
        LatestSnapshot = BuildSnapshot();
        LatestState = BuildState(LatestSnapshot);
        SnapshotUpdated?.Invoke(LatestSnapshot);
        StateUpdated?.Invoke(LatestState);
        LatestTensor = BuildTensor(LatestSnapshot);
        TensorUpdated?.Invoke(LatestTensor);
        return LatestSnapshot;
    }

    public SituationState GetCurrentState(bool forceEvaluate = false)
    {
        if (forceEvaluate || LatestSnapshot.timestamp <= 0d)
        {
            EvaluateNow();
        }

        return LatestState;
    }

    public void RegisterSensor(IEnemySituationSensor sensor)
    {
        if (sensor == null || sensorProviders.Contains(sensor))
        {
            return;
        }
        sensorProviders.Add(sensor);
    }

    public void UnregisterSensor(IEnemySituationSensor sensor)
    {
        if (sensor == null)
        {
            return;
        }
        sensorProviders.Remove(sensor);
    }

    SituationSnapshot BuildSnapshot()
    {
        SituationSnapshot snapshot = new SituationSnapshot
        {
            timestamp = Time.timeAsDouble,
            enemyPosition = transform.position,
            enemyVelocity = ownerBody != null ? ownerBody.velocity : Vector2.zero,
            enemyHp = owner.HP,
            enemyMaxHp = owner.MAX_HP,
            baseSpeed = owner.SPEED,
            currentSpeed = owner.CURRENT_SPEED,
            isSpirit = owner.IS_SPIRIT,
            isKnockedBack = owner.IS_KNOCKED_BACK,
            attackCooldownRemaining = owner.ATTACK_COOLDOWN_REMAINING,
            isObstructed = objectsDetection != null && objectsDetection.IsDetected()
        };

        snapshot.enemyHpRatio = snapshot.enemyMaxHp > 0f ? snapshot.enemyHp / snapshot.enemyMaxHp : 0f;

        if (playerTransform == null)
        {
            CachePlayerReferences();
        }

        if (playerTransform != null)
        {
            snapshot.playerPosition = playerTransform.position;
            snapshot.distanceToPlayer = Vector2.Distance(snapshot.enemyPosition, snapshot.playerPosition);
        }
        else
        {
            snapshot.playerPosition = Vector2.zero;
            snapshot.distanceToPlayer = 0f;
        }

        if (playerCharacter != null)
        {
            snapshot.playerHp = playerCharacter.CURRENT_HP;
            snapshot.playerMaxHp = playerCharacter.MAX_HP;
            snapshot.playerHpRatio = snapshot.playerMaxHp > 0f ? snapshot.playerHp / snapshot.playerMaxHp : 0f;
        }
        else
        {
            snapshot.playerHp = 0f;
            snapshot.playerMaxHp = 0f;
            snapshot.playerHpRatio = 0f;
        }

        snapshot.nearbyTargets = CaptureNearbyTargets(snapshot.enemyPosition);

        for (int i = 0; i < sensorProviders.Count; ++i)
        {
            sensorProviders[i]?.Capture(ref snapshot);
        }

        return snapshot;
    }

    public SituationState BuildState(SituationSnapshot snapshot)
    {
        SituationState state = new SituationState
        {
            timestamp = snapshot.timestamp,
            enemyPosition = snapshot.enemyPosition,
            enemyVelocity = snapshot.enemyVelocity,
            playerPosition = snapshot.playerPosition,
            enemyHpRatio = snapshot.enemyHpRatio,
            playerHpRatio = snapshot.playerHpRatio,
            distanceToPlayer = snapshot.distanceToPlayer,
            attackCooldownRemaining = snapshot.attackCooldownRemaining,
            isSpirit = snapshot.isSpirit,
            isObstructed = snapshot.isObstructed,
            nearbyTargetCount = snapshot.nearbyTargets?.Length ?? 0
        };

        float distanceFactor = targetDetection.radius > 0f
            ? Mathf.Clamp01(1f - snapshot.distanceToPlayer / targetDetection.radius)
            : Mathf.Clamp01(1f - snapshot.distanceToPlayer * 0.2f);

        state.attackOpportunity = Mathf.Clamp01(distanceFactor * (1f - Mathf.Clamp01(snapshot.attackCooldownRemaining)));
        state.retreatUrgency = Mathf.Clamp01((1f - snapshot.enemyHpRatio) + (snapshot.isObstructed ? 0.25f : 0f));

        float targetDensity = targetSlotCount > 0
            ? Mathf.Clamp01((snapshot.nearbyTargets?.Length ?? 0) / (float)targetSlotCount)
            : 0f;
        state.exploreValue = Mathf.Clamp01(targetDensity * 0.6f + snapshot.playerHpRatio * 0.4f);

        return state;
    }

    SituationTensor BuildTensor(SituationSnapshot snapshot)
    {
        int totalFeatureCount = BaseFeatureCount + targetSlotCount * 3;
        float[] features = new float[totalFeatureCount];
        int index = 0;

        features[index++] = (float)snapshot.timestamp;
        features[index++] = snapshot.enemyPosition.x;
        features[index++] = snapshot.enemyPosition.y;
        features[index++] = snapshot.enemyVelocity.x;
        features[index++] = snapshot.enemyVelocity.y;
        features[index++] = snapshot.enemyHp;
        features[index++] = snapshot.enemyMaxHp;
        features[index++] = snapshot.enemyHpRatio;
        features[index++] = snapshot.baseSpeed;
        features[index++] = snapshot.currentSpeed;
        features[index++] = snapshot.isSpirit ? 1f : 0f;
        features[index++] = snapshot.isKnockedBack ? 1f : 0f;
        features[index++] = snapshot.attackCooldownRemaining;
        features[index++] = snapshot.isObstructed ? 1f : 0f;
        features[index++] = snapshot.playerPosition.x;
        features[index++] = snapshot.playerPosition.y;
        features[index++] = snapshot.playerHp;
        features[index++] = snapshot.playerMaxHp;
        features[index++] = snapshot.playerHpRatio;
        features[index++] = snapshot.distanceToPlayer;

        TargetInfo[] targets = snapshot.nearbyTargets ?? Array.Empty<TargetInfo>();
        for (int i = 0; i < targetSlotCount; ++i)
        {
            if (i < targets.Length)
            {
                features[index++] = targets[i].position.x;
                features[index++] = targets[i].position.y;
                features[index++] = targets[i].distance;
            }
            else
            {
                features[index++] = 0f;
                features[index++] = 0f;
                features[index++] = 0f;
            }
        }

        return new SituationTensor(features, targetSlotCount);
    }

    TargetInfo[] CaptureNearbyTargets(Vector2 origin)
    {
        if (!targetDetection.enabled || targetDetection.radius <= 0f || targetDetection.maxTargets <= 0)
        {
            return Array.Empty<TargetInfo>();
        }

        int detected = Physics2D.OverlapCircleNonAlloc(origin, targetDetection.radius, targetBuffer, targetDetection.layerMask);
        int count = Mathf.Min(detected, targetDetection.maxTargets);

        if (count <= 0)
        {
            return Array.Empty<TargetInfo>();
        }

        TargetInfo[] results = new TargetInfo[count];
        int filled = 0;

        for (int i = 0; i < count; ++i)
        {
            Collider2D col = targetBuffer[i];
            if (col == null)
            {
                continue;
            }

            Vector2 targetPosition = col.transform.position;
            results[filled] = new TargetInfo
            {
                position = targetPosition,
                distance = Vector2.Distance(origin, targetPosition),
                tag = col.tag
            };
            filled++;
        }

        Array.Clear(targetBuffer, 0, targetBuffer.Length);

        if (filled == results.Length)
        {
            return results;
        }

        if (filled == 0)
        {
            return Array.Empty<TargetInfo>();
        }

        TargetInfo[] trimmed = new TargetInfo[filled];
        Array.Copy(results, trimmed, filled);
        return trimmed;
    }

    void CachePlayerReferences()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        playerTransform = GameManager.instance.playerTransform;
        if (playerTransform != null)
        {
            playerCharacter = playerTransform.GetComponent<Character>();
        }
    }

    void CacheSensorProviders()
    {
        if (extraSensors == null)
        {
            return;
        }

        foreach (MonoBehaviour behaviour in extraSensors)
        {
            if (behaviour is IEnemySituationSensor sensor)
            {
                RegisterSensor(sensor);
            }
        }
    }

    void AllocateTargetBuffer()
    {
        targetSlotCount = Mathf.Max(targetDetection.maxTargets, 0);

        if (targetSlotCount > 0)
        {
            targetBuffer = new Collider2D[targetSlotCount];
        }
    }
}

public interface IEnemySituationSensor
{
    void Capture(ref EnemySituationEvaluator.SituationSnapshot snapshot);
}
