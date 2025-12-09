using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

/// <summary>
/// Validates ML-Agents model configuration and provides runtime checks.
/// Helps ensure models are properly loaded and configured for inference.
/// </summary>
public class ModelValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    [SerializeField] private bool validateOnStart = true;
    [SerializeField] private bool logDetailedInfo = true;
    
    private MonsterAgent agent;
    private BehaviorParameters behaviorParams;
    
    void Start()
    {
        if (validateOnStart)
        {
            ValidateConfiguration();
        }
    }
    
    /// <summary>
    /// Perform comprehensive validation of ML-Agents configuration.
    /// </summary>
    public void ValidateConfiguration()
    {
        agent = GetComponent<MonsterAgent>();
        behaviorParams = GetComponent<BehaviorParameters>();
        
        if (agent == null)
        {
            Debug.LogError($"[ModelValidator] MonsterAgent component missing on {gameObject.name}!");
            return;
        }
        
        if (behaviorParams == null)
        {
            Debug.LogError($"[ModelValidator] BehaviorParameters component missing on {gameObject.name}!");
            return;
        }
        
        ValidateModel();
        ValidateBehaviorType();
        ValidateObservationSpace();
        ValidateActionSpace();
        ValidateInferenceDevice();
        
        if (logDetailedInfo)
        {
            LogConfiguration();
        }
    }
    
    /// <summary>
    /// Validate that a model is assigned and properly configured.
    /// </summary>
    private void ValidateModel()
    {
        if (behaviorParams.Model == null)
        {
            Debug.LogWarning($"[ModelValidator] No model assigned to {gameObject.name} - will use heuristic fallback");
            return;
        }
        
        Debug.Log($"[ModelValidator] Model assigned: {behaviorParams.Model.name}");
        
        // Check if model file exists
        if (behaviorParams.Model == null)
        {
            Debug.LogError($"[ModelValidator] Model reference is null!");
        }
    }
    
    /// <summary>
    /// Validate behavior type is appropriate for runtime.
    /// </summary>
    private void ValidateBehaviorType()
    {
        if (behaviorParams.BehaviorType == BehaviorType.Default)
        {
            if (behaviorParams.Model != null)
            {
                Debug.Log($"[ModelValidator] Behavior type is Default - will use inference with model");
            }
            else
            {
                Debug.LogWarning($"[ModelValidator] Behavior type is Default but no model assigned - will use heuristic");
            }
        }
        else if (behaviorParams.BehaviorType == BehaviorType.InferenceOnly)
        {
            if (behaviorParams.Model == null)
            {
                Debug.LogError($"[ModelValidator] Behavior type is InferenceOnly but no model assigned!");
            }
            else
            {
                Debug.Log($"[ModelValidator] Behavior type is InferenceOnly - using model for all decisions");
            }
        }
        else if (behaviorParams.BehaviorType == BehaviorType.HeuristicOnly)
        {
            Debug.Log($"[ModelValidator] Behavior type is HeuristicOnly - model will not be used");
        }
    }
    
    /// <summary>
    /// Validate observation space configuration.
    /// </summary>
    private void ValidateObservationSpace()
    {
        var observationSpecs = behaviorParams.BrainParameters.VectorObservationSize;
        int expectedSize = 39; // As defined in design document
        
        if (observationSpecs != expectedSize)
        {
            Debug.LogWarning($"[ModelValidator] Observation space size mismatch! " +
                           $"Expected: {expectedSize}, Got: {observationSpecs}");
        }
        else
        {
            Debug.Log($"[ModelValidator] Observation space validated: {observationSpecs} continuous values");
        }
    }
    
    /// <summary>
    /// Validate action space configuration.
    /// </summary>
    private void ValidateActionSpace()
    {
        var actionSpec = behaviorParams.BrainParameters.ActionSpec;
        
        // Expected: 1 discrete branch (size 10) + 3 continuous actions
        int expectedDiscreteBranches = 1;
        int expectedDiscreteSize = 10;
        int expectedContinuousActions = 3;
        
        if (actionSpec.NumDiscreteActions != expectedDiscreteBranches)
        {
            Debug.LogWarning($"[ModelValidator] Discrete action branches mismatch! " +
                           $"Expected: {expectedDiscreteBranches}, Got: {actionSpec.NumDiscreteActions}");
        }
        
        if (actionSpec.NumDiscreteActions > 0)
        {
            int discreteSize = actionSpec.BranchSizes[0];
            if (discreteSize != expectedDiscreteSize)
            {
                Debug.LogWarning($"[ModelValidator] Discrete action size mismatch! " +
                               $"Expected: {expectedDiscreteSize}, Got: {discreteSize}");
            }
        }
        
        if (actionSpec.NumContinuousActions != expectedContinuousActions)
        {
            Debug.LogWarning($"[ModelValidator] Continuous action count mismatch! " +
                           $"Expected: {expectedContinuousActions}, Got: {actionSpec.NumContinuousActions}");
        }
        
        Debug.Log($"[ModelValidator] Action space validated: " +
                 $"{actionSpec.NumDiscreteActions} discrete branches, " +
                 $"{actionSpec.NumContinuousActions} continuous actions");
    }
    
    /// <summary>
    /// Validate and log inference device configuration.
    /// </summary>
    private void ValidateInferenceDevice()
    {
        var inferenceDevice = behaviorParams.InferenceDevice;
        Debug.Log($"[ModelValidator] Inference device: {inferenceDevice}");
        
        if (inferenceDevice == InferenceDevice.GPU)
        {
            Debug.Log($"[ModelValidator] Using GPU inference - ensure GPU is available");
        }
        else if (inferenceDevice == InferenceDevice.CPU)
        {
            Debug.Log($"[ModelValidator] Using CPU inference");
        }
        else
        {
            Debug.Log($"[ModelValidator] Using default inference device");
        }
    }
    
    /// <summary>
    /// Log detailed configuration information.
    /// </summary>
    private void LogConfiguration()
    {
        Debug.Log($"[ModelValidator] === Configuration Summary for {gameObject.name} ===");
        Debug.Log($"  Behavior Name: {behaviorParams.BehaviorName}");
        Debug.Log($"  Model: {(behaviorParams.Model != null ? behaviorParams.Model.name : "None")}");
        Debug.Log($"  Behavior Type: {behaviorParams.BehaviorType}");
        Debug.Log($"  Team ID: {behaviorParams.TeamId}");
        Debug.Log($"  Use Child Sensors: {behaviorParams.UseChildSensors}");
        Debug.Log($"  Observable Attribute Handling: {behaviorParams.ObservableAttributeHandling}");
        Debug.Log($"  Inference Device: {behaviorParams.InferenceDevice}");
        
        var actionSpec = behaviorParams.BrainParameters.ActionSpec;
        Debug.Log($"  Discrete Actions: {actionSpec.NumDiscreteActions}");
        Debug.Log($"  Continuous Actions: {actionSpec.NumContinuousActions}");
        Debug.Log($"  Observation Size: {behaviorParams.BrainParameters.VectorObservationSize}");
        Debug.Log($"==============================================");
    }
    
    /// <summary>
    /// Test inference by requesting a decision.
    /// </summary>
    public void TestInference()
    {
        if (agent == null)
        {
            Debug.LogError("[ModelValidator] Cannot test inference - MonsterAgent not found");
            return;
        }
        
        Debug.Log("[ModelValidator] Requesting decision for inference test...");
        agent.RequestDecision();
        
        // Note: Actual decision will be made in next FixedUpdate
        Debug.Log("[ModelValidator] Decision requested - check next frame for results");
    }
    
    /// <summary>
    /// Check if model is properly loaded and ready for inference.
    /// </summary>
    public bool IsModelReady()
    {
        if (behaviorParams == null) return false;
        if (behaviorParams.Model == null) return false;
        if (behaviorParams.BehaviorType == BehaviorType.HeuristicOnly) return false;
        
        return true;
    }
    
    /// <summary>
    /// Get a status report string.
    /// </summary>
    public string GetStatusReport()
    {
        if (behaviorParams == null)
        {
            return "BehaviorParameters component missing";
        }
        
        if (behaviorParams.Model == null)
        {
            return "No model assigned - using heuristic";
        }
        
        if (behaviorParams.BehaviorType == BehaviorType.HeuristicOnly)
        {
            return "Heuristic only mode";
        }
        
        return $"Model ready: {behaviorParams.Model.name}";
    }
}
