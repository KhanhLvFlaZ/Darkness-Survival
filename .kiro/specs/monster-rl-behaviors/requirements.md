# Requirements Document

## Introduction

This document specifies the requirements for implementing advanced Reinforcement Learning (RL) behaviors for monsters in Darkness Survival. The system will use ML-Agents to train monsters with tactical combat behaviors, cooperative strategies, and visual feedback mechanisms that make the learning process observable to players. The goal is to create challenging, adaptive AI opponents that demonstrate intelligent decision-making through learned behaviors rather than scripted patterns.

## Glossary

- **Monster**: An enemy entity in the game that can engage in combat with the player
- **ML-Agents**: Unity's machine learning framework for training intelligent agents using reinforcement learning
- **RL (Reinforcement Learning)**: A machine learning paradigm where agents learn optimal behaviors through trial and error by receiving rewards and penalties
- **Tactical Positioning**: Strategic movement behaviors such as kiting, flanking, and maintaining optimal combat distance
- **Kiting**: A combat tactic where a monster attacks then retreats to avoid counterattack
- **Flanking**: Moving to attack from the side or rear rather than directly approaching
- **Cooperative Behavior**: Coordinated actions between multiple monsters to achieve tactical advantages
- **Ranged Monster**: A monster type (e.g., Cult_Mage_Toxin) that attacks from a distance using projectiles
- **Melee Monster**: A monster type that engages in close-range combat
- **Attack Timing**: The decision of when to initiate an attack based on situational factors
- **Visual Indicator**: UI or graphical elements that communicate AI state to the player
- **Episode**: A complete training session from monster spawn to death or timeout
- **Observation Space**: The set of environmental data the AI agent perceives
- **Action Space**: The set of possible actions the AI agent can perform
- **Reward Signal**: Numerical feedback given to the agent to reinforce desired behaviors
- **Policy Network**: The neural network that maps observations to actions
- **Heuristic Fallback**: Rule-based AI behavior used when ML policy is unavailable
- **AI Tier**: A classification level indicating the sophistication of monster AI behavior

## Requirements

### Requirement 1

**User Story:** As a player, I want monsters to use tactical positioning during combat, so that encounters feel challenging and require strategic thinking rather than simple dodging.

#### Acceptance Criteria

1. WHEN a monster is within combat range THEN the system SHALL select from tactical movement behaviors including kiting, flanking, spacing, and corner-cutting based on the current situation
2. WHEN a monster performs a kiting maneuver THEN the system SHALL cause the monster to attack and immediately retreat outside the player's effective counterattack range
3. WHEN multiple approach paths exist THEN the system SHALL enable monsters to choose flanking routes that approach from the player's sides or rear rather than direct frontal approaches
4. WHEN a monster is at close range THEN the system SHALL maintain an optimal combat distance that balances attack opportunity against vulnerability to player attacks
5. WHEN the player moves around obstacles THEN the system SHALL enable monsters to learn and execute shortcut paths that intercept the player more efficiently

### Requirement 2

**User Story:** As a player, I want monsters to attack at opportune moments rather than constantly, so that combat feels more realistic and rewards my defensive play.

#### Acceptance Criteria

1. WHEN the player is executing an attack animation THEN the system SHALL increase the monster's attack opportunity score to encourage attacks during player vulnerability windows
2. WHEN multiple monsters are near the player THEN the system SHALL coordinate attack timing to create overlapping threat windows
3. WHEN the player has active defensive buffs or shields THEN the system SHALL reduce the monster's attack opportunity score to discourage ineffective attacks
4. WHEN a monster successfully baits the player into overextending THEN the system SHALL reward the monster for the tactical deception
5. WHEN a monster's attack cooldown is active THEN the system SHALL prevent attack attempts and encourage repositioning behaviors

### Requirement 3

**User Story:** As a player, I want monsters to work together tactically, so that fighting groups feels meaningfully different from fighting individuals.

#### Acceptance Criteria

1. WHEN two or more monsters engage the same player THEN the system SHALL enable coordinated pincer attacks from multiple directions
2. WHEN monsters have different HP pools THEN the system SHALL enable tank-and-spank tactics where high-HP monsters block while low-HP monsters attack from range
3. WHEN a monster is pursuing the player THEN the system SHALL enable relay chase behavior where monsters take turns applying pressure
4. WHEN a low-HP monster can create an opening THEN the system SHALL enable sacrifice plays where the monster accepts damage to benefit allies
5. WHEN monsters are positioned near each other THEN the system SHALL provide observation data about ally positions and states to enable coordination

### Requirement 4

**User Story:** As a player, I want monsters to adapt their aggression based on the situation, so that each encounter feels dynamic and unpredictable.

#### Acceptance Criteria

1. WHEN the player's HP falls below 30% THEN the system SHALL increase monster aggression levels to apply pressure
2. WHEN the player has multiple active buffs THEN the system SHALL increase monster caution levels to encourage defensive play
3. WHEN the player is engaged with other monsters THEN the system SHALL increase opportunistic behavior scores
4. WHEN a monster successfully survives through defensive play THEN the system SHALL reinforce cautious behavior patterns through positive rewards
5. WHEN a monster's learned personality traits change THEN the system SHALL persist these traits across the monster's lifetime

### Requirement 5

**User Story:** As a player, I want monsters to use the environment tactically, so that the arena layout matters and creates interesting combat scenarios.

#### Acceptance Criteria

1. WHEN a monster's HP is below 40% THEN the system SHALL enable the monster to seek cover behind obstacles
2. WHEN the player uses ranged attacks THEN the system SHALL enable monsters to position obstacles between themselves and the player
3. WHEN the environment contains dead-end areas THEN the system SHALL enable monsters to herd the player toward these tactical disadvantages
4. WHEN multiple paths exist around obstacles THEN the system SHALL enable monsters to learn optimal navigation routes through repeated episodes
5. WHEN a monster is obstructed THEN the system SHALL apply a penalty to discourage remaining in blocked positions

### Requirement 6

**User Story:** As a player fighting ranged monsters, I want them to maintain distance intelligently, so that closing the gap requires skill and creates tactical gameplay.

#### Acceptance Criteria

1. WHEN a ranged monster type is within its configured minimum safe distance of the player THEN the system SHALL cause the monster to retreat while maintaining line of sight for attacks
2. WHEN a ranged monster is retreating THEN the system SHALL enable the monster to continue firing projectiles at the player
3. WHEN a ranged monster's retreat path is blocked THEN the system SHALL cause the monster to strafe perpendicular to the player's approach vector
4. WHEN a ranged monster reaches its configured maximum engagement distance THEN the system SHALL cause the monster to stop retreating and maintain position
5. WHEN the player changes direction during pursuit THEN the system SHALL enable ranged monsters to predict and adjust their retreat vector accordingly

### Requirement 7

**User Story:** As a player, I want ranged monsters to aim predictively, so that their attacks feel skillful and dodging requires timing rather than just movement.

#### Acceptance Criteria

1. WHEN a ranged monster fires a projectile THEN the system SHALL calculate a lead vector based on the player's current velocity
2. WHEN the player maintains a consistent movement pattern THEN the system SHALL enable monsters to learn and exploit the pattern through repeated observations
3. WHEN a ranged monster is strafing THEN the system SHALL enable simultaneous movement and accurate projectile firing
4. WHEN a ranged monster predicts player movement incorrectly THEN the system SHALL apply a small penalty to encourage accuracy improvement
5. WHEN a ranged monster's projectile hits the player THEN the system SHALL apply a reward proportional to the difficulty of the shot

### Requirement 8

**User Story:** As a player, I want to see visual feedback when monsters are using learned behaviors, so that I can recognize and appreciate the AI's intelligence.

#### Acceptance Criteria

1. WHEN a monster makes a tactical decision THEN the system SHALL display a visual indicator such as a brain icon or glow effect for 0.5 to 1.0 seconds
2. WHEN a monster executes a particularly intelligent maneuver THEN the system SHALL trigger a distinct particle effect to highlight the behavior
3. WHEN a monster's AI tier increases through learning THEN the system SHALL display a level-up effect and update the monster's visual appearance
4. WHEN debug mode is enabled THEN the system SHALL display text labels showing the current action type and cumulative reward above each monster
5. WHEN a monster transitions between behavior states THEN the system SHALL smoothly blend visual indicators to avoid jarring transitions

### Requirement 9

**User Story:** As a developer, I want monsters to have different AI sophistication tiers, so that I can control difficulty progression and provide appropriate challenges at different game stages.

#### Acceptance Criteria

1. WHEN a monster is spawned THEN the system SHALL assign an AI tier from the set {Novice, Learning, Trained, Expert}
2. WHEN a monster has Novice tier THEN the system SHALL use only heuristic-based decision making without ML policy
3. WHEN a monster has Learning tier THEN the system SHALL blend heuristic and ML policy decisions with exploration noise
4. WHEN a monster has Trained tier THEN the system SHALL primarily use ML policy with minimal exploration
5. WHEN a monster has Expert tier THEN the system SHALL use ML policy exclusively and enable advanced prediction features

### Requirement 10

**User Story:** As a developer, I want comprehensive metrics tracking for monster AI, so that I can evaluate learning progress and tune reward functions effectively.

#### Acceptance Criteria

1. WHEN a training episode completes THEN the system SHALL record average reward per episode, survival time, damage efficiency, positioning score, attack accuracy, and cooperation score
2. WHEN a monster deals damage to the player THEN the system SHALL update the damage efficiency metric as the ratio of damage dealt to damage taken
3. WHEN a monster maintains optimal positioning THEN the system SHALL increment the positioning score based on time spent in ideal combat ranges
4. WHEN a monster's attack connects with the player THEN the system SHALL update the attack accuracy metric
5. WHEN multiple monsters coordinate successfully THEN the system SHALL update the cooperation score for all participating monsters

### Requirement 11

**User Story:** As a developer, I want to train monster AI using ML-Agents, so that behaviors emerge from learning rather than manual scripting.

#### Acceptance Criteria

1. WHEN the ML-Agents training environment is initialized THEN the system SHALL configure observation space with at minimum 32 continuous values representing game state
2. WHEN the ML-Agents training environment is initialized THEN the system SHALL configure action space with discrete action selection and continuous movement vectors
3. WHEN a monster takes an action THEN the system SHALL provide reward signals based on damage dealt, damage taken, positioning quality, cooperation success, and survival time
4. WHEN a training episode ends THEN the system SHALL reset the environment and provide episode summary statistics to the policy network
5. WHEN a trained policy model is exported THEN the system SHALL enable runtime inference using Unity Barracuda for in-game AI execution

### Requirement 12

**User Story:** As a developer, I want the AI system to gracefully handle missing or invalid ML models, so that the game remains playable during development and if models fail to load.

#### Acceptance Criteria

1. WHEN an ML policy model is not assigned to a monster THEN the system SHALL automatically fall back to heuristic-based behavior
2. WHEN an ML policy model fails to load at runtime THEN the system SHALL log an error and switch to heuristic fallback without crashing
3. WHEN the ML policy produces invalid actions THEN the system SHALL clamp or sanitize the actions to valid ranges before execution
4. WHEN switching between ML and heuristic modes THEN the system SHALL maintain behavioral continuity without sudden movement discontinuities
5. WHEN a monster uses heuristic fallback THEN the system SHALL still record observations for potential offline training

### Requirement 13

**User Story:** As a developer, I want to configure reward functions per monster type, so that different monsters can learn specialized behaviors appropriate to their combat roles.

#### Acceptance Criteria

1. WHEN a monster type is defined THEN the system SHALL allow assignment of a RewardConfig asset specifying reward weights for different behaviors
2. WHEN a melee monster deals damage THEN the system SHALL apply a higher damage dealt reward weight than for ranged monsters
3. WHEN a ranged monster maintains optimal distance THEN the system SHALL apply a higher positioning reward weight than for melee monsters
4. WHEN a tank monster absorbs damage THEN the system SHALL apply reduced damage taken penalties compared to fragile monsters
5. WHEN reward configurations are modified THEN the system SHALL allow hot-reloading during training without restarting the environment

### Requirement 14

**User Story:** As a developer, I want to observe monster decision-making in real-time, so that I can debug issues and understand learned behaviors.

#### Acceptance Criteria

1. WHEN debug visualization is enabled THEN the system SHALL render gizmos showing movement vectors, attack ranges, and optimal positioning zones
2. WHEN a monster evaluates its situation THEN the system SHALL display the current observation values in an inspector-friendly format
3. WHEN a monster selects an action THEN the system SHALL log the action type, movement direction, and decision confidence to the console
4. WHEN a reward is applied THEN the system SHALL display the reward value and reason as a floating text above the monster
5. WHEN multiple monsters are coordinating THEN the system SHALL draw lines connecting cooperating monsters with color-coded coordination states
