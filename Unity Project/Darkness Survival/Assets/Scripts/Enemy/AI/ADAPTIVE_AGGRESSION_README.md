# Adaptive Aggression System

## Overview

The Adaptive Aggression System enables monsters to dynamically adjust their behavior based on personality traits and situational factors. This system implements Requirements 4.1-4.5 from the monster RL behaviors specification.

## Components

### PersonalityTraits (Struct)

A data structure representing a monster's personality characteristics:

- **Aggression** (0-1): Likelihood to attack and take risks
- **Caution** (0-1): Tendency to play defensively and avoid damage
- **Teamwork** (0-1): Willingness to cooperate with allies
- **Opportunism** (0-1): Ability to exploit player weaknesses

**Preset Personalities:**
- `Balanced`: All traits at 0.5
- `Aggressive`: High aggression (0.8), low caution (0.2)
- `Cautious`: Low aggression (0.3), high caution (0.8)
- `Cooperative`: High teamwork (0.9)
- `Random(min, max)`: Randomized traits within range

### AdaptiveAggressionSystem (Component)

Manages personality traits and dynamic behavior adjustment for monsters.

**Key Features:**

1. **Personality Initialization** (Requirement 4.5)
   - Traits are initialized on monster spawn
   - Can be randomized or set to specific presets
   - Persists across the monster's lifetime

2. **Dynamic Aggression Adjustment** (Requirements 4.1, 4.2, 4.3)
   - **Low Player HP**: Increases aggression by 0.3 when player HP < 30%
   - **Player Buffs**: Increases caution by 0.15 per buff
   - **Player Engaged**: Increases opportunism by 0.25 when player fights other monsters

3. **Behavior Reinforcement** (Requirement 4.4)
   - Tracks successful defensive play (maintaining HP while cautious)
   - Applies positive rewards for survival through defensive tactics
   - Gradually increases base caution when defensive play succeeds
   - Creates learning effect over monster lifetime

## Usage

### Basic Setup

1. Add `AdaptiveAggressionSystem` component to monster prefab
2. Configure personality settings in inspector:
   - Enable/disable randomization on spawn
   - Set randomization range (default: 0.3-0.7)
   - Adjust dynamic modifier strengths

### Accessing Personality Data

```csharp
AdaptiveAggressionSystem aggression = GetComponent<AdaptiveAggressionSystem>();

// Get current effective personality (base + dynamic modifiers)
PersonalityTraits current = aggression.CurrentPersonality;

// Get individual trait levels
float aggressionLevel = aggression.GetAggressionLevel();
float cautionLevel = aggression.GetCautionLevel();
float teamworkLevel = aggression.GetTeamworkLevel();
float opportunismLevel = aggression.GetOpportunismLevel();

// Get behavior statistics
int defensiveSuccesses = aggression.GetSuccessfulDefensiveActions();
float survivalTime = aggression.GetSurvivalTime();
```

### Setting Custom Personality

```csharp
// Create custom personality
PersonalityTraits custom = new PersonalityTraits(
    aggression: 0.7f,
    caution: 0.4f,
    teamwork: 0.6f,
    opportunism: 0.8f
);

// Apply to monster
aggression.SetBasePersonality(custom);
```

### Listening to Personality Changes

```csharp
void Start()
{
    AdaptiveAggressionSystem aggression = GetComponent<AdaptiveAggressionSystem>();
    aggression.OnPersonalityChanged += HandlePersonalityChanged;
}

void HandlePersonalityChanged(PersonalityTraits newPersonality)
{
    Debug.Log($"Personality changed: {newPersonality}");
    // Update AI behavior based on new personality
}
```

## Integration with AI Brain

The adaptive aggression system is designed to work with the existing AI brain system:

1. **Decision Making**: AI brains can query personality traits to influence action selection
2. **Reward Signals**: Successful defensive play generates reward signals
3. **Behavior Tracking**: System tracks behavior success rates for reinforcement learning

Example integration in a brain:

```csharp
public EnemyAction Decide(in SituationState state, EnemyWorkingMemory memory)
{
    AdaptiveAggressionSystem aggression = GetComponent<AdaptiveAggressionSystem>();
    
    if (aggression != null)
    {
        float aggressionLevel = aggression.GetAggressionLevel();
        float cautionLevel = aggression.GetCautionLevel();
        
        // Use personality to influence decisions
        if (aggressionLevel > 0.7f && state.attackOpportunity > 0.5f)
        {
            return new EnemyAction { type = EnemyActionType.Chase, attemptAttack = true };
        }
        else if (cautionLevel > 0.7f && state.enemyHpRatio < 0.4f)
        {
            return new EnemyAction { type = EnemyActionType.Retreat };
        }
    }
    
    // Default behavior...
}
```

## Configuration Parameters

### Personality Configuration
- **Base Personality**: Default personality if not randomized
- **Randomize On Spawn**: Whether to randomize traits on spawn
- **Randomization Min/Max**: Range for random trait values (0.3-0.7 recommended)

### Dynamic Adjustment Settings
- **Low Player HP Threshold**: HP ratio to trigger aggression increase (default: 0.3)
- **Aggression Increase On Low HP**: Modifier applied when player HP is low (default: 0.3)
- **Caution Increase Per Buff**: Modifier per player buff (default: 0.15)
- **Opportunism Increase When Engaged**: Modifier when player fights others (default: 0.25)

### Behavior Reinforcement
- **Defensive Success Reward**: Reward per second of successful defensive play (default: 0.2)
- **Caution Reinforcement Rate**: Rate at which caution increases from success (default: 0.05)
- **Reinforcement Decay Rate**: Rate at which modifiers decay (default: 0.02)

## Debug Visualization

Enable `Show Debug Info` in the inspector to see:
- Current personality trait values
- Number of successful defensive actions
- Real-time personality changes in console

When selected in editor, Gizmos display personality information above the monster.

## Requirements Validation

This system implements the following requirements:

- **Requirement 4.1**: Aggression increases when player HP < 30%
- **Requirement 4.2**: Caution increases with player buffs
- **Requirement 4.3**: Opportunism increases when player is engaged
- **Requirement 4.4**: Defensive behavior is reinforced through rewards
- **Requirement 4.5**: Personality traits persist across monster lifetime

## Property-Based Tests

The following properties should hold for this system:

- **Property 12**: Aggression level must be higher when player HP < 30% than when HP > 30%
- **Property 13**: Caution level must increase proportionally with player buff count
- **Property 14**: Personality trait values must remain consistent across all frames within monster lifetime (base traits)

## Performance Considerations

- Personality updates occur once per frame (Update)
- Dynamic modifiers are calculated based on current situation state
- Minimal memory overhead (4 floats for base traits + tracking variables)
- No allocations during runtime (struct-based design)

## Future Enhancements

Potential improvements for future iterations:

1. **Personality Evolution**: Allow traits to evolve based on long-term success patterns
2. **Social Learning**: Monsters learn from observing successful allies
3. **Mood System**: Short-term emotional states that affect behavior
4. **Personality Archetypes**: Pre-defined personality profiles for different monster types
5. **Visual Feedback**: Particle effects or color changes based on personality state
