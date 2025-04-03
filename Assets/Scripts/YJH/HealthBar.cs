using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public PlayerController target;
    public Image fillImage;
    public Vector3 offset = new Vector3(0, 0, 0); // 머리 위 위치
    public bool useLocalData = false;

    private float curHP;
    private float maxHP;

    void Update()
    {
        if (useLocalData && target != null && target.character != null)
        {
            curHP = target.character.CurHP;
            maxHP = target.character.HP;
        }

        float ratio = Mathf.Clamp01(curHP / Mathf.Max(1f, maxHP));
        fillImage.fillAmount = ratio;

        if (target != null)
        {
            transform.position = target.transform.position + offset;
            transform.forward = Camera.main.transform.forward;
        }
    }

    public void UpdateFromNetwork(float cur, float max)
    {
        curHP = cur;
        maxHP = max;
    }
}