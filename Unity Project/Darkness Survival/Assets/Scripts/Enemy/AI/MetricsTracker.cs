using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Tracks comprehensive learning metrics for monster AI training and evaluation.
/// Records performance statistics including rewards, survival, combat efficiency, and cooperation.
/// </summary>
[Serializable]
public struct LearningMetrics
{
    public float averageRewardPerEpisode;
    public float survivalTimeAverage;
    public float damageEfficiency;        // damageDealt / damageTaken
    public float positioningScore;        // % time in optimal range
    public float attackAccuracy;          // hits / attempts
    public float cooperationScore;        // successful coordinations
    public int episodesCompleted;
    public float explorationRate;
    
    // Additional tracking fields
    public float totalReward;
    public float totalSurvivalTime;
    public float totalDamageDealt;
    public float totalDamageTaken;
    public int totalAttackAttempts;
    public int totalAttackHits;
    public float totalTimeInOptimalRange;
    public float totalTimeAlive;
    public int totalCoordinatedActions;
    public int successfulCoordinatedActions;
}

/// <summary>
/// Component that tracks and records AI learning metrics for a monster.
/// Provides methods to update metrics based on game events and export data for analysis.
/// </summary>
public class MetricsTracker : MonoBehaviour
{
    [Header("Current Metrics")]
    [SerializeField] private LearningMetrics currentMetrics;
    
    [Header("Configuration")]
    [SerializeField] private float optimalDistanceMin = 2f;
    [SerializeField] private float optimalDistanceMax = 4f;
    [SerializeField] private bool enableLogging = false;
    
    // Runtime tracking
    private float episodeStartTime;
    private float lastPositionCheckTime;
    private bool isInitialized = false;
    
    public LearningMetrics CurrentMetrics => currentMetrics;
    
    private void Awake()
    {
        InitializeMetrics();
    }
    
    /// <summary>
    /// Initialize metrics on monster spawn.
    /// </summary>
    public void InitializeMetrics()
    {
        currentMetrics = new LearningMetrics
        {
            averageRewardPerEpisode = 0f,
            survivalTimeAverage = 0f,
            damageEfficiency = 0f,
            positioningScore = 0f,
            attackAccuracy = 0f,
            cooperationScore = 0f,
            episodesCompleted = 0,
            explorationRate = 0f,
            totalReward = 0f,
            totalSurvivalTime = 0f,
            totalDamageDealt = 0f,
            totalDamageTaken = 0f,
            totalAttackAttempts = 0,
            totalAttackHits = 0,
            totalTimeInOptimalRange = 0f,
            totalTimeAlive = 0f,
            totalCoordinatedActions = 0,
            successfulCoordinatedActions = 0
        };
        
        episodeStartTime = Time.time;
        lastPositionCheckTime = Time.time;
        isInitialized = true;
        
        if (enableLogging)
        {
            Debug.Log($"[MetricsTracker] Initialized metrics for {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Record episode end and aggregate metrics.
    /// </summary>
    public void RecordEpisodeEnd(EpisodeSummary summary)
    {
        if (!isInitialized) return;
        
        currentMetrics.episodesCompleted++;
        currentMetrics.totalReward += summary.cumulativeReward;
        currentMetrics.totalSurvivalTime += (float)summary.duration;
        currentMetrics.totalDamageDealt += summary.damageDealt;
        currentMetrics.totalDamageTaken += summary.damageTaken;
        
        // Calculate averages
        if (currentMetrics.episodesCompleted > 0)
        {
            currentMetrics.averageRewardPerEpisode = 
                currentMetrics.totalReward / currentMetrics.episodesCompleted;
            currentMetrics.survivalTimeAverage = 
                currentMetrics.totalSurvivalTime / currentMetrics.episodesCompleted;
        }
        
        // Calculate damage efficiency
        UpdateDamageEfficiency(summary.damageDealt, summary.damageTaken);
        
        if (enableLogging)
        {
            Debug.Log($"[MetricsTracker] Episode {currentMetrics.episodesCompleted} completed. " +
                     $"Reward: {summary.cumulativeReward:F2}, Duration: {summary.duration:F2}s");
        }
    }
    
    /// <summary>
    /// Update damage efficiency metric based on damage dealt and taken.
    /// </summary>
    public void UpdateDamageEfficiency(float dealt, float taken)
    {
        if (!isInitialized) return;
        
        currentMetrics.totalDamageDealt += dealt;
        currentMetrics.totalDamageTaken += taken;
        
        // Calculate efficiency as dealt/taken ratio
        // Avoid division by zero
        if (currentMetrics.totalDamageTaken > 0.001f)
        {
            currentMetrics.damageEfficiency = 
                currentMetrics.totalDamageDealt / currentMetrics.totalDamageTaken;
        }
        else if (currentMetrics.totalDamageDealt > 0f)
        {
            // If dealt damage but took none, efficiency is very high
            currentMetrics.damageEfficiency = currentMetrics.totalDamageDealt * 10f;
        }
        else
        {
            currentMetrics.damageEfficiency = 0f;
        }
        
        if (enableLogging && (dealt > 0 || taken > 0))
        {
            Debug.Log($"[MetricsTracker] Damage efficiency updated: {currentMetrics.damageEfficiency:F2} " +
                     $"(Dealt: {currentMetrics.totalDamageDealt:F1}, Taken: {currentMetrics.totalDamageTaken:F1})");
        }
    }
    
    /// <summary>
    /// Update positioning score based on whether monster is in optimal range.
    /// </summary>
    public void UpdatePositioningScore(bool inOptimalRange, float deltaTime)
    {
        if (!isInitialized) return;
        
        currentMetrics.totalTimeAlive += deltaTime;
        
        if (inOptimalRange)
        {
            currentMetrics.totalTimeInOptimalRange += deltaTime;
        }
        
        // Calculate positioning score as percentage of time in optimal range
        if (currentMetrics.totalTimeAlive > 0.001f)
        {
            currentMetrics.positioningScore = 
                currentMetrics.totalTimeInOptimalRange / currentMetrics.totalTimeAlive;
        }
    }
    
    /// <summary>
    /// Check if current distance is within optimal range.
    /// </summary>
    public bool IsInOptimalRange(float distance)
    {
        return distance >= optimalDistanceMin && distance <= optimalDistanceMax;
    }
    
    /// <summary>
    /// Update attack accuracy metric.
    /// </summary>
    public void UpdateAttackAccuracy(bool hit)
    {
        if (!isInitialized) return;
        
        currentMetrics.totalAttackAttempts++;
        if (hit)
        {
            currentMetrics.totalAttackHits++;
        }
        
        // Calculate accuracy as hits/attempts ratio
        if (currentMetrics.totalAttackAttempts > 0)
        {
            currentMetrics.attackAccuracy = 
                (float)currentMetrics.totalAttackHits / currentMetrics.totalAttackAttempts;
        }
        
        if (enableLogging)
        {
            Debug.Log($"[MetricsTracker] Attack accuracy: {currentMetrics.attackAccuracy:F2} " +
                     $"({currentMetrics.totalAttackHits}/{currentMetrics.totalAttackAttempts})");
        }
    }
    
    /// <summary>
    /// Update cooperation score for coordinated actions.
    /// </summary>
    public void UpdateCooperationScore(bool successful)
    {
        if (!isInitialized) return;
        
        currentMetrics.totalCoordinatedActions++;
        if (successful)
        {
            currentMetrics.successfulCoordinatedActions++;
        }
        
        // Calculate cooperation score as success rate
        if (currentMetrics.totalCoordinatedActions > 0)
        {
            currentMetrics.cooperationScore = 
                (float)currentMetrics.successfulCoordinatedActions / currentMetrics.totalCoordinatedActions;
        }
        
        if (enableLogging)
        {
            Debug.Log($"[MetricsTracker] Cooperation score: {currentMetrics.cooperationScore:F2} " +
                     $"({currentMetrics.successfulCoordinatedActions}/{currentMetrics.totalCoordinatedActions})");
        }
    }
    
    /// <summary>
    /// Export metrics to JSON format.
    /// </summary>
    public string ExportMetricsToJson()
    {
        return JsonUtility.ToJson(currentMetrics, true);
    }
    
    /// <summary>
    /// Export metrics to CSV format.
    /// </summary>
    public string ExportMetricsToCSV()
    {
        StringBuilder csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Metric,Value");
        
        // Data rows
        csv.AppendLine($"AverageRewardPerEpisode,{currentMetrics.averageRewardPerEpisode}");
        csv.AppendLine($"SurvivalTimeAverage,{currentMetrics.survivalTimeAverage}");
        csv.AppendLine($"DamageEfficiency,{currentMetrics.damageEfficiency}");
        csv.AppendLine($"PositioningScore,{currentMetrics.positioningScore}");
        csv.AppendLine($"AttackAccuracy,{currentMetrics.attackAccuracy}");
        csv.AppendLine($"CooperationScore,{currentMetrics.cooperationScore}");
        csv.AppendLine($"EpisodesCompleted,{currentMetrics.episodesCompleted}");
        csv.AppendLine($"ExplorationRate,{currentMetrics.explorationRate}");
        csv.AppendLine($"TotalReward,{currentMetrics.totalReward}");
        csv.AppendLine($"TotalSurvivalTime,{currentMetrics.totalSurvivalTime}");
        csv.AppendLine($"TotalDamageDealt,{currentMetrics.totalDamageDealt}");
        csv.AppendLine($"TotalDamageTaken,{currentMetrics.totalDamageTaken}");
        csv.AppendLine($"TotalAttackAttempts,{currentMetrics.totalAttackAttempts}");
        csv.AppendLine($"TotalAttackHits,{currentMetrics.totalAttackHits}");
        csv.AppendLine($"TotalTimeInOptimalRange,{currentMetrics.totalTimeInOptimalRange}");
        csv.AppendLine($"TotalTimeAlive,{currentMetrics.totalTimeAlive}");
        csv.AppendLine($"TotalCoordinatedActions,{currentMetrics.totalCoordinatedActions}");
        csv.AppendLine($"SuccessfulCoordinatedActions,{currentMetrics.successfulCoordinatedActions}");
        
        return csv.ToString();
    }
    
    /// <summary>
    /// Export metrics to file with timestamp and episode metadata.
    /// </summary>
    public void ExportMetrics(string filepath, string format = "json")
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"{filepath}_{gameObject.name}_{timestamp}.{format}";
            
            string content;
            if (format.ToLower() == "csv")
            {
                content = ExportMetricsToCSV();
            }
            else
            {
                content = ExportMetricsToJson();
            }
            
            // Add metadata header for JSON
            if (format.ToLower() == "json")
            {
                var metadata = new
                {
                    timestamp = timestamp,
                    monsterName = gameObject.name,
                    episodesCompleted = currentMetrics.episodesCompleted,
                    metrics = currentMetrics
                };
                content = JsonUtility.ToJson(metadata, true);
            }
            
            File.WriteAllText(filename, content);
            
            Debug.Log($"[MetricsTracker] Metrics exported to {filename}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MetricsTracker] Failed to export metrics: {e.Message}");
        }
    }
    
    /// <summary>
    /// Update exploration rate (typically set by AI tier system).
    /// </summary>
    public void SetExplorationRate(float rate)
    {
        currentMetrics.explorationRate = Mathf.Clamp01(rate);
    }
    
    private void Update()
    {
        // Track positioning score continuously
        if (isInitialized)
        {
            float deltaTime = Time.time - lastPositionCheckTime;
            lastPositionCheckTime = Time.time;
            
            // This will be called from external systems that know the player position
            // For now, we just track time alive
            currentMetrics.totalTimeAlive += deltaTime;
        }
    }
}
