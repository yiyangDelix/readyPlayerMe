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
    public async Task<string> SendDoctorSpeech(string doctorSpeech)
{
    return await GetPatientResponse(doctorSpeech);
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
        if (response == null || string.IsNullOrEmpty(response.response_text))
        {
            Debug.LogError("Failed to parse LLM response");
            return "Failed to parse LLM response";
        }

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