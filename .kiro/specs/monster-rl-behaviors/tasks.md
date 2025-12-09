# Implementation Plan

- [x] 1. Extend core AI infrastructure with new action types and enhanced observations





  - Create enhanced action type enum with tactical behaviors (Kite, Flank, Ambush, SeekCover, HerdPlayer, CoordinatedAttack)
  - Extend SituationState struct with player state fields (isAttacking, isVulnerable, buffStrength, velocity)
  - Add ally information arrays to observation space (positions, HP ratios, attack states)
  - Add environment data to observations (obstacle positions, line of sight, cover positions)
  - Add tactical score fields (flanking opportunity, kiting feasibility, cooperation potential)
  - _Requirements: 1.1, 3.5, 5.1, 5.2_

- [ ]* 1.1 Write property test for valid action selection
  - **Property 1: Valid action selection**
  - **Validates: Requirements 1.1**

- [x] 2. Implement tactical positioning behaviors





  - [x] 2.1 Implement kiting behavior logic


    - Calculate attack-then-retreat movement vectors
    - Ensure distance increases beyond counterattack range
    - Integrate with existing attack cooldown system
    - _Requirements: 1.2_

  - [ ]* 2.2 Write property test for kiting distance increase
    - **Property 2: Kiting distance increase**
    - **Validates: Requirements 1.2**


  - [x] 2.3 Implement flanking behavior logic


    - Calculate approach angles relative to player facing
    - Select paths that approach from sides/rear (>45 degrees)
    - Integrate with pathfinding system
    - _Requirements: 1.3_

  - [ ]* 2.4 Write property test for flanking angle constraint
    - **Property 3: Flanking angle constraint**
    - **Validates: Requirements 1.3**


  - [x] 2.5 Implement optimal distance maintenance


    - Define optimal distance ranges per monster type
    - Create distance-keeping movement logic
    - Balance approach and retreat based on current distance
    - _Requirements: 1.4_

  - [ ]* 2.6 Write property test for optimal distance maintenance
    - **Property 4: Optimal distance maintenance**
    - **Validates: Requirements 1.4**

  - [x] 2.7 Implement corner-cutting pathfinding

    - Detect when player moves around obstacles
    - Calculate shortcut paths that intercept player
    - Integrate with existing navigation system
    - _Requirements: 1.5_

- [x] 3. Implement attack timing optimization system




  - [x] 3.1 Create attack opportunity scoring system


    - Calculate base opportunity score from distance and cooldown
    - Increase score when player is attacking (vulnerable)
    - Decrease score when player has buffs/shields
    - Integrate score into attack decision logic
    - _Requirements: 2.1, 2.3_

  - [ ]* 3.2 Write property test for attack opportunity on vulnerability
    - **Property 5: Attack opportunity increase on player vulnerability**
    - **Validates: Requirements 2.1**

  - [ ]* 3.3 Write property test for attack opportunity decrease with buffs
    - **Property 7: Attack opportunity decrease with buffs**
    - **Validates: Requirements 2.3**

  - [x] 3.4 Implement coordinated attack timing


    - Detect when multiple monsters are near same player
    - Stagger attack timings (minimum 0.3s between attacks)
    - Use shared timing coordinator or communication system
    - _Requirements: 2.2_

  - [ ]* 3.5 Write property test for coordinated attack timing
    - **Property 6: Coordinated attack timing**
    - **Validates: Requirements 2.2**

  - [x] 3.6 Implement bait-and-punish behavior


    - Detect when player overextends
    - Reward monsters for successful baiting
    - Integrate with reward calculation system
    - _Requirements: 2.4_

  - [x] 3.7 Enforce attack prevention during cooldown


    - Block attack attempts when cooldown is active
    - Prefer repositioning actions during cooldown
    - _Requirements: 2.5_

  - [ ]* 3.8 Write property test for attack prevention during cooldown
    - **Property 8: Attack prevention during cooldown**
    - **Validates: Requirements 2.5**


- [x] 4. Implement cooperative behavior system





  - [x] 4.1 Create ally detection and tracking system


    - Detect nearby ally monsters (up to 5 nearest)
    - Track ally positions, HP ratios, and attack states
    - Update ally data in observation space each frame
    - _Requirements: 3.5_

  - [ ]* 4.2 Write test for ally observation data completeness
    - **Property 11: Ally observation data completeness**
    - **Validates: Requirements 3.5**

  - [x] 4.3 Implement pincer attack coordination


    - Calculate approach vectors for multiple monsters
    - Ensure vectors diverge by at least 60 degrees
    - Coordinate timing for simultaneous pressure
    - _Requirements: 3.1_

  - [ ]* 4.4 Write property test for pincer attack divergence
    - **Property 9: Pincer attack divergence**
    - **Validates: Requirements 3.1**

  - [x] 4.5 Implement tank-and-spank role assignment

    - Assign roles based on HP pools (high HP = tank, low HP = damage)
    - Position high-HP monsters closer to player
    - Position low-HP monsters at range
    - _Requirements: 3.2_

  - [ ]* 4.6 Write property test for tank positioning priority
    - **Property 10: Tank positioning priority**
    - **Validates: Requirements 3.2**

  - [x] 4.7 Implement relay chase behavior

    - Track which monster is actively pursuing
    - Alternate pursuit between monsters over time
    - Prevent all monsters from chasing simultaneously
    - _Requirements: 3.3_

  - [x] 4.8 Implement sacrifice play logic

    - Detect when low-HP monster can create opening
    - Allow aggressive actions even when retreat is safer
    - Reward successful sacrifice plays
    - _Requirements: 3.4_

- [x] 5. Implement adaptive aggression system




  - [x] 5.1 Create personality trait system


    - Define trait fields (aggression, caution, teamwork levels)
    - Initialize traits on monster spawn
    - Persist traits across monster lifetime
    - _Requirements: 4.5_

  - [ ]* 5.2 Write property test for personality trait persistence
    - **Property 14: Personality trait persistence**
    - **Validates: Requirements 4.5**


  - [x] 5.3 Implement dynamic aggression adjustment

    - Increase aggression when player HP < 30%
    - Increase caution when player has multiple buffs
    - Increase opportunism when player is engaged with others
    - Update aggression levels each frame based on conditions
    - _Requirements: 4.1, 4.2, 4.3_

  - [ ]* 5.4 Write property test for aggression increase on low player HP
    - **Property 12: Aggression increase on low player HP**
    - **Validates: Requirements 4.1**

  - [ ]* 5.5 Write property test for caution increase with buffs
    - **Property 13: Caution increase with player buffs**
    - **Validates: Requirements 4.2**


  - [x] 5.6 Implement behavior reinforcement through rewards


    - Apply positive rewards for successful defensive play
    - Reinforce cautious patterns when they lead to survival
    - Track behavior success rates over time
    - _Requirements: 4.4_

- [x] 6. Implement obstacle utilization system





  - [x] 6.1 Create obstacle detection system


    - Detect nearby obstacles (up to 8 nearest)
    - Calculate obstacle positions and sizes
    - Update obstacle data in observation space
    - _Requirements: 5.1, 5.2_

  - [x] 6.2 Implement cover-seeking behavior

    - Identify nearest cover position
    - Move toward cover when HP < 40%
    - Maintain line of sight to player when possible
    - _Requirements: 5.1_

  - [ ]* 6.3 Write property test for cover seeking on low HP
    - **Property 15: Cover seeking on low HP**
    - **Validates: Requirements 5.1**

  - [x] 6.4 Implement line-of-sight blocking

    - Calculate if obstacles block line of sight to player
    - Position monsters to use obstacles as shields
    - Prioritize cover when player uses ranged attacks
    - _Requirements: 5.2_

  - [x] 6.5 Implement player herding behavior

    - Detect dead-end areas in environment
    - Position monsters to restrict player movement options
    - Push player toward tactical disadvantages
    - _Requirements: 5.3_

  - [x] 6.6 Implement obstruction penalty

    - Detect when monster is obstructed
    - Apply negative reward for remaining in blocked position
    - Encourage movement to unobstructed positions
    - _Requirements: 5.5_

  - [ ]* 6.7 Write property test for obstruction penalty
    - **Property 16: Obstruction penalty application**
    - **Validates: Requirements 5.5**

  - [x] 6.8 Optimize pathfinding for obstacle navigation

    - Learn efficient routes through repeated episodes
    - Cache successful paths for reuse
    - Adapt paths based on player movement patterns
    - _Requirements: 5.4_


- [x] 7. Implement ranged combat behavior system





  - [x] 7.1 Create RangedCombatBehavior component


    - Define distance management parameters (min safe, max engagement, optimal)
    - Define predictive aiming parameters (prediction strength, accuracy variance)
    - Attach component to ranged monster prefabs (Cult_Mage_Toxin, etc.)
    - _Requirements: 6.1, 6.4_



  - [x] 7.2 Implement distance-based retreat logic
    - Check if current distance < min safe distance
    - Calculate retreat vector away from player
    - Maintain line of sight during retreat
    - Stop retreating at max engagement distance
    - _Requirements: 6.1, 6.4_

  - [ ]* 7.3 Write property test for ranged retreat activation
    - **Property 17: Ranged retreat activation**
    - **Validates: Requirements 6.1**

  - [ ]* 7.4 Write property test for retreat cessation at max distance
    - **Property 20: Retreat cessation at max distance**


    - **Validates: Requirements 6.4**

  - [x] 7.5 Implement simultaneous retreat and attack
    - Enable movement and attack actions to occur together
    - Maintain attack capability while retreating
    - Update Monsters.cs to support concurrent actions
    - _Requirements: 6.2_


  - [ ]* 7.6 Write property test for simultaneous retreat and attack
    - **Property 18: Simultaneous retreat and attack**
    - **Validates: Requirements 6.2**

  - [x] 7.7 Implement blocked retreat handling
    - Detect when retreat path is obstructed
    - Calculate perpendicular strafe direction
    - Move perpendicular to player approach vector
    - _Requirements: 6.3_



  - [ ]* 7.8 Write property test for perpendicular strafe on blocked retreat
    - **Property 19: Perpendicular strafe on blocked retreat**
    - **Validates: Requirements 6.3**



  - [x] 7.9 Implement adaptive retreat vector
    - Monitor player direction changes
    - Adjust retreat vector in response to player movement
    - Predict player interception attempts
    - _Requirements: 6.5_

  - [x] 7.10 Implement predictive aiming system
    - Calculate player velocity vector

    - Compute lead vector based on projectile speed
    - Apply prediction strength multiplier
    - Add accuracy variance for realism
    - _Requirements: 7.1_

  - [ ]* 7.11 Write property test for predictive aim calculation
    - **Property 21: Predictive aim calculation**
    - **Validates: Requirements 7.1**


  - [x] 7.12 Implement strafe-and-shoot behavior
    - Enable lateral movement while firing
    - Maintain accuracy during strafing
    - Coordinate strafe direction with retreat logic
    - _Requirements: 7.3_


  - [ ]* 7.13 Write property test for simultaneous strafe and fire
    - **Property 22: Simultaneous strafe and fire**
    - **Validates: Requirements 7.3**

  - [x] 7.14 Implement aim accuracy rewards
    - Apply small penalty for missed shots
    - Apply reward for hits, scaled by shot difficulty
    - Track prediction accuracy over time
    - _Requirements: 7.4, 7.5_

  - [x] 7.15 Implement pattern learning for player movement
    - Record player movement patterns in working memory
    - Detect consistent patterns over time
    - Exploit patterns for improved prediction
    - _Requirements: 7.2_

- [x] 8. Implement visual feedback system





  - [x] 8.1 Create AIVisualFeedback component


    - Define indicator prefab references (brain icon, level-up effect)
    - Define particle system references (tactical decision effect)
    - Define color schemes for different AI tiers
    - Add debug visualization flags
    - _Requirements: 8.1, 8.2, 8.3_

  - [x] 8.2 Implement tactical decision indicators


    - Show brain icon or glow when tactical decision is made
    - Display for 0.5-1.0 seconds
    - Vary effect based on action type
    - _Requirements: 8.1_

  - [ ]* 8.3 Write property test for visual indicator duration
    - **Property 23: Visual indicator duration**
    - **Validates: Requirements 8.1**

  - [x] 8.4 Implement intelligent maneuver effects


    - Trigger distinct particle effects for flanking, kiting, etc.
    - Scale effect intensity based on maneuver success
    - Ensure effects are visible against various backgrounds
    - _Requirements: 8.2_

  - [x] 8.5 Implement AI tier visual updates


    - Display level-up effect when tier increases
    - Update glow color based on current tier (Novice=blue, Expert=gold)
    - Smoothly transition between tier appearances
    - _Requirements: 8.3_

  - [x] 8.6 Implement debug visualization


    - Display action type and reward labels above monsters
    - Render movement vector gizmos
    - Draw attack range and optimal positioning zones
    - Show coordination lines between cooperating monsters
    - Enable/disable via debug flag
    - _Requirements: 8.4, 14.1, 14.5_

  - [x] 8.7 Implement smooth visual transitions


    - Use interpolation for color changes
    - Fade effects in/out rather than instant on/off
    - Blend between different indicator states
    - _Requirements: 8.5_

  - [ ]* 8.8 Write property test for smooth visual transitions
    - **Property 24: Smooth visual transitions**
    - **Validates: Requirements 8.5**


- [x] 9. Implement AI tier system





  - [x] 9.1 Create AITier enum and AITierManager component


    - Define tier levels (Novice, Learning, Trained, Expert)
    - Create manager component to control tier behavior
    - Add tier assignment logic on monster spawn
    - _Requirements: 9.1_

  - [ ]* 9.2 Write property test for valid AI tier assignment
    - **Property 25: Valid AI tier assignment**
    - **Validates: Requirements 9.1**

  - [x] 9.3 Implement tier-based decision backend selection


    - Novice: Heuristic only
    - Learning: Blend ML and heuristic with exploration
    - Trained: Primarily ML with minimal exploration
    - Expert: ML only with advanced features
    - _Requirements: 9.2, 9.3, 9.4, 9.5_

  - [ ]* 9.4 Write property test for Novice tier heuristic usage
    - **Property 26: Novice tier uses heuristic only**
    - **Validates: Requirements 9.2**

  - [ ]* 9.5 Write property test for Expert tier ML usage
    - **Property 27: Expert tier uses ML only**
    - **Validates: Requirements 9.5**

  - [x] 9.6 Implement exploration rate management


    - Calculate exploration rate based on tier
    - Apply exploration noise to actions (Learning tier)
    - Reduce exploration over time (Trained tier)
    - Disable exploration for Expert tier
    - _Requirements: 9.3, 9.4_

  - [x] 9.7 Implement policy blending


    - Calculate blend weight based on tier
    - Interpolate between heuristic and ML decisions
    - Ensure smooth transitions between decision sources
    - _Requirements: 9.3_

- [x] 10. Implement metrics tracking system





  - [x] 10.1 Create LearningMetrics struct and MetricsTracker component


    - Define all metric fields (reward, survival, efficiency, etc.)
    - Create tracker component to record metrics
    - Initialize metrics on monster spawn
    - _Requirements: 10.1_

  - [x] 10.2 Implement damage efficiency tracking


    - Record damage dealt and damage taken
    - Calculate efficiency as dealt/taken ratio
    - Update metric on each damage event
    - _Requirements: 10.2_

  - [ ]* 10.3 Write property test for damage efficiency calculation
    - **Property 28: Damage efficiency calculation**
    - **Validates: Requirements 10.2**

  - [x] 10.4 Implement positioning score tracking


    - Check if monster is in optimal range each frame
    - Increment score based on time in optimal range
    - Normalize score by total time alive
    - _Requirements: 10.3_

  - [ ]* 10.5 Write property test for positioning score increment
    - **Property 29: Positioning score increment**
    - **Validates: Requirements 10.3**

  - [x] 10.6 Implement attack accuracy tracking


    - Record attack attempts and hits
    - Calculate accuracy as hits/attempts ratio
    - Update metric on each attack
    - _Requirements: 10.4_

  - [x] 10.7 Implement cooperation score tracking


    - Detect successful coordinated actions
    - Update score for all participating monsters
    - Track cooperation frequency and success rate
    - _Requirements: 10.5_

  - [ ]* 10.8 Write property test for cooperation score distribution
    - **Property 30: Cooperation score distribution**
    - **Validates: Requirements 10.5**

  - [x] 10.9 Implement episode summary recording


    - Aggregate all metrics at episode end
    - Calculate averages and derived statistics
    - Store summary for training analysis
    - _Requirements: 10.1_

  - [x] 10.10 Implement metrics export functionality

    - Serialize metrics to JSON or CSV format
    - Export to file for external analysis
    - Include timestamp and episode metadata
    - _Requirements: 10.1_

- [x] 11. Integrate ML-Agents training infrastructure





  - [x] 11.1 Install ML-Agents Unity package


    - Add ML-Agents package via Package Manager
    - Install Python ml-agents package for training
    - Verify installation and compatibility
    - _Requirements: 11.1_

  - [x] 11.2 Create ML-Agents training environment


    - Create training scene with multiple monsters and player
    - Configure environment parameters (max steps, time scale)
    - Set up episode reset logic
    - _Requirements: 11.4_

  - [x] 11.3 Configure observation space for ML-Agents

    - Define observation vector with minimum 32 continuous values
    - Implement CollectObservations method
    - Normalize all observation values to appropriate ranges
    - _Requirements: 11.1_

  - [x] 11.4 Configure action space for ML-Agents

    - Define discrete action branch for action type selection
    - Define continuous action branches for movement (x, y)
    - Define continuous action branch for attack attempt
    - _Requirements: 11.2_

  - [x] 11.5 Implement reward signal integration


    - Connect RewardCalculator to ML-Agents AddReward
    - Ensure rewards incorporate all factors (damage, positioning, cooperation, survival)
    - Clamp rewards to prevent instability
    - _Requirements: 11.3_

  - [ ]* 11.6 Write property test for multi-factor reward calculation
    - **Property 31: Multi-factor reward calculation**
    - **Validates: Requirements 11.3**

  - [x] 11.7 Implement episode reset and summary

    - Reset monster and player positions on episode end
    - Provide episode summary to policy network
    - Clear working memory and metrics
    - _Requirements: 11.4_

  - [ ]* 11.8 Write property test for episode reset and summary
    - **Property 32: Episode reset and summary**
    - **Validates: Requirements 11.4**

  - [x] 11.9 Configure ML-Agents training hyperparameters


    - Set learning rate, batch size, buffer size
    - Configure network architecture (hidden layers)
    - Set discount factor (gamma) and other RL parameters
    - Create training configuration YAML file
    - _Requirements: 11.5_

  - [x] 11.10 Implement policy model export and loading


    - Train initial policy using ML-Agents
    - Export trained model as .nn file
    - Implement runtime model loading with Barracuda
    - Verify inference produces valid actions
    - _Requirements: 11.5_


- [x] 12. Implement graceful fallback and error handling





  - [x] 12.1 Implement heuristic fallback on missing model


    - Check if ML model is assigned on initialization
    - Automatically switch to heuristic if model is null
    - Log warning message in development builds
    - _Requirements: 12.1_

  - [ ]* 12.2 Write property test for heuristic fallback on missing model
    - **Property 33: Heuristic fallback on missing model**
    - **Validates: Requirements 12.1**

  - [x] 12.3 Implement ML model loading error handling


    - Wrap model loading in try-catch block
    - Log detailed error message on failure
    - Switch to heuristic fallback without crashing
    - Set AI tier to Novice on failure
    - _Requirements: 12.2_

  - [x] 12.4 Implement action validation and sanitization


    - Check for NaN and Infinity in action outputs
    - Clamp continuous values to valid ranges [-1, 1]
    - Default discrete actions to Idle if invalid
    - Apply small penalty for invalid outputs
    - _Requirements: 12.3_

  - [ ]* 12.5 Write property test for action validation
    - **Property 34: Action validation and clamping**
    - **Validates: Requirements 12.3**

  - [x] 12.6 Implement smooth mode switching


    - Detect when switching between ML and heuristic
    - Interpolate velocity changes to avoid discontinuities
    - Limit velocity delta to 2.0 units/frame
    - _Requirements: 12.4_

  - [ ]* 12.7 Write property test for behavioral continuity
    - **Property 35: Behavioral continuity on mode switch**
    - **Validates: Requirements 12.4**

  - [x] 12.8 Implement observation recording continuity


    - Continue recording observations even in heuristic mode
    - Store observations in working memory for offline training
    - Ensure observation format matches ML requirements
    - _Requirements: 12.5_

  - [ ]* 12.9 Write property test for observation recording continuity
    - **Property 36: Observation recording continuity**
    - **Validates: Requirements 12.5**

  - [x] 12.10 Implement missing data handling


    - Use default/zero values for missing observations
    - Set validity flags in observation vector
    - Log warnings for critical missing data
    - Continue execution with partial information
    - _Requirements: 12.1, 12.2_

- [ ] 13. Implement configurable reward system
  - [ ] 13.1 Create EnhancedRewardConfig ScriptableObject
    - Define all reward weight fields (combat, positioning, tactical, cooperation, survival)
    - Create asset creation menu item
    - Implement constraint fields (max reward magnitude)
    - _Requirements: 13.1_

  - [ ] 13.2 Create reward configs for different monster types
    - Create melee monster reward config (high damage dealt weight)
    - Create ranged monster reward config (high positioning weight)
    - Create tank monster reward config (reduced damage taken penalty)
    - Assign configs to monster prefabs
    - _Requirements: 13.2, 13.3, 13.4_

  - [ ]* 13.3 Write property test for type-specific reward weights
    - **Property 37: Type-specific reward weights**
    - **Validates: Requirements 13.2**

  - [ ]* 13.4 Write property test for ranged positioning reward priority
    - **Property 38: Ranged positioning reward priority**
    - **Validates: Requirements 13.3**

  - [ ] 13.5 Integrate reward configs with RewardCalculator
    - Load reward config from monster on initialization
    - Apply config weights to all reward calculations
    - Support config changes during runtime
    - _Requirements: 13.1_

  - [ ] 13.6 Implement hot-reloading for reward configs
    - Detect when config asset is modified
    - Reload config values without restarting training
    - Apply new weights to ongoing episodes
    - _Requirements: 13.5_

- [ ] 14. Implement debug and visualization tools
  - [ ] 14.1 Implement gizmo rendering for AI state
    - Draw movement vectors as arrows
    - Draw attack ranges as circles
    - Draw optimal positioning zones as colored rings
    - Enable/disable via inspector flag
    - _Requirements: 14.1_

  - [ ] 14.2 Implement observation inspector display
    - Create custom inspector for EnemySituationEvaluator
    - Display all observation values in readable format
    - Update values in real-time during play mode
    - _Requirements: 14.2_

  - [ ] 14.3 Implement action logging
    - Log action type, movement direction, and confidence
    - Include monster ID and timestamp
    - Make logging optional via flag
    - _Requirements: 14.3_

  - [ ] 14.4 Implement reward visualization
    - Display reward value as floating text above monster
    - Show reward reason (e.g., "Flanking Bonus +0.3")
    - Color-code positive (green) and negative (red) rewards
    - _Requirements: 14.4_

  - [ ] 14.5 Implement coordination visualization
    - Draw lines between cooperating monsters
    - Color-code lines by coordination type (pincer=red, relay=blue)
    - Show coordination state labels
    - _Requirements: 14.5_

- [ ] 15. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 16. Integration and polish
  - [ ] 16.1 Integrate all systems with existing Monsters.cs
    - Update Monsters.cs to use enhanced action types
    - Connect visual feedback system to monster events
    - Integrate ranged combat behavior for applicable monsters
    - Ensure backward compatibility with existing monsters

  - [ ] 16.2 Create monster prefab variants for different AI tiers
    - Create Novice, Learning, Trained, Expert variants
    - Configure appropriate AI tier and visual indicators
    - Set up reward configs per variant
    - Test each variant in gameplay

  - [ ] 16.3 Balance and tune reward functions
    - Playtest with different reward configurations
    - Adjust weights based on observed behavior
    - Ensure rewards encourage desired tactics
    - Document final reward values

  - [ ] 16.4 Optimize performance
    - Profile observation collection and decision-making
    - Optimize ally and obstacle detection queries
    - Reduce unnecessary calculations in Update loops
    - Ensure 60 FPS with 20+ monsters active

  - [ ] 16.5 Create training documentation
    - Document ML-Agents setup process
    - Provide training command examples
    - Explain hyperparameter tuning
    - Include troubleshooting guide

  - [ ]* 16.6 Write integration tests for complete AI pipeline
    - Test observation → decision → action → reward flow
    - Test ML and heuristic modes
    - Test all tactical behaviors in realistic scenarios
    - Verify visual feedback triggers correctly

- [ ] 17. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
