public class LLMService : MonoBehaviour
{
    public CaseData currentCase;
    public AnxietyManager anxietyManager;
    
    public async Task<string> GetPatientResponse(string doctorSpeech)
    {
        // 构建Prompt
        string prompt = BuildPrompt(doctorSpeech);
        
        // 调用LLM API
        string jsonResponse = await CallLLM(prompt);
        
        // 解析JSON
        PatientResponse response = JsonUtility.FromJson<PatientResponse>(jsonResponse);
        
        // 更新焦虑程度（核心逻辑！）
        anxietyManager.UpdateAnxiety(response.anxiety_delta);
        
        // 根据emotion触发动画
        anxietyManager.SetEmotion(response.emotion);
        
        // 返回患者说的话
        return response.response_text;
    }
    
    private string BuildPrompt(string doctorSpeech)
    {
        // 从JSON病例注入当前信息
        return $@"
[系统提示词...]

当前患者档案：
身体状况：{currentCase.symptoms}
性格类型：{currentCase.personality}
当前焦虑程度：{anxietyManager.GetCurrentAnxiety()}

医生刚才说：{doctorSpeech}

请按照格式返回JSON...";
    }
}
