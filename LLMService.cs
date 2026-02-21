using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

[Serializable]
public class PatientResponse
{
    public string response_text;
    public float anxiety_delta;
    public string anxiety_level;
    public bool understands;
}

[Serializable]
public class CaseData
{
    public string case_id;
    public string personality;
    public string case_name;
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

public class LLMService : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string apiUrl = "http://localhost:1234/v1/chat/completions"; // LM Studio默认地址
    [SerializeField] private string systemPromptPath = "SystemPrompts/patient_prompt_v1";

    [Header("Dependencies")]
    [SerializeField] private AnxietyManager anxietyManager;

    [Header("Current Case")]
    [SerializeField] private TextAsset caseJsonFile; // 在Inspector中拖入JSON文件
    private CaseData currentCase;
    private string systemPromptTemplate;

    private void Start()
    {
        // 加载病例
        if (caseJsonFile != null)
        {
            LoadCase(caseJsonFile.text);
        }

        // 加载系统提示词模板
        TextAsset promptAsset = Resources.Load<TextAsset>(systemPromptPath);
        if (promptAsset != null)
        {
            systemPromptTemplate = promptAsset.text;
        }

        // 初始化AnxietyManager
        if (anxietyManager != null && currentCase != null)
        {
            anxietyManager.Initialize(currentCase);
        }
    }

    public void LoadCase(string jsonContent)
    {
        currentCase = JsonUtility.FromJson<CaseData>(jsonContent);
        Debug.Log($"Loaded case: {currentCase.case_name} ({currentCase.personality})");

        if (anxietyManager != null)
        {
            anxietyManager.Initialize(currentCase);
        }
    }

    // 公开方法：发送医生说的话，获取患者回应
    public async void SendDoctorSpeech(string doctorSpeech, System.Action<string> onResponse)
    {
        string patientResponse = await GetPatientResponse(doctorSpeech);
        onResponse?.Invoke(patientResponse);
    }

    private async System.Threading.Tasks.Task<string> GetPatientResponse(string doctorSpeech)
    {
        if (anxietyManager.CurrentState == null)
        {
            Debug.LogError("PatientState not initialized!");
            return "Error: Patient not initialized";
        }

        // 1. 通过AnxietyManager生成Prompt
        string prompt = anxietyManager.GeneratePrompt(doctorSpeech);

        // 2. 调用LLM...
        string jsonResponse = await CallLLM(prompt);

        // 3. 解析响应
        PatientResponse response = JsonUtility.FromJson<PatientResponse>(jsonResponse);

        // 4. 验证回答长度
        Vector2Int lengthRange = anxietyManager.GetResponseLengthRange();
        int actualLength = response.response_text.Length;
        if (actualLength < lengthRange.x || actualLength > lengthRange.y)
        {
            Debug.LogWarning($"Response length {actualLength} outside range [{lengthRange.x}-{lengthRange.y}]");
        }

        // 5. 更新焦虑管理器的状态
        anxietyManager.UpdateAnxiety(
            response.anxiety_delta,
            response.anxiety_level,
            doctorSpeech,
            response.response_text,
            response.understands
        );

        return response.response_text;
    }

    private string BuildPrompt(string doctorSpeech, Vector2Int lengthRange)
    {
        // 从模板构建完整的system prompt
        string prompt = systemPromptTemplate
            .Replace("{symptoms}", currentCase.symptoms)
            .Replace("{personality_type}", currentCase.personality == "extrovert" ? "extrovert" : "introvert")
            .Replace("{anxiety_level}", anxietyManager.CurrentAnxietyLevel) // 这里现在会返回none/mild/significant/extreme
            .Replace("{min_length}", lengthRange.x.ToString())
            .Replace("{max_length}", lengthRange.y.ToString());

        // 添加性格特定的额外提示
        if (currentCase.personality == "extrovert")
        {
            prompt += "\n\nRemember that you are an extroverted patient: speak frankly, proactively provide information, and trust your doctor.";
        }
        else
        {
            prompt += "\n\nRemember that you are an introverted patient: speak briefly and hesitantly, respond passively, and be wary of the hospital environment.";
        }

        return prompt;
    }

    // 手动设置病例（用于调试）
    public void SetCase(CaseData caseData)
    {
        currentCase = caseData;
        if (anxietyManager != null)
        {
            anxietyManager.Initialize(currentCase);
        }
    }
}