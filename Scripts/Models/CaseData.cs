using System;
using UnityEngine;


// 这个类：映射JSON病例文件的结构，包含患者的基本信息、人格特质参数、初始焦虑值和症状描述等字段，供LLMService加载和使用。
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