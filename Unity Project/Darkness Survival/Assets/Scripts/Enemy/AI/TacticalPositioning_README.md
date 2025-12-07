# Tactical Positioning Behaviors

This document describes the tactical positioning behaviors implemented for monster AI in Darkness Survival.

## Overview

The `TacticalPositioningBehavior` component provides four core tactical behaviors:

1. **Kiting** - Attack then retreat to avoid counterattacks
2. **Flanking** - Approach from sides/rear rather than frontal assault
3. **Optimal Distance Maintenance** - Keep ideal combat range
4. **Corner Cutting** - Intercept player when they move around obstacles

## Components

### TacticalPositioningBehavior

The main component that implements all tactical positioning logic.

**Required Components:**
- `Monsters` - The monster component this behavior controls

**Configuration:**

#### Kiting Settings
- `kiteCounterattackRange` - Minimum safe distance from player (default: 2.5)
- `kiteRetreatDistance` - How far to retreat (default: 4.0)
- `kiteMinimumDistanceIncrease` - Minimum distance gain per kite (default: 1.5)

#### Flanking Settings
- `flankingMinAngle` - Minimum angle to be considered flanking (default: 45°)
- `flankingOptimalAngle` - Target flanking angle (default: 120°)
- `flankingDetectionRadius` - Range for flanking path detection (default: 8.0)

#### Optimal Distance Settings
- `optimalDistanceMin` - Minimum optimal range (default: 2.0)
- `optimalDistanceMax` - Maximum optimal range (default: 4.0)
- `optimalDistanceApproachSpeed` - Speed multiplier when approaching (default: 1.0)
- `optimalDistanceRetreatSpeed` - Speed multiplier when retreating (default: 1.2)

#### Corner Cutting Settings
- `cornerCutDetectionRadius` - Range for obstacle detection (default: 6.0)
- `cornerCutInterceptAngle` - Angle threshold for intercept (default: 30°)
- `obstacleLayerMask` - Layer mask for obstacles

### TacticalBrain

Example brain implementation that demonstrates how to use `TacticalPositioningBehavior`.

**Configuration:**
- Enable/disable individual behaviors
- Set priority thresholds for each behavior
- Behavior selection is priority-based

## Usage

### Basic Setup

1. Add `TacticalPositioningBehavior` component to your monster prefab
2. Configure the behavior settings in the inspector
3. Set the `obstacleLayerMask` to include obstacle layers

### Integration with Custom Brain

```csharp
public class MyCustomBrain : MonoBehaviour, IEnemyBrain
{
    TacticalPositioningBehavior tactical;
    
    void Awake()
    {
        tactical = GetComponent<TacticalPositioningBehavior>();
    }
    
    public EnemyAction Decide(in SituationState state, EnemyWorkingMemory memory)
    {
        EnemyAction action = EnemyAction.Idle;
        
        // Use kiting when attack is on cooldown
        if (state.attackCooldownRemaining > 0f)
        {
            action.type = EnemyActionType.Kite;
            action.moveDirection = tactical.CalculateKitingVector(
                state.enemyPosition,
                state.playerPosition,
                state.attackCooldownRemaining
            );
        }
        // Use flanking when opportunity exists
        else if (state.flankingOpportunity > 0.7f)
        {
            action.type = EnemyActionType.Flank;
            action.moveDirection = tactical.CalculateFlankingVector(
                state.enemyPosition,
                state.playerPosition,
                state.playerVelocity
            );
        }
        // Default to optimal distance
        else
        {
            action.type = EnemyActionType.Chase;
            action.moveDirection = tactical.CalculateOptimalDistanceVector(
                state.enemyPosition,
                state.playerPosition
            );
        }
        
        action.attemptAttack = state.attackCooldownRemaining <= 0f && state.distanceToPlayer < 2f;
        
        return action;
    }
    
    // ... implement other IEnemyBrain methods
}
```

## Behavior Details

### Kiting Behavior

**When to use:** Attack is on cooldown and monster is close to player

**How it works:**
1. During attack cooldown, calculates retreat direction away from player
2. Ensures retreat distance exceeds counterattack range
3. If retreat path is blocked, tries perpendicular directions
4. When cooldown expires, approaches player to attack

**Requirements validated:** 1.2

### Flanking Behavior

**When to use:** High flanking opportunity score (player facing away)

**How it works:**
1. Determines player facing direction from velocity
2. Calculates current approach angle
3. If angle < 45°, repositions to optimal flanking angle (120°)
4. Chooses left or right flank based on proximity
5. If path blocked, tries opposite flank

**Requirements validated:** 1.3

### Optimal Distance Maintenance

**When to use:** Default behavior for maintaining combat range

**How it works:**
1. Compares current distance to optimal range
2. If too close, retreats
3. If too far, approaches
4. If within range, strafes to maintain engagement
5. Speed multipliers control approach/retreat rates

**Requirements validated:** 1.4

### Corner Cutting

**When to use:** Player is moving and obstacles are present

**How it works:**
1. Predicts player's future position based on velocity
2. Checks if direct path is blocked by obstacles
3. If blocked, calculates intercept path to predicted position
4. If intercept is clear, takes shortcut
5. Otherwise, finds path around obstacle

**Requirements validated:** 1.5

## Integration with Existing Systems

The tactical behaviors integrate seamlessly with:

- **EnemySituationEvaluator** - Provides tactical scores (kitingFeasibility, flankingOpportunity, etc.)
- **Monsters** - Executes movement and attack actions
- **IEnemyBrain** - Decision-making interface
- **RewardCalculator** - Can reward successful tactical maneuvers

## Performance Considerations

- Raycasts are used for obstacle detection (cached when possible)
- Calculations are performed per-frame but are lightweight
- Obstacle layer mask should be configured to minimize raycast overhead
- Consider using FixedUpdate for physics-based calculations if needed

## Future Enhancements

Potential improvements:
- Path caching for corner cutting
- Cooperative flanking with multiple monsters
- Dynamic optimal distance based on player weapons
- Learning-based behavior selection weights
