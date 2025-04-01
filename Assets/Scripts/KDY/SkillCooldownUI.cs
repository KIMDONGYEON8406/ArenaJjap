using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    // 타겟 대상 컨트롤러
    public PlayerController targetController;

    // Q 슬롯 스킬 Q UI
    [Header("Q Slot")]
    public Image qFillImage;        // Q 스킬 쿨타임 이미지
    public Text qCooldownText;      // Q 스킬 쿨타임 텍스트

    // W 슬롯 스킬 W UI
    [Header("W Slot")]
    public Image wFillImage;        // W 스킬 쿨타임 이미지
    public Text wCooldownText;      // W 스킬 쿨타임 텍스트

    // E 슬롯 스킬 E UI
    [Header("E Slot")]
    public Image eFillImage;        // E 스킬 쿨타임 이미지
    public Text eCooldownText;      // E 스킬 쿨타임 텍스트

    // R 슬롯 스킬 R UI
    [Header("R Slot")]
    public Image rFillImage;        // R 스킬 쿨타임 이미지
    public Text rCooldownText;      // R 스킬 쿨타임 텍스트

    // D 슬롯 돌진/Rush UI
    [Header("D Slot (Rush)")]
    public Image dFillImage;        // 돌진 쿨타임 이미지
    public Text dCooldownText;      // 돌진 쿨타임 텍스트
    public float dCooldownDuration = 240f;   // 돌진 쿨타임 (초)
    private float dLastUsedTime = -999f;     // 마지막 사용 시각

    // F 슬롯 점멸/Flash UI
    [Header("F Slot (Flash)")]
    public Image fFillImage;        // 점멸 쿨타임 이미지
    public Text fCooldownText;      // 점멸 쿨타임 텍스트
    public float fCooldownDuration = 300f;   // 점멸 쿨타임 (초)
    private float fLastUsedTime = -999f;     // 마지막 사용 시각

    // 체력 UI 요소
    [Header("HP UI")]
    public Image hpFillImage;       // 체력바 이미지

    [Header("HP Text (Split)")]
    public Text hpLeftText;         // 현재 체력 표시 텍스트
    public Text hpRightText;        // 최대 체력 표시 텍스트

    // 최대 체력
    private float cachedMaxHP;


    void Start()
    {
        // 초기 최대 체력값 저장 (최초 1회)
        if (targetController != null && targetController.character != null)
        {
            cachedMaxHP = targetController.character.HP;
        }
    }

    void Update()
    {
        // 대상이 없거나 캐릭터가 연결 안 됐을 경우 처리 중단
        if (targetController == null || targetController.character == null)
        {
            Debug.LogWarning("targetController 또는 character가 연결되지 않았습니다.");
            return;
        }

        // 각 스킬 슬롯 UI 업데이트
        UpdateSkillUI(targetController.character.CurQCool, GetSkillMaxCooldown("qCoolDown"), qFillImage, qCooldownText, "Q");
        UpdateSkillUI(targetController.character.CurWCool, GetSkillMaxCooldown("wCoolDown"), wFillImage, wCooldownText, "W");
        UpdateSkillUI(targetController.character.CurECool, GetSkillMaxCooldown("eCoolDown"), eFillImage, eCooldownText, "E");
        UpdateSkillUI(targetController.character.CurRCool, GetSkillMaxCooldown("rCoolDown"), rFillImage, rCooldownText, "R");

        // D (돌진), F (점멸) 스킬 상태 UI 갱신
        UpdateDFCooldown("D", targetController.character.CanRush, dFillImage, dCooldownText, dCooldownDuration, ref dLastUsedTime);
        UpdateDFCooldown("F", targetController.character.CanFlash, fFillImage, fCooldownText, fCooldownDuration, ref fLastUsedTime);

        // 체력 UI 업데이트
        UpdateHPUI();

        // 테스트: 스페이스바로 데미지 주기 (디버그용)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryndamereController trynd = targetController as TryndamereController;
            if (trynd != null)
            {
                trynd.TakeDamage(200, true, 0, 0f);
            }
        }
    }

    // 스킬 쿨타임 UI 반영
    void UpdateSkillUI(float currentCool, float maxCool, Image fillImage, Text cooldownText, string label)
    {
        float ratio = Mathf.Clamp01(currentCool / maxCool);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (cooldownText != null)
            cooldownText.text = currentCool > 0 ? Mathf.CeilToInt(currentCool).ToString() : "";
    }

    // D/F 스킬 쿨다운 상태 UI 반영
    void UpdateDFCooldown(string label, bool isAvailable, Image fillImage, Text cooldownText, float cooldownDuration, ref float lastUsedTime)
    {
        if (isAvailable)
        {
            if (fillImage != null) fillImage.fillAmount = 0f;
            if (cooldownText != null) cooldownText.text = "";
            lastUsedTime = Time.time;
        }
        else
        {
            float elapsed = Time.time - lastUsedTime;
            float remain = Mathf.Clamp(cooldownDuration - elapsed, 0f, cooldownDuration);
            float ratio = remain / cooldownDuration;

            if (fillImage != null) fillImage.fillAmount = ratio;
            if (cooldownText != null) cooldownText.text = Mathf.CeilToInt(remain).ToString();
        }
    }

    // 체력 UI 갱신 (현재 체력만 반영, 최대 체력은 캐시된 값 사용)
    void UpdateHPUI()
    {
        float curHP = Mathf.Max(0f, targetController.character.CurHP);

        if (hpFillImage != null)
            hpFillImage.fillAmount = Mathf.Clamp01(curHP / cachedMaxHP);

        if (hpLeftText != null)
            hpLeftText.text = $"{(int)curHP}";

        if (hpRightText != null)
            hpRightText.text = $"/ {(int)cachedMaxHP}";
    }

    // 스킬의 최대 쿨타임 값을 리플렉션으로 가져옴
    float GetSkillMaxCooldown(string fieldName)
    {
        var field = targetController.character.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (float)field.GetValue(targetController.character);
        }
        else
        {
            Debug.LogWarning($"쿨타임 필드 {fieldName} 를 찾을 수 없습니다.");
            return 1f;
        }
    }
}
