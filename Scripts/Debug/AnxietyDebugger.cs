using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 实时可视化焦虑变化和关键词匹配的调试工具
/// </summary>
/// 这个类：调试可视化工具，显示当前焦虑值、关键词匹配情况和LLM分析结果，帮助开发者理解系统的行为。
public class AnxietyDebugger : MonoBehaviour
{
    [Header("引用组件")]
    public LLMService llmService;
    public AnxietyManager anxietyManager;

    [Header("UI元素")]
    public Text currentAnxietyText;
    public Text lastDeltaText;
    public Text keywordMatchesText;
    public Slider anxietySlider;
    public Text personalityText;
    public Text conversationTurnText;

    private List<string> lastPositiveMatches = new List<string>();
    private List<string> lastNegativeMatches = new List<string>();
    private float lastDelta;
    private int lastTurn;

    private void Update()
    {
        if (anxietyManager != null && anxietyManager.CurrentState != null)
        {
            // 显示当前焦虑值和等级
            float anxiety = anxietyManager.CurrentAnxiety;
            string level = anxietyManager.CurrentAnxietyLevel;
            int turn = anxietyManager.CurrentState.conversation_turn;

            if (currentAnxietyText != null)
                currentAnxietyText.text = $"焦虑值: {anxiety:F2} ({level})";

            if (anxietySlider != null)
                anxietySlider.value = anxiety;

            if (personalityText != null)
                personalityText.text = $"性格: {anxietyManager.CurrentState.personality}";

            if (conversationTurnText != null && turn != lastTurn)
            {
                conversationTurnText.text = $"对话轮次: {turn}";
                lastTurn = turn;
            }
        }

        // 显示关键词匹配数量
        if (keywordMatchesText != null)
        {
            string posStr = lastPositiveMatches.Count > 0 ? $"+{lastPositiveMatches.Count}" : "";
            string negStr = lastNegativeMatches.Count > 0 ? $"-{lastNegativeMatches.Count}" : "";
            keywordMatchesText.text = $"关键词: {posStr} {negStr}".Trim();
        }

        // 显示上次变化值（颜色区分正负）
        if (lastDeltaText != null)
        {
            string sign = lastDelta > 0 ? "+" : "";
            lastDeltaText.text = $"上次变化: {sign}{lastDelta:F2}";
            lastDeltaText.color = lastDelta > 0 ? Color.red : Color.green;
        }
    }

    /// <summary>
    /// 调用此方法以更新调试显示
    /// </summary>
    public void ShowAnalysisResult(AnxietyAnalyzer.AnalysisResult result, float finalDelta)
    {
        lastPositiveMatches = result.matchedPositive;
        lastNegativeMatches = result.matchedNegative;
        lastDelta = finalDelta;
    }

    /// <summary>
    /// 清除所有调试显示
    /// </summary>
    public void Clear()
    {
        lastPositiveMatches.Clear();
        lastNegativeMatches.Clear();
        lastDelta = 0;

        if (keywordMatchesText != null)
            keywordMatchesText.text = "关键词:";

        if (lastDeltaText != null)
            lastDeltaText.text = "上次变化: 0.00";
    }
}