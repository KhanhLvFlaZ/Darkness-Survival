using UnityEngine;
using System.Text;

/// <summary>
/// Requirement 14.3: Implements action logging for AI decisions.
/// Logs action type, movement direction, and confidence.
/// Includes monster ID and timestamp.
/// Makes logging optional via flag.
/// </summary>
[RequireComponent(typeof(Monsters))]
public class AIActionLogger : MonoBehaviour
{
    [Header("Logging Settings")]
    [SerializeField] bool enableLogging = false;
    [SerializeField] bool logToConsole = true;
    [SerializeField] bool logToFile = false;
    [SerializeField] string logFilePath = "Logs/AIActions.log";
    
    [Header("Log Filtering")]
    [SerializeField] bool logIdleActions = false;
    [SerializeField] bool logMovementDetails = true;
    [SerializeField] bool logConfidenceScores = true;
    [SerializeField] float minLogInterval = 0.5f; // Minimum time between logs (to avoid spam)
    
    // Component references
    Monsters monster;
    EnemySituationEvaluator situationEvaluator;
    
    // State tracking
    string monsterID;
    float lastLogTime;
    int actionCount;
    StringBuilder logBuilder;
    
    void Awake()
    {
        monster = GetComponent<Monsters>();
        situationEvaluator = GetComponent<EnemySituationEvaluator>();
        
        // Generate unique monster ID
        monsterID = $"{gameObject.name}_{GetInstanceID()}";
        
        logBuilder = new StringBuilder();
        lastLogTime = -minLogInterval; // Allow immediate first log
        actionCount = 0;
    }
    
    void Start()
    {
        if (enableLogging && logToFile)
        {
            InitializeLogFile();
        }
    }
    
    void Update()
    {
        if (!enableLogging) return;
        
        // Check if enough time has passed since last log
        if (Time.time - lastLogTime < minLogInterval) return;
        
        // Get latest action from monster
        if (monster != null && monster.HAS_LATEST_STATE)
        {
            EnemyAction action = monster.LATEST_ACTION;
            SituationState state = monster.LATEST_STATE;
            
            // Skip idle actions if filtering is enabled
            if (!logIdleActions && action.type == EnemyActionType.Idle) return;
            
            LogAction(action, state);
            lastLogTime = Time.time;
        }
    }
    
    /// <summary>
    /// Requirement 14.3: Log action type, movement direction, and confidence.
    /// Include monster ID and timestamp.
    /// </summary>
    public void LogAction(EnemyAction action, SituationState state)
    {
        if (!enableLogging) return;
        
        actionCount++;
        
        logBuilder.Clear();
        logBuilder.Append($"[{Time.time:F2}] ");
        logBuilder.Append($"Monster: {monsterID} | ");
        logBuilder.Append($"Action #{actionCount} | ");
        logBuilder.Append($"Type: {action.type}");
        
        if (logMovementDetails)
        {
            logBuilder.Append($" | Move: ({action.moveDirection.x:F2}, {action.moveDirection.y:F2})");
            logBuilder.Append($" | Magnitude: {action.moveDirection.magnitude:F2}");
        }
        
        logBuilder.Append($" | Attack: {action.attemptAttack}");
        
        if (logConfidenceScores)
        {
            logBuilder.Append($" | AttackOpp: {state.attackOpportunity:F2}");
            logBuilder.Append($" | RetreatUrg: {state.retreatUrgency:F2}");
            logBuilder.Append($" | Explore: {state.exploreValue:F2}");
        }
        
        string logMessage = logBuilder.ToString();
        
        if (logToConsole)
        {
            Debug.Log(logMessage);
        }
        
        if (logToFile)
        {
            WriteToLogFile(logMessage);
        }
    }
    
    /// <summary>
    /// Log a specific action with custom message.
    /// </summary>
    public void LogActionWithMessage(EnemyAction action, string message)
    {
        if (!enableLogging) return;
        
        actionCount++;
        
        string logMessage = $"[{Time.time:F2}] Monster: {monsterID} | Action #{actionCount} | {message} | Type: {action.type}";
        
        if (logToConsole)
        {
            Debug.Log(logMessage);
        }
        
        if (logToFile)
        {
            WriteToLogFile(logMessage);
        }
    }
    
    /// <summary>
    /// Log a tactical decision with reasoning.
    /// </summary>
    public void LogTacticalDecision(EnemyActionType actionType, string reasoning, float confidence)
    {
        if (!enableLogging) return;
        
        string logMessage = $"[{Time.time:F2}] Monster: {monsterID} | TACTICAL | Type: {actionType} | Reason: {reasoning} | Confidence: {confidence:F2}";
        
        if (logToConsole)
        {
            Debug.Log(logMessage);
        }
        
        if (logToFile)
        {
            WriteToLogFile(logMessage);
        }
    }
    
    /// <summary>
    /// Initialize log file with header.
    /// </summary>
    void InitializeLogFile()
    {
        try
        {
            string directory = System.IO.Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            string header = $"=== AI Action Log Started at {System.DateTime.Now} ===\n";
            header += $"Monster ID: {monsterID}\n";
            header += $"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}\n";
            header += "===========================================\n\n";
            
            System.IO.File.AppendAllText(logFilePath, header);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIActionLogger] Failed to initialize log file: {e.Message}");
            logToFile = false;
        }
    }
    
    /// <summary>
    /// Write message to log file.
    /// </summary>
    void WriteToLogFile(string message)
    {
        try
        {
            System.IO.File.AppendAllText(logFilePath, message + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIActionLogger] Failed to write to log file: {e.Message}");
            logToFile = false;
        }
    }
    
    /// <summary>
    /// Requirement 14.3: Make logging optional via flag.
    /// </summary>
    public void SetLoggingEnabled(bool enabled)
    {
        enableLogging = enabled;
    }
    
    /// <summary>
    /// Get current logging state.
    /// </summary>
    public bool IsLoggingEnabled()
    {
        return enableLogging;
    }
    
    /// <summary>
    /// Get total action count.
    /// </summary>
    public int GetActionCount()
    {
        return actionCount;
    }
    
    /// <summary>
    /// Reset action counter.
    /// </summary>
    public void ResetActionCount()
    {
        actionCount = 0;
    }
    
    void OnDestroy()
    {
        if (enableLogging && logToFile)
        {
            string footer = $"\n=== AI Action Log Ended at {System.DateTime.Now} ===\n";
            footer += $"Total Actions Logged: {actionCount}\n";
            footer += "===========================================\n\n";
            
            try
            {
                System.IO.File.AppendAllText(logFilePath, footer);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AIActionLogger] Failed to write footer to log file: {e.Message}");
            }
        }
    }
}
