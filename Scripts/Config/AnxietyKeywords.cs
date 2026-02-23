using System.Collections.Generic;
using UnityEngine;


/// 这个类：定义关键词和权重配置，作为ScriptableObject方便在Unity编辑器中配置和调整。
[CreateAssetMenu(fileName = "AnxietyKeywords", menuName = "VR Patient/Anxiety Keywords")]
public class AnxietyKeywords : ScriptableObject
{
    [Header("正向词汇（降低焦虑）")]
    [Tooltip("这些词出现时，焦虑值会降低")]
    public List<string> positiveWords = new List<string>()
    {
        "good", "great", "easy", "relaxed", "yes", "clear",
        "reassuring", "fine", "better", "hope", "curable",
        "simple", "normal", "healthy", "recover", "improve"
    };

    [Header("负向词汇（升高焦虑）")]
    [Tooltip("这些词出现时，焦虑值会升高")]
    public List<string> negativeWords = new List<string>()
    {
        "bad", "hard", "serious", "no", "die", "death",
        "terrible", "difficult", "wrong", "pain", "painful",
        "cancer", "tumor", "surgery", "chronic", "incurable",
        "severe", "emergency", "critical", "failure", "risk"
    };

    [Header("强化词（加倍效果）")]
    [Tooltip("这些词会放大附近情感词的效果")]
    public List<string> intensifiers = new List<string>()
    {
        "very", "extremely", "absolutely", "terribly",
        "really", "so", "too", "completely", "totally"
    };

    [Header("弱化词（减半效果）")]
    [Tooltip("这些词会减弱附近情感词的效果")]
    public List<string> softeners = new List<string>()
    {
        "maybe", "possibly", "perhaps", "a little", "slightly",
        "somewhat", "kind of", "a bit", "relatively"
    };

    [Header("权重设置")]
    public float positiveWeight = -0.1f;    // 每个正向词的影响值
    public float negativeWeight = 0.15f;     // 每个负向词的影响值
    public float intensifierMultiplier = 1.5f; // 强化词的倍数
    public float softenerMultiplier = 0.5f;    // 弱化词的倍数
}