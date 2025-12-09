# AI Visual Feedback System

## Overview

The AI Visual Feedback System provides visual indicators that communicate monster AI state and decisions to players. This makes the learning process observable and helps players recognize intelligent behaviors.

## Components

### AITier Enum
Defines AI sophistication levels:
- **Novice**: Blue glow - Heuristic only, predictable
- **Learning**: Cyan glow - ML + heuristic blend with exploration
- **Trained**: Yellow glow - Primarily ML with minimal exploration
- **Expert**: Gold glow - ML only with advanced features

### AIVisualFeedback Component

Main component that handles all visual feedback for monster AI.

#### Inspector Configuration

**Indicator Prefabs:**
- `brainIconPrefab`: Brain icon shown when tactical decisions are made
- `levelUpEffectPrefab`: Effect displayed when AI tier increases

**Particle Systems:**
- `tacticalDecisionEffect`: Generic tactical decision particle effect
- `flankingEffect`: Specific effect for flanking maneuvers
- `kitingEffect`: Specific effect for kiting maneuvers
- `coordinatedAttackEffect`: Effect for coordinated attacks

**AI Tier Colors:**
- `noviceGlowColor`: Blue (default: rgba(0.3, 0.5, 1, 0.5))
- `learningGlowColor`: Cyan (default: rgba(0.3, 0.7, 1, 0.6))
- `trainedGlowColor`: Yellow (default: rgba(1, 0.9, 0.3, 0.7))
- `expertGlowColor`: Gold (default: rgba(1, 0.8, 0.2, 0.8))

**Debug Visualization:**
- `showDebugLabels`: Display action type and reward above monster
- `showGizmos`: Render movement vectors, attack ranges, and positioning zones
- `showCoordinationLines`: Draw lines between cooperating monsters

**Visual Settings:**
- `indicatorDuration`: How long to show tactical indicators (0.5-1.0s)
- `colorTransitionSpeed`: Speed of glow color transitions
- `indicatorOffset`: Vertical offset for floating indicators

**Debug Gizmo Settings:**
- `attackRange`: Attack range circle radius
- `optimalRangeMin`: Inner optimal positioning circle
- `optimalRangeMax`: Outer optimal positioning circle

## Usage

### Basic Setup

1. Add `AIVisualFeedback` component to monster prefab
2. Assign prefabs and particle systems in Inspector
3. Configure colors and debug settings as needed

### Showing Tactical Decisions

```csharp
AIVisualFeedback feedback = GetComponent<AIVisualFeedback>();
feedback.ShowTacticalDecision(EnemyActionType.Flank);
```

This will:
- Display brain icon for 0.5-1.0 seconds
- Trigger tactical decision particle effect
- Trigger specific maneuver effect (if configured)

### Updating AI Tier

```csharp
feedback.ShowLevelUp(AITier.Expert);
```

This will:
- Display level-up effect
- Smoothly transition glow color to expert gold
- Update internal tier state

### Debug Information

```csharp
feedback.UpdateDebugInfo("Kite", 0.5f, new Vector2(1, 0));
```

When `showDebugLabels` is enabled, this displays:
- Current action type
- Current reward value (green if positive, red if negative)
- Movement direction (shown as gizmo arrow)

### Coordination Visualization

```csharp
// Add ally to coordination visualization
feedback.AddCoordinatingAlly(allyFeedback);

// Remove ally
feedback.RemoveCoordinatingAlly(allyFeedback);

// Clear all
feedback.ClearCoordinatingAllies();
```

When `showCoordinationLines` is enabled, orange lines connect cooperating monsters.

## Visual Feedback Behaviors

### Tactical Decision Indicators (Requirement 8.1)
- Brain icon appears when monster makes tactical decision
- Duration: 0.5-1.0 seconds (configurable)
- Fades in/out smoothly
- Varies by action type

### Intelligent Maneuver Effects (Requirement 8.2)
- Distinct particle effects for:
  - Flanking maneuvers
  - Kiting maneuvers
  - Coordinated attacks
- Effects can be scaled based on success (configure in particle system)

### AI Tier Visual Updates (Requirement 8.3)
- Level-up effect on tier increase
- Glow color indicates current tier:
  - Novice: Blue
  - Learning: Cyan
  - Trained: Yellow
  - Expert: Gold
- Smooth color transitions (no popping)

### Debug Visualization (Requirements 8.4, 14.1, 14.5)
- Action type and reward labels (OnGUI)
- Movement vector arrows (Gizmos)
- Attack range circles (red)
- Optimal positioning zones (green rings)
- Coordination lines (orange)

### Smooth Visual Transitions (Requirement 8.5)
- Color interpolation for glow changes
- Fade in/out for indicators
- Configurable transition speed

## Debug Mode

Enable debug visualization to see:
- **Labels**: Action type and reward above each monster
- **Gizmos**: Movement vectors, ranges, and zones
- **Coordination**: Lines connecting cooperating monsters

Useful for:
- Training observation
- Behavior debugging
- Performance tuning
- Understanding AI decisions

## Performance Considerations

- Particle systems are only played when needed
- Gizmos only render in editor with flag enabled
- OnGUI labels only render when debug mode is on
- Color interpolation uses efficient lerp
- Indicator coroutines clean up properly

## Integration with Other Systems

The visual feedback system integrates with:
- **HybridEnemyBrain**: Calls `ShowTacticalDecision` on action selection
- **AITierManager**: Calls `ShowLevelUp` and `UpdateGlow` on tier changes
- **RewardCalculator**: Provides reward values for debug display
- **CooperativeBehaviorSystem**: Manages coordination visualization

## Requirements Validation

This implementation satisfies:
- ✅ Requirement 8.1: Tactical decision indicators
- ✅ Requirement 8.2: Intelligent maneuver effects
- ✅ Requirement 8.3: AI tier visual updates
- ✅ Requirement 8.4: Debug visualization
- ✅ Requirement 8.5: Smooth visual transitions
- ✅ Requirement 14.1: Gizmo rendering for AI state
- ✅ Requirement 14.5: Coordination visualization

## Future Enhancements

Potential additions:
- Sound effects for tactical decisions
- More granular particle effects per action type
- Configurable indicator icons per action
- Network synchronization for multiplayer
- Replay system integration
