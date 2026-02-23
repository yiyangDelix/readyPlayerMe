// 这个类：LLM返回格式定义，包含患者回应文本、焦虑值变化、焦虑等级和是否理解医生话语等字段，供LLMService解析LLM响应时使用。
[Serializable]
public class PatientResponse
{
    public string response_text;
    public float anxiety_delta;
    public string anxiety_level;
    public bool understands;
}