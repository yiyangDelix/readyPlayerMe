using System;
using UnityEngine;

public class AnxietyManager : MonoBehaviour
{
    [Header("Current State")]
    [SerializeField] private float currentAnxiety;
    [SerializeField] private int conversationTurn = 0;
    
    [Header("Animation")]
    [SerializeField] private Animator avatarAnimator;
    
    // 当前病例数据
    private CaseData currentCase;
    
    // 公开属性
    public float CurrentAnxiety => currentAnxiety;
    public string CurrentAnxietyLevel => GetAnxietyLevel(currentAnxiety);
    public int ConversationTurn => conversationTurn;
    
    public void Initialize(CaseData caseData)
    {
        currentCase = caseData;
        currentAnxiety = caseData.initial_anxiety;
        conversationTurn = 0;
        
        Debug.Log($"AnxietyManager initialized: {caseData.case_name}, " +
                 $"Personality={caseData.personality}, " +
                 $"Initial Anxiety={currentAnxiety:F2} ({CurrentAnxietyLevel})");
    }
    
    // 核心方法：更新焦虑程度
    public void UpdateAnxiety(float deltaFromLLM, string anxietyLevelFromLLM)
    {
        conversationTurn++;
        
        // 1. 基础衰减：性格决定的自然放松过程
        float baseDecay = currentCase.personality_params.base_anxiety_decay;
        
        // 2. 对话轮次因子（越往后衰减越明显，前5轮线性增长，之后稳定）
        float turnFactor = Mathf.Min(1.0f, conversationTurn * 0.2f); // 5轮后达到最大
        
        // 3. 综合计算：LLM的delta + 基础衰减 * 轮次因子
        float totalDelta = deltaFromLLM + (baseDecay * turnFactor);
        
        // 4. 确保在合理范围内
        totalDelta = Mathf.Clamp(totalDelta, -0.3f, 0.3f);
        
        // 5. 更新焦虑值
        float oldAnxiety = currentAnxiety;
        currentAnxiety = Mathf.Clamp01(currentAnxiety + totalDelta);
        
        // 6. 日志记录
        Debug.Log($"Turn {conversationTurn}: " +
                 $"LLM Delta={deltaFromLLM:F2}, " +
                 $"BaseDecay={baseDecay:F2}×{turnFactor:F2}={baseDecay * turnFactor:F2}, " +
                 $"Total={totalDelta:F2}, " +
                 $"Anxiety: {oldAnxiety:F2} ({GetAnxietyLevel(oldAnxiety)}) → " +
                 $"{currentAnxiety:F2} ({CurrentAnxietyLevel})");
        
        // 7. 触发动画更新
        UpdateAvatarAnimation(anxietyLevelFromLLM);
    }
    
    // 获取当前性格下的回答长度范围
    public Vector2Int GetResponseLengthRange()
    {
        return new Vector2Int(
            currentCase.personality_params.response_length_min,
            currentCase.personality_params.response_length_max
        );
    }
    
    // 将数值焦虑转换为文字等级（修正版）
    private string GetAnxietyLevel(float anxiety)
    {
        if (anxiety < 0.3f) return "none";
        if (anxiety < 0.6f) return "mild";
        if (anxiety < 0.8f) return "significant";
        return "extreme";
    }
    
    // 可选：从文字等级获取对应的数值范围中值（用于动画混合）
    private float GetAnxietyLevelMidValue(string level)
    {
        switch (level)
        {
            case "none": return 0.15f;  // 0-0.3 的中值
            case "mild": return 0.45f;  // 0.3-0.6 的中值
            case "significant": return 0.7f;  // 0.6-0.8 的中值
            case "extreme": return 0.9f;  // 0.8-1.0 的中值
            default: return 0.5f;
        }
    }
    
    // 更新Avatar动画
    private void UpdateAvatarAnimation(string anxietyLevelFromLLM)
    {
        if (avatarAnimator == null) return;
        
        // 使用LLM返回的文字等级（更准确）
        switch (anxietyLevelFromLLM)
        {
            case "none":
                avatarAnimator.SetFloat("AnxietyBlend", 0.0f);
                avatarAnimator.SetTrigger("Relaxed");
                break;
            case "mild":
                avatarAnimator.SetFloat("AnxietyBlend", 0.33f);
                break;
            case "significant":
                avatarAnimator.SetFloat("AnxietyBlend", 0.66f);
                break;
            case "extreme":
                avatarAnimator.SetFloat("AnxietyBlend", 1.0f);
                avatarAnimator.SetTrigger("Distressed");
                break;
        }
        
        // 同时传入实际数值，让Blend Tree做更平滑的过渡
        avatarAnimator.SetFloat("AnxietyValue", currentAnxiety);
        
        // 也可以传入等级对应的中值，根据需要选择
        // avatarAnimator.SetFloat("AnxietyLevelMid", GetAnxietyLevelMidValue(anxietyLevelFromLLM));
    }
    
    // 检查是否需要澄清（用于UI提示）
    public bool CheckNeedsClarification(bool understands)
    {
        return !understands;
    }
    
    // 调试方法：手动设置焦虑值（用于测试动画）
    public void DebugSetAnxiety(float value)
    {
        currentAnxiety = Mathf.Clamp01(value);
        Debug.Log($"Debug: Anxiety set to {currentAnxiety:F2} ({CurrentAnxietyLevel})");
        UpdateAvatarAnimation(CurrentAnxietyLevel);
    }
}