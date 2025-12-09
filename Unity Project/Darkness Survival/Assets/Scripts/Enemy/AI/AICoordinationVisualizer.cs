using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Requirement 14.5: Implements coordination visualization for AI.
/// Draws lines between cooperating monsters.
/// Color-codes lines by coordination type (pincer=red, relay=blue).
/// Shows coordination state labels.
/// </summary>
[RequireComponent(typeof(Monsters))]
public class AICoordinationVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] bool showCoordinationLines = false;
    [SerializeField] bool showCoordinationLabels = true;
    [SerializeField] float lineWidth = 0.1f;
    [SerializeField] float labelOffset = 0.5f;
    
    [Header("Color Settings")]
    [SerializeField] Color pincerAttackColor = Color.red;
    [SerializeField] Color relayChaseColor = Color.blue;
    [SerializeField] Color tankAndSpankColor = Color.yellow;
    [SerializeField] Color generalCoordinationColor = Color.cyan;
    [SerializeField] float lineAlpha = 0.5f;
    
    [Header("Detection Settings")]
    [SerializeField] float coordinationDetectionRadius = 10f;
    [SerializeField] float updateInterval = 0.2f;
    
    // Component references
    Monsters monster;
    CooperativeBehaviorSystem cooperativeSystem;
    
    // Coordination tracking
    List<CoordinationLink> activeCoordinations = new List<CoordinationLink>();
    float updateTimer;
    
    // Line renderer pool
    List<LineRenderer> lineRendererPool = new List<LineRenderer>();
    int poolSize = 5;
    
    void Awake()
    {
        monster = GetComponent<Monsters>();
        cooperativeSystem = GetComponent<CooperativeBehaviorSystem>();
        
        // Initialize line renderer pool
        InitializeLineRendererPool();
    }
    
    void Update()
    {
        if (!showCoordinationLines) return;
        
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            updateTimer = updateInterval;
            UpdateCoordinations();
        }
        
        RenderCoordinationLines();
    }
    
    /// <summary>
    /// Requirement 14.5: Draw lines between cooperating monsters.
    /// </summary>
    void UpdateCoordinations()
    {
        activeCoordinations.Clear();
        
        // Find nearby allies
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, coordinationDetectionRadius);
        
        foreach (Collider2D col in nearbyColliders)
        {
            if (col == null || col.gameObject == gameObject) continue;
            
            Monsters allyMonster = col.GetComponent<Monsters>();
            if (allyMonster != null && allyMonster.gameObject.CompareTag(gameObject.tag))
            {
                // Determine coordination type
                CoordinationType coordType = DetermineCoordinationType(allyMonster);
                
                if (coordType != CoordinationType.None)
                {
                    CoordinationLink link = new CoordinationLink
                    {
                        ally = allyMonster,
                        coordinationType = coordType,
                        startPosition = transform.position,
                        endPosition = allyMonster.transform.position
                    };
                    
                    activeCoordinations.Add(link);
                }
            }
        }
    }
    
    /// <summary>
    /// Determine the type of coordination with an ally.
    /// </summary>
    CoordinationType DetermineCoordinationType(Monsters ally)
    {
        if (cooperativeSystem == null) return CoordinationType.General;
        
        // Check for pincer attack (approaching from different angles)
        Vector2 playerPos = GetPlayerPosition();
        if (playerPos != Vector2.zero)
        {
            Vector2 myDirection = (playerPos - (Vector2)transform.position).normalized;
            Vector2 allyDirection = (playerPos - (Vector2)ally.transform.position).normalized;
            
            float angle = Vector2.Angle(myDirection, allyDirection);
            
            // Pincer attack: approaching from significantly different angles
            if (angle > 60f && angle < 120f)
            {
                return CoordinationType.PincerAttack;
            }
        }
        
        // Check for tank and spank (HP-based roles)
        float myHpRatio = monster.HP / monster.MAX_HP;
        float allyHpRatio = ally.HP / ally.MAX_HP;
        
        if (Mathf.Abs(myHpRatio - allyHpRatio) > 0.3f)
        {
            return CoordinationType.TankAndSpank;
        }
        
        // Check for relay chase (alternating pursuit)
        // This would require more state tracking, so default to general for now
        
        return CoordinationType.General;
    }
    
    /// <summary>
    /// Requirement 14.5: Draw lines between cooperating monsters.
    /// Color-code lines by coordination type.
    /// </summary>
    void RenderCoordinationLines()
    {
        // Return all line renderers to pool first
        foreach (LineRenderer lr in lineRendererPool)
        {
            if (lr != null)
            {
                lr.enabled = false;
            }
        }
        
        // Render active coordinations
        for (int i = 0; i < activeCoordinations.Count && i < lineRendererPool.Count; i++)
        {
            CoordinationLink link = activeCoordinations[i];
            LineRenderer lr = lineRendererPool[i];
            
            if (lr == null || link.ally == null) continue;
            
            lr.enabled = true;
            lr.SetPosition(0, link.startPosition);
            lr.SetPosition(1, link.endPosition);
            
            // Set color based on coordination type
            Color lineColor = GetCoordinationColor(link.coordinationType);
            lineColor.a = lineAlpha;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
        }
    }
    
    /// <summary>
    /// Requirement 14.5: Color-code lines by coordination type (pincer=red, relay=blue).
    /// </summary>
    Color GetCoordinationColor(CoordinationType type)
    {
        switch (type)
        {
            case CoordinationType.PincerAttack:
                return pincerAttackColor;
            case CoordinationType.RelayChase:
                return relayChaseColor;
            case CoordinationType.TankAndSpank:
                return tankAndSpankColor;
            case CoordinationType.General:
            default:
                return generalCoordinationColor;
        }
    }
    
    /// <summary>
    /// Requirement 14.5: Show coordination state labels.
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showCoordinationLines || !showCoordinationLabels) return;
        
        foreach (CoordinationLink link in activeCoordinations)
        {
            if (link.ally == null) continue;
            
            // Draw label at midpoint
            Vector3 midpoint = (link.startPosition + link.endPosition) / 2f;
            midpoint.y += labelOffset;
            
            // Draw coordination type label
            string label = GetCoordinationLabel(link.coordinationType);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(midpoint, label);
            #endif
        }
    }
    
    /// <summary>
    /// Get label text for coordination type.
    /// </summary>
    string GetCoordinationLabel(CoordinationType type)
    {
        switch (type)
        {
            case CoordinationType.PincerAttack:
                return "PINCER";
            case CoordinationType.RelayChase:
                return "RELAY";
            case CoordinationType.TankAndSpank:
                return "TANK&SPANK";
            case CoordinationType.General:
            default:
                return "COORDINATING";
        }
    }
    
    /// <summary>
    /// Initialize line renderer pool.
    /// </summary>
    void InitializeLineRendererPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject lineObj = new GameObject($"CoordinationLine_{i}");
            lineObj.transform.SetParent(transform);
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.enabled = false;
            
            // Set material (use default material for now)
            lr.material = new Material(Shader.Find("Sprites/Default"));
            
            lineRendererPool.Add(lr);
        }
    }
    
    /// <summary>
    /// Get player position for coordination calculations.
    /// </summary>
    Vector2 GetPlayerPosition()
    {
        if (GameManager.instance != null && GameManager.instance.playerTransform != null)
        {
            return GameManager.instance.playerTransform.position;
        }
        return Vector2.zero;
    }
    
    /// <summary>
    /// Enable/disable coordination visualization.
    /// </summary>
    public void SetShowCoordinationLines(bool show)
    {
        showCoordinationLines = show;
        
        if (!show)
        {
            // Disable all line renderers
            foreach (LineRenderer lr in lineRendererPool)
            {
                if (lr != null)
                {
                    lr.enabled = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Get current visualization state.
    /// </summary>
    public bool IsShowingCoordinationLines()
    {
        return showCoordinationLines;
    }
    
    /// <summary>
    /// Get count of active coordinations.
    /// </summary>
    public int GetActiveCoordinationCount()
    {
        return activeCoordinations.Count;
    }
    
    void OnDestroy()
    {
        // Clean up line renderers
        foreach (LineRenderer lr in lineRendererPool)
        {
            if (lr != null && lr.gameObject != null)
            {
                Destroy(lr.gameObject);
            }
        }
        lineRendererPool.Clear();
    }
    
    /// <summary>
    /// Coordination type enum.
    /// </summary>
    public enum CoordinationType
    {
        None,
        PincerAttack,
        RelayChase,
        TankAndSpank,
        General
    }
    
    /// <summary>
    /// Coordination link data structure.
    /// </summary>
    struct CoordinationLink
    {
        public Monsters ally;
        public CoordinationType coordinationType;
        public Vector2 startPosition;
        public Vector2 endPosition;
    }
}
