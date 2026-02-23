using System;
using System.Collections.Generic;
using UnityEngine;

// 这个类：管理患者状态，处理焦虑值更新，生成对话提示，并与动画系统交互。
public class AnxietyManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator avatarAnimator;

    [Header("Current State")]
    [SerializeField] private PatientState patientState;

    // 公开属性
    public PatientState CurrentState => patientState;
    public float CurrentAnxiety => patientState?.current_anxiety ?? 0.5f;
    public string CurrentAnxietyLevel => patientState?.GetAnxietyLevel() ?? "mild";

    public void Initialize(string caseJsonContent)
    {
        patientState = new PatientState(caseJsonContent);
        Debug.Log($"AnxietyManager initialized with {patientState.case_name}");
    }

    // 重载：直接传入CaseData
    public void Initialize(CaseData caseData)
    {
        string json = JsonUtility.ToJson(caseData);
        Initialize(json);
    }

    public string GeneratePrompt(string doctorSpeech)
    {
        if (patientState == null)
        {
            Debug.LogError("PatientState not initialized!");
            return "Error: Patient not initialized";
        }

        return patientState.GeneratePrompt(doctorSpeech);
    }

    public Vector2Int GetResponseLengthRange()
    {
        return patientState?.GetResponseLengthRange() ?? new Vector2Int(0, 100);
    }

    public void UpdateAnxiety(float deltaFromLLM, string anxietyLevelFromLLM,
                          string doctorSpeech, string patientSpeech, bool understands)
    {
        if (patientState == null)
        {
            Debug.LogError("PatientState not initialized!");
            return;
        }

        // 记录变化前的焦虑值用于日志
        float oldAnxiety = patientState.current_anxiety;

        // 更新状态（内部已经包含衰减机制）
        patientState.UpdateState(deltaFromLLM, anxietyLevelFromLLM,
                                doctorSpeech, patientSpeech, understands);

        // 更新动画
        UpdateAvatarAnimation(anxietyLevelFromLLM);
    }

    // 调试方法：导出对话记录
    public void ExportDialogueLog()
    {
        if (patientState == null) return;

        string log = patientState.ExportDialogueLog();
        Debug.Log(log);

        // 可选：保存到文件
        System.IO.File.WriteAllText(Application.dataPath + $"/Logs/{patientState.case_name}_log.txt", log);
    }


    private void UpdateAvatarAnimation(string anxietyLevel)
    {
        if (avatarAnimator == null) return;

        // 简单示例：根据焦虑等级设置不同的动画状态
        switch (anxietyLevel.ToLower())
        {
            case "mild":
                avatarAnimator.SetFloat("AnxietyLevel", 0.3f);
                break;
            case "moderate":
                avatarAnimator.SetFloat("AnxietyLevel", 0.6f);
                break;
            case "severe":
                avatarAnimator.SetFloat("AnxietyLevel", 1.0f);
                break;
            default:
                avatarAnimator.SetFloat("AnxietyLevel", 0.5f);
                break;
        }
    }
}