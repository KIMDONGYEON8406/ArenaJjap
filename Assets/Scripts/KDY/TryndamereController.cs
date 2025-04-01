using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TryndamereController : PlayerController
{
    // 상태 및 애니메이션 관련 변수
    public GameObject healEffectPrefab;      // Q 스킬: 힐 이펙트 프리팹
    public GameObject rEffectObject;         // R 스킬: 무적 파티클 오브젝트
    public GameObject slowEffectPrefab;      // W 스킬: 슬로우 이펙트 프리팹
    private Animator anim;                   // 애니메이터 컨트롤용
    private bool isMoving = false;           // 현재 이동 중인지 여부 판단

    // 전투 및 분노 관련 변수
    public int rage = 0;                     // 현재 분노 수치
    public int maxRage = 100;                // 최대 분노 수치
    private float critChance = 0.25f;        // 기본 치명타 확률 (25%)

    // E 스킬 (스핀 슬래시) 관련 변수
    private bool isDashing = false;          // 현재 E 스킬 돌진 중 여부
    private Vector3 eTargetPosition;         // 돌진 목표 위치
    public float eSpeed = 10f;               // 돌진 이동 속도

    // R 스킬 (불사의 분노) 관련 변수
    private bool isImmortal = false;         // 무적 상태 여부
    public float rDuration = 5f;             // 무적 지속 시간 (초)

    //  W 스킬 (모킹 샤우트) 관련 변수
    float skillRange = 50.5f;                // 스킬 발동 범위
    float attackReductionPercent = 0.2f;     // 적 공격력 감소 비율 (20%)
    float moveSlowAmount = 0.4f;             // 적 이동 속도 감소 비율 (40%)
    float effectDuration = 4f;               // 효과 지속 시간 (초)

    // 이동 관련 변수
    private NavMeshAgent navMeshAgent;       // 유닛 이동 제어용 네비메시 에이전트

    // 기본 공격 쿨타임 관련
    //[SerializeField] private float attackCooldown = 0.3f;  // 기본 공격 후 후딜 타임
    //private bool canAttack = true;                         // 공격 가능 여부


    // 컴포넌트 초기화 (Awake에서 NavMeshAgent 연결)
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
    }

    private void CancelAttackDelay()
    {
        StopCoroutine("AAHandle");
        canAA = true;
    }


    // 목표 위치로 이동
    public override void Move(Vector3 pos)
    {
        base.Move(pos);

        anim.SetFloat("isMovingBlend", 1f); // 이동 중 애니메이션 설정
        isMoving = true;

        StartCoroutine(CheckIfStopped()); // 이동 상태 확인 코루틴 시작
    }

    // 캐릭터가 멈췄는지 주기적으로 확인
    private IEnumerator CheckIfStopped()
    {
        yield return new WaitForSeconds(0.1f); // 초깃값 안정화 대기

        Vector3 lastPosition = transform.position;

        while (isMoving)
        {
            yield return new WaitForSeconds(0.1f); // 0.1초마다 위치 비교

            // 이전 위치와 현재 위치가 거의 같으면 정지 상태로 간주
            if (Vector3.Distance(lastPosition, transform.position) < 0.01f)
            {
                anim.SetFloat("isMovingBlend", 0f); // 정지 애니메이션
                isMoving = false;
                yield break;
            }

            lastPosition = transform.position; // 위치 갱신
        }
    }

    // 기본 공격 실행
    public override void AutoAttack(PlayerController target)
    {
        // 사거리 내에 있을 경우에만 공격 실행
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > character.Range * 0.01f) return;

        base.AutoAttack(target); // 기본 공격 로직 실행
        PerformAttack(); // 공격 애니메이션 및 분노 증가 처리

        Debug.Log($"AutoAttack 실행! 대상: {target?.name}, 현재 분노: {rage}");

        // 후딜 캔슬 가능 부분 (현재 비활성화)
        // StartCoroutine(AttackDelayReset());
    }

    // 트린다미어 Q 스킬 - 분노를 소모하여 체력 회복
    public override void SkillQ(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        // Q 쿨타임이 없고, 분노가 1 이상일 때 사용 가능
        if (character.CurQCool <= 0 && rage > 0)
        {
            CancelAttackDelay(); // 평캔

            // 체력 회복량 계산: 분노의 절반
            int healAmount = rage / 2;
            Debug.Log($"[Q Heal] rage={rage} → healAmount={rage / 2}");
            
            character.Heal(healAmount); // 체력 회복
            rage = 0; // 분노 초기화
            Debug.Log($"Q 스킬 사용! 체력 {healAmount} 회복");

            // 힐 이펙트 생성 및 2초 후 제거
            if (healEffectPrefab != null)
            {
                GameObject healEffect = Instantiate(healEffectPrefab, transform.position, Quaternion.identity);
                healEffect.transform.SetParent(transform);
                healEffect.SetActive(true);
                Destroy(healEffect, 2f);

            }

            anim.SetTrigger("UseQ"); // Q 사용 애니메이션 실행
            character.SetQCooldown(); // 쿨타임 설정


            Debug.Log("Q사용");
        }
    }

    // 트린다미어 W 스킬 - 적의 공격력과 이동 속도 감소
    public override void SkillW(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        Debug.Log(character.CurWCool + "w쿨");

        if (character.CurWCool <= 0)
        {
            CancelAttackDelay(); // 평캔

            // 스킬 범위 내 적 유닛 탐색
            Collider[] targets = Physics.OverlapSphere(transform.position, skillRange, LayerMask.GetMask("Enemy"));
            Debug.Log($"W 스킬 사용! 대상 검색 중... 감지된 개수: {targets.Length}");

            foreach (Collider targetCollider in targets)
            {
                PlayerController targetPlayer = targetCollider.GetComponent<PlayerController>();

                // 자기 자신이거나 유효하지 않은 대상은 건너뜀
                if (targetPlayer == null || targetPlayer == this) continue;

                // 공격력 감소 적용
                float attackReduction = targetPlayer.character.ATK * attackReductionPercent;
                targetPlayer.character.AdjustATK(-attackReduction);
                StartCoroutine(RestoreAttackPower(targetPlayer, attackReduction, effectDuration));

                // 대상이 등 돌리고 있을 경우 이동속도 감소 적용
                if (IsEnemyFacingAway(targetPlayer.transform))
                {
                    StartCoroutine(SlowCharacter(targetPlayer, moveSlowAmount, effectDuration));
                    Debug.Log($"{targetPlayer.name} 이동 속도 감소 적용!");
                }
            }

            anim?.SetTrigger("UseW");      // 애니메이션 실행
            character.SetWCooldown();      // 쿨타임 설정
            Debug.Log("W사용");
        }
    }

    // 공격력 감소를 일정 시간 후 원상복구
    private IEnumerator RestoreAttackPower(PlayerController target, float amount, float duration)
    {
        yield return new WaitForSeconds(duration);
        target.character.AdjustATK(amount); // 공격력 복구
        Debug.Log($"{target.name} ATK 복구 완료! (+{amount})");
    }

    // 이동속도 감소 적용 및 지속 시간 후 원복
    private IEnumerator SlowCharacter(PlayerController target, float slowAmount, float duration)
    {
        if (target == null) yield break;

        GameObject slowEffect = null;
        Debug.Log("이펙트생성");

        // 이펙트 생성 및 붙이기
        if (slowEffectPrefab != null)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * 1.8f;
            slowEffect = Instantiate(slowEffectPrefab, spawnPos, Quaternion.identity);
            slowEffect.transform.SetParent(target.transform);
            Destroy(slowEffect, 4f);
        }

        // 이동 속도 감소 적용
        target.character.SetCanRush(false);
        int tempSpeed = (int)(target.character.MoveSpeed * slowAmount);
        target.character.AdjustMoveSpeed(-tempSpeed);
        Debug.Log($"{target.name} 이동 속도 감소 시작! (-{tempSpeed})");

        yield return new WaitForSeconds(duration);

        // 이동 속도 복구
        target.character.AdjustMoveSpeed(tempSpeed);
        target.character.SetCanRush(true);
        Debug.Log($"{target.name} 이동 속도 복구 완료!");

        if (slowEffect != null)
        {
            Destroy(slowEffect);
        }
    }

    // 적이 등 돌리고 있는지 판단 (W 슬로우 조건)
    private bool IsEnemyFacingAway(Transform target)
    {
        Vector3 directionToEnemy = (transform.position - target.position).normalized;
        directionToEnemy.y = 0;

        float dot = Vector3.Dot(target.forward, directionToEnemy);
        Debug.Log($"[W 사용] {target.name} Dot: {dot} → {(dot < 0 ? "등 돌림" : "정면")}");

        return dot < 0;
    }

    // 트린다미어 E 스킬 - 지정 위치로 돌진하는 스킬
    public override void SkillE(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        // 쿨타임이 없고 돌진 중이 아닐 때만 실행
        if (character.CurECool <= 0 && !isDashing)
        {
            CancelAttackDelay(); // 평캔

            eTargetPosition = location;
            isDashing = true;

            anim.SetTrigger("UseE"); // 돌진 애니메이션 실행
            StartCoroutine(SmoothDash()); // 자연스러운 이동 시작
            character.SetECooldown();     // 쿨타임 설정

            Debug.Log("E 사용 - 자연스러운 돌진 시작");
        }
    }

    // 일정 시간 동안 방향으로 돌진하는 코루틴
    private IEnumerator SmoothDash()
    {
        Vector3 dir = (eTargetPosition - transform.position).normalized;
        float dashDuration = 0.7f; // 돌진 시간 (애니메이션 길이에 따라 조절)
        float elapsed = 0f;

        transform.forward = dir; // 캐릭터 방향 설정

        // 네비게이션 이동 중단 및 수동 제어로 전환
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.updatePosition = false;
        }

        // 일정 시간 동안 지정 방향으로 이동
        while (elapsed < dashDuration)
        {
            transform.position += dir * eSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        // 도착 위치에 강제로 위치 동기화 (NavMesh 재개)
        if (navMeshAgent != null)
        {
            navMeshAgent.Warp(transform.position);
            navMeshAgent.updatePosition = true;
            navMeshAgent.isStopped = false;
        }

        Debug.Log("E 돌진 (애니 길이 기반) 종료");
    }

    // R 스킬 (불사의 분노) - 일정 시간 동안 무적 상태 유지
    public override void SkillR(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurRCool <= 0)
        {
            CancelAttackDelay(); // 평캔

            if (isImmortal) return; // 이미 무적 상태면 중복 실행 방지

            isImmortal = true; // 무적 상태 적용
            character.SetState(State.Invincible); // 상태 설정

            anim.SetTrigger("UseR");      // 궁극기 애니메이션
            rEffectObject.SetActive(true); // 이펙트 표시

            StartCoroutine(EndRSkill());  // 지속 시간 후 상태 복구
            character.SetRCooldown();     // 쿨타임 시작

            Debug.Log("R 사용");
        }
    }

    // R 스킬 종료 처리 - 무적 상태 해제 및 이펙트 제거
    private IEnumerator EndRSkill()
    {
        yield return new WaitForSeconds(5f); // 무적 지속 시간

        isImmortal = false;
        character.SetState(State.Neutral); // 원래 상태로 복귀

        if (rEffectObject != null)
        {
            rEffectObject.SetActive(false); // 이펙트 제거
        }

        Debug.Log("R 스킬 종료, 무적 해제 및 이펙트 끔");
    }

    // 피해 처리 함수 - 무적 상태일 경우 피해 무시
    public void TakeDamage(float damage, bool isTrueDamage, float lethality, float armorPenetration)
    {
        if (isImmortal)
        {
            return;
        }

        character.AdjustHP(-damage); // 피해 적용
    }

    // 기본 공격 실행 - 치명타 판정 및 애니메이션 실행
    public void PerformAttack()
    {
        bool isCritical = Random.value < critChance;

        GainRage(isCritical); // 분노 증가

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        anim.SetTrigger(isCritical ? "CriticalHit" : "Attack");

        Debug.Log(isCritical ? "치명타 공격!" : "일반 공격!");
    }

    // 분노 증가 처리 - 치명타일 경우 더 많은 분노 획득
    private void GainRage(bool isCritical)
    {
        int rageGain = isCritical ? 10 : 5;
        rage += rageGain;
        rage = Mathf.Clamp(rage, 0, maxRage);

        Debug.Log($"분노 증가: {rageGain}, 현재 분노: {rage}");
    }

    // 공격 후딜 쿨다운 처리 (현재 미사용 상태)
    // private IEnumerator AttackDelayReset()
    // {
    //     canAttack = false;
    //     yield return new WaitForSeconds(attackCooldown);
    //     canAttack = true;
    // }

}
