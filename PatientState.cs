using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CaseData;
using PersonalityParams;


// 负责管理患者的焦虑状态，提供接口供LLMService调用，并控制动画表现
[Serializable]
public class PatientState
{
    // 基本信息（从JSON加载）
    public string case_id;
    public string case_name;
    public string personality;              // "extrovert" / "introvert"
    public PersonalityParams personality_params;
    public float initial_anxiety;
    public string symptoms;

    // 动态状态（随对话变化）
    public float current_anxiety;
    public int conversation_turn;
    public List<string> symptoms_mentioned;      // 已提及的症状
    public List<DialogueTurn> dialogue_history;  // 对话历史

    // SPIKES进度追踪（可选，用于提示）
    public Dictionary<string, bool> spikes_progress;

    public PatientState(string jsonContent)
    {
        // 从JSON解析
        CaseData caseData = JsonUtility.FromJson<CaseData>(jsonContent);

        // 赋值
        case_id = caseData.case_id;
        case_name = caseData.case_name;
        personality = caseData.personality;
        personality_params = caseData.personality_params;
        initial_anxiety = caseData.initial_anxiety;
        symptoms = caseData.symptoms;

        // 初始化动态状态
        current_anxiety = initial_anxiety;
        conversation_turn = 0;
        symptoms_mentioned = new List<string>();
        dialogue_history = new List<DialogueTurn>();

        // 初始化SPIKES进度
        spikes_progress = new Dictionary<string, bool>
        {
            {"setting", false},
            {"perception", false},
            {"invitation", false},
            {"knowledge", false},
            {"emotion", false},
            {"strategy", false}
        };

        Debug.Log($"PatientState initialized: {case_name}, " +
                 $"Personality={personality}, " +
                 $"Initial Anxiety={current_anxiety:F2}");
    }

    // 更新状态
    public void UpdateState(float deltaFromLLM, string anxietyLevelFromLLM,
                        string doctorSpeech, string patientSpeech, bool understands)
    {
        conversation_turn++;

        // 记录变化前的焦虑值
        float oldAnxiety = current_anxiety;

        // ===== 核心改进：变化幅度衰减机制 =====
        // 原理：越焦虑的人，情绪越难再升高，但越容易下降
        float attenuationFactor;
        if (deltaFromLLM > 0)
        {
            // 焦虑上升时：越焦虑越难再升
            attenuationFactor = 1 - current_anxiety;
        }
        else
        {
            // 焦虑下降时：越焦虑越容易下降
            attenuationFactor = current_anxiety;
        }

        // 应用衰减
        float adjustedDelta = deltaFromLLM * attenuationFactor;

        // 确保调整后的delta仍在合理范围内
        adjustedDelta = Mathf.Clamp(adjustedDelta, -0.3f, 0.3f);

        // 更新焦虑值
        current_anxiety = Mathf.Clamp01(current_anxiety + adjustedDelta);
        // ===== 核心改进结束 =====

        // 记录对话
        dialogue_history.Add(new DialogueTurn
        {
            turn = conversation_turn,
            doctor = doctorSpeech,
            patient = patientSpeech,
            anxiety_before = oldAnxiety,
            anxiety_after = current_anxiety,
            delta_raw = deltaFromLLM,
            delta_attenuated = adjustedDelta,
            attenuation_factor = attenuationFactor,
            understands = understands,
            timestamp = DateTime.Now
        });

        // 详细日志
        Debug.Log($"Turn {conversation_turn}: " +
                 $"Raw Δ={deltaFromLLM:F3}, " +
                 $"Attenuation={attenuationFactor:F3}, " +
                 $"Adjusted Δ={adjustedDelta:F3}, " +
                 $"Anxiety: {oldAnxiety:F3} → {current_anxiety:F3} ({GetAnxietyLevel()})");
    }

    // 获取焦虑等级
    public string GetAnxietyLevel()
    {
        if (current_anxiety < 0.3f) return "none";
        if (current_anxiety < 0.6f) return "mild";
        if (current_anxiety < 0.8f) return "significant";
        return "extreme";
    }

    // 获取回答长度范围
    public Vector2Int GetResponseLengthRange()
    {
        return new Vector2Int(
            personality_params.response_length_min,
            personality_params.response_length_max
        );
    }

    // 生成Prompt
    public string GeneratePrompt(string doctorSpeech)
    {
        StringBuilder prompt = new StringBuilder();

        // 1. 角色设定
        // # 角色设定
        prompt.AppendLine("# Character settings");
        // 你扮演一位前来就诊的患者，正在医生的诊室里进行初次问诊。
        prompt.AppendLine("You play a patient who has come to the doctor for an initial consultation in the doctor's office.");
        // 重要限制：你没有接受过任何医学教育，对医学术语完全不了解。
        prompt.AppendLine("Important constraints: You have not received any medical education and do not understand medical terminology.");
        prompt.AppendLine();

        // 2. 当前状态
        // ## 当前患者档案
        prompt.AppendLine("## Current patient profile");
        // 身体状况
        prompt.AppendLine($"physical condition: {symptoms}");
        // 性格类型
        prompt.AppendLine($"Personality type: {(personality == "extrovert" ? "extroverted" : "introverted")}");
        // 当前焦虑程度
        prompt.AppendLine($"Current anxiety level: {GetAnxietyLevel()} ({current_anxiety:F2})");

        Vector2Int lengthRange = GetResponseLengthRange();
        // 回答长度要求
        prompt.AppendLine($"Response length range: {lengthRange.x} to {lengthRange.y} characters");
        prompt.AppendLine();

        // 3. 性格特征强化
        if (personality == "extrovert")
        {
            // 外向型患者
            prompt.AppendLine("You are an extroverted patient:");
            prompt.AppendLine("- Expression style: proactive, direct, talkative");
            prompt.AppendLine("- Information provision: proactively supplement information not asked by the doctor");
            prompt.AppendLine("- Attitude: trust the doctor and cooperate actively");
        }
        else
        {
            // 内向型患者
            prompt.AppendLine("You are an introverted patient:");
            prompt.AppendLine("- Expression style: brief, hesitant, passive");
            prompt.AppendLine("- Information provision: only answer direct questions and keep responses short");
            prompt.AppendLine("- Attitude: afraid of the hospital and resistant to the doctor");
        }
        prompt.AppendLine();

        // 4. 最近的对话历史（最后3轮，帮助保持上下文连贯）
        if (dialogue_history.Count > 0)
        {
            // ## 最近的对话历史
            prompt.AppendLine("## Recent dialogue history (last 3 turns)");
            int startIdx = Math.Max(0, dialogue_history.Count - 3);
            for (int i = startIdx; i < dialogue_history.Count; i++)
            {
                var turn = dialogue_history[i];
                prompt.AppendLine($"Doctor: {turn.doctor}");
                prompt.AppendLine($"Patient: {turn.patient}");
            }
            prompt.AppendLine();
        }

        // 5. 医生当前的话
        prompt.AppendLine($"## Doctor's current speech");
        prompt.AppendLine(doctorSpeech);
        prompt.AppendLine();

        // 6. 输出格式要求
        prompt.AppendLine("## Output format");
        prompt.AppendLine("Please strictly return in the following JSON format (do not include any other text):");
        prompt.AppendLine(@"{");
        prompt.AppendLine("  \"response_text\": \"your response content\",");
        prompt.AppendLine("  \"anxiety_delta\": -0.15,");
        prompt.AppendLine("  \"anxiety_level\": \"none/mild/significant/extreme\",");
        prompt.AppendLine("  \"understands\": true/false");
        prompt.AppendLine("}");

        return prompt.ToString();
    }

    // 导出对话记录（用于论文分析）
    public string ExportDialogueLog()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine($"Case: {case_name} ({case_id})");
        log.AppendLine($"Personality: {personality}");
        log.AppendLine($"Initial Anxiety: {initial_anxiety}");
        log.AppendLine("=== Dialogue Transcript ===");

        foreach (var turn in dialogue_history)
        {
            log.AppendLine($"\n[Turn {turn.turn}]");
            log.AppendLine($"Doctor: {turn.doctor}");
            log.AppendLine($"Patient: {turn.patient}");
            log.AppendLine($"Anxiety: {turn.anxiety_before:F2} → {turn.anxiety_after:F2} " +
                          $"(understands: {turn.understands})");
        }

        return log.ToString();
    }
}

[Serializable]
public class DialogueTurn
{
    public int turn;
    public string doctor;
    public string patient;
    public float anxiety_before;
    public float anxiety_after;
    public float delta_raw;           // LLM返回的原始delta
    public float delta_attenuated;     // 衰减后的实际delta
    public float attenuation_factor;   // 衰减因子
    public bool understands;
    public DateTime timestamp;

    // 方便查看的摘要
    public string GetSummary()
    {
        return $"Turn {turn}: Anxiety {anxiety_before:F2}→{anxiety_after:F2} " +
               $"(Δ raw={delta_raw:F2}, atten={attenuation_factor:F2}→{delta_attenuated:F2})";
    }
}
