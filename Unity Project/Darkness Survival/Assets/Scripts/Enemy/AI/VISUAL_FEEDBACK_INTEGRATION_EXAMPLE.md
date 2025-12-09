# Visual Feedback System - Integration Example

## Quick Integration Guide

This document shows how to integrate the AIVisualFeedback system with existing AI components.

## Step 1: Add Component to Monster Prefab

```csharp
// The AIVisualFeedback component should be added to your monster prefab
// in the Unity Inspector alongside other AI components like:
// - Monsters.cs
// - HybridEnemyBrain / TacticalBrain
// - EnemySituationEvaluator
// - RewardCalculator
```

## Step 2: Configure in Inspector

1. Assign prefabs:
   - Brain Icon Prefab (create a simple sprite with brain icon)
   - Level Up Effect Prefab (particle system or animated sprite)

2. Assign particle systems:
   - Tactical Decision Effect
   - Flanking Effect
   - Kiting Effect
   - Coordinated Attack Effect

3. Configure colors (or use defaults):
   - Novice: Blue
   - Learning: Cyan
   - Trained: Yellow
   - Expert: Gold

4. Enable debug features as needed:
   - Show Debug Labels (for development)
   - Show Gizmos (for editor visualization)
   - Show Coordination Lines (for multi-monster tactics)

## Step 3: Integrate with Decision Making

### In HybridEnemyBrain or TacticalBrain

```csharp
public class HybridEnemyBrain : MonoBehaviour, IEnemyBrain
{
    private AIVisualFeedback visualFeedback;
    
    private void Awake()
    {
        visualFeedback = GetComponent<AIVisualFeedback>();
    }
    
    public EnemyAction Decide(in SituationState state, EnemyWorkingMemory memory)
    {
        // Your existing decision logic
        EnemyAction action = MakeDecision(state, memory);
        
        // Show visual feedback for tactical decisions
        if (visualFeedback != null && IsTacticalAction(action.type))
        {
            visualFeedback.ShowTacticalDecision(action.type);
        }
        
        return action;
    }
    
    private bool IsTacticalAction(EnemyActionType type)
    {
        return type == EnemyActionType.Flank ||
               type == EnemyActionType.Kite ||
               type == EnemyActionType.CoordinatedAttack ||
               type == EnemyActionType.SeekCover ||
               type == EnemyActionType.HerdPlayer;
    }
}
```

## Step 4: Integrate with Reward System

### In RewardCalculator

```csharp
public class RewardCalculator : MonoBehaviour
{
    private AIVisualFeedback visualFeedback;
    private float cumulativeReward = 0f;
    
    private void Awake()
    {
        visualFeedback = GetComponent<AIVisualFeedback>();
    }
    
    public void ApplyReward(float reward)
    {
        cumulativeReward += reward;
        
        // Update debug visualization
        if (visualFeedback != null)
        {
            // Get current action from brain
            string actionType = GetCurrentActionType();
            Vector2 moveDirection = GetCurrentMoveDirection();
            
            visualFeedback.UpdateDebugInfo(actionType, cumulativeReward, moveDirection);
        }
    }
}
```

## Step 5: Integrate with AI Tier System (Task 9)

### In AITierManager (to be implemented)

```csharp
public class AITierManager : MonoBehaviour
{
    private AIVisualFeedback visualFeedback;
    private AITier currentTier = AITier.Novice;
    
    private void Awake()
    {
        visualFeedback = GetComponent<AIVisualFeedback>();
        
        // Initialize visual feedback with starting tier
        if (visualFeedback != null)
        {
            visualFeedback.UpdateGlow(currentTier);
        }
    }
    
    public void SetTier(AITier newTier)
    {
        if (newTier != currentTier)
        {
            currentTier = newTier;
            
            // Show level-up effect and update glow
            if (visualFeedback != null)
            {
                visualFeedback.ShowLevelUp(newTier);
            }
        }
    }
}
```

## Step 6: Integrate with Cooperative Behavior

### In CooperativeBehaviorSystem

```csharp
public class CooperativeBehaviorSystem : MonoBehaviour
{
    private AIVisualFeedback visualFeedback;
    private List<CooperativeBehaviorSystem> coordinatingAllies = new List<CooperativeBehaviorSystem>();
    
    private void Awake()
    {
        visualFeedback = GetComponent<AIVisualFeedback>();
    }
    
    public void StartCoordination(CooperativeBehaviorSystem ally)
    {
        if (!coordinatingAllies.Contains(ally))
        {
            coordinatingAllies.Add(ally);
            
            // Update visual feedback
            if (visualFeedback != null && ally.visualFeedback != null)
            {
                visualFeedback.AddCoordinatingAlly(ally.visualFeedback);
            }
        }
    }
    
    public void EndCoordination(CooperativeBehaviorSystem ally)
    {
        coordinatingAllies.Remove(ally);
        
        // Update visual feedback
        if (visualFeedback != null && ally.visualFeedback != null)
        {
            visualFeedback.RemoveCoordinatingAlly(ally.visualFeedback);
        }
    }
    
    public void ClearAllCoordination()
    {
        coordinatingAllies.Clear();
        
        if (visualFeedback != null)
        {
            visualFeedback.ClearCoordinatingAllies();
        }
    }
}
```

## Step 7: Update Loop Integration

### In Monsters.cs or main AI controller

```csharp
public class Monsters : MonoBehaviour
{
    private AIVisualFeedback visualFeedback;
    private IEnemyBrain brain;
    private RewardCalculator rewardCalculator;
    
    private void Awake()
    {
        visualFeedback = GetComponent<AIVisualFeedback>();
        brain = GetComponent<IEnemyBrain>();
        rewardCalculator = GetComponent<RewardCalculator>();
    }
    
    private void Update()
    {
        // Your existing update logic
        
        // Update visual feedback debug info each frame
        if (visualFeedback != null && visualFeedback.showDebugLabels)
        {
            string currentAction = GetCurrentActionType();
            float currentReward = rewardCalculator?.GetCumulativeReward() ?? 0f;
            Vector2 moveDir = GetCurrentMoveDirection();
            
            visualFeedback.UpdateDebugInfo(currentAction, currentReward, moveDir);
        }
    }
}
```

## Debug Mode Usage

### Enable Debug Visualization

1. Select monster in hierarchy
2. Find AIVisualFeedback component
3. Check "Show Debug Labels" to see action/reward above monster
4. Check "Show Gizmos" to see ranges and vectors in Scene view
5. Check "Show Coordination Lines" to see ally connections

### What You'll See

**Debug Labels (Game View):**
- Action type (e.g., "Flank", "Kite")
- Current cumulative reward
- Color-coded: Green for positive, Red for negative

**Gizmos (Scene View):**
- Yellow arrow: Current movement direction
- Red circle: Attack range
- Green circles: Optimal positioning zone (min/max)
- Orange lines: Coordination connections to allies

## Testing the Integration

### Test Tactical Decisions
1. Enable debug labels
2. Observe monster behavior
3. Verify brain icon appears on tactical actions
4. Check that action type updates in label

### Test AI Tier Progression
1. Manually call `ShowLevelUp(AITier.Expert)` in code
2. Verify level-up effect plays
3. Verify glow color transitions to gold
4. Check smooth color interpolation

### Test Coordination
1. Enable coordination lines
2. Spawn multiple monsters near player
3. Verify orange lines connect cooperating monsters
4. Check lines update as monsters move

### Test Particle Effects
1. Assign particle systems in inspector
2. Trigger flanking/kiting/coordinated actions
3. Verify appropriate effects play
4. Check effects are visible against backgrounds

## Performance Tips

1. **Disable debug features in production:**
   ```csharp
   #if UNITY_EDITOR
   visualFeedback.showDebugLabels = true;
   visualFeedback.showGizmos = true;
   #else
   visualFeedback.showDebugLabels = false;
   visualFeedback.showGizmos = false;
   #endif
   ```

2. **Pool indicator prefabs** if showing many indicators frequently

3. **Limit particle system max particles** to maintain performance

4. **Use LOD** for distant monsters (disable visual feedback)

## Troubleshooting

**Brain icon doesn't appear:**
- Check brainIconPrefab is assigned
- Verify ShowTacticalDecision is being called
- Check indicator duration is in valid range (0.5-1.0s)

**Glow color doesn't change:**
- Verify SpriteRenderer is found (check Awake)
- Check UpdateGlow is being called
- Ensure colorTransitionSpeed > 0

**Debug labels don't show:**
- Enable showDebugLabels flag
- Verify UpdateDebugInfo is being called
- Check camera is tagged "MainCamera"

**Gizmos don't appear:**
- Enable showGizmos flag
- Verify you're in Scene view (not Game view)
- Check Gizmos button is enabled in Scene view

## Complete Example

See `TASK_8_IMPLEMENTATION_SUMMARY.md` for a complete implementation overview and `VISUAL_FEEDBACK_README.md` for detailed API documentation.
