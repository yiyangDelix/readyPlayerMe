public class AnxietyManager : MonoBehaviour
{
    [SerializeField] private float currentAnxiety;
    [SerializeField] private float minAnxiety = 0f;
    [SerializeField] private float maxAnxiety = 1f;
    
    // 从LLM返回的JSON中获取delta并更新
    public void UpdateAnxiety(float deltaFromLLM)
    {
        // 可以在这里添加额外的逻辑调整
        // 比如：如果医生说了某些关键词，可以额外调整
        
        currentAnxiety = Mathf.Clamp(currentAnxiety + deltaFromLLM, minAnxiety, maxAnxiety);
        
        // 触发动画更新
        UpdateAvatarAnimation();
    }
    
    // 根据焦虑程度驱动动画
    private void UpdateAvatarAnimation()
    {
        // 0-0.3: 放松姿态
        // 0.3-0.6: 轻微紧张（搓手、坐立不安）
        // 0.6-0.8: 明显焦虑（身体紧缩、频繁小动作）
        // 0.8-1.0: 极度焦虑（颤抖、回避眼神）
        
        animator.SetFloat("AnxietyLevel", currentAnxiety);
    }
    
    // 也可以让LLM返回emotion，做更精细的控制
    public void SetEmotion(string emotion)
    {
        switch(emotion)
        {
            case "worried": animator.SetTrigger("Worried"); break;
            case "confused": animator.SetTrigger("Confused"); break;
            case "relieved": animator.SetTrigger("Relieved"); break;
        }
    }
}
