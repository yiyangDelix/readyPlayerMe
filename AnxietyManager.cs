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

        // 可以在这里添加额外的业务逻辑
        // 例如：如果焦虑值超过阈值，触发特殊动画
        if (oldAnxiety < 0.8f && patientState.current_anxiety >= 0.8f)
        {
            Debug.Log("Patient reached extreme anxiety level!");
            // 触发特殊动画或事件
        }

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
}