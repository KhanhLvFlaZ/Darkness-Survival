# Cooperative Behavior System Implementation

## Overview

This document describes the cooperative behavior system implemented for monster AI in Darkness Survival. The system enables monsters to work together tactically through pincer attacks, role-based positioning, relay chasing, and sacrifice plays.

## Components

### 1. CooperativeBehaviorSystem.cs

Main component that implements all cooperative behaviors. Attach this to monster prefabs alongside `TacticalPositioningBehavior`.

**Features:**
- **Pincer Attack Coordination**: Ensures multiple monsters approach from divergent angles (minimum 60 degrees)
- **Tank-and-Spank Role Assignment**: Assigns roles based on HP (high HP = tank, low HP = damage dealer)
- **Relay Chase Behavior**: Alternates pursuit between monsters to prevent exhaustion
- **Sacrifice Play Logic**: Allows low-HP monsters to make aggressive plays for team benefit

**Inspector Settings:**
- `pincerMinAngleDivergence`: Minimum angle between approach vectors (default: 60°)
- `pincerCoordinationRadius`: Range for pincer coordination (default: 8f)
- `tankHpThreshold`: HP ratio threshold for tank role (default: 0.6)
- `tankPreferredDistance`: Preferred distance for tanks (default: 2f)
- `damagePreferredDistance`: Preferred distance for damage dealers (default: 5f)
- `relayPursuitDuration`: How long each monster pursues before switching (default: 3s)
- `sacrificeHpThreshold`: HP threshold for sacrifice plays (default: 0.25)

### 2. CooperativeTacticalBrain.cs

Brain implementation that combines tactical positioning with cooperative behaviors. Use this as the brain component on monsters that should use cooperative tactics.

**Features:**
- Integrates all tactical positioning behaviors (kiting, flanking, optimal distance, corner-cutting)
- Applies cooperative adjustments to movement based on ally positions
- Prioritizes sacrifice plays when conditions are met
- Blends individual and cooperative behaviors smoothly

**Inspector Settings:**
- Enable/disable individual behaviors
- Adjust behavior priorities (0-1 range)
- Configure cooperative behavior weights

## Requirements Implemented

### Requirement 3.1: Pincer Attack Coordination
✅ Calculates approach vectors for multiple monsters
✅ Ensures vectors diverge by at least 60 degrees
✅ Coordinates timing for simultaneous pressure

**Implementation:** `CalculatePincerPosition()` method in `CooperativeBehaviorSystem`

### Requirement 3.2: Tank-and-Spank Role Assignment
✅ Assigns roles based on HP pools (high HP = tank, low HP = damage)
✅ Positions high-HP monsters closer to player
✅ Positions low-HP monsters at range

**Implementation:** `AssignRole()` and `CalculateRoleBasedPosition()` methods

### Requirement 3.3: Relay Chase Behavior
✅ Tracks which monster is actively pursuing
✅ Alternates pursuit between monsters over time
✅ Prevents all monsters from chasing simultaneously

**Implementation:** `ShouldPursueInRelay()` and `CalculateRelaySupportPosition()` methods

### Requirement 3.4: Sacrifice Play Logic
✅ Detects when low-HP monster can create opening
✅ Allows aggressive actions even when retreat is safer
✅ Rewards successful sacrifice plays

**Implementation:** `ShouldMakeSacrificePlay()` and `RewardSacrificePlay()` methods

### Requirement 3.5: Ally Detection and Tracking
✅ Detects nearby ally monsters (up to 5 nearest)
✅ Tracks ally positions, HP ratios, and attack states
✅ Updates ally data in observation space each frame

**Implementation:** Already implemented in `EnemySituationEvaluator.PopulateAllyInformation()`

## Usage

### Basic Setup

1. Add `CooperativeBehaviorSystem` component to monster prefab
2. Add `CooperativeTacticalBrain` component to monster prefab
3. Ensure `TacticalPositioningBehavior` is also attached
4. Set the brain reference in `Monsters.cs` to `CooperativeTacticalBrain`

### Configuration

Adjust the inspector settings to tune cooperative behaviors:

```
Pincer Attacks:
- Increase pincerMinAngleDivergence for wider flanking
- Increase pincerCoordinationRadius to coordinate from farther away

Tank and Spank:
- Adjust tankHpThreshold to change role assignment threshold
- Modify preferred distances to change positioning behavior

Relay Chase:
- Increase relayPursuitDuration for longer pursuit phases
- Decrease for more frequent switching

Sacrifice Plays:
- Lower sacrificeHpThreshold for earlier sacrifice attempts
- Increase sacrificeOpportunityRadius for more aggressive plays
```

### Testing

To test cooperative behaviors:

1. Spawn multiple monsters with `CooperativeTacticalBrain`
2. Observe pincer formations when 2+ monsters engage player
3. Check role assignment by monitoring monster distances (tanks closer, damage farther)
4. Watch for relay chase behavior (monsters taking turns pursuing)
5. Reduce monster HP to trigger sacrifice plays

## Integration with Existing Systems

### Ally Detection
The system uses ally data populated by `EnemySituationEvaluator`:
- `state.allyCount`: Number of nearby allies
- `state.allyPositions[]`: Positions of up to 5 nearest allies
- `state.allyHpRatios[]`: HP ratios of allies
- `state.allyIsAttacking[]`: Attack states of allies

### Reward System
Cooperative behaviors integrate with the reward system via `Monsters.LogReward()`:
- Sacrifice plays apply +2.0 reward on death
- Successful coordination can be rewarded through the reward system
- Rewards are automatically logged and passed to the brain and RewardCalculator

### Attack Timing
Works with `AttackTimingOptimizer` for coordinated attacks:
- Respects attack cooldowns
- Coordinates timing between multiple monsters

## Performance Considerations

- Ally detection uses Physics2D.OverlapCircleAll (limited to 5 allies)
- Relay chase uses static dictionaries for cross-monster coordination
- Role assignment updates every 2 seconds (configurable via `roleReassignmentInterval`)
- Pincer calculations only run when allies are in coordination radius

## Future Enhancements

Potential improvements:
- Add visual indicators for cooperative behaviors (lines between coordinating monsters)
- Implement formation patterns (V-formation, encirclement, etc.)
- Add communication system for explicit coordination messages
- Integrate with ML-Agents for learned cooperative strategies
- Add cooperative behavior metrics tracking

## Troubleshooting

**Monsters not coordinating:**
- Check that multiple monsters have `CooperativeBehaviorSystem` attached
- Verify `enableCooperativeBehaviors` is true in `CooperativeTacticalBrain`
- Ensure monsters are within coordination radius

**Pincer attacks not forming:**
- Increase `pincerCoordinationRadius`
- Check that `enablePincerAttacks` is true
- Verify at least 2 monsters are engaging same player

**Role assignment not working:**
- Check `enableTankAndSpank` is true
- Adjust `tankHpThreshold` if roles seem incorrect
- Verify monsters have different HP ratios

**Relay chase not switching:**
- Increase `relayPursuitDuration` for longer pursuit phases
- Check that `enableRelayChase` is true
- Ensure multiple monsters are within relay radius

## Code Examples

### Checking Current Role
```csharp
CooperativeBehaviorSystem coop = GetComponent<CooperativeBehaviorSystem>();
if (coop.CurrentRole == CooperativeBehaviorSystem.CooperativeRole.Tank)
{
    // This monster is a tank
}
```

### Manual Sacrifice Trigger
```csharp
CooperativeBehaviorSystem coop = GetComponent<CooperativeBehaviorSystem>();
if (coop.ShouldMakeSacrificePlay(state))
{
    // Execute sacrifice play
    coop.RewardSacrificePlay();
}
```

### Custom Cooperative Behavior
```csharp
// In your custom brain
Vector2 pincerAdjustment = cooperativeBehavior.CalculatePincerPosition(state);
if (pincerAdjustment.sqrMagnitude > 0.01f)
{
    // Apply pincer positioning
    moveDirection = Vector2.Lerp(moveDirection, pincerAdjustment, 0.6f);
}
```
