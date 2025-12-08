# Obstacle Utilization System

## Overview

The Obstacle Utilization System enables monsters to tactically use the environment during combat. This includes seeking cover when injured, blocking line of sight, herding players into disadvantageous positions, and learning optimal paths through obstacles.

## Components

### ObstacleUtilizationSystem

The main component that handles all obstacle-related behaviors.

**Key Features:**
- **Obstacle Detection**: Detects up to 8 nearby obstacles within a configurable radius
- **Cover Seeking**: Identifies nearest cover positions and calculates movement vectors
- **Line of Sight**: Tracks whether obstacles block vision to the player
- **Player Herding**: Detects dead-end areas and calculates herding vectors
- **Obstruction Penalty**: Applies negative rewards when monster is stuck
- **Pathfinding Cache**: Learns and caches successful paths for reuse

**Configuration:**
```csharp
[Header("Obstacle Detection")]
obstacleDetectionRadius = 8f;      // How far to detect obstacles
maxObstacles = 8;                   // Maximum obstacles to track
obstacleLayerMask;                  // Which layers count as obstacles

[Header("Cover Seeking")]
coverSeekingHpThreshold = 0.4f;     // HP ratio to trigger cover seeking
coverDistanceThreshold = 2f;        // How close to get to cover
maintainLineOfSightWhenSeeking = true; // Keep player in sight while seeking cover

[Header("Line of Sight")]
lineOfSightCheckDistance = 15f;     // Max distance for LOS checks
prioritizeCoverAgainstRanged = true; // Prefer cover vs ranged attacks

[Header("Player Herding")]
herdingDetectionRadius = 12f;       // How far to look for dead ends
deadEndAngleThreshold = 120f;       // Angle threshold for dead end detection
herdingPushDistance = 3f;           // How far to push player

[Header("Obstruction Penalty")]
obstructionPenaltyAmount = -0.05f;  // Penalty per check interval
obstructionCheckInterval = 0.5f;    // How often to check obstruction

[Header("Pathfinding Optimization")]
maxCachedPaths = 10;                // Maximum cached paths
pathCacheValidityDuration = 5f;     // How long paths remain valid
enablePathLearning = true;          // Enable path caching
```

### ObstacleAwareBrain

An example brain implementation that uses the obstacle system.

**Behavior Priority:**
1. **Cover Seeking** (0.9) - Highest priority when HP < 40%
2. **Kiting** (0.8) - High priority during attack cooldown
3. **Line of Sight Blocking** (0.7) - Use obstacles as shields
4. **Optimal Distance** (0.7) - Maintain combat range
5. **Flanking** (0.6) - Attack from sides/rear
6. **Player Herding** (0.6) - Push player into corners
7. **Corner Cutting** (0.5) - Intercept player movement

## Usage

### Adding to a Monster

1. Add `ObstacleUtilizationSystem` component to monster prefab
2. Configure obstacle detection settings
3. Set layer mask to include obstacle layers
4. Optionally add `ObstacleAwareBrain` for automatic behavior

### Manual Integration

```csharp
// Get the system
ObstacleUtilizationSystem obstacleSystem = GetComponent<ObstacleUtilizationSystem>();

// Check if should seek cover
if (obstacleSystem.ShouldSeekCover())
{
    Vector2 coverDirection = obstacleSystem.GetCoverSeekingDirection();
    // Move toward cover
}

// Check if can herd player
if (obstacleSystem.CanHerdPlayer())
{
    Vector2 herdDirection = obstacleSystem.GetHerdingDirection();
    // Move to herding position
}

// Get shield position
if (obstacleSystem.IsPlayerLineOfSightBlocked())
{
    Vector2 shieldPos = obstacleSystem.GetShieldPosition();
    // Move to shield position
}

// Find optimal path
Vector2[] path = obstacleSystem.FindOptimalPath(currentPos, targetPos);
// Follow path waypoints
```

## Requirements Validation

### Requirement 5.1: Cover Seeking
- ✅ Detects nearby obstacles (up to 8 nearest)
- ✅ Identifies nearest cover position
- ✅ Moves toward cover when HP < 40%
- ✅ Maintains line of sight to player when possible

### Requirement 5.2: Line of Sight Blocking
- ✅ Calculates if obstacles block line of sight to player
- ✅ Positions monsters to use obstacles as shields
- ✅ Prioritizes cover when player uses ranged attacks

### Requirement 5.3: Player Herding
- ✅ Detects dead-end areas in environment
- ✅ Positions monsters to restrict player movement options
- ✅ Pushes player toward tactical disadvantages

### Requirement 5.4: Pathfinding Optimization
- ✅ Learns efficient routes through repeated episodes
- ✅ Caches successful paths for reuse
- ✅ Adapts paths based on player movement patterns

### Requirement 5.5: Obstruction Penalty
- ✅ Detects when monster is obstructed
- ✅ Applies negative reward for remaining in blocked position
- ✅ Encourages movement to unobstructed positions

## Integration with Observation Space

The system automatically integrates with `EnemySituationEvaluator`:

```csharp
// In SituationState
public Vector2[] nearbyObstaclePositions;  // Up to 8 obstacles
public int obstacleCount;
public bool hasLineOfSight;
public Vector2 nearestCoverPosition;
```

The evaluator checks for `ObstacleUtilizationSystem` and uses it if available, otherwise falls back to basic detection.

## Debug Visualization

Enable `showDebugGizmos` to see:
- Yellow sphere: Obstacle detection radius
- Red cubes: Detected obstacles
- Green sphere + line: Nearest cover position
- Green/Red line: Line of sight to player (green = clear, red = blocked)
- Magenta spheres + rays: Detected dead ends for herding

## Performance Considerations

- Obstacle detection runs every frame
- Line of sight checks use raycasts (optimized)
- Path caching reduces repeated pathfinding calculations
- Obstruction penalty checks run at configurable intervals (default 0.5s)
- Dead end detection only runs when obstacles are present

## Future Enhancements

- More sophisticated pathfinding (A* algorithm)
- Dynamic obstacle avoidance
- Cooperative herding with multiple monsters
- Predictive cover selection based on player weapon type
- Learning which obstacles provide best cover
