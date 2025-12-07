using UnityEngine;

/// <summary>
/// Implements tactical positioning behaviors for monsters including kiting, flanking,
/// optimal distance maintenance, and corner-cutting pathfinding.
/// </summary>
[RequireComponent(typeof(Monsters))]
public class TacticalPositioningBehavior : MonoBehaviour
{
    [Header("Kiting Settings")]
    [SerializeField] float kiteCounterattackRange = 2.5f;
    [SerializeField] float kiteRetreatDistance = 4f;
    [SerializeField] float kiteMinimumDistanceIncrease = 1.5f;
    
    [Header("Flanking Settings")]
    [SerializeField] float flankingMinAngle = 45f;
    [SerializeField] float flankingOptimalAngle = 120f;
    [SerializeField] float flankingDetectionRadius = 8f;
    
    [Header("Optimal Distance Settings")]
    [SerializeField] float optimalDistanceMin = 2f;
    [SerializeField] float optimalDistanceMax = 4f;
    [SerializeField] float optimalDistanceApproachSpeed = 1f;
    [SerializeField] float optimalDistanceRetreatSpeed = 1.2f;
    
    [Header("Corner Cutting Settings")]
    [SerializeField] float cornerCutDetectionRadius = 6f;
    [SerializeField] float cornerCutInterceptAngle = 30f;
    [SerializeField] LayerMask obstacleLayerMask;
    
    Monsters monster;
    Transform playerTransform;
    Vector2 lastPlayerPosition;
    float lastPlayerPositionUpdateTime;
    
    void Awake()
    {
        monster = GetComponent<Monsters>();
    }
    
    void Start()
    {
        if (GameManager.instance != null)
        {
            playerTransform = GameManager.instance.playerTransform;
        }
        
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
            lastPlayerPositionUpdateTime = Time.time;
        }
    }
    
    void Update()
    {
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
            lastPlayerPositionUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Calculates kiting behavior: attack then retreat movement vector.
    /// Ensures distance increases beyond counterattack range.
    /// </summary>
    /// <param name="currentPosition">Current monster position</param>
    /// <param name="playerPosition">Player position</param>
    /// <param name="attackCooldownRemaining">Remaining attack cooldown</param>
    /// <returns>Movement vector for kiting behavior</returns>
    public Vector2 CalculateKitingVector(Vector2 currentPosition, Vector2 playerPosition, float attackCooldownRemaining)
    {
        float currentDistance = Vector2.Distance(currentPosition, playerPosition);
        
        // If attack is on cooldown, retreat away from player
        if (attackCooldownRemaining > 0f)
        {
            // Calculate retreat direction (away from player)
            Vector2 retreatDirection = (currentPosition - playerPosition).normalized;
            
            // Calculate how far we need to retreat to be safe
            float targetDistance = currentDistance + kiteMinimumDistanceIncrease;
            if (targetDistance < kiteCounterattackRange)
            {
                targetDistance = kiteCounterattackRange + 0.5f;
            }
            
            // Check if path is clear for retreat
            Vector2 retreatVector = retreatDirection;
            if (IsPathBlocked(currentPosition, retreatDirection, kiteRetreatDistance))
            {
                // Try perpendicular directions if direct retreat is blocked
                Vector2 perpendicular1 = new Vector2(-retreatDirection.y, retreatDirection.x);
                Vector2 perpendicular2 = new Vector2(retreatDirection.y, -retreatDirection.x);
                
                if (!IsPathBlocked(currentPosition, perpendicular1, kiteRetreatDistance * 0.5f))
                {
                    retreatVector = perpendicular1;
                }
                else if (!IsPathBlocked(currentPosition, perpendicular2, kiteRetreatDistance * 0.5f))
                {
                    retreatVector = perpendicular2;
                }
            }
            
            return retreatVector;
        }
        else
        {
            // Attack phase: move toward player to attack
            Vector2 approachDirection = (playerPosition - currentPosition).normalized;
            return approachDirection;
        }
    }
    
    /// <summary>
    /// Calculates flanking approach angle relative to player facing direction.
    /// Selects paths that approach from sides/rear (>45 degrees).
    /// </summary>
    /// <param name="currentPosition">Current monster position</param>
    /// <param name="playerPosition">Player position</param>
    /// <param name="playerVelocity">Player velocity (used to determine facing)</param>
    /// <returns>Movement vector for flanking approach</returns>
    public Vector2 CalculateFlankingVector(Vector2 currentPosition, Vector2 playerPosition, Vector2 playerVelocity)
    {
        // Determine player facing direction
        Vector2 playerFacing;
        if (playerVelocity.sqrMagnitude > 0.01f)
        {
            playerFacing = playerVelocity.normalized;
        }
        else
        {
            // If player is stationary, assume they're facing the monster
            playerFacing = (currentPosition - playerPosition).normalized;
        }
        
        // Calculate direct approach vector
        Vector2 toMonster = (currentPosition - playerPosition).normalized;
        
        // Calculate current angle relative to player facing
        float currentAngle = Vector2.SignedAngle(playerFacing, toMonster);
        
        // If already flanking (angle > 45 degrees), maintain approach
        if (Mathf.Abs(currentAngle) >= flankingMinAngle)
        {
            // Move toward player while maintaining flanking angle
            Vector2 approachDirection = (playerPosition - currentPosition).normalized;
            return approachDirection;
        }
        
        // Need to reposition to flanking angle
        // Choose left or right flank based on which is closer
        float targetAngle = currentAngle > 0 ? flankingOptimalAngle : -flankingOptimalAngle;
        
        // Calculate target position at flanking angle
        float angleInRadians = (targetAngle + Vector2.SignedAngle(Vector2.right, playerFacing)) * Mathf.Deg2Rad;
        float distance = Vector2.Distance(currentPosition, playerPosition);
        Vector2 targetPosition = playerPosition + new Vector2(
            Mathf.Cos(angleInRadians) * distance,
            Mathf.Sin(angleInRadians) * distance
        );
        
        // Move toward target flanking position
        Vector2 flankingDirection = (targetPosition - currentPosition).normalized;
        
        // Check if path is clear
        if (IsPathBlocked(currentPosition, flankingDirection, flankingDetectionRadius))
        {
            // If blocked, try the opposite flank
            targetAngle = -targetAngle;
            angleInRadians = (targetAngle + Vector2.SignedAngle(Vector2.right, playerFacing)) * Mathf.Deg2Rad;
            targetPosition = playerPosition + new Vector2(
                Mathf.Cos(angleInRadians) * distance,
                Mathf.Sin(angleInRadians) * distance
            );
            flankingDirection = (targetPosition - currentPosition).normalized;
        }
        
        return flankingDirection;
    }
    
    /// <summary>
    /// Calculates movement to maintain optimal combat distance.
    /// Balances approach and retreat based on current distance.
    /// </summary>
    /// <param name="currentPosition">Current monster position</param>
    /// <param name="playerPosition">Player position</param>
    /// <returns>Movement vector for optimal distance maintenance</returns>
    public Vector2 CalculateOptimalDistanceVector(Vector2 currentPosition, Vector2 playerPosition)
    {
        float currentDistance = Vector2.Distance(currentPosition, playerPosition);
        float optimalDistance = (optimalDistanceMin + optimalDistanceMax) / 2f;
        
        // If within optimal range, maintain position with slight adjustments
        if (currentDistance >= optimalDistanceMin && currentDistance <= optimalDistanceMax)
        {
            // Strafe around player to maintain engagement
            Vector2 toPlayer = (playerPosition - currentPosition).normalized;
            Vector2 strafeDirection = new Vector2(-toPlayer.y, toPlayer.x);
            
            // Randomly choose strafe direction
            if (Time.time % 4f < 2f)
            {
                strafeDirection = -strafeDirection;
            }
            
            return strafeDirection * 0.5f;
        }
        
        // Too close - retreat
        if (currentDistance < optimalDistanceMin)
        {
            Vector2 retreatDirection = (currentPosition - playerPosition).normalized;
            return retreatDirection * optimalDistanceRetreatSpeed;
        }
        
        // Too far - approach
        if (currentDistance > optimalDistanceMax)
        {
            Vector2 approachDirection = (playerPosition - currentPosition).normalized;
            return approachDirection * optimalDistanceApproachSpeed;
        }
        
        return Vector2.zero;
    }
    
    /// <summary>
    /// Detects when player moves around obstacles and calculates shortcut paths.
    /// </summary>
    /// <param name="currentPosition">Current monster position</param>
    /// <param name="playerPosition">Player position</param>
    /// <param name="playerVelocity">Player velocity</param>
    /// <returns>Movement vector for corner-cutting intercept</returns>
    public Vector2 CalculateCornerCuttingVector(Vector2 currentPosition, Vector2 playerPosition, Vector2 playerVelocity)
    {
        // Check if player is moving
        if (playerVelocity.sqrMagnitude < 0.1f)
        {
            // Player not moving, use direct approach
            return (playerPosition - currentPosition).normalized;
        }
        
        // Predict player's future position
        float predictionTime = 1f;
        Vector2 predictedPlayerPosition = playerPosition + playerVelocity * predictionTime;
        
        // Check if there's an obstacle between current position and predicted position
        Vector2 toPlayer = playerPosition - currentPosition;
        Vector2 toPredicted = predictedPlayerPosition - currentPosition;
        
        // If player is moving around an obstacle, try to intercept
        RaycastHit2D hitToPlayer = Physics2D.Raycast(currentPosition, toPlayer.normalized, toPlayer.magnitude, obstacleLayerMask);
        RaycastHit2D hitToPredicted = Physics2D.Raycast(currentPosition, toPredicted.normalized, toPredicted.magnitude, obstacleLayerMask);
        
        // If direct path to player is blocked but path to predicted position is clear
        if (hitToPlayer.collider != null && hitToPredicted.collider == null)
        {
            // Calculate intercept point
            Vector2 interceptDirection = (predictedPlayerPosition - currentPosition).normalized;
            return interceptDirection;
        }
        
        // If both paths are blocked, try to find a path around the obstacle
        if (hitToPlayer.collider != null)
        {
            // Get obstacle position
            Vector2 obstaclePosition = hitToPlayer.point;
            
            // Calculate two potential paths around the obstacle
            Vector2 toObstacle = (obstaclePosition - currentPosition).normalized;
            Vector2 perpendicular1 = new Vector2(-toObstacle.y, toObstacle.x);
            Vector2 perpendicular2 = new Vector2(toObstacle.y, -toObstacle.x);
            
            // Choose the path that gets us closer to the player
            Vector2 path1Target = obstaclePosition + perpendicular1 * 2f;
            Vector2 path2Target = obstaclePosition + perpendicular2 * 2f;
            
            float dist1 = Vector2.Distance(path1Target, playerPosition);
            float dist2 = Vector2.Distance(path2Target, playerPosition);
            
            Vector2 chosenPath = dist1 < dist2 ? perpendicular1 : perpendicular2;
            
            // Verify chosen path is not blocked
            if (!IsPathBlocked(currentPosition, chosenPath, 2f))
            {
                return chosenPath;
            }
        }
        
        // Default to direct approach
        return (playerPosition - currentPosition).normalized;
    }
    
    /// <summary>
    /// Checks if a path is blocked by obstacles.
    /// </summary>
    bool IsPathBlocked(Vector2 origin, Vector2 direction, float distance)
    {
        if (obstacleLayerMask == 0)
        {
            // If no obstacle layer mask is set, assume path is clear
            return false;
        }
        
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, obstacleLayerMask);
        return hit.collider != null;
    }
    
    /// <summary>
    /// Gets the optimal distance range for this monster type.
    /// </summary>
    public Vector2 GetOptimalDistanceRange()
    {
        return new Vector2(optimalDistanceMin, optimalDistanceMax);
    }
    
    /// <summary>
    /// Sets the optimal distance range for this monster type.
    /// </summary>
    public void SetOptimalDistanceRange(float min, float max)
    {
        optimalDistanceMin = Mathf.Max(min, 0.5f);
        optimalDistanceMax = Mathf.Max(max, optimalDistanceMin + 0.5f);
    }
}
