using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TryndamereController : PlayerController
{
    [SerializeField] private float attackCooldown = 0.3f; // 후딜 시간
    //private bool canAttack = true;

    // 애니메이션 및 상태 관련 변수
    public GameObject healEffectPrefab; // Q 스킬 사용 시 힐 이펙트 프리팹
    public GameObject rEffectObject; // R스킬 사용 시 파티클 
    private Animator anim; // 애니메이터
    private bool isMoving = false; // 이동 상태 확인
    public GameObject slowEffectPrefab;


    // 전투 관련 변수
    public int rage = 0; // 현재 분노 수치
    public int maxRage = 100; // 최대 분노 수치
    private float critChance = 0.25f; // 기본 치명타 확률 (25%)

    // E 스킬 관련 변수
    private bool isDashing = false; // 돌진 여부
    private Vector3 eTargetPosition; // 목표 위치 저장
    public float eSpeed = 10f; // 이동 속도

    // R 스킬 관련 변수
    private bool isImmortal = false; // 무적 상태 여부
    public float rDuration = 5f; // R 스킬 지속 시간

    // W 스킬 관련 변수
    float skillRange = 50.5f; // W 스킬 범위
    float attackReductionPercent = 0.2f; // 공격력 감소율 (20%)
    float moveSlowAmount = 0.4f; // 이동 속도 감소율
    float effectDuration = 4f; // 효과 지속 시간 (공격력 & 이동 속도 복구 시간)

    // 네비게이션 에이전트 (이동 관련)
    private NavMeshAgent navMeshAgent;


    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    // 이동 처리 함수
    public override void Move(Vector3 pos)
    {
        base.Move(pos);
        if (anim == null) anim = GetComponentInChildren<Animator>(); //  애니메이터 가져오기 (필요할 때만)
        anim.SetFloat("isMovingBlend", 1f); //  이동 애니메이션 적용

        isMoving = true;
        StartCoroutine(CheckIfStopped());
    }

    // 이동 멈춤 감지
    private IEnumerator CheckIfStopped()
    {
        yield return new WaitForSeconds(0.1f); // 이동이 시작된 후 잠시 대기

        Vector3 lastPosition = transform.position;

        while (isMoving)
        {
            yield return new WaitForSeconds(0.1f); //  일정 시간마다 이동 여부 확인

            //  캐릭터의 위치가 변하지 않으면 이동 종료 (즉, Idle 상태로 전환)
            if (Vector3.Distance(lastPosition, transform.position) < 0.01f)
            {
                anim.SetFloat("isMovingBlend", 0f); //  Idle 애니메이션 실행
                isMoving = false;
                yield break;
            }

            lastPosition = transform.position; //  이전 위치 업데이트
        }
    }



    //기본 공격 실행
    public override void AutoAttack(PlayerController target)
    {
        //  변경: 사거리 비교 추가
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > character.Range * 0.01f) return;

        base.AutoAttack(target);

        if (anim == null) anim = GetComponentInChildren<Animator>();
        PerformAttack(); // → 애니메이션 + 분노 증가 둘 다 여기서 처


        //StartCoroutine(AttackDelayReset()); // 후딜 도중 평캔 가능
    }

    // 트린다미어 Q스킬 - 체력회복
    public override void SkillQ(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurQCool <= 0 && rage > 0)
        {
            // 평캔 처리: 평타 후딜 캔슬
            //StopAllCoroutines();
            //canAttack = true;

            Debug.Log("QQ");
            int healAmount = rage / 2;
            character.Heal(healAmount);
            rage = 0;

            if (healEffectPrefab != null)
            {
                GameObject healEffect = Instantiate(healEffectPrefab, transform.position, Quaternion.identity);
                healEffect.transform.SetParent(transform); //  캐릭터에 붙이기
                healEffect.SetActive(true); // 비활성화되어 있다면 활성화
                Destroy(healEffect, 2f); //  2초 후 삭제
            }
            if (anim == null) anim = GetComponentInChildren<Animator>();
            anim.SetTrigger("UseQ");

            character.SetQCooldown();
        }    
        
    }

    // 트린다미어 W스킬 - 적 공격력이랑 이속 감소
    public override void SkillW(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        //base.SkillW( isTargeting,  isChanneling,  target,  location);
        if (character.CurWCool <= 0)
        {

            //StopAllCoroutines();
            //canAttack = true;

            //대충 스킬쓰기
            Collider[] targets = Physics.OverlapSphere(transform.position, skillRange, LayerMask.GetMask("Enemy"));

            foreach (Collider targetCollider in targets)
            {

                PlayerController targetPlayer = targetCollider.GetComponent<PlayerController>();

                if (targetPlayer == null || targetPlayer == this) continue;

                //  공격력 감소 적용
                float attackReduction = targetPlayer.character.ATK * attackReductionPercent;
                targetPlayer.character.AdjustATK(-attackReduction);
                StartCoroutine(RestoreAttackPower(targetPlayer, attackReduction, effectDuration));

                // 등 돌린 경우에만 슬로우 적용 + 로그 출력
                if (IsEnemyFacingAway(targetPlayer.transform))
                {
                    StartCoroutine(SlowCharacter(targetPlayer, moveSlowAmount, effectDuration));
                }
            }
               anim?.SetTrigger("UseW");
               character.SetWCooldown();
        }
    }

    //  공격력 4초 후 감소 해제
    private IEnumerator RestoreAttackPower(PlayerController target, float amount, float duration)
    {
        yield return new WaitForSeconds(duration);
        target.character.AdjustATK(amount);
    }

    //  이동 속도 4초 후 감소 해제
    private IEnumerator SlowCharacter(PlayerController target, float slowAmount, float duration)
    {
        if (target == null) yield break;

        // 1️ 이펙트 생성 (고정 위치)
        GameObject slowEffect = null;
        if (slowEffectPrefab != null)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * 1.8f;
            slowEffect = Instantiate(slowEffectPrefab, spawnPos, Quaternion.identity);
            slowEffect.transform.SetParent(target.transform); // 캐릭터 따라다님
            Destroy(slowEffect, 4f); // 4초 뒤 삭제
        }

        // 2️ 이동속도 감소 적용
        target.character.SetCanRush(false);
        int tempSpeed = (int)(target.character.MoveSpeed * slowAmount);
        target.character.AdjustMoveSpeed(-tempSpeed);

        yield return new WaitForSeconds(duration);

        // 3️ 원래 속도 복구
        target.character.AdjustMoveSpeed(tempSpeed);
        target.character.SetCanRush(true);

        // 4️ 이펙트 제거
        if (slowEffect != null)
        {
            Destroy(slowEffect);
        }
    }

    private bool IsEnemyFacingAway(Transform target)
    {
        Vector3 directionToEnemy = (transform.position - target.position).normalized;
        directionToEnemy.y = 0;

        float dot = Vector3.Dot(target.forward, directionToEnemy);
        Debug.Log($"[W 사용] {target.name} Dot: {dot} → {(dot < 0 ? "등 돌림" : "정면")}");

        return dot < 0;
    }

    // 트린다미어 E스킬 - 돌진공격
    public override void SkillE(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurECool <= 0 && !isDashing)
        {
            // 평캔 처리: 평타 후딜 캔슬
            //StopAllCoroutines();
            //canAttack = true;

            eTargetPosition = location;
            isDashing = true;

            if (anim == null) anim = GetComponentInChildren<Animator>();
            anim.SetTrigger("UseE");

            StartCoroutine(SmoothDash());
            character.SetECooldown();
        }
    }

    private IEnumerator SmoothDash()
    {
        Vector3 dir = (eTargetPosition - transform.position).normalized;

        float dashDuration = 0.7f; //  애니메이션 시간에 맞춰 조절 (예: 0.45초)
        float elapsed = 0f;

        transform.forward = dir;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.updatePosition = false;
        }

        while (elapsed < dashDuration)
        {
            transform.position += dir * eSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        //  도착한 그 자리에서 강제 위치 동기화
        if (navMeshAgent != null)
        {
            navMeshAgent.Warp(transform.position);
            navMeshAgent.updatePosition = true;
            navMeshAgent.isStopped = false;
        }

    }


    // R 스킬 (불사의 분노) - 일정 시간 동안 무적 상태 유지
    public override void SkillR(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurRCool <= 0)
        {
            if (isImmortal) return; // 이미 무적이면 실행 안 함

            isImmortal = true; // 무적 상태 활성화
            character.SetState(State.Invincible,5f); // 캐릭터 상태를 무적으로 설정

            if (anim == null) anim = GetComponentInChildren<Animator>();
            anim.SetTrigger("UseR"); // 애니메이션 실행
            rEffectObject.SetActive(true); // 궁극기 이펙트 활성화

            StartCoroutine(EndRSkill()); // 5초 후 무적 해제 및 이펙트 제거

            character.SetRCooldown();
        }
    }

    //  무적 상태 해제 + 이펙트 종료
    private IEnumerator EndRSkill()
    {
        yield return new WaitForSeconds(5f); // 무적 지속 시간

        isImmortal = false; // 무적 해제

        if (rEffectObject != null)
        {
            rEffectObject.SetActive(false); // 궁극기 이펙트 종료
        }

    }


    // 피해 처리 (무적 상태일 경우 피해를 받지 않음)
    public void TakeDamage(float damage, bool isTrueDamage, float lethality, float armorPenetration)
    {
        if (isImmortal)
        {
            return; // 무적 상태일 경우 피해 무시
        }

        // 체력 감소 처리
        character.AdjustHP(-damage);

        // 무적 상태일 경우 최소 체력 1 유지
        if (isImmortal && character.CurHP <= 1)
        {
            character.AdjustHP(1 - character.CurHP);
        }
    }

    // 기본 공격 실행 (치명타 여부 확인 및 애니메이션 실행)
    public void PerformAttack()
    {
        bool isCritical = Random.value < critChance; // 치명타 확률 판정

        GainRage(isCritical); // 분노 증가 처리

        if (anim == null) anim = GetComponentInChildren<Animator>();

        // 애니메이션 실행
        anim.SetTrigger(isCritical ? "CriticalHit" : "Attack");

    }


    // 분노 증가 처리 (치명타 발생 시 추가 증가)
    private void GainRage(bool isCritical)
    {
        int rageGain = isCritical ? 10 : 5;
        rage += rageGain;
        rage = Mathf.Clamp(rage, 0, maxRage);

    }

    //private IEnumerator AttackDelayReset()
    //{
    //    canAttack = false;
    //    yield return new WaitForSeconds(attackCooldown); // 딜레이 후 공격 가능
    //    canAttack = true;
    //}
}
