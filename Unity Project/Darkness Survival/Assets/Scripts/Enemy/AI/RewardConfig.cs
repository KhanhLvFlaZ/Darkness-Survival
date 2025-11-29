using UnityEngine;

[CreateAssetMenu(fileName = "RewardConfig", menuName = "AI/Reward Config", order = 0)]
public class RewardConfig : ScriptableObject
{
    [Header("Combat")]
    [SerializeField] float damageDealtWeight = 0.01f;
    [SerializeField] float damageTakenWeight = -0.015f;
    [SerializeField] float killReward = 1f;
    [SerializeField] float deathPenalty = -1f;

    [Header("Positioning / Survival")]
    [SerializeField] float survivalTickReward = 0.02f;
    [SerializeField] float survivalTickInterval = 2f;
    [SerializeField] float obstructedPenalty = -0.05f;
    [SerializeField] float idealDistanceMin = 1.5f;
    [SerializeField] float idealDistanceMax = 3.5f;
    [SerializeField] float idealDistanceReward = 0.05f;

    [Header("Spirit Mode")]
    [SerializeField] float spiritEnterReward = 0.1f;
    [SerializeField] float spiritExitPenalty = -0.05f;

    [Header("General")]
    [SerializeField] float maxRewardMagnitude = 0.5f;

    public float DamageDealtWeight => damageDealtWeight;
    public float DamageTakenWeight => damageTakenWeight;
    public float KillReward => killReward;
    public float DeathPenalty => deathPenalty;
    public float SurvivalTickReward => survivalTickReward;
    public float SurvivalTickInterval => Mathf.Max(0.1f, survivalTickInterval);
    public float ObstructedPenalty => obstructedPenalty;
    public float IdealDistanceMin => idealDistanceMin;
    public float IdealDistanceMax => idealDistanceMax;
    public float IdealDistanceReward => idealDistanceReward;
    public float SpiritEnterReward => spiritEnterReward;
    public float SpiritExitPenalty => spiritExitPenalty;
    public float MaxRewardMagnitude => Mathf.Max(0.01f, maxRewardMagnitude);
}
