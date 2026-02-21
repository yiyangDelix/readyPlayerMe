using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
        case_name = case_data.case_name;
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
    public void UpdateState(float deltaAnxiety, string doctorSpeech, string patientSpeech, bool understands)
    {
        conversation_turn++;

        // 更新焦虑值
        float oldAnxiety = current_anxiety;
        current_anxiety = Mathf.Clamp01(current_anxiety + deltaAnxiety);

        // 记录对话
        dialogue_history.Add(new DialogueTurn
        {
            turn = conversation_turn,
            doctor = doctorSpeech,
            patient = patientSpeech,
            anxiety_before = oldAnxiety,
            anxiety_after = current_anxiety,
            understands = understands,
            timestamp = DateTime.Now
        });

        // 简单症状提取（示例：如果医生问症状，患者回答中包含症状描述）
        if (doctorSpeech.Contains("什么症状") || doctorSpeech.Contains("哪里不舒服"))
        {
            // 这里可以添加自然语言处理，简单起见只记录有症状对话轮次
            symptoms_mentioned.Add($"Turn {conversation_turn}: {patientSpeech}");
        }

        Debug.Log($"State updated - Turn {conversation_turn}: " +
                 $"Anxiety {oldAnxiety:F2} → {current_anxiety:F2} ({GetAnxietyLevel()})");
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
        prompt.AppendLine("# 角色设定");
        prompt.AppendLine("你扮演一位前来就诊的患者，正在医生的诊室里进行初次问诊。");
        prompt.AppendLine("重要限制：你没有接受过任何医学教育，对医学术语完全不了解。");
        prompt.AppendLine();

        // 2. 当前状态
        prompt.AppendLine("## 当前患者档案");
        prompt.AppendLine($"身体状况：{symptoms}");
        prompt.AppendLine($"性格类型：{(personality == "extrovert" ? "外向型" : "内向型")}");
        prompt.AppendLine($"当前焦虑程度：{GetAnxietyLevel()} ({current_anxiety:F2})");

        Vector2Int lengthRange = GetResponseLengthRange();
        prompt.AppendLine($"回答长度要求：{lengthRange.x}到{lengthRange.y}字之间");
        prompt.AppendLine();

        // 3. 性格特征强化
        if (personality == "extrovert")
        {
            prompt.AppendLine("你是外向型患者：");
            prompt.AppendLine("- 表达方式：主动、直白、话多");
            prompt.AppendLine("- 信息提供：主动补充医生没问到的信息");
            prompt.AppendLine("- 态度：信任医生，积极配合");
        }
        else
        {
            prompt.AppendLine("你是内向型患者：");
            prompt.AppendLine("- 表达方式：简短、犹豫、被动");
            prompt.AppendLine("- 信息提供：只回答直接提问，且很简短");
            prompt.AppendLine("- 态度：害怕医院，对医生有抵触");
        }
        prompt.AppendLine();

        // 4. 最近的对话历史（最后3轮，帮助保持上下文连贯）
        if (dialogue_history.Count > 0)
        {
            prompt.AppendLine("## 最近的对话历史");
            int startIdx = Math.Max(0, dialogue_history.Count - 3);
            for (int i = startIdx; i < dialogue_history.Count; i++)
            {
                var turn = dialogue_history[i];
                prompt.AppendLine($"医生：{turn.doctor}");
                prompt.AppendLine($"患者：{turn.patient}");
            }
            prompt.AppendLine();
        }

        // 5. 医生当前的话
        prompt.AppendLine($"## 医生现在说");
        prompt.AppendLine(doctorSpeech);
        prompt.AppendLine();

        // 6. 输出格式要求
        prompt.AppendLine("## 输出格式");
        prompt.AppendLine("请严格按照以下JSON格式返回（不要包含其他文字）：");
        prompt.AppendLine(@"{");
        prompt.AppendLine("  \"response_text\": \"你的回答内容\",");
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
    public bool understands;
    public DateTime timestamp;
}

// 注意：CaseData类应该单独放在一个文件里
[Serializable]
public class CaseData
{
    public string case_id;
    public string case_name;
    public string personality;
    public PersonalityParams personality_params;
    public float initial_anxiety;
    public string symptoms;
}

[Serializable]
public class PersonalityParams
{
    public float base_anxiety_decay;
    public int response_length_min;
    public int response_length_max;
}