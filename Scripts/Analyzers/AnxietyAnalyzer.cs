using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// 这个类：分析医生话语，计算关键词delta，并提供一个方法将LLM的delta和关键词分析的delta进行加权合并。
public class AnxietyAnalyzer
{
    private AnxietyKeywords keywords;

    // 存储分析结果的结构体
    public struct AnalysisResult
    {
        public float anxietyDelta;           // 计算出的焦虑变化值
        public List<string> matchedPositive;  // 匹配到的正向词
        public List<string> matchedNegative;  // 匹配到的负向词
        public float rawScore;                // 应用倍数前的原始分数

        public override string ToString()
        {
            return $"Delta={anxietyDelta:F3}, " +
                   $"Positive={matchedPositive.Count}, Negative={matchedNegative.Count}";
        }
    }

    public AnxietyAnalyzer(AnxietyKeywords keywordConfig)
    {
        keywords = keywordConfig;
    }

    /// <summary>
    /// 分析医生说的话，基于关键词计算焦虑变化值
    /// </summary>
    public AnalysisResult AnalyzeDoctorSpeech(string doctorSpeech)
    {
        if (string.IsNullOrEmpty(doctorSpeech) || keywords == null)
        {
            return new AnalysisResult { anxietyDelta = 0 };
        }

        string lowerSpeech = doctorSpeech.ToLower();

        // 检测情感词
        var positiveMatches = FindWords(lowerSpeech, keywords.positiveWords);
        var negativeMatches = FindWords(lowerSpeech, keywords.negativeWords);

        // 检测强化词和弱化词
        float intensifierMultiplier = GetIntensifierMultiplier(lowerSpeech);

        // 计算基础分数
        float baseScore = (positiveMatches.Count * keywords.positiveWeight) +
                          (negativeMatches.Count * keywords.negativeWeight);

        // 应用倍数
        float finalDelta = baseScore * intensifierMultiplier;

        // 限制在合理范围内
        finalDelta = Mathf.Clamp(finalDelta, -0.3f, 0.3f);

        return new AnalysisResult
        {
            anxietyDelta = finalDelta,
            matchedPositive = positiveMatches,
            matchedNegative = negativeMatches,
            rawScore = baseScore
        };
    }

    /// <summary>
    /// 在文本中查找匹配的词（考虑单词边界）
    /// </summary>
    private List<string> FindWords(string text, List<string> wordList)
    {
        var matches = new List<string>();

        foreach (string word in wordList)
        {
            // 使用正则表达式匹配完整单词
            string pattern = @"\b" + Regex.Escape(word.ToLower()) + @"\b";
            if (Regex.IsMatch(text, pattern))
            {
                matches.Add(word);
            }
        }

        return matches;
    }

    /// <summary>
    /// 根据文本中的强化词和弱化词计算倍数
    /// </summary>
    private float GetIntensifierMultiplier(string text)
    {
        float multiplier = 1.0f;

        // 检查强化词
        foreach (string intensifier in keywords.intensifiers)
        {
            if (text.Contains(intensifier.ToLower()))
            {
                multiplier *= keywords.intensifierMultiplier;
            }
        }

        // 检查弱化词
        foreach (string softener in keywords.softeners)
        {
            if (text.Contains(softener.ToLower()))
            {
                multiplier *= keywords.softenerMultiplier;
            }
        }

        return multiplier;
    }

    /// <summary>
    /// 使用加权平均合并LLM的delta和关键词分析的delta
    /// </summary>
    public float CombineDeltas(float llmDelta, AnalysisResult keywordAnalysis,
                              float llmWeight = 0.7f, float keywordWeight = 0.3f)
    {
        // 加权平均
        float combined = (llmDelta * llmWeight) + (keywordAnalysis.anxietyDelta * keywordWeight);

        // 如果两个delta方向相同（都正或都负），效果增强20%
        if (Mathf.Sign(llmDelta) == Mathf.Sign(keywordAnalysis.anxietyDelta))
        {
            combined *= 1.2f;
        }

        // 确保在合理范围内
        combined = Mathf.Clamp(combined, -0.3f, 0.3f);

        // 记录合并结果用于调试
        Debug.Log($"[AnxietyAnalyzer] 合并结果: LLM={llmDelta:F3}, 关键词={keywordAnalysis.anxietyDelta:F3} → {combined:F3}");

        return combined;
    }
}