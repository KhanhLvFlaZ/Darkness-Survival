using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Requirement 14.4: Implements reward visualization for AI.
/// Displays reward value as floating text above monster.
/// Shows reward reason (e.g., "Flanking Bonus +0.3").
/// Color-codes positive (green) and negative (red) rewards.
/// </summary>
[RequireComponent(typeof(Monsters))]
public class AIRewardVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] bool showRewardText = false;
    [SerializeField] float textDisplayDuration = 1.5f;
    [SerializeField] float textRiseSpeed = 1f;
    [SerializeField] float textFadeSpeed = 1f;
    [SerializeField] Vector3 textOffset = new Vector3(0f, 1.5f, 0f);
    
    [Header("Color Settings")]
    [SerializeField] Color positiveRewardColor = Color.green;
    [SerializeField] Color negativeRewardColor = Color.red;
    [SerializeField] Color neutralRewardColor = Color.yellow;
    [SerializeField] float minRewardToShow = 0.01f;
    
    [Header("Text Settings")]
    [SerializeField] int fontSize = 14;
    [SerializeField] Font textFont;
    
    // Component references
    Monsters monster;
    RewardCalculator rewardCalculator;
    
    // Active floating texts
    List<FloatingRewardText> activeTexts = new List<FloatingRewardText>();
    
    // Pooling for performance
    Queue<GameObject> textPool = new Queue<GameObject>();
    int poolSize = 10;
    
    void Awake()
    {
        monster = GetComponent<Monsters>();
        rewardCalculator = GetComponent<RewardCalculator>();
        
        // Initialize text pool
        InitializeTextPool();
    }
    
    void Start()
    {
        // Subscribe to reward events
        if (rewardCalculator != null)
        {
            rewardCalculator.OnRewardCalculated += HandleRewardCalculated;
        }
    }
    
    void Update()
    {
        if (!showRewardText) return;
        
        // Update all active floating texts
        for (int i = activeTexts.Count - 1; i >= 0; i--)
        {
            FloatingRewardText text = activeTexts[i];
            
            if (text.Update(Time.deltaTime, textRiseSpeed, textFadeSpeed))
            {
                // Text has finished, return to pool
                ReturnTextToPool(text);
                activeTexts.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Requirement 14.4: Display reward value as floating text above monster.
    /// Show reward reason. Color-code positive (green) and negative (red) rewards.
    /// </summary>
    void HandleRewardCalculated(float rewardValue, string reason)
    {
        if (!showRewardText) return;
        
        // Filter out very small rewards
        if (Mathf.Abs(rewardValue) < minRewardToShow) return;
        
        ShowRewardText(rewardValue, reason);
    }
    
    /// <summary>
    /// Show floating reward text.
    /// </summary>
    public void ShowRewardText(float rewardValue, string reason)
    {
        if (!showRewardText) return;
        
        // Get text object from pool
        GameObject textObj = GetTextFromPool();
        if (textObj == null) return;
        
        // Position above monster
        Vector3 startPosition = transform.position + textOffset;
        textObj.transform.position = startPosition;
        
        // Get TextMesh component
        TextMesh textMesh = textObj.GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = textObj.AddComponent<TextMesh>();
        }
        
        // Format text
        string sign = rewardValue >= 0 ? "+" : "";
        string displayText = $"{reason} {sign}{rewardValue:F2}";
        textMesh.text = displayText;
        
        // Set color based on reward value
        Color textColor = GetRewardColor(rewardValue);
        textMesh.color = textColor;
        
        // Set font and size
        if (textFont != null)
        {
            textMesh.font = textFont;
            textObj.GetComponent<MeshRenderer>().material = textFont.material;
        }
        textMesh.fontSize = fontSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // Create floating text controller
        FloatingRewardText floatingText = new FloatingRewardText(textObj, textDisplayDuration);
        activeTexts.Add(floatingText);
    }
    
    /// <summary>
    /// Requirement 14.4: Color-code positive (green) and negative (red) rewards.
    /// </summary>
    Color GetRewardColor(float rewardValue)
    {
        if (rewardValue > 0f)
            return positiveRewardColor;
        else if (rewardValue < 0f)
            return negativeRewardColor;
        else
            return neutralRewardColor;
    }
    
    /// <summary>
    /// Initialize text object pool.
    /// </summary>
    void InitializeTextPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject textObj = new GameObject($"RewardText_{i}");
            textObj.SetActive(false);
            textObj.transform.SetParent(transform);
            textPool.Enqueue(textObj);
        }
    }
    
    /// <summary>
    /// Get text object from pool.
    /// </summary>
    GameObject GetTextFromPool()
    {
        if (textPool.Count > 0)
        {
            GameObject textObj = textPool.Dequeue();
            textObj.SetActive(true);
            return textObj;
        }
        
        // Pool exhausted, create new object
        GameObject newTextObj = new GameObject($"RewardText_Extra");
        newTextObj.transform.SetParent(transform);
        return newTextObj;
    }
    
    /// <summary>
    /// Return text object to pool.
    /// </summary>
    void ReturnTextToPool(FloatingRewardText floatingText)
    {
        if (floatingText.textObject != null)
        {
            floatingText.textObject.SetActive(false);
            textPool.Enqueue(floatingText.textObject);
        }
    }
    
    /// <summary>
    /// Enable/disable reward visualization.
    /// </summary>
    public void SetShowRewardText(bool show)
    {
        showRewardText = show;
    }
    
    /// <summary>
    /// Get current visualization state.
    /// </summary>
    public bool IsShowingRewardText()
    {
        return showRewardText;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from reward events
        if (rewardCalculator != null)
        {
            rewardCalculator.OnRewardCalculated -= HandleRewardCalculated;
        }
        
        // Clean up active texts
        foreach (FloatingRewardText text in activeTexts)
        {
            if (text.textObject != null)
            {
                Destroy(text.textObject);
            }
        }
        activeTexts.Clear();
        
        // Clean up pool
        while (textPool.Count > 0)
        {
            GameObject textObj = textPool.Dequeue();
            if (textObj != null)
            {
                Destroy(textObj);
            }
        }
    }
    
    /// <summary>
    /// Helper class to manage floating text animation.
    /// </summary>
    class FloatingRewardText
    {
        public GameObject textObject;
        public float lifetime;
        public float elapsedTime;
        public Vector3 startPosition;
        
        public FloatingRewardText(GameObject obj, float duration)
        {
            textObject = obj;
            lifetime = duration;
            elapsedTime = 0f;
            startPosition = obj.transform.position;
        }
        
        /// <summary>
        /// Update floating text. Returns true when finished.
        /// </summary>
        public bool Update(float deltaTime, float riseSpeed, float fadeSpeed)
        {
            elapsedTime += deltaTime;
            
            if (elapsedTime >= lifetime)
            {
                return true; // Finished
            }
            
            // Move text upward
            if (textObject != null)
            {
                Vector3 newPosition = startPosition + Vector3.up * (elapsedTime * riseSpeed);
                textObject.transform.position = newPosition;
                
                // Fade out
                TextMesh textMesh = textObject.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    float alpha = 1f - (elapsedTime / lifetime);
                    Color color = textMesh.color;
                    color.a = alpha;
                    textMesh.color = color;
                }
            }
            
            return false; // Still active
        }
    }
}
