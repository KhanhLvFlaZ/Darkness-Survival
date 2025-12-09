using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides visual feedback for monster AI behaviors and decisions.
/// Shows tactical indicators, AI tier status, and debug information.
/// Requirements: 8.1, 8.2, 8.3, 8.4, 8.5
/// </summary>
public class AIVisualFeedback : MonoBehaviour
{
    [Header("Indicator Prefabs")]
    [Tooltip("Brain icon prefab to show when tactical decisions are made")]
    public GameObject brainIconPrefab;
    
    [Tooltip("Level-up effect prefab to show when AI tier increases")]
    public GameObject levelUpEffectPrefab;
    
    [Header("Particle Systems")]
    [Tooltip("Particle effect for tactical decisions")]
    public ParticleSystem tacticalDecisionEffect;
    
    [Tooltip("Particle effect for flanking maneuvers")]
    public ParticleSystem flankingEffect;
    
    [Tooltip("Particle effect for kiting maneuvers")]
    public ParticleSystem kitingEffect;
    
    [Tooltip("Particle effect for coordinated attacks")]
    public ParticleSystem coordinatedAttackEffect;
    
    [Header("AI Tier Colors")]
    [Tooltip("Glow color for Novice tier (blue)")]
    public Color noviceGlowColor = new Color(0.3f, 0.5f, 1f, 0.5f);
    
    [Tooltip("Glow color for Learning tier (cyan)")]
    public Color learningGlowColor = new Color(0.3f, 0.7f, 1f, 0.6f);
    
    [Tooltip("Glow color for Trained tier (yellow)")]
    public Color trainedGlowColor = new Color(1f, 0.9f, 0.3f, 0.7f);
    
    [Tooltip("Glow color for Expert tier (gold)")]
    public Color expertGlowColor = new Color(1f, 0.8f, 0.2f, 0.8f);
    
    [Header("Debug Visualization")]
    [Tooltip("Show action type and reward labels above monster")]
    public bool showDebugLabels = false;
    
    [Tooltip("Render movement vectors and ranges as gizmos")]
    public bool showGizmos = false;
    
    [Tooltip("Show coordination lines between cooperating monsters")]
    public bool showCoordinationLines = false;
    
    [Header("Visual Settings")]
    [Tooltip("Duration to display tactical decision indicators (0.5-1.0 seconds)")]
    [Range(0.5f, 1.0f)]
    public float indicatorDuration = 0.75f;
    
    [Tooltip("Speed of color transitions")]
    [Range(0.1f, 5.0f)]
    public float colorTransitionSpeed = 2.0f;
    
    [Tooltip("Offset above monster for floating indicators")]
    public Vector3 indicatorOffset = new Vector3(0, 1.5f, 0);
    
    [Header("Debug Gizmo Settings")]
    [Tooltip("Attack range to visualize")]
    public float attackRange = 2.0f;
    
    [Tooltip("Optimal positioning range (min)")]
    public float optimalRangeMin = 1.5f;
    
    [Tooltip("Optimal positioning range (max)")]
    public float optimalRangeMax = 3.0f;
    
    // Internal state
    private AITier currentTier = AITier.Novice;
    private SpriteRenderer spriteRenderer;
    private Color targetGlowColor;
    private Color currentGlowColor;
    private GameObject activeIndicator;
    private Coroutine indicatorCoroutine;
    
    // Debug info
    private string currentActionType = "";
    private float currentReward = 0f;
    private Vector2 currentMoveDirection = Vector2.zero;
    
    // Coordination visualization
    private List<AIVisualFeedback> coordinatingAllies = new List<AIVisualFeedback>();
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // Initialize with novice color
        targetGlowColor = noviceGlowColor;
        currentGlowColor = noviceGlowColor;
    }
    
    private void Update()
    {
        // Smoothly interpolate glow color
        if (spriteRenderer != null && Vector4.Distance(currentGlowColor, targetGlowColor) > 0.01f)
        {
            currentGlowColor = Color.Lerp(currentGlowColor, targetGlowColor, Time.deltaTime * colorTransitionSpeed);
            spriteRenderer.color = currentGlowColor;
        }
    }
    
    /// <summary>
    /// Display a visual indicator when a tactical decision is made.
    /// Requirements: 8.1
    /// </summary>
    /// <param name="actionType">The type of action being performed</param>
    /// <param name="duration">How long to display the indicator (default uses configured duration)</param>
    public void ShowTacticalDecision(EnemyActionType actionType, float duration = -1f)
    {
        if (duration < 0)
        {
            duration = indicatorDuration;
        }
        
        // Clamp duration to specification range (0.5-1.0 seconds)
        duration = Mathf.Clamp(duration, 0.5f, 1.0f);
        
        // Stop any existing indicator
        if (indicatorCoroutine != null)
        {
            StopCoroutine(indicatorCoroutine);
        }
        
        // Show brain icon if available
        if (brainIconPrefab != null)
        {
            indicatorCoroutine = StartCoroutine(ShowIndicatorCoroutine(brainIconPrefab, duration));
        }
        
        // Trigger tactical decision particle effect
        if (tacticalDecisionEffect != null)
        {
            tacticalDecisionEffect.Play();
        }
        
        // Trigger specific maneuver effects
        TriggerManeuverEffect(actionType);
        
        // Update debug info
        currentActionType = actionType.ToString();
    }
    
    /// <summary>
    /// Trigger distinct particle effects for intelligent maneuvers.
    /// Requirements: 8.2
    /// </summary>
    /// <param name="actionType">The type of maneuver being performed</param>
    private void TriggerManeuverEffect(EnemyActionType actionType)
    {
        switch (actionType)
        {
            case EnemyActionType.Flank:
                if (flankingEffect != null)
                {
                    flankingEffect.Play();
                }
                break;
                
            case EnemyActionType.Kite:
                if (kitingEffect != null)
                {
                    kitingEffect.Play();
                }
                break;
                
            case EnemyActionType.CoordinatedAttack:
                if (coordinatedAttackEffect != null)
                {
                    coordinatedAttackEffect.Play();
                }
                break;
        }
    }
    
    /// <summary>
    /// Display level-up effect when AI tier increases.
    /// Requirements: 8.3
    /// </summary>
    /// <param name="newTier">The new AI tier</param>
    public void ShowLevelUp(AITier newTier)
    {
        if (newTier == currentTier)
        {
            return;
        }
        
        currentTier = newTier;
        
        // Show level-up effect
        if (levelUpEffectPrefab != null)
        {
            GameObject effect = Instantiate(levelUpEffectPrefab, transform.position + indicatorOffset, Quaternion.identity);
            Destroy(effect, 2.0f);
        }
        
        // Update glow color based on tier
        UpdateGlow(newTier);
    }
    
    /// <summary>
    /// Update the glow color based on current AI tier.
    /// Requirements: 8.3
    /// </summary>
    /// <param name="tier">The current AI tier</param>
    public void UpdateGlow(AITier tier)
    {
        currentTier = tier;
        
        switch (tier)
        {
            case AITier.Novice:
                targetGlowColor = noviceGlowColor;
                break;
            case AITier.Learning:
                targetGlowColor = learningGlowColor;
                break;
            case AITier.Trained:
                targetGlowColor = trainedGlowColor;
                break;
            case AITier.Expert:
                targetGlowColor = expertGlowColor;
                break;
        }
    }
    
    /// <summary>
    /// Update debug information for visualization.
    /// Requirements: 8.4
    /// </summary>
    /// <param name="actionType">Current action type</param>
    /// <param name="reward">Current reward value</param>
    /// <param name="moveDirection">Current movement direction</param>
    public void UpdateDebugInfo(string actionType, float reward, Vector2 moveDirection)
    {
        currentActionType = actionType;
        currentReward = reward;
        currentMoveDirection = moveDirection;
    }
    
    /// <summary>
    /// Add an ally to the coordination visualization.
    /// Requirements: 14.5
    /// </summary>
    public void AddCoordinatingAlly(AIVisualFeedback ally)
    {
        if (ally != null && !coordinatingAllies.Contains(ally))
        {
            coordinatingAllies.Add(ally);
        }
    }
    
    /// <summary>
    /// Remove an ally from the coordination visualization.
    /// </summary>
    public void RemoveCoordinatingAlly(AIVisualFeedback ally)
    {
        coordinatingAllies.Remove(ally);
    }
    
    /// <summary>
    /// Clear all coordinating allies.
    /// </summary>
    public void ClearCoordinatingAllies()
    {
        coordinatingAllies.Clear();
    }
    
    private IEnumerator ShowIndicatorCoroutine(GameObject prefab, float duration)
    {
        // Clean up any existing indicator
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
        }
        
        // Instantiate new indicator
        activeIndicator = Instantiate(prefab, transform.position + indicatorOffset, Quaternion.identity, transform);
        
        // Fade in
        float fadeInTime = 0.1f;
        float elapsed = 0f;
        
        SpriteRenderer indicatorRenderer = activeIndicator.GetComponent<SpriteRenderer>();
        if (indicatorRenderer != null)
        {
            Color startColor = indicatorRenderer.color;
            startColor.a = 0f;
            indicatorRenderer.color = startColor;
            
            while (elapsed < fadeInTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
                Color color = indicatorRenderer.color;
                color.a = alpha;
                indicatorRenderer.color = color;
                yield return null;
            }
        }
        
        // Wait for display duration
        yield return new WaitForSeconds(duration - fadeInTime - 0.1f);
        
        // Fade out
        elapsed = 0f;
        float fadeOutTime = 0.1f;
        
        if (indicatorRenderer != null)
        {
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutTime);
                Color color = indicatorRenderer.color;
                color.a = alpha;
                indicatorRenderer.color = color;
                yield return null;
            }
        }
        
        // Clean up
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
            activeIndicator = null;
        }
        
        indicatorCoroutine = null;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos)
        {
            return;
        }
        
        // Draw movement vector
        if (currentMoveDirection.magnitude > 0.01f)
        {
            Gizmos.color = Color.yellow;
            Vector3 start = transform.position;
            Vector3 end = start + new Vector3(currentMoveDirection.x, currentMoveDirection.y, 0) * 2f;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.1f);
        }
        
        // Draw attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        DrawCircle(transform.position, attackRange, 32);
        
        // Draw optimal positioning zone (green ring)
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        DrawCircle(transform.position, optimalRangeMin, 32);
        DrawCircle(transform.position, optimalRangeMax, 32);
        
        // Draw coordination lines
        if (showCoordinationLines)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
            foreach (var ally in coordinatingAllies)
            {
                if (ally != null)
                {
                    Gizmos.DrawLine(transform.position, ally.transform.position);
                }
            }
        }
    }
    
    /// <summary>
    /// Helper method to draw a circle gizmo in 2D space.
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    private void OnGUI()
    {
        if (!showDebugLabels)
        {
            return;
        }
        
        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + indicatorOffset);
        
        // Only draw if on screen
        if (screenPos.z > 0)
        {
            // Flip Y coordinate for GUI
            screenPos.y = Screen.height - screenPos.y;
            
            // Create label content
            string label = $"{currentActionType}\nReward: {currentReward:F2}";
            
            // Set color based on reward
            Color labelColor = currentReward >= 0 ? Color.green : Color.red;
            
            // Draw background
            GUIStyle bgStyle = new GUIStyle();
            bgStyle.normal.background = Texture2D.whiteTexture;
            
            GUIStyle textStyle = new GUIStyle();
            textStyle.normal.textColor = labelColor;
            textStyle.fontSize = 12;
            textStyle.alignment = TextAnchor.MiddleCenter;
            
            Vector2 labelSize = textStyle.CalcSize(new GUIContent(label));
            Rect bgRect = new Rect(screenPos.x - labelSize.x / 2 - 5, screenPos.y - labelSize.y / 2 - 5, 
                                   labelSize.x + 10, labelSize.y + 10);
            
            // Draw semi-transparent background
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(bgRect, "", bgStyle);
            GUI.color = oldColor;
            
            // Draw text
            Rect labelRect = new Rect(screenPos.x - labelSize.x / 2, screenPos.y - labelSize.y / 2, 
                                     labelSize.x, labelSize.y);
            GUI.Label(labelRect, label, textStyle);
        }
    }
}
