using System;
using UnityEngine;

/// <summary>
/// Represents the personality traits of a monster that influence its behavior.
/// These traits persist across the monster's lifetime and affect decision-making.
/// </summary>
[Serializable]
public struct PersonalityTraits
{
    [Header("Core Traits")]
    [Tooltip("Aggression level (0-1): Higher values make the monster more likely to attack")]
    [Range(0f, 1f)]
    public float aggression;
    
    [Tooltip("Caution level (0-1): Higher values make the monster more defensive")]
    [Range(0f, 1f)]
    public float caution;
    
    [Tooltip("Teamwork level (0-1): Higher values make the monster more cooperative")]
    [Range(0f, 1f)]
    public float teamwork;
    
    [Tooltip("Opportunism level (0-1): Higher values make the monster exploit weaknesses")]
    [Range(0f, 1f)]
    public float opportunism;

    /// <summary>
    /// Creates a new PersonalityTraits instance with specified values.
    /// </summary>
    public PersonalityTraits(float aggression, float caution, float teamwork, float opportunism)
    {
        this.aggression = Mathf.Clamp01(aggression);
        this.caution = Mathf.Clamp01(caution);
        this.teamwork = Mathf.Clamp01(teamwork);
        this.opportunism = Mathf.Clamp01(opportunism);
    }

    /// <summary>
    /// Creates a balanced personality with all traits at 0.5.
    /// </summary>
    public static PersonalityTraits Balanced => new PersonalityTraits(0.5f, 0.5f, 0.5f, 0.5f);

    /// <summary>
    /// Creates an aggressive personality (high aggression, low caution).
    /// </summary>
    public static PersonalityTraits Aggressive => new PersonalityTraits(0.8f, 0.2f, 0.4f, 0.6f);

    /// <summary>
    /// Creates a cautious personality (low aggression, high caution).
    /// </summary>
    public static PersonalityTraits Cautious => new PersonalityTraits(0.3f, 0.8f, 0.5f, 0.4f);

    /// <summary>
    /// Creates a cooperative personality (high teamwork).
    /// </summary>
    public static PersonalityTraits Cooperative => new PersonalityTraits(0.5f, 0.5f, 0.9f, 0.5f);

    /// <summary>
    /// Creates a random personality with values between min and max.
    /// </summary>
    public static PersonalityTraits Random(float min = 0.3f, float max = 0.7f)
    {
        return new PersonalityTraits(
            UnityEngine.Random.Range(min, max),
            UnityEngine.Random.Range(min, max),
            UnityEngine.Random.Range(min, max),
            UnityEngine.Random.Range(min, max)
        );
    }

    /// <summary>
    /// Validates that all trait values are within [0, 1] range.
    /// </summary>
    public bool IsValid()
    {
        return aggression >= 0f && aggression <= 1f &&
               caution >= 0f && caution <= 1f &&
               teamwork >= 0f && teamwork <= 1f &&
               opportunism >= 0f && opportunism <= 1f;
    }

    /// <summary>
    /// Clamps all trait values to [0, 1] range.
    /// </summary>
    public PersonalityTraits Clamped()
    {
        return new PersonalityTraits(
            Mathf.Clamp01(aggression),
            Mathf.Clamp01(caution),
            Mathf.Clamp01(teamwork),
            Mathf.Clamp01(opportunism)
        );
    }

    public override string ToString()
    {
        return $"Personality(Aggression: {aggression:F2}, Caution: {caution:F2}, Teamwork: {teamwork:F2}, Opportunism: {opportunism:F2})";
    }
}
