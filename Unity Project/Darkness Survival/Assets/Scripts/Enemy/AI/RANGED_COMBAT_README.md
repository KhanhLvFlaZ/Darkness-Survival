# Ranged Combat Behavior System

## Overview

The Ranged Combat Behavior System implements sophisticated AI behaviors for ranged monsters (e.g., Cult_Mage_Toxin) including intelligent distance maintenance, predictive aiming, adaptive retreat logic, and player movement pattern learning.

## Components

### RangedCombatBehavior.cs

Main component that handles all ranged combat behaviors.

**Key Features:**
- **Distance Management**: Maintains optimal combat distance by retreating when too close and advancing when too far
- **Predictive Aiming**: Calculates lead vectors based on player velocity for accurate projectile firing
- **Adaptive Retreat**: Monitors player movement and adjusts retreat vectors to avoid interception
- **Blocked Retreat Handling**: Automatically switches to perpendicular strafing when retreat path is obstructed
- **Pattern Learning**: Analyzes player movement history to detect and exploit consistent patterns
- **Simultaneous Actions**: Enables movement and attack to occur together
- **Strafe-and-Shoot**: Lateral movement while maintaining fire capability

## Configuration

### Distance Parameters

```csharp
[SerializeField] float minSafeDistance = 4f;      // Triggers retreat
[SerializeField] float maxEngagementDistance = 8f; // Stops retreat
[SerializeField] float optimalDistance = 6f;       // Ideal combat range
```

### Predictive Aiming Parameters

```csharp
[SerializeField, Range(0f, 1f)] float predictionStrength = 0.7f;     // Lead calculation strength
[SerializeField, Range(0f, 1f)] float aimAccuracyVariance = 0.1f;    // Realism variance
```

### Pattern Learning Parameters

```csharp
[SerializeField] bool enablePatternLearning = true;
[SerializeField] int patternMemorySize = 10;                          // Positions to track
[SerializeField, Range(0f, 1f)] float patternConsistencyThreshold = 0.7f;
```

## Integration with Monsters.cs

The system integrates seamlessly with the existing monster AI:

1. **Distance-Based Retreat**: Automatically retreats when player is within `minSafeDistance`
2. **Simultaneous Actions**: Continues firing projectiles while retreating
3. **Predictive Aiming**: All projectiles use velocity-based lead calculation
4. **Accuracy Rewards**: Tracks shot results and applies rewards/penalties via RewardCalculator

## Usage

### Setup for Ranged Monsters

1. Add `RangedCombatBehavior` component to ranged monster prefab
2. Enable ranged attack in Monsters component:
   ```csharp
   enableRangedAttack = true
   rangedAttackRange = 6f
   ```
3. Assign projectile prefab and spawn point
4. Configure distance and aiming parameters

### Example Configuration

**Cult_Mage_Toxin (Cautious Ranged)**
- Min Safe Distance: 5f
- Max Engagement Distance: 9f
- Optimal Distance: 7f
- Prediction Strength: 0.8f
- Accuracy Variance: 0.05f

**Aggressive Ranged Monster**
- Min Safe Distance: 3f
- Max Engagement Distance: 6f
- Optimal Distance: 4.5f
- Prediction Strength: 0.6f
- Accuracy Variance: 0.15f

## Behavior Flow

```
1. Evaluate Distance to Player
   ├─ < minSafeDistance → RETREAT
   │  ├─ Path Clear → Move Away
   │  └─ Path Blocked → Strafe Perpendicular
   ├─ > maxEngagementDistance → ADVANCE
   └─ In Optimal Range → STRAFE or MAINTAIN

2. Calculate Aim Point
   ├─ Get Player Velocity
   ├─ Calculate Lead Vector
   ├─ Apply Pattern Learning (if detected)
   ├─ Add Accuracy Variance
   └─ Fire Projectile

3. Track Shot Result
   ├─ Hit → Apply Reward (scaled by difficulty)
   └─ Miss → Apply Small Penalty

4. Update Pattern Learning
   ├─ Record Player Position
   ├─ Analyze Movement History
   ├─ Detect Consistent Patterns
   └─ Update Confidence Score
```

## Adaptive Retreat Logic

The system monitors player movement and adapts retreat vectors:

1. **Direction Change Detection**: Tracks player velocity changes >30 degrees
2. **Interception Prediction**: Detects when player is moving toward monster
3. **Evasive Adjustment**: Blends perpendicular movement to avoid interception
4. **Continuous Update**: Adjusts retreat vector in real-time

## Pattern Learning

The system learns player movement patterns:

1. **History Tracking**: Records last N player positions
2. **Velocity Analysis**: Calculates average velocity vector
3. **Consistency Measurement**: Determines pattern reliability
4. **Pattern Exploitation**: Uses detected patterns for improved prediction

**Pattern Confidence Calculation:**
```
consistency = 1 - (avgDeviation / avgVelocityMagnitude)
```

When confidence exceeds threshold, the system blends detected pattern with current velocity for more accurate predictions.

## Reward System Integration

The system integrates with RewardCalculator:

- **Hit Reward**: `+0.4 * shotDifficulty`
- **Miss Penalty**: `-0.05`
- **Shot Difficulty**: Based on distance and player velocity
  - Distance Factor: `distance / maxEngagementDistance`
  - Velocity Factor: `playerSpeed / 5.0`
  - Combined: `distanceFactor * 0.6 + velocityFactor * 0.4`

## Debug Visualization

Enable `showDebugGizmos` to visualize:
- **Red Circle**: Minimum safe distance
- **Yellow Circle**: Optimal distance
- **Green Circle**: Maximum engagement distance
- **Cyan Ray**: Current retreat vector
- **Magenta Ray**: Current strafe direction
- **Yellow Sphere**: Predicted aim point
- **Orange Ray**: Detected movement pattern (when confident)

## Performance Considerations

- Pattern learning updates once per frame
- Raycast checks for obstacle detection (2 per retreat calculation)
- History array size configurable (default: 10 positions)
- Adaptive retreat updates only when retreating

## Requirements Validation

This implementation satisfies:
- **Requirement 6.1**: Distance-based retreat with line of sight maintenance
- **Requirement 6.2**: Simultaneous retreat and attack
- **Requirement 6.3**: Blocked retreat handling with perpendicular strafing
- **Requirement 6.4**: Retreat cessation at max engagement distance
- **Requirement 6.5**: Adaptive retreat vector responding to player movement
- **Requirement 7.1**: Predictive aiming with velocity-based lead calculation
- **Requirement 7.2**: Pattern learning for player movement
- **Requirement 7.3**: Strafe-and-shoot behavior
- **Requirement 7.4**: Aim accuracy penalties for missed shots
- **Requirement 7.5**: Aim accuracy rewards scaled by shot difficulty

## Future Enhancements

Potential improvements:
1. **Cover-Aware Retreat**: Prefer retreat paths toward cover positions
2. **Ammo Management**: Track projectile count and retreat when low
3. **Burst Fire Patterns**: Implement firing patterns (single, burst, sustained)
4. **Team Coordination**: Coordinate firing patterns with ally ranged monsters
5. **Elevation Awareness**: Consider height differences in aim calculation
6. **Wind Simulation**: Add environmental factors to projectile trajectory

## Testing

Property-based tests validate:
- Retreat activation within min safe distance
- Retreat cessation at max engagement distance
- Simultaneous movement and attack capability
- Perpendicular strafe on blocked retreat
- Predictive aim calculation accuracy
- Pattern learning convergence

See `Tests/RangedCombatBehaviorTests.cs` for implementation.
