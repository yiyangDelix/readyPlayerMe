
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