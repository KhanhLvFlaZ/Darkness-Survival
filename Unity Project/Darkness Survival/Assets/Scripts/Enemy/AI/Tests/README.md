# Monster AI Property-Based Tests

This directory contains property-based tests for the Monster AI system, validating correctness properties defined in the design specification.

## Running Tests

### In Unity Editor

1. Open the Unity project in Unity Editor
2. Go to **Window > General > Test Runner**
3. Select the **EditMode** tab
4. Click **Run All** to execute all tests
5. Or expand the test tree and run individual tests

### From Command Line

```bash
# Windows
"C:\Program Files\Unity\Hub\Editor\2021.3.45f2\Editor\Unity.exe" -runTests -batchmode -projectPath "Unity Project\Darkness Survival" -testResults "TestResults.xml" -testPlatform EditMode

# Note: Close Unity Editor before running command-line tests
```

## Test Files

### PropertyTests_ValidActionSelection.cs

**Feature**: monster-rl-behaviors, Property 1: Valid action selection  
**Validates**: Requirements 1.1

Tests that for any combat situation, when the system selects a tactical behavior, the selected action type must be one of the valid EnemyActionType enum values.

**Test Methods**:
- `Property_ValidActionSelection_ForAllCombatSituations()` - Tests 100 random combat situations
- `Property_ValidActionSelection_WithExtremeValues()` - Tests edge cases with extreme values
- `Property_ValidActionSelection_AtDecisionBoundaries()` - Tests at critical decision thresholds

**Configuration**:
- Minimum iterations: 100
- Random seed: 42 (for reproducibility)
- Tests all valid EnemyActionType enum values

### PropertyTests_KitingDistanceIncrease.cs

**Feature**: monster-rl-behaviors, Property 2: Kiting distance increase  
**Validates**: Requirements 1.2

Tests that for any monster performing a kiting maneuver, the distance to the player after the maneuver must be greater than before the maneuver and exceed the configured counterattack range.

**Test Methods**:
- `Property_KitingIncreasesDistance_ForAllMonsters()` - Tests 100 random kiting scenarios
- `Property_KitingReachesSafeDistance_AfterMultipleMovements()` - Tests multi-frame kiting sequences
- `Property_KitingBehavior_AtVariousDistances()` - Tests kiting at critical distance thresholds
- `Property_KitingApproaches_WhenCooldownZero()` - Tests attack phase behavior (no cooldown)

**Configuration**:
- Minimum iterations: 100
- Random seed: 43 (for reproducibility)
- Tests TacticalPositioningBehavior.CalculateKitingVector()
- Validates distance increase and safe distance achievement

## Test Results

After running tests, check:
- Unity Test Runner window for pass/fail status
- Console window for detailed logs
- TestResults.xml (if running from command line)

## Troubleshooting

### Tests Not Appearing

If tests don't appear in Test Runner:
1. Ensure the `EnemyAI.Tests.asmdef` file is properly configured
2. Check that Unity Test Framework package is installed
3. Reimport the Tests folder (right-click > Reimport)

### Compilation Errors

If you see compilation errors:
1. Ensure all required scripts are present in the AI folder
2. Check that `EnemyAIContracts.cs` defines all required types
3. Verify Unity version compatibility (2021.3+)

## Adding New Property Tests

To add a new property test:

1. Create a new test file: `PropertyTests_<PropertyName>.cs`
2. Add the test fixture attribute: `[TestFixture]`
3. Include the property documentation comment:
   ```csharp
   /// <summary>
   /// Feature: monster-rl-behaviors, Property X: <Property Name>
   /// Validates: Requirements X.Y
   /// </summary>
   ```
4. Implement test methods with `[Test]` attribute
5. Run minimum 100 iterations for property-based tests
6. Use fixed random seed for reproducibility

## References

- Design Document: `.kiro/specs/monster-rl-behaviors/design.md`
- Requirements: `.kiro/specs/monster-rl-behaviors/requirements.md`
- Tasks: `.kiro/specs/monster-rl-behaviors/tasks.md`
