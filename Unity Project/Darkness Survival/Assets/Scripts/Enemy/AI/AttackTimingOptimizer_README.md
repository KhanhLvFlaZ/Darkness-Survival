# Attack Timing Optimizer

## Overview
The `AttackTimingOptimizer` component implements advanced attack timing optimization for monster AI, including opportunity scoring, coordinated attacks, bait-and-punish behavior, and cooldown enforcement.

## Features Implemented

### 1. Attack Opportunity Scoring (Requirement 2.1, 2.3)
- **Base Score Calculation**: Combines distance factor and cooldown factor
- **Vulnerability Bonus**: Increases score when player is attacking (vulnerable state)
- **Buff Penalty**: Decreases score proportionally to player buff strength
- **Distance-based Scoring**: Optimal at close range, falls off linearly to max distance

### 2. Coordinated Attack Timing (Requirement 2.2)
- **Global Coordination**: Uses static shared timing across all monsters
- **Minimum Interval**: Enforces 0.3s minimum between consecutive attacks
- **Thread-Safe**: Uses lock mechanism for safe concurrent access
- **Attack Registration**: Tracks both local and global attack timing

### 3. Bait-and-Punish Behavior (Requirement 2.4)
- **Overextension Detection**: Identifies when player is far from safety with low HP
- **Reward on Success**: Applies reward when player transitions to overextended state
- **Configurable Thresholds**: Distance and HP thresholds can be tuned

### 4. Attack Prevention During Cooldown (Requirement 2.5)
- **Cooldown Blocking**: Prevents attacks when cooldown is active
- **Repositioning Suggestions**: Recommends tactical actions during cooldown
- **Integration with Monsters**: Blocks attacks at the execution level

## Configuration Parameters

### Attack Opportunity Settings
- `baseOpportunityWeight`: Base weight for opportunity calculation (0-1)
- `vulnerabilityBonus`: Bonus when player is vulnerable (0-2)
- `buffPenaltyPerBuff`: Penalty per player buff (0-1)
- `optimalAttackDistance`: Distance for maximum opportunity
- `maxAttackDistance`: Distance beyond which opportunity is zero

### Coordinated Attack Settings
- `coordinationRadius`: Range for detecting nearby allies
- `minimumAttackInterval`: Minimum time between attacks (default 0.3s)
- `enableCoordination`: Toggle coordination system

### Bait and Punish Settings
- `overextensionDistance`: Distance threshold for overextension
- `baitSuccessReward`: Reward for successful bait
- `enableBaitDetection`: Toggle bait detection

## Integration

### With EnemySituationEvaluator
The evaluator automatically uses the optimizer's attack opportunity score if the component is present:
```csharp
AttackTimingOptimizer attackOptimizer = GetComponent<AttackTimingOptimizer>();
if (attackOptimizer != null)
{
    state.attackOpportunity = attackOptimizer.GetAttackOpportunityScore();
}
```

### With Monsters
The Monsters class checks the optimizer before allowing attacks:
```csharp
if (attackOptimizer != null && attackOptimizer.ShouldBlockAttack())
{
    return; // Block attack
}

if (attackOptimizer != null)
{
    attackOptimizer.RegisterAttackAttempt(); // Register for coordination
}
```

### With RewardCalculator
New reward types added to RewardConfig:
- `baitSuccessReward`: Reward for successful bait-and-punish
- `coordinatedAttackBonus`: Bonus for coordinated attacks
- `vulnerableAttackBonus`: Bonus for attacking vulnerable player

## Usage

1. Add `AttackTimingOptimizer` component to monster prefab
2. Configure parameters in inspector
3. Ensure `EnemySituationEvaluator` and `RewardCalculator` are present
4. The system will automatically integrate with existing AI

## Public API

### Properties
- `CurrentAttackOpportunity`: Current attack opportunity score (0-1)
- `CanAttackNow`: Whether attack is allowed now

### Methods
- `GetAttackOpportunityScore()`: Get current opportunity score
- `ShouldBlockAttack()`: Check if attack should be blocked
- `RegisterAttackAttempt()`: Register attack for coordination
- `GetCooldownAction(state)`: Get recommended action during cooldown

## Requirements Validation

✅ **Requirement 2.1**: Attack opportunity increases when player is vulnerable
✅ **Requirement 2.2**: Multiple monsters coordinate attack timing with 0.3s minimum interval
✅ **Requirement 2.3**: Attack opportunity decreases when player has buffs
✅ **Requirement 2.4**: Monsters are rewarded for successful bait-and-punish
✅ **Requirement 2.5**: Attacks are prevented during cooldown, repositioning is preferred
