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

    public void UpdateAnxiety(float deltaFromLLM, string anxietyLevelFromLLM,
                              string doctorSpeech, string patientSpeech, bool understands)
    {
        if (patientState == null)
        {
            Debug.LogError("PatientState not initialized!");
            return;
        }

        // 更新状态
        patientState.UpdateState(deltaFromLLM, doctorSpeech, patientSpeech, understands);

        // 更新动画
        UpdateAvatarAnimation(anxietyLevelFromLLM);
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

    private void UpdateAvatarAnimation(string anxietyLevelFromLLM)
    {
        if (avatarAnimator == null) return;

        // 使用LLM返回的文字等级
        switch (anxietyLevelFromLLM)
        {
            case "none":
                avatarAnimator.SetFloat("AnxietyBlend", 0.0f);
                break;
            case "mild":
                avatarAnimator.SetFloat("AnxietyBlend", 0.33f);
                break;
            case "significant":
                avatarAnimator.SetFloat("AnxietyBlend", 0.66f);
                break;
            case "extreme":
                avatarAnimator.SetFloat("AnxietyBlend", 1.0f);
                break;
        }

        // 同时传入实际数值
        avatarAnimator.SetFloat("AnxietyValue", patientState?.current_anxiety ?? 0.5f);
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
}