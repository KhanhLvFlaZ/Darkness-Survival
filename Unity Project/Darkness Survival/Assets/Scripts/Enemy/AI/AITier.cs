using System;

/// <summary>
/// Defines the AI sophistication levels for monsters.
/// Used to control difficulty progression and visual feedback.
/// </summary>
[Serializable]
public enum AITier
{
    Novice,      // Heuristic only, predictable
    Learning,    // ML + heuristic blend, exploration
    Trained,     // Primarily ML, minimal exploration
    Expert       // ML only, advanced features enabled
}
