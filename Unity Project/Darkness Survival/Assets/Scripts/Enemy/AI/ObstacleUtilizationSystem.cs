using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles obstacle detection, cover-seeking, line-of-sight blocking, and player herding behaviors.
/// Integrates with the existing AI system to enable tactical use of environment.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Monsters))]
public class ObstacleUtilizationSystem : MonoBehaviour
{
    [Header("Obstacle Detection")]
    [SerializeField] float obstacleDetectionRadius = 8f;
    [SerializeField] int maxObstacles = 8;
    [SerializeField] LayerMask obstacleLayerMask;
    
    [Header("Cover Seeking")]
    [SerializeField] float coverSeekingHpThreshold = 0.4f;
    [SerializeField] float coverDistanceThreshold = 2f;
    [SerializeField] bool maintainLineOfSightWhenSeeking = true;
    
    [Header("Line of Sight")]
    [SerializeField] float lineOfSightCheckDistance = 15f;
    [SerializeField] bool prioritizeCoverAgainstRanged = true;
    
    [Header("Player Herding")]
    [SerializeField] float herdingDetectionRadius = 12f;
    [SerializeField] float deadEndAngleThreshold = 120f;
    [SerializeField] float herdingPushDistance = 3f;
    
    [Header("Obstruction Penalty")]
    [SerializeField] float obstructionPenaltyAmount = -0.05f;
    [SerializeField] float obstructionCheckInterval = 0.5f;
    
    [Header("Pathfinding Optimization")]
    [SerializeField] int maxCachedPaths = 10;
    [SerializeField] float pathCacheValidityDuration = 5f;
    [SerializeField] bool enablePathLearning = true;
    
    [Header("Debug")]
    [SerializeField] bool showDebugGizmos = false;
    
    // Cached components
    Monsters monster;
    RewardCalculator rewardCalculator;
    Transform playerTransform;
    
    // Obstacle data
    struct ObstacleInfo
    {
        public Vector2 position;
        public float distance;
        public Collider2D collider;
        public Bounds bounds;
    }
    
    List<ObstacleInfo> detectedObstacles = new List<ObstacleInfo>();
    Vector2 nearestCoverPosition;
    bool hasNearestCover;
    
    // Line of sight
    bool hasLineOfSightToPlayer;
    
    // Herding data
    struct DeadEndInfo
    {
        public Vector2 position;
        public Vector2 direction;
        public float confinementScore;
    }
    
    List<DeadEndInfo> detectedDeadEnds = new List<DeadEndInfo>();
    
    // Pathfinding cache
    struct CachedPath
    {
        public Vector2 start;
        public Vector2 end;
        public Vector2[] waypoints;
        public float timestamp;
        public int useCount;
    }
    
    List<CachedPath> pathCache = new List<CachedPath>();
    
    // Timers
    float obstructionCheckTimer;
    
    void Awake()
    {
        monster = GetComponent<Monsters>();
        rewardCalculator = GetComponent<RewardCalculator>();
        
        // Set default layer mask if not configured
        if (obstacleLayerMask == 0)
        {
            obstacleLayerMask = LayerMask.GetMask("Default", "Obstacle", "Wall");
        }
    }
    
    void Start()
    {
        CachePlayerReference();
        obstructionCheckTimer = obstructionCheckInterval;
    }
    
    void Update()
    {
        if (playerTransform == null)
        {
            CachePlayerReference();
            if (playerTransform == null) return;
        }
        
        // Update obstacle detection
        DetectNearbyObstacles();
        
        // Update line of sight
        UpdateLineOfSight();
        
        // Check for obstruction penalty
        obstructionCheckTimer -= Time.deltaTime;
        if (obstructionCheckTimer <= 0f)
        {
            obstructionCheckTimer = obstructionCheckInterval;
            CheckObstructionPenalty();
        }
        
        // Update dead end detection for herding
        DetectDeadEnds();
    }
    
    void CachePlayerReference()
    {
        if (GameManager.instance != null)
        {
            playerTransform = GameManager.instance.playerTransform;
        }
    }
    
    /// <summary>
    /// Detects nearby obstacles and updates the obstacle list.
    /// Requirement 5.1, 5.2
    /// </summary>
    void DetectNearbyObstacles()
    {
        detectedObstacles.Clear();
        hasNearestCover = false;
        
        Vector2 position = transform.position;
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(position, obstacleDetectionRadius, obstacleLayerMask);
        
        float nearestCoverDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue;
            
            Vector2 obstaclePos = col.transform.position;
            float distance = Vector2.Distance(position, obstaclePos);
            
            ObstacleInfo info = new ObstacleInfo
            {
                position = obstaclePos,
                distance = distance,
                collider = col,
                bounds = col.bounds
            };
            
            detectedObstacles.Add(info);
            
            // Track nearest cover
            if (distance < nearestCoverDistance)
            {
                nearestCoverDistance = distance;
                nearestCoverPosition = obstaclePos;
                hasNearestCover = true;
            }
            
            if (detectedObstacles.Count >= maxObstacles)
            {
                break;
            }
        }
        
        // Sort by distance
        detectedObstacles.Sort((a, b) => a.distance.CompareTo(b.distance));
    }
    
    /// <summary>
    /// Updates line of sight status to player.
    /// Requirement 5.2
    /// </summary>
    void UpdateLineOfSight()
    {
        if (playerTransform == null)
        {
            hasLineOfSightToPlayer = false;
            return;
        }
        
        Vector2 position = transform.position;
        Vector2 playerPos = playerTransform.position;
        Vector2 direction = (playerPos - position).normalized;
        float distance = Vector2.Distance(position, playerPos);
        
        RaycastHit2D hit = Physics2D.Raycast(position, direction, Mathf.Min(distance, lineOfSightCheckDistance), obstacleLayerMask);
        hasLineOfSightToPlayer = hit.collider == null;
    }
    
    /// <summary>
    /// Calculates the direction to move toward cover when HP is low.
    /// Requirement 5.1
    /// </summary>
    public Vector2 GetCoverSeekingDirection()
    {
        if (!hasNearestCover || playerTransform == null)
        {
            return Vector2.zero;
        }
        
        Vector2 position = transform.position;
        Vector2 toCover = (nearestCoverPosition - position).normalized;
        
        // If we need to maintain line of sight, adjust direction
        if (maintainLineOfSightWhenSeeking)
        {
            Vector2 playerPos = playerTransform.position;
            Vector2 toPlayer = (playerPos - position).normalized;
            
            // Blend between moving to cover and maintaining sight
            toCover = Vector2.Lerp(toCover, toPlayer, 0.3f).normalized;
        }
        
        return toCover;
    }
    
    /// <summary>
    /// Checks if the monster should seek cover based on HP.
    /// Requirement 5.1
    /// </summary>
    public bool ShouldSeekCover()
    {
        if (!hasNearestCover) return false;
        
        float hpRatio = monster.MAX_HP > 0f ? monster.HP / monster.MAX_HP : 0f;
        return hpRatio < coverSeekingHpThreshold;
    }
    
    /// <summary>
    /// Checks if an obstacle blocks line of sight to player.
    /// Requirement 5.2
    /// </summary>
    public bool IsPlayerLineOfSightBlocked()
    {
        return !hasLineOfSightToPlayer;
    }
    
    /// <summary>
    /// Gets the best position to use an obstacle as a shield against the player.
    /// Requirement 5.2
    /// </summary>
    public Vector2 GetShieldPosition()
    {
        if (detectedObstacles.Count == 0 || playerTransform == null)
        {
            return transform.position;
        }
        
        Vector2 position = transform.position;
        Vector2 playerPos = playerTransform.position;
        
        // Find the obstacle that best blocks line of sight
        ObstacleInfo bestObstacle = detectedObstacles[0];
        float bestScore = 0f;
        
        foreach (ObstacleInfo obstacle in detectedObstacles)
        {
            // Calculate if this obstacle is between us and player
            Vector2 toObstacle = obstacle.position - position;
            Vector2 toPlayer = playerPos - position;
            
            float alignment = Vector2.Dot(toObstacle.normalized, toPlayer.normalized);
            float distanceScore = 1f - Mathf.Clamp01(obstacle.distance / obstacleDetectionRadius);
            float score = alignment * distanceScore;
            
            if (score > bestScore)
            {
                bestScore = score;
                bestObstacle = obstacle;
            }
        }
        
        // Position on the opposite side of the obstacle from the player
        Vector2 playerToObstacle = (bestObstacle.position - playerPos).normalized;
        return bestObstacle.position + playerToObstacle * 1.5f;
    }
    
    /// <summary>
    /// Detects dead-end areas where the player can be herded.
    /// Requirement 5.3
    /// </summary>
    void DetectDeadEnds()
    {
        detectedDeadEnds.Clear();
        
        if (playerTransform == null || detectedObstacles.Count < 2)
        {
            return;
        }
        
        Vector2 playerPos = playerTransform.position;
        
        // Look for clusters of obstacles that form corners or dead ends
        for (int i = 0; i < detectedObstacles.Count - 1; i++)
        {
            for (int j = i + 1; j < detectedObstacles.Count; j++)
            {
                ObstacleInfo obs1 = detectedObstacles[i];
                ObstacleInfo obs2 = detectedObstacles[j];
                
                // Check if these obstacles form a corner
                Vector2 toObs1 = (obs1.position - playerPos).normalized;
                Vector2 toObs2 = (obs2.position - playerPos).normalized;
                
                float angle = Vector2.Angle(toObs1, toObs2);
                
                // If angle is less than threshold, this could be a dead end
                if (angle < deadEndAngleThreshold)
                {
                    Vector2 deadEndCenter = (obs1.position + obs2.position) * 0.5f;
                    Vector2 directionToDeadEnd = (deadEndCenter - playerPos).normalized;
                    
                    float confinementScore = 1f - (angle / deadEndAngleThreshold);
                    
                    DeadEndInfo deadEnd = new DeadEndInfo
                    {
                        position = deadEndCenter,
                        direction = directionToDeadEnd,
                        confinementScore = confinementScore
                    };
                    
                    detectedDeadEnds.Add(deadEnd);
                }
            }
        }
        
        // Sort by confinement score
        detectedDeadEnds.Sort((a, b) => b.confinementScore.CompareTo(a.confinementScore));
    }
    
    /// <summary>
    /// Gets the direction to push the player toward a tactical disadvantage.
    /// Requirement 5.3
    /// </summary>
    public Vector2 GetHerdingDirection()
    {
        if (detectedDeadEnds.Count == 0 || playerTransform == null)
        {
            return Vector2.zero;
        }
        
        Vector2 position = transform.position;
        Vector2 playerPos = playerTransform.position;
        DeadEndInfo bestDeadEnd = detectedDeadEnds[0];
        
        // Calculate position to push player toward dead end
        Vector2 pushPosition = playerPos + bestDeadEnd.direction * herdingPushDistance;
        
        // Move to intercept between player and dead end
        Vector2 interceptPosition = Vector2.Lerp(playerPos, bestDeadEnd.position, 0.7f);
        
        return (interceptPosition - position).normalized;
    }
    
    /// <summary>
    /// Checks if the player is in a position that can be herded.
    /// Requirement 5.3
    /// </summary>
    public bool CanHerdPlayer()
    {
        return detectedDeadEnds.Count > 0 && playerTransform != null;
    }
    
    /// <summary>
    /// Applies penalty if monster is obstructed.
    /// Requirement 5.5
    /// </summary>
    void CheckObstructionPenalty()
    {
        if (monster.OBJECTS_DETECTION != null && monster.OBJECTS_DETECTION.IsDetected())
        {
            if (rewardCalculator != null)
            {
                rewardCalculator.ApplyObstructionPenalty(obstructionPenaltyAmount);
            }
        }
    }
    
    /// <summary>
    /// Finds an optimal path around obstacles.
    /// Requirement 5.4
    /// </summary>
    public Vector2[] FindOptimalPath(Vector2 start, Vector2 goal)
    {
        if (!enablePathLearning)
        {
            return new Vector2[] { goal };
        }
        
        // Check cache first
        CachedPath? cachedPath = GetCachedPath(start, goal);
        if (cachedPath.HasValue)
        {
            UpdatePathUsage(cachedPath.Value);
            return cachedPath.Value.waypoints;
        }
        
        // Simple pathfinding: check if direct path is clear
        Vector2 direction = (goal - start).normalized;
        float distance = Vector2.Distance(start, goal);
        
        RaycastHit2D hit = Physics2D.Raycast(start, direction, distance, obstacleLayerMask);
        
        if (hit.collider == null)
        {
            // Direct path is clear
            Vector2[] path = new Vector2[] { goal };
            CachePath(start, goal, path);
            return path;
        }
        
        // Path is blocked, find waypoint around obstacle
        Vector2 hitPoint = hit.point;
        Vector2 hitNormal = hit.normal;
        
        // Try going around the obstacle
        Vector2 perpendicular = new Vector2(-hitNormal.y, hitNormal.x);
        Vector2 waypoint1 = hitPoint + perpendicular * 2f;
        Vector2 waypoint2 = hitPoint - perpendicular * 2f;
        
        // Choose the waypoint closer to the goal
        float dist1 = Vector2.Distance(waypoint1, goal);
        float dist2 = Vector2.Distance(waypoint2, goal);
        
        Vector2 chosenWaypoint = dist1 < dist2 ? waypoint1 : waypoint2;
        
        Vector2[] pathWithWaypoint = new Vector2[] { chosenWaypoint, goal };
        CachePath(start, goal, pathWithWaypoint);
        
        return pathWithWaypoint;
    }
    
    CachedPath? GetCachedPath(Vector2 start, Vector2 goal)
    {
        float currentTime = Time.time;
        
        foreach (CachedPath cached in pathCache)
        {
            // Check if path is still valid
            if (currentTime - cached.timestamp > pathCacheValidityDuration)
            {
                continue;
            }
            
            // Check if start and goal are close enough to cached path
            float startDist = Vector2.Distance(start, cached.start);
            float goalDist = Vector2.Distance(goal, cached.end);
            
            if (startDist < 1f && goalDist < 1f)
            {
                return cached;
            }
        }
        
        return null;
    }
    
    void CachePath(Vector2 start, Vector2 goal, Vector2[] waypoints)
    {
        // Remove old paths if cache is full
        if (pathCache.Count >= maxCachedPaths)
        {
            // Remove least used path
            int minUseIndex = 0;
            int minUseCount = int.MaxValue;
            
            for (int i = 0; i < pathCache.Count; i++)
            {
                if (pathCache[i].useCount < minUseCount)
                {
                    minUseCount = pathCache[i].useCount;
                    minUseIndex = i;
                }
            }
            
            pathCache.RemoveAt(minUseIndex);
        }
        
        CachedPath newPath = new CachedPath
        {
            start = start,
            end = goal,
            waypoints = waypoints,
            timestamp = Time.time,
            useCount = 1
        };
        
        pathCache.Add(newPath);
    }
    
    void UpdatePathUsage(CachedPath path)
    {
        for (int i = 0; i < pathCache.Count; i++)
        {
            if (pathCache[i].start == path.start && pathCache[i].end == path.end)
            {
                CachedPath updated = pathCache[i];
                updated.useCount++;
                pathCache[i] = updated;
                break;
            }
        }
    }
    
    // Public getters for observation space
    public Vector2[] GetDetectedObstaclePositions()
    {
        Vector2[] positions = new Vector2[maxObstacles];
        for (int i = 0; i < maxObstacles; i++)
        {
            if (i < detectedObstacles.Count)
            {
                positions[i] = detectedObstacles[i].position;
            }
            else
            {
                positions[i] = Vector2.zero;
            }
        }
        return positions;
    }
    
    public int GetObstacleCount()
    {
        return detectedObstacles.Count;
    }
    
    public Vector2 GetNearestCoverPosition()
    {
        return hasNearestCover ? nearestCoverPosition : Vector2.zero;
    }
    
    public bool HasLineOfSight()
    {
        return hasLineOfSightToPlayer;
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw obstacle detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, obstacleDetectionRadius);
        
        // Draw detected obstacles
        Gizmos.color = Color.red;
        foreach (ObstacleInfo obstacle in detectedObstacles)
        {
            Gizmos.DrawWireCube(obstacle.position, Vector3.one * 0.5f);
        }
        
        // Draw nearest cover
        if (hasNearestCover)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(nearestCoverPosition, 0.5f);
            Gizmos.DrawLine(transform.position, nearestCoverPosition);
        }
        
        // Draw line of sight
        if (playerTransform != null)
        {
            Gizmos.color = hasLineOfSightToPlayer ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
        
        // Draw dead ends
        Gizmos.color = Color.magenta;
        foreach (DeadEndInfo deadEnd in detectedDeadEnds)
        {
            Gizmos.DrawWireSphere(deadEnd.position, 1f);
            Gizmos.DrawRay(deadEnd.position, deadEnd.direction * 2f);
        }
    }
}
