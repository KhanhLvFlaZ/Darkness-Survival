# Design Document

## Overview

This design document specifies the architecture and implementation approach for integrating ML-Agents-based Reinforcement Learning into the monster AI system of Darkness Survival. The system will enable monsters to learn and execute sophisticated combat behaviors including tactical positioning, attack timing optimization, cooperative strategies, obstacle utilization, and specialized ranged combat tactics.

The design builds upon the existing AI infrastructure (IEnemyBrain, EnemySituationEvaluator, EnemyWorkingMemory, RewardCalculator) and extends it with new action types, enhanced observation spaces, visual feedback systems, and ML-Agents training integration.

Key design goals:
- Seamless integration with existing monster and combat systems
- Observable AI behaviors through visual feedback
- Graceful fallback to heuristic AI when ML models are unavailable
- Configurable reward functions per monster type
- Comprehensive metrics for training evaluation
- Support for both training and inference modes

## Architecture

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        Game Manager                          │
│                    (Orchestrates gameplay)                   │
└────────────────────────┬────────────────────────────────────┘
                         │
         ┌───────────────┴───────────────┐
         │                               │
┌────────▼────────┐            ┌────────▼────────┐
│  Player System  │            │  Monster System │
│  - Character    │◄───────────┤  - Monsters.cs  │
│  - PlayerMove   │  Combat    │  - AI Brain     │
│  - Weapons      │  Events    │  - Rewards      │
└─────────────────┘            └────────┬────────┘
                                        │
                    ┌───────────────────┼───────────────────┐
                    │                   │                   │
          ┌─────────▼─────────┐ ┌──────▼──────┐  ┌────────▼────────┐
          │ EnemySituation    │ │ IEnemyBrain │  │ RewardCalculator│
          │ Evaluator         │ │ Interface   │  │                 │
          │ (Observations)    │ └──────┬──────┘  │ (Reward Signals)│
          └───────────────────┘        │         └─────────────────┘
                                       │
                    ┌──────────────────┼──────────────────┐
                    │                  │                  │
          ┌─────────▼─────────┐ ┌─────▼──────┐  ┌───────▼────────┐
          │ HybridEnemyBrain  │ │ ML-Agents  │  │   Heuristic    │
          │ (Decision Maker)  │ │ Policy Net │  │   Fallback     │
          └─────────┬─────────┘ └────────────┘  └────────────────┘
                    │
          ┌─────────▼─────────┐
          │ Visual Feedback   │
          │ System            │
          │ - Indicators      │
          │ - Debug Gizmos    │
          └───────────────────┘
```

### Data Flow

1. **Observation Collection**: EnemySituationEvaluator gathers game state (player position, monster state, ally positions, obstacles)
2. **Decision Making**: HybridEnemyBrain receives observations and outputs actions (via ML policy or heuristic)
3. **Action Execution**: Monsters.cs executes the selected action (movement, attack, mode changes)
4. **Reward Calculation**: RewardCalculator evaluates action outcomes and provides reward signals
5. **Learning**: ML-Agents policy network updates based on rewards (training mode only)
6. **Visual Feedback**: Visual indicators display AI state to player


## Components and Interfaces

### 1. Enhanced Action Space

**Purpose**: Extend the existing EnemyActionType enum to support new tactical behaviors.

**Interface**:
```csharp
public enum EnemyActionType
{
    Idle,
    Chase,
    Strafe,
    Retreat,
    Kite,           // NEW: Attack then retreat
    Flank,          // NEW: Approach from sides/rear
    Ambush,         // NEW: Wait at strategic position
    SeekCover,      // NEW: Move to obstacle cover
    HerdPlayer,     // NEW: Push player toward disadvantage
    CoordinatedAttack // NEW: Synchronized multi-monster attack
}
```

**Responsibilities**:
- Define all possible tactical behaviors
- Provide semantic meaning for ML policy outputs
- Enable heuristic fallback to interpret actions

### 2. Enhanced Observation Space

**Purpose**: Provide comprehensive game state information to the AI decision-making system.

**Interface**:
```csharp
public struct EnhancedSituationState
{
    // Existing fields
    public Vector2 enemyPosition;
    public Vector2 playerPosition;
    public float enemyHpRatio;
    public float playerHpRatio;
    public float distanceToPlayer;
    
    // NEW: Player state
    public bool playerIsAttacking;
    public bool playerIsVulnerable;
    public float playerBuffStrength;
    public Vector2 playerVelocity;
    
    // NEW: Ally information
    public Vector2[] allyPositions;      // Up to 5 nearest allies
    public float[] allyHpRatios;
    public bool[] allyIsAttacking;
    public int allyCount;
    
    // NEW: Environment
    public Vector2[] nearbyObstaclePositions; // Up to 8 obstacles
    public int obstacleCount;
    public bool hasLineOfSight;
    public Vector2 nearestCoverPosition;
    
    // NEW: Tactical scores
    public float flankingOpportunity;    // 0-1 score
    public float kitingFeasibility;      // 0-1 score
    public float cooperationPotential;   // 0-1 score
}
```

**Responsibilities**:
- Aggregate all relevant game state
- Normalize values to [0,1] or [-1,1] ranges for ML
- Update every frame or on significant events
- Provide both raw and processed tactical scores


### 3. Ranged Combat Behavior System

**Purpose**: Implement specialized behaviors for ranged monsters (e.g., Cult_Mage_Toxin) including distance maintenance and predictive aiming.

**Interface**:
```csharp
public class RangedCombatBehavior : MonoBehaviour
{
    [Header("Distance Management")]
    public float minSafeDistance = 4f;
    public float maxEngagementDistance = 8f;
    public float optimalDistance = 6f;
    
    [Header("Predictive Aiming")]
    public float predictionStrength = 0.7f; // 0 = no prediction, 1 = full prediction
    public float aimAccuracyVariance = 0.1f;
    
    public Vector2 CalculateRetreatVector(Vector2 playerPosition, Vector2 currentPosition);
    public Vector2 CalculatePredictiveAimPoint(Vector2 playerPosition, Vector2 playerVelocity);
    public bool ShouldRetreat(float currentDistance);
    public bool ShouldAdvance(float currentDistance);
    public Vector2 GetStrafeDirection(Vector2 playerApproachVector);
}
```

**Responsibilities**:
- Calculate optimal retreat vectors when player is too close
- Enable simultaneous movement and shooting
- Compute predictive aim points based on player velocity
- Handle blocked retreat paths with perpendicular strafing
- Maintain optimal engagement distance

### 4. Visual Feedback System

**Purpose**: Provide visual indicators that communicate AI state and decisions to the player.

**Interface**:
```csharp
public class AIVisualFeedback : MonoBehaviour
{
    [Header("Indicators")]
    public GameObject brainIconPrefab;
    public GameObject levelUpEffectPrefab;
    public ParticleSystem tacticalDecisionEffect;
    
    [Header("Colors")]
    public Color learningGlowColor = new Color(0.3f, 0.7f, 1f, 0.5f);
    public Color expertGlowColor = new Color(1f, 0.8f, 0.2f, 0.7f);
    
    [Header("Debug")]
    public bool showDebugLabels = false;
    public bool showGizmos = false;
    
    public void ShowTacticalDecision(EnemyActionType actionType, float duration = 0.75f);
    public void ShowLevelUp(AITier newTier);
    public void UpdateGlow(AITier currentTier);
    public void DrawDebugInfo(string actionType, float reward, Vector2 moveDirection);
}
```

**Responsibilities**:
- Display brain icons when tactical decisions are made
- Show particle effects for intelligent maneuvers
- Update visual appearance based on AI tier
- Render debug information in editor
- Smoothly transition between visual states


### 5. AI Tier System

**Purpose**: Classify monsters by AI sophistication level to control difficulty progression.

**Interface**:
```csharp
public enum AITier
{
    Novice,      // Heuristic only, predictable
    Learning,    // ML + heuristic blend, exploration
    Trained,     // Primarily ML, minimal exploration
    Expert       // ML only, advanced features enabled
}

public class AITierManager : MonoBehaviour
{
    public AITier currentTier = AITier.Novice;
    public float explorationRate = 0.2f;
    
    public void SetTier(AITier tier);
    public float GetExplorationRate();
    public bool ShouldUseMlPolicy();
    public bool ShouldUseHeuristic();
    public float GetPolicyBlendWeight(); // 0 = heuristic, 1 = ML
}
```

**Responsibilities**:
- Manage AI sophistication level
- Control exploration vs exploitation balance
- Determine decision backend (ML vs heuristic)
- Adjust visual feedback based on tier

### 6. Metrics Tracking System

**Purpose**: Record comprehensive statistics for training evaluation and debugging.

**Interface**:
```csharp
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
}

public class MetricsTracker : MonoBehaviour
{
    public LearningMetrics currentMetrics;
    
    public void RecordEpisodeEnd(EpisodeSummary summary);
    public void UpdateDamageEfficiency(float dealt, float taken);
    public void UpdatePositioningScore(bool inOptimalRange, float deltaTime);
    public void UpdateAttackAccuracy(bool hit);
    public void UpdateCooperationScore(bool successful);
    public void ExportMetrics(string filepath);
}
```

**Responsibilities**:
- Track all relevant performance metrics
- Calculate derived statistics (efficiency, accuracy)
- Provide data for training visualization
- Export metrics for analysis


### 7. Enhanced Reward Configuration

**Purpose**: Allow per-monster-type reward function customization.

**Interface**:
```csharp
[CreateAssetMenu(fileName = "RewardConfig", menuName = "AI/Reward Configuration")]
public class EnhancedRewardConfig : ScriptableObject
{
    [Header("Combat Rewards")]
    public float damageDealtWeight = 1.0f;
    public float damageTakenWeight = -0.5f;
    public float killReward = 10f;
    public float deathPenalty = -10f;
    
    [Header("Positioning Rewards")]
    public float idealDistanceReward = 0.1f;
    public float idealDistanceMin = 2f;
    public float idealDistanceMax = 4f;
    public float obstructedPenalty = -0.05f;
    public float coverBonusWhenLowHp = 0.2f;
    
    [Header("Tactical Rewards")]
    public float kitingSuccessReward = 0.5f;
    public float flankingBonusReward = 0.3f;
    public float predictiveHitBonus = 0.4f;
    public float baitSuccessReward = 1.0f;
    
    [Header("Cooperation Rewards")]
    public float coordinatedAttackBonus = 0.6f;
    public float pincerAttackBonus = 0.8f;
    public float sacrificePlayReward = 2.0f;
    
    [Header("Survival Rewards")]
    public float survivalTickReward = 0.01f;
    public float survivalTickInterval = 1.0f;
    
    [Header("Constraints")]
    public float maxRewardMagnitude = 5.0f;
}
```

**Responsibilities**:
- Define reward weights for all behaviors
- Allow designer tuning per monster type
- Support hot-reloading during training
- Clamp rewards to prevent instability

## Data Models

### Monster AI State Machine

```
┌─────────┐
│  Spawn  │
└────┬────┘
     │
     ▼
┌─────────────┐     ┌──────────────┐
│  Evaluate   │────▶│  Observe     │
│  Situation  │     │  Environment │
└─────┬───────┘     └──────────────┘
      │
      ▼
┌─────────────┐     ┌──────────────┐
│   Decide    │────▶│  ML Policy   │
│   Action    │     │  or          │
└─────┬───────┘     │  Heuristic   │
      │             └──────────────┘
      ▼
┌─────────────┐
│   Execute   │
│   Action    │
└─────┬───────┘
      │
      ▼
┌─────────────┐     ┌──────────────┐
│  Calculate  │────▶│  Update      │
│  Rewards    │     │  Metrics     │
└─────┬───────┘     └──────────────┘
      │
      ▼
┌─────────────┐
│  Visual     │
│  Feedback   │
└─────┬───────┘
      │
      └──────────┐
                 │
      ┌──────────▼──────────┐
      │  Death or Continue  │
      └──────────┬──────────┘
                 │
      ┌──────────▼──────────┐
      │  Episode End        │
      └─────────────────────┘
```

### ML-Agents Integration Model

**Observation Vector Structure** (minimum 32 floats):
```
[0-1]   : Enemy position (x, y)
[2-3]   : Player position (x, y)
[4-5]   : Enemy velocity (x, y)
[6-7]   : Player velocity (x, y)
[8]     : Enemy HP ratio
[9]     : Player HP ratio
[10]    : Distance to player (normalized)
[11]    : Attack cooldown remaining (normalized)
[12]    : Is spirit mode (0 or 1)
[13]    : Is obstructed (0 or 1)
[14]    : Player is attacking (0 or 1)
[15]    : Player is vulnerable (0 or 1)
[16]    : Player buff strength (0-1)
[17]    : Has line of sight (0 or 1)
[18]    : Flanking opportunity (0-1)
[19]    : Kiting feasibility (0-1)
[20]    : Cooperation potential (0-1)
[21-30] : Ally positions and states (5 allies × 2 values)
[31-38] : Obstacle positions (4 obstacles × 2 values)
[39+]   : Reserved for future expansion
```

**Action Vector Structure**:
```
Discrete Actions:
- Action Type Selection: [0-9] (maps to EnemyActionType)

Continuous Actions:
- Movement Direction X: [-1, 1]
- Movement Direction Y: [-1, 1]
- Attack Attempt: [0, 1] (threshold at 0.5)
```


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Valid action selection
*For any* combat situation, when the system selects a tactical behavior, the selected action type must be one of the valid EnemyActionType enum values
**Validates: Requirements 1.1**

### Property 2: Kiting distance increase
*For any* monster performing a kiting maneuver, the distance to the player after the maneuver must be greater than before the maneuver and exceed the configured counterattack range
**Validates: Requirements 1.2**

### Property 3: Flanking angle constraint
*For any* monster choosing a flanking approach, the approach angle relative to the player's facing direction must be greater than 45 degrees
**Validates: Requirements 1.3**

### Property 4: Optimal distance maintenance
*For any* monster at close range, the monster's distance to the player must remain within the configured optimal distance bounds (min to max) over time
**Validates: Requirements 1.4**

### Property 5: Attack opportunity increase on player vulnerability
*For any* monster observing a player in attack animation, the monster's attack opportunity score must increase compared to when the player is not attacking
**Validates: Requirements 2.1**

### Property 6: Coordinated attack timing
*For any* group of monsters near the same player, attack timings must be staggered rather than simultaneous, with at least 0.3 seconds between consecutive attacks
**Validates: Requirements 2.2**

### Property 7: Attack opportunity decrease with buffs
*For any* monster observing a player with active buffs, the attack opportunity score must decrease proportionally to the buff strength
**Validates: Requirements 2.3**

### Property 8: Attack prevention during cooldown
*For any* monster with active attack cooldown, attack attempts must be blocked and movement actions must be preferred
**Validates: Requirements 2.5**

### Property 9: Pincer attack divergence
*For any* two or more monsters engaging the same player, their approach vectors must diverge by at least 60 degrees to create a pincer formation
**Validates: Requirements 3.1**

### Property 10: Tank positioning priority
*For any* group of monsters with varying HP, high-HP monsters must be positioned closer to the player than low-HP monsters on average
**Validates: Requirements 3.2**

### Property 11: Ally observation data completeness
*For any* monster with nearby allies, the observation space must contain valid position and state data for all allies within detection range
**Validates: Requirements 3.5**

### Property 12: Aggression increase on low player HP
*For any* monster observing player HP below 30%, the aggression level must be higher than when player HP is above 30%
**Validates: Requirements 4.1**

### Property 13: Caution increase with player buffs
*For any* monster observing multiple player buffs, the caution level must increase proportionally to the buff count
**Validates: Requirements 4.2**

### Property 14: Personality trait persistence
*For any* monster with learned personality traits, the trait values must remain consistent across all frames within the monster's lifetime
**Validates: Requirements 4.5**

### Property 15: Cover seeking on low HP
*For any* monster with HP below 40%, the movement direction must be toward the nearest cover position when cover is available
**Validates: Requirements 5.1**

### Property 16: Obstruction penalty application
*For any* monster in an obstructed state, a negative reward must be applied to discourage remaining in the blocked position
**Validates: Requirements 5.5**

### Property 17: Ranged retreat activation
*For any* ranged monster within its minimum safe distance, the monster must move away from the player while maintaining line of sight
**Validates: Requirements 6.1**

### Property 18: Simultaneous retreat and attack
*For any* ranged monster in retreat mode, both movement and attack actions must be active simultaneously
**Validates: Requirements 6.2**

### Property 19: Perpendicular strafe on blocked retreat
*For any* ranged monster with blocked retreat path, the movement direction must be perpendicular (within 15 degrees) to the player's approach vector
**Validates: Requirements 6.3**

### Property 20: Retreat cessation at max distance
*For any* ranged monster at or beyond maximum engagement distance, retreat velocity must approach zero
**Validates: Requirements 6.4**

### Property 21: Predictive aim calculation
*For any* ranged monster firing a projectile, the aim direction must include a velocity-based lead vector proportional to player speed
**Validates: Requirements 7.1**

### Property 22: Simultaneous strafe and fire
*For any* ranged monster in strafe mode, both lateral movement and projectile firing must be active
**Validates: Requirements 7.3**

### Property 23: Visual indicator duration
*For any* tactical decision made by a monster, a visual indicator must be displayed for a duration between 0.5 and 1.0 seconds
**Validates: Requirements 8.1**

### Property 24: Smooth visual transitions
*For any* monster transitioning between behavior states, visual indicator changes must use interpolation rather than instant changes
**Validates: Requirements 8.5**

### Property 25: Valid AI tier assignment
*For any* spawned monster, the assigned AI tier must be one of {Novice, Learning, Trained, Expert}
**Validates: Requirements 9.1**

### Property 26: Novice tier uses heuristic only
*For any* monster with Novice tier, the decision backend must be set to heuristic-only mode
**Validates: Requirements 9.2**

### Property 27: Expert tier uses ML only
*For any* monster with Expert tier, the decision backend must use ML policy exclusively without heuristic blending
**Validates: Requirements 9.5**

### Property 28: Damage efficiency calculation
*For any* monster that has dealt and taken damage, the damage efficiency metric must equal the ratio of damage dealt to damage taken
**Validates: Requirements 10.2**

### Property 29: Positioning score increment
*For any* monster maintaining optimal positioning, the positioning score must increase over time proportional to the duration in optimal range
**Validates: Requirements 10.3**

### Property 30: Cooperation score distribution
*For any* successful coordinated action, all participating monsters must receive cooperation score updates
**Validates: Requirements 10.5**

### Property 31: Multi-factor reward calculation
*For any* action taken by a monster, the reward signal must incorporate contributions from damage dealt, damage taken, positioning quality, cooperation success, and survival time
**Validates: Requirements 11.3**

### Property 32: Episode reset and summary
*For any* completed training episode, the environment must reset and provide episode summary statistics to the policy network
**Validates: Requirements 11.4**

### Property 33: Heuristic fallback on missing model
*For any* monster without an assigned ML policy model, the decision system must automatically use heuristic-based behavior
**Validates: Requirements 12.1**

### Property 34: Action validation and clamping
*For any* ML policy output, invalid action values must be clamped or sanitized to valid ranges before execution
**Validates: Requirements 12.3**

### Property 35: Behavioral continuity on mode switch
*For any* transition between ML and heuristic modes, the monster's velocity must not change abruptly (delta < 2.0 units/frame)
**Validates: Requirements 12.4**

### Property 36: Observation recording continuity
*For any* monster using heuristic fallback, observation data must still be recorded for potential offline training
**Validates: Requirements 12.5**

### Property 37: Type-specific reward weights
*For any* melee monster and ranged monster of the same base type, the damage dealt reward weight for melee must be higher than for ranged
**Validates: Requirements 13.2**

### Property 38: Ranged positioning reward priority
*For any* ranged monster, the positioning reward weight must be higher than for melee monsters of the same base type
**Validates: Requirements 13.3**


## Error Handling

### ML Model Loading Failures

**Scenario**: ML policy model fails to load or is corrupted
**Handling**:
1. Log detailed error message with model path and exception details
2. Automatically switch to heuristic fallback mode
3. Set AI tier to Novice to indicate degraded capability
4. Continue game execution without interruption
5. Display warning in editor console (development builds only)

**Code Pattern**:
```csharp
try {
    LoadMLModel(modelPath);
} catch (Exception e) {
    Debug.LogError($"Failed to load ML model: {e.Message}");
    SwitchToHeuristicFallback();
    currentTier = AITier.Novice;
}
```

### Invalid Action Outputs

**Scenario**: ML policy produces out-of-range or NaN values
**Handling**:
1. Detect invalid values (NaN, Infinity, out of bounds)
2. Clamp continuous values to valid ranges [-1, 1]
3. Default discrete actions to Idle if invalid
4. Log warning with monster ID and invalid values
5. Apply small penalty to discourage policy from producing invalid outputs

**Code Pattern**:
```csharp
if (float.IsNaN(action.moveDirection.x) || float.IsInfinity(action.moveDirection.x)) {
    action.moveDirection.x = 0f;
    Debug.LogWarning($"Invalid action from monster {gameObject.name}");
    ApplyPenalty(-0.1f);
}
action.moveDirection.x = Mathf.Clamp(action.moveDirection.x, -1f, 1f);
```

### Missing Observation Data

**Scenario**: Required game state is unavailable (e.g., player destroyed, ally not found)
**Handling**:
1. Use default/zero values for missing data
2. Set validity flags in observation vector
3. Continue decision-making with partial information
4. Log warning if critical data is missing repeatedly

**Code Pattern**:
```csharp
if (player == null) {
    observation.playerPosition = Vector2.zero;
    observation.playerValid = false;
    Debug.LogWarning("Player reference lost, using default values");
}
```

### Reward Calculation Overflow

**Scenario**: Cumulative rewards exceed safe numerical ranges
**Handling**:
1. Clamp individual rewards to configured maximum magnitude
2. Reset cumulative reward if it exceeds threshold (e.g., ±1000)
3. Log warning when clamping occurs
4. Normalize rewards before sending to ML-Agents

**Code Pattern**:
```csharp
float clampedReward = Mathf.Clamp(rawReward, -maxRewardMagnitude, maxRewardMagnitude);
if (Mathf.Abs(cumulativeReward) > 1000f) {
    Debug.LogWarning($"Reward overflow detected, resetting");
    cumulativeReward = 0f;
}
```

### Cooperative Behavior Failures

**Scenario**: Ally monsters are destroyed or out of range during coordination
**Handling**:
1. Validate ally references before accessing
2. Remove destroyed allies from observation list
3. Recalculate cooperation potential with remaining allies
4. Gracefully degrade to solo behavior if no allies remain

**Code Pattern**:
```csharp
allyPositions = allyPositions.Where(a => a != null && a.gameObject != null).ToArray();
if (allyPositions.Length == 0) {
    cooperationPotential = 0f;
    // Fall back to solo tactics
}
```

## Testing Strategy

### Unit Testing Approach

**Core Logic Tests**:
- Action validation and clamping functions
- Observation space normalization
- Reward calculation formulas
- Distance and angle calculations for tactical behaviors
- Heuristic decision logic for each action type

**Integration Tests**:
- ML model loading and inference pipeline
- Observation collection from game state
- Action execution through Monsters.cs
- Reward signal flow from RewardCalculator to brain
- Visual feedback triggering on state changes

**Edge Case Tests**:
- Null/missing player reference
- Empty ally lists
- Zero-length vectors in movement calculations
- Extreme reward values (very large positive/negative)
- Rapid AI tier changes

### Property-Based Testing Approach

**Testing Framework**: Use Unity Test Framework with custom property test generators

**Test Configuration**:
- Minimum 100 iterations per property test
- Random seed logging for reproducibility
- Configurable value ranges for generated inputs

**Property Test Categories**:

1. **Behavioral Properties**: Test that actions produce expected state changes
2. **Numerical Properties**: Test that calculations stay within valid ranges
3. **Invariant Properties**: Test that certain conditions always hold
4. **Coordination Properties**: Test multi-agent interactions

**Example Property Tests**:

```csharp
// Property 2: Kiting distance increase
[Test]
public void Property_KitingIncreasesDistance_ForAllMonsters()
{
    for (int i = 0; i < 100; i++)
    {
        // Generate random monster and player positions
        Vector2 monsterPos = RandomPosition();
        Vector2 playerPos = RandomPosition();
        float initialDistance = Vector2.Distance(monsterPos, playerPos);
        
        // Execute kiting behavior
        EnemyAction kiteAction = new EnemyAction { type = EnemyActionType.Kite };
        Vector2 newMonsterPos = ExecuteAction(kiteAction, monsterPos, playerPos);
        float finalDistance = Vector2.Distance(newMonsterPos, playerPos);
        
        // Verify distance increased
        Assert.Greater(finalDistance, initialDistance);
        Assert.Greater(finalDistance, counterattackRange);
    }
}

// Property 9: Pincer attack divergence
[Test]
public void Property_PincerAttacksDiverge_ForAllMonsterPairs()
{
    for (int i = 0; i < 100; i++)
    {
        // Generate random positions for 2 monsters and player
        Vector2 monster1Pos = RandomPosition();
        Vector2 monster2Pos = RandomPosition();
        Vector2 playerPos = RandomPosition();
        
        // Calculate approach vectors
        Vector2 approach1 = (playerPos - monster1Pos).normalized;
        Vector2 approach2 = (playerPos - monster2Pos).normalized;
        
        // Calculate angle between approaches
        float angle = Vector2.Angle(approach1, approach2);
        
        // Verify divergence
        Assert.GreaterOrEqual(angle, 60f);
    }
}
```

### ML-Agents Training Validation

**Training Metrics to Monitor**:
- Cumulative reward trend (should increase over episodes)
- Episode length (should increase as monsters survive longer)
- Policy loss and value loss convergence
- Exploration rate decay
- Success rate for specific behaviors (kiting, flanking, etc.)

**Validation Episodes**:
- Run 100 validation episodes every 10k training steps
- Record success rates for each tactical behavior
- Compare against baseline heuristic performance
- Ensure no catastrophic forgetting of basic behaviors

**Hyperparameter Testing**:
- Learning rate: [1e-5, 1e-3]
- Batch size: [512, 2048, 8192]
- Hidden layer sizes: [128, 256, 512]
- Discount factor (gamma): [0.95, 0.99, 0.995]

### Visual Feedback Testing

**Manual Testing Checklist**:
- [ ] Brain icon appears on tactical decisions
- [ ] Glow color matches AI tier
- [ ] Particle effects trigger on intelligent maneuvers
- [ ] Level-up effect plays on tier increase
- [ ] Debug labels display correct information
- [ ] Visual transitions are smooth (no popping)
- [ ] Effects are visible against various backgrounds

**Automated Visual Tests**:
- Verify effect prefabs are instantiated
- Check effect duration matches specification
- Validate color values are within expected ranges
- Ensure effects are cleaned up properly

