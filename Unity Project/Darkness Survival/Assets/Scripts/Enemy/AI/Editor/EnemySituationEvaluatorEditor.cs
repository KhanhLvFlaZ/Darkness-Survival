using UnityEngine;
using UnityEditor;

/// <summary>
/// Requirement 14.2: Custom inspector for EnemySituationEvaluator.
/// Displays all observation values in readable format.
/// Updates values in real-time during play mode.
/// </summary>
[CustomEditor(typeof(EnemySituationEvaluator))]
public class EnemySituationEvaluatorEditor : Editor
{
    private bool showBasicObservations = true;
    private bool showPlayerState = true;
    private bool showAllyInformation = true;
    private bool showEnvironmentData = true;
    private bool showTacticalScores = true;
    private bool showValidityFlags = true;
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        // Only show observation data in play mode
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Observation data will be displayed here during play mode.", MessageType.Info);
            return;
        }
        
        EnemySituationEvaluator evaluator = (EnemySituationEvaluator)target;
        SituationState state = evaluator.LatestState;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Real-Time Observation Data", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Basic Observations
        showBasicObservations = EditorGUILayout.Foldout(showBasicObservations, "Basic Observations", true);
        if (showBasicObservations)
        {
            EditorGUI.indentLevel++;
            DrawBasicObservations(state);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Player State
        showPlayerState = EditorGUILayout.Foldout(showPlayerState, "Player State", true);
        if (showPlayerState)
        {
            EditorGUI.indentLevel++;
            DrawPlayerState(state);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Ally Information
        showAllyInformation = EditorGUILayout.Foldout(showAllyInformation, "Ally Information", true);
        if (showAllyInformation)
        {
            EditorGUI.indentLevel++;
            DrawAllyInformation(state);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Environment Data
        showEnvironmentData = EditorGUILayout.Foldout(showEnvironmentData, "Environment Data", true);
        if (showEnvironmentData)
        {
            EditorGUI.indentLevel++;
            DrawEnvironmentData(state);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Tactical Scores
        showTacticalScores = EditorGUILayout.Foldout(showTacticalScores, "Tactical Scores", true);
        if (showTacticalScores)
        {
            EditorGUI.indentLevel++;
            DrawTacticalScores(state);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Validity Flags
        showValidityFlags = EditorGUILayout.Foldout(showValidityFlags, "Validity Flags", true);
        if (showValidityFlags)
        {
            EditorGUI.indentLevel++;
            DrawValidityFlags(state);
            EditorGUI.indentLevel--;
        }
        
        // Force repaint to update values in real-time
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
    
    void DrawBasicObservations(SituationState state)
    {
        EditorGUILayout.LabelField("Timestamp", state.timestamp.ToString("F3"));
        EditorGUILayout.Vector2Field("Enemy Position", state.enemyPosition);
        EditorGUILayout.Vector2Field("Enemy Velocity", state.enemyVelocity);
        EditorGUILayout.Vector2Field("Player Position", state.playerPosition);
        
        EditorGUILayout.Space();
        
        DrawProgressBar("Enemy HP Ratio", state.enemyHpRatio, GetHealthColor(state.enemyHpRatio));
        DrawProgressBar("Player HP Ratio", state.playerHpRatio, GetHealthColor(state.playerHpRatio));
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Distance to Player", state.distanceToPlayer.ToString("F2"));
        EditorGUILayout.LabelField("Attack Cooldown", state.attackCooldownRemaining.ToString("F2"));
        EditorGUILayout.Toggle("Is Spirit", state.isSpirit);
        EditorGUILayout.Toggle("Is Obstructed", state.isObstructed);
        EditorGUILayout.IntField("Nearby Target Count", state.nearbyTargetCount);
        
        EditorGUILayout.Space();
        
        DrawProgressBar("Attack Opportunity", state.attackOpportunity, Color.red);
        DrawProgressBar("Retreat Urgency", state.retreatUrgency, Color.yellow);
        DrawProgressBar("Explore Value", state.exploreValue, Color.cyan);
    }
    
    void DrawPlayerState(SituationState state)
    {
        EditorGUILayout.Vector2Field("Player Velocity", state.playerVelocity);
        EditorGUILayout.Toggle("Player Is Attacking", state.playerIsAttacking);
        EditorGUILayout.Toggle("Player Is Vulnerable", state.playerIsVulnerable);
        DrawProgressBar("Player Buff Strength", state.playerBuffStrength, Color.magenta);
    }
    
    void DrawAllyInformation(SituationState state)
    {
        EditorGUILayout.IntField("Ally Count", state.allyCount);
        
        if (state.allyCount > 0 && state.allyPositions != null)
        {
            EditorGUILayout.Space();
            
            for (int i = 0; i < state.allyCount && i < state.allyPositions.Length; i++)
            {
                EditorGUILayout.LabelField($"Ally {i + 1}", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.Vector2Field("Position", state.allyPositions[i]);
                
                if (state.allyHpRatios != null && i < state.allyHpRatios.Length)
                {
                    DrawProgressBar("HP Ratio", state.allyHpRatios[i], GetHealthColor(state.allyHpRatios[i]));
                }
                
                if (state.allyIsAttacking != null && i < state.allyIsAttacking.Length)
                {
                    EditorGUILayout.Toggle("Is Attacking", state.allyIsAttacking[i]);
                }
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }
        else
        {
            EditorGUILayout.LabelField("No allies detected");
        }
    }
    
    void DrawEnvironmentData(SituationState state)
    {
        EditorGUILayout.IntField("Obstacle Count", state.obstacleCount);
        EditorGUILayout.Toggle("Has Line of Sight", state.hasLineOfSight);
        EditorGUILayout.Vector2Field("Nearest Cover Position", state.nearestCoverPosition);
        
        if (state.obstacleCount > 0 && state.nearbyObstaclePositions != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Nearby Obstacles:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < state.obstacleCount && i < state.nearbyObstaclePositions.Length; i++)
            {
                EditorGUILayout.Vector2Field($"Obstacle {i + 1}", state.nearbyObstaclePositions[i]);
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    void DrawTacticalScores(SituationState state)
    {
        DrawProgressBar("Flanking Opportunity", state.flankingOpportunity, new Color(1f, 0.5f, 0f));
        DrawProgressBar("Kiting Feasibility", state.kitingFeasibility, new Color(0f, 0.8f, 1f));
        DrawProgressBar("Cooperation Potential", state.cooperationPotential, new Color(0.5f, 1f, 0.5f));
    }
    
    void DrawValidityFlags(SituationState state)
    {
        EditorGUILayout.Toggle("Player Data Valid", state.playerDataValid);
        EditorGUILayout.Toggle("Ally Data Valid", state.allyDataValid);
        EditorGUILayout.Toggle("Environment Data Valid", state.environmentDataValid);
    }
    
    /// <summary>
    /// Helper method to draw a progress bar with color.
    /// </summary>
    void DrawProgressBar(string label, float value, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
        
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
        
        // Draw background
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
        
        // Draw filled portion
        Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height);
        EditorGUI.DrawRect(fillRect, color);
        
        // Draw border
        Handles.BeginGUI();
        Handles.color = Color.black;
        Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.black);
        Handles.EndGUI();
        
        // Draw value text
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        EditorGUI.LabelField(rect, value.ToString("F2"), style);
        
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// Helper method to get color based on health ratio.
    /// </summary>
    Color GetHealthColor(float healthRatio)
    {
        if (healthRatio > 0.6f)
            return Color.green;
        else if (healthRatio > 0.3f)
            return Color.yellow;
        else
            return Color.red;
    }
}
