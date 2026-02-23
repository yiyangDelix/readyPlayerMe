
// 这个类：定义患者的人格特质参数，包括焦虑衰减速率和回应长度范围，供LLMService在生成对话提示词时使用。

[Serializable]
public class PersonalityParams
{
    public float base_anxiety_decay;
    public int response_length_min;
    public int response_length_max;
}