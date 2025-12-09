using UnityEngine;

/// <summary>
/// Requirement 14.1: Implements gizmo rendering for AI state visualization.
/// Draws movement vectors, attack ranges, and optimal positioning zones.
/// </summary>
[RequireComponent(typeof(Monsters))]
public class AIDebugVisualizer : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] bool showGizmos = false;
    [SerializeField] bool showMovementVectors = true;
    [SerializeField] bool showAttackRanges = true;
    [SerializeField] bool showOptimalPositioning = true;
    [SerializeField] bool showCoordinationLines = true;
    
    [Header("Visual Settings")]
    [SerializeField] float movementVectorScale = 2f;
    [SerializeField] float arrowHeadSize = 0.3f;
    [SerializeField] Color movementVectorColor = Color.cyan;
    [SerializeField] Color attackRangeColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] Color optimalRangeColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] Color coordinationLineColor = new Color(1f, 1f, 0f, 0.5f);
    
    [Header("Range Settings")]
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float optimalDistanceMin = 2f;
    [SerializeField] float optimalDistanceMax = 4f;
    
    // Component references
    Monsters monster;
    Rigidbody2D rb;
    Transform playerTransform;
    CooperativeBehaviorSystem cooperativeSystem;
    
    // Cached state
    Vector2 currentVelocity;
    Vector2 currentPosition;
    
    void Awake()
    {
        monster = GetComponent<Monsters>();
        rb = GetComponent<Rigidbody2D>();
        cooperativeSystem = GetComponent<CooperativeBehaviorSystem>();
    }
    
    void Start()
    {
        // Cache player reference
        if (GameManager.instance != null)
        {
            playerTransform = GameManager.instance.playerTransform;
        }
    }
    
    void Update()
    {
        if (!showGizmos) return;
        
        // Update cached state
        currentPosition = transform.position;
        currentVelocity = rb != null ? rb.velocity : Vector2.zero;
    }
    
    /// <summary>
    /// Requirement 14.1: Draw movement vectors as arrows.
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Vector2 position = Application.isPlaying ? currentPosition : (Vector2)transform.position;
        
        // Draw movement vectors
        if (showMovementVectors)
        {
            DrawMovementVector(position);
        }
        
        // Draw attack ranges
        if (showAttackRanges)
        {
            DrawAttackRange(position);
        }
        
        // Draw optimal positioning zones
        if (showOptimalPositioning)
        {
            DrawOptimalPositioningZone(position);
        }
        
        // Draw coordination lines
        if (showCoordinationLines && Application.isPlaying)
        {
            DrawCoordinationLines(position);
        }
    }
    
    /// <summary>
    /// Requirement 14.1: Draw movement vectors as arrows.
    /// </summary>
    void DrawMovementVector(Vector2 position)
    {
        Vector2 velocity = Application.isPlaying ? currentVelocity : Vector2.zero;
        
        if (velocity.sqrMagnitude < 0.01f) return;
        
        Vector2 direction = velocity.normalized;
        Vector2 endPoint = position + direction * movementVectorScale;
        
        // Draw main arrow line
        Gizmos.color = movementVectorColor;
        Gizmos.DrawLine(position, endPoint);
        
        // Draw arrow head
        DrawArrowHead(endPoint, direction, arrowHeadSize, movementVectorColor);
    }
    
    /// <summary>
    /// Requirement 14.1: Draw attack ranges as circles.
    /// </summary>
    void DrawAttackRange(Vector2 position)
    {
        Gizmos.color = attackRangeColor;
        DrawCircle(position, attackRange, 32);
    }
    
    /// <summary>
    /// Requirement 14.1: Draw optimal positioning zones as colored rings.
    /// </summary>
    void DrawOptimalPositioningZone(Vector2 position)
    {
        Gizmos.color = optimalRangeColor;
        
        // Draw inner ring
        DrawCircle(position, optimalDistanceMin, 32);
        
        // Draw outer ring
        DrawCircle(position, optimalDistanceMax, 32);
        
        // Draw connecting lines to show the zone
        int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            Vector2 innerPoint = position + direction * optimalDistanceMin;
            Vector2 outerPoint = position + direction * optimalDistanceMax;
            
            Gizmos.DrawLine(innerPoint, outerPoint);
        }
    }
    
    /// <summary>
    /// Requirement 14.5: Draw lines between cooperating monsters.
    /// </summary>
    void DrawCoordinationLines(Vector2 position)
    {
        if (cooperativeSystem == null) return;
        
        // Get nearby allies
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(position, 10f);
        
        foreach (Collider2D col in nearbyColliders)
        {
            if (col == null || col.gameObject == gameObject) continue;
            
            Monsters allyMonster = col.GetComponent<Monsters>();
            if (allyMonster != null && allyMonster.gameObject.CompareTag(gameObject.tag))
            {
                // Draw line to ally
                Gizmos.color = coordinationLineColor;
                Gizmos.DrawLine(position, col.transform.position);
            }
        }
    }
    
    /// <summary>
    /// Helper method to draw a circle using gizmos.
    /// </summary>
    void DrawCircle(Vector2 center, float radius, int segments)
    {
        if (segments < 3) segments = 3;
        
        float angleStep = 360f / segments;
        Vector2 prevPoint = center + new Vector2(radius, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    /// <summary>
    /// Helper method to draw an arrow head.
    /// </summary>
    void DrawArrowHead(Vector2 tip, Vector2 direction, float size, Color color)
    {
        Gizmos.color = color;
        
        // Calculate perpendicular vector
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        
        // Draw two lines forming the arrow head
        Vector2 left = tip - direction * size + perpendicular * size * 0.5f;
        Vector2 right = tip - direction * size - perpendicular * size * 0.5f;
        
        Gizmos.DrawLine(tip, left);
        Gizmos.DrawLine(tip, right);
    }
    
    /// <summary>
    /// Public method to enable/disable gizmo rendering via inspector flag.
    /// Requirement 14.1: Enable/disable via inspector flag.
    /// </summary>
    public void SetShowGizmos(bool show)
    {
        showGizmos = show;
    }
    
    /// <summary>
    /// Get current gizmo visibility state.
    /// </summary>
    public bool IsShowingGizmos()
    {
        return showGizmos;
    }
}
