# Ranged Combat Behavior Flow Diagram

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Monsters.cs                              │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                    FixedUpdate()                            │ │
│  │  • Calculate base direction to player                       │ │
│  │  • Check if RangedCombatBehavior exists                     │ │
│  │  • Apply distance-based movement logic                      │ │
│  └────────────────────┬───────────────────────────────────────┘ │
│                       │                                          │
│                       ▼                                          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              TryRangedAttack()                              │ │
│  │  • Check attack cooldown                                    │ │
│  │  • Verify within engagement distance                        │ │
│  │  • Call Attack() if conditions met                          │ │
│  └────────────────────┬───────────────────────────────────────┘ │
│                       │                                          │
│                       ▼                                          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │               FireProjectile()                              │ │
│  │  • Get predictive aim point from RangedCombatBehavior       │ │
│  │  • Calculate direction to predicted position                │ │
│  │  • Spawn projectile with lead vector                        │ │
│  │  • Start tracking coroutine for accuracy rewards            │ │
│  └────────────────────────────────────────────────────────────┘ │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ Uses
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                  RangedCombatBehavior.cs                         │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                    Update()                                 │ │
│  │  • UpdateAdaptiveRetreatVector()                            │ │
│  │  • UpdatePatternLearning()                                  │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │          Distance Management Methods                        │ │
│  │  • ShouldRetreat(distance)                                  │ │
│  │  • ShouldAdvance(distance)                                  │ │
│  │  • ShouldStopRetreating(distance)                           │ │
│  │  • CalculateRetreatVector(playerPos, currentPos)            │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │          Obstacle Handling Methods                          │ │
│  │  • CalculatePerpendicularStrafeDirection()                  │ │
│  │  • GetStrafeDirection(playerApproachVector)                 │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │          Predictive Aiming Methods                          │ │
│  │  • CalculatePredictiveAimPoint(pos, vel, speed)             │ │
│  │  • CalculateShotDifficulty(pos, vel)                        │ │
│  │  • RecordShotResult(hit, difficulty)                        │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │          Pattern Learning Methods                           │ │
│  │  • UpdatePatternLearning()                                  │ │
│  │  • AnalyzeMovementPattern()                                 │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │          Adaptive Retreat Methods                           │ │
│  │  • UpdateAdaptiveRetreatVector()                            │ │
│  │  • Monitor player direction changes                         │ │
│  │  • Predict interception attempts                            │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Decision Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Every FixedUpdate                             │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ Calculate Distance   │
              │ to Player            │
              └──────────┬───────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
    ┌─────────┐    ┌─────────┐    ┌─────────┐
    │Distance │    │Distance │    │Distance │
    │< Min    │    │Optimal  │    │> Max    │
    │Safe     │    │Range    │    │Engage   │
    └────┬────┘    └────┬────┘    └────┬────┘
         │              │              │
         ▼              ▼              ▼
    ┌─────────┐    ┌─────────┐    ┌─────────┐
    │RETREAT  │    │STRAFE   │    │ADVANCE  │
    │MODE     │    │MODE     │    │MODE     │
    └────┬────┘    └────┬────┘    └────┬────┘
         │              │              │
         ▼              │              │
    ┌─────────┐         │              │
    │Check    │         │              │
    │Path     │         │              │
    │Clear?   │         │              │
    └────┬────┘         │              │
         │              │              │
    ┌────┴────┐         │              │
    │         │         │              │
    ▼         ▼         │              │
┌────────┐ ┌────────┐  │              │
│Move    │ │Strafe  │  │              │
│Away    │ │Perp.   │  │              │
└────┬───┘ └────┬───┘  │              │
     │          │       │              │
     └──────────┴───────┴──────────────┘
                │
                ▼
     ┌──────────────────────┐
     │ Apply Movement       │
     │ rigidbody2d.velocity │
     └──────────────────────┘
```

## Attack Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Every Update                                  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ TryRangedAttack()    │
              └──────────┬───────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ Cooldown Ready?      │
              └──────────┬───────────┘
                         │
                    Yes  │  No
         ┌───────────────┴──────────┐
         │                          │
         ▼                          ▼
┌─────────────────┐        ┌────────────┐
│Within Engagement│        │Wait for    │
│Distance?        │        │Cooldown    │
└────────┬────────┘        └────────────┘
         │
    Yes  │  No
         │
         ▼
┌─────────────────────────────────────────┐
│         FireProjectile()                 │
├─────────────────────────────────────────┤
│ 1. Get Player Velocity                  │
│ 2. Calculate Predictive Aim Point       │
│    ├─ Time to Impact                    │
│    ├─ Lead Vector                       │
│    ├─ Pattern Learning Blend            │
│    └─ Accuracy Variance                 │
│ 3. Calculate Direction                  │
│ 4. Spawn Projectile                     │
│ 5. Start Tracking Coroutine             │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│    TrackProjectileResult()               │
├─────────────────────────────────────────┤
│ Wait for projectile to hit or timeout   │
│ Determine if hit or miss                │
│ Calculate shot difficulty                │
│ Apply reward/penalty                     │
└──────────────────────────────────────────┘
```

## Pattern Learning Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Every Update                                  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ Record Player        │
              │ Position in History  │
              └──────────┬───────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ History Full?        │
              └──────────┬───────────┘
                         │
                    Yes  │  No
         ┌───────────────┴──────────┐
         │                          │
         ▼                          ▼
┌─────────────────┐        ┌────────────┐
│Analyze Pattern  │        │Continue    │
│                 │        │Recording   │
└────────┬────────┘        └────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│    Calculate Average Velocity            │
│    ├─ Sum all velocity vectors           │
│    └─ Divide by sample count             │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│    Calculate Consistency                 │
│    ├─ Measure deviation from average     │
│    ├─ Normalize by velocity magnitude    │
│    └─ Convert to confidence (0-1)        │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│    Confidence > Threshold?               │
└────────────┬────────────────────────────┘
             │
        Yes  │  No
 ┌───────────┴──────────┐
 │                      │
 ▼                      ▼
┌──────────┐    ┌────────────┐
│Use       │    │Use Current │
│Pattern   │    │Velocity    │
│for Aim   │    │Only        │
└──────────┘    └────────────┘
```

## Adaptive Retreat Flow

```
┌─────────────────────────────────────────────────────────────────┐
│              UpdateAdaptiveRetreatVector()                       │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
              ┌──────────────────────┐
              │ Is Retreating?       │
              └──────────┬───────────┘
                         │
                    Yes  │  No
         ┌───────────────┴──────────┐
         │                          │
         ▼                          ▼
┌─────────────────┐        ┌────────────┐
│Track Player     │        │Skip Update │
│Velocity         │        └────────────┘
└────────┬────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│    Direction Changed > 30°?              │
└────────────┬────────────────────────────┘
             │
        Yes  │  No
 ┌───────────┴──────────┐
 │                      │
 ▼                      ▼
┌──────────────┐  ┌──────────┐
│Calculate Dot │  │Keep      │
│Product       │  │Current   │
└──────┬───────┘  │Vector    │
       │          └──────────┘
       ▼
┌──────────────────────────────────────────┐
│    Player Moving Toward Monster?         │
│    (Dot Product > 0.3)                   │
└────────────┬─────────────────────────────┘
             │
        Yes  │  No
 ┌───────────┴──────────┐
 │                      │
 ▼                      ▼
┌──────────────┐  ┌──────────┐
│Blend         │  │Keep      │
│Perpendicular │  │Current   │
│Movement      │  │Vector    │
│(70% away +   │  └──────────┘
│ 30% perp)    │
└──────────────┘
```

## State Transitions

```
                    ┌─────────────┐
                    │   ADVANCE   │
                    │  (Too Far)  │
                    └──────┬──────┘
                           │
                           │ Distance < Max
                           ▼
    ┌──────────────────────────────────────────┐
    │              OPTIMAL RANGE                │
    │         (Strafe or Maintain)              │
    └──────┬───────────────────────────────┬───┘
           │                               │
Distance   │                               │ Distance
< Min      │                               │ > Max
           ▼                               ▼
    ┌─────────────┐                 ┌─────────────┐
    │   RETREAT   │                 │   ADVANCE   │
    │  (Too Close)│                 │  (Too Far)  │
    └──────┬──────┘                 └─────────────┘
           │
           │ Path Blocked
           ▼
    ┌─────────────┐
    │   STRAFE    │
    │(Perpendicular)│
    └─────────────┘
```

## Integration Points

```
┌─────────────────────────────────────────────────────────────────┐
│                         Game Systems                             │
└────────────────────────┬────────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
┌─────────────┐  ┌──────────────┐  ┌──────────────┐
│ Monsters.cs │  │ Reward       │  │ Situation    │
│             │  │ Calculator   │  │ Evaluator    │
└──────┬──────┘  └──────┬───────┘  └──────┬───────┘
       │                │                  │
       │                │                  │
       └────────────────┼──────────────────┘
                        │
                        ▼
         ┌──────────────────────────┐
         │  RangedCombatBehavior    │
         │  • Distance Management   │
         │  • Predictive Aiming     │
         │  • Pattern Learning      │
         │  • Adaptive Retreat      │
         └──────────────────────────┘
```

## Event Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    RangedCombatBehavior Events                   │
└────────────────────────┬────────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
┌─────────────────┐ ┌─────────────┐ ┌─────────────────┐
│OnRetreatState   │ │OnPredictive │ │OnAccuracyReward │
│Changed          │ │AimCalculated│ │Applied          │
└─────────────────┘ └─────────────┘ └─────────────────┘
         │               │               │
         ▼               ▼               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Potential Subscribers                         │
│  • Visual Feedback System                                        │
│  • Debug Visualization                                           │
│  • Metrics Tracking                                              │
│  • AI Tier Manager                                               │
└─────────────────────────────────────────────────────────────────┘
```

## Key Algorithms

### Predictive Aim Calculation
```
Input: playerPosition, playerVelocity, projectileSpeed
Output: predictedAimPoint

1. Calculate distance to player
2. Calculate time to impact = distance / projectileSpeed
3. Calculate lead vector = playerVelocity * timeToImpact
4. Apply prediction strength = leadVector * predictionStrength
5. If pattern detected and confident:
   - Blend pattern: leadVector = lerp(leadVector, pattern * timeToImpact, confidence)
6. Add accuracy variance = random offset
7. Return playerPosition + leadVector
```

### Pattern Confidence Calculation
```
Input: playerPositionHistory[]
Output: patternConfidence (0-1)

1. Calculate average velocity from history
2. For each velocity in history:
   - Calculate deviation from average
3. Calculate average deviation
4. Normalize by average velocity magnitude
5. Convert to confidence: 1 - (avgDeviation / avgMagnitude)
6. Clamp to [0, 1]
```

### Adaptive Retreat Adjustment
```
Input: currentPlayerVelocity, lastPlayerVelocity
Output: adjustedRetreatVector

1. Calculate direction change angle
2. If angle > 30°:
   - Calculate player-to-monster vector
   - Calculate dot product with player velocity
   - If dot > 0.3 (player approaching):
     - Calculate perpendicular to player velocity
     - Blend: 70% away + 30% perpendicular
3. Return adjusted vector
```

This flow diagram provides a comprehensive visual understanding of how the ranged combat behavior system operates and integrates with the existing monster AI infrastructure.
