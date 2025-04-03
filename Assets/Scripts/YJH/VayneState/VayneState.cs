using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface IVayneState
{
    public void EnterState(VayneState VS, float time = 0);
    public void UpdateState();
    public void ExitState();        
}

public class VayneState : PlayerController
{
    public static event System.Action OnAutoAttackGlobal;

    IVayneState currentState;

    public bool IsUlt = false;

    public float ultTime = 16;
    [Header("애니메이션")]
    public Animator anim;

    //마지막으로 때린놈
    GameObject lastAttackedTarget = null;
    GameObject firstHitEffect = null;
    GameObject secondHitEffect = null;

    [SerializeField] GameObject firstHit;
    [SerializeField] GameObject secondHit;
    int hitCount = 0;
    public override void Start()
    {
        base.Start();
        ChangeState(new DefaultState());        
    }

    public override void Update()
    {
        base.Update();
        currentState.UpdateState();

        if (IsUlt)
        {
            ultTime -= Time.deltaTime;
            Debug.Log(ultTime);
        }        
    }

    public void ChangeState(IVayneState newState)
    {
        if(currentState != null)
        {
            currentState.ExitState();
        }
        currentState = newState;
        currentState.EnterState(this);
    }
    public override void SkillQ(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurQCool <= 0 && !IsUlt)
        {
            Debug.Log("q눌림");
            character.SetQCooldown();
            ChangeState(new TumbleState(location));
        }
        else if (character.CurQCool <= 0 && IsUlt)
        {
            character.SetQCooldown();
            ChangeState(new UltTumbleState(location));
        }
    }
    public override void Death()
    {
        base.Death();
        anim.SetTrigger("Dead");
    }
    public override void SkillE(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurECool <= 0)
        {
            Debug.Log("E스킬사용가능");
            if (target != null && target.CompareTag(enemyTag))
            {
                anim.SetTrigger("ESkill");

                Vector3 look = target.transform.position;
                look.y = transform.position.y;
                transform.LookAt(look, transform.forward);

                // 기본 피해
                target.character.TakeDamage(character, 190 + (character.ATK * 0.5f), false, character.Lethality, character.ArmorPenetration);

                // 방향 계산
                Vector3 dir = target.transform.position - transform.position;
                dir.y = 0f; // Y 제거
                dir = dir.normalized;
                float knockbackDistance = 4.7f;
                float knockbackSpeed = 20f;

                Rigidbody targetRb = target.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    target.StartCoroutine(WallCheck(target, dir, knockbackDistance, knockbackSpeed));
                }

                //character.SetECooldown();

                
                if(currentState is UltState)
                {
                    anim.SetTrigger("UltIdle");
                }
                else if(currentState is UltTumbleState)
                {
                    anim.SetTrigger("UltTumbleIdle");
                }
                else if(currentState is TumbleState)
                {
                    anim.SetTrigger("TumbleIdle");
                }
                else
                {
                    anim.SetTrigger("Idle");
                }
            }
        }
    }

    public override void SkillR(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurRCool <= 0)
        {            
            character.SetRCooldown();
            ChangeState(new UltState());
        }
    }      

    public override void AutoAttack(PlayerController target)
    {
        Vector3 look = target.transform.position;
        look.y = transform.position.y;
        transform.LookAt(look, transform.forward);
        anim.SetTrigger("Attack");
        if(currentState is TumbleState || currentState is UltTumbleState)
        {
            character.AdjustATK(115.14f);
        }
        OnAutoAttackGlobal?.Invoke();
        base.AutoAttack(target);

        VayneWSkill(target);
    }
    

    public void VayneWSkill(PlayerController target)
    {
        GameObject currentTarget = target.gameObject;

        if (lastAttackedTarget != currentTarget)
        {
            if (firstHitEffect != null)
            {
                Destroy(firstHitEffect);
            }
            if (secondHitEffect != null)
            {
                Destroy(secondHitEffect);
            }

            hitCount = 1;
            firstHitEffect = Instantiate(firstHit, target.transform);
            secondHitEffect = null;
            lastAttackedTarget = currentTarget;
            return;
        }

        // 같은 대상이면 히트 누적
        hitCount++;

        if (hitCount == 2)
        {
            if (secondHitEffect != null)
            {
                Destroy(secondHitEffect);
            }
            secondHitEffect = Instantiate(secondHit, target.transform);
        }
        else if (hitCount == 3)
        {
            target.character.TakeDamage(character, target.character.HP * 0.1f, true, 0, 0);
            Debug.Log("추뎀적용");

            // 초기화
            hitCount = 0;
            if (firstHitEffect != null) Destroy(firstHitEffect);
            if (secondHitEffect != null) Destroy(secondHitEffect);
            firstHitEffect = null;
            secondHitEffect = null;
            lastAttackedTarget = null;
        }
    }
    private IEnumerator WallCheck(PlayerController target, Vector3 dir, float distance, float speed)
    {
        float pushed = 0f;
        float delta;

        Rigidbody rb = target.GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;

        while (pushed < distance)
        {
            delta = speed * Time.fixedDeltaTime;
            Vector3 move = dir * delta;

            // 벽 충돌 체크
            if (Physics.Raycast(target.transform.position, dir, out RaycastHit hit, 0.5f))
            {
                if (hit.collider.CompareTag("Wall"))
                {
                    target.character.SetState(State.Stun,1.5f);
                    target.character.TakeDamage(character, 285 + (character.ATK * 0.75f), false, character.Lethality, character.ArmorPenetration);
                    yield return null;
                }
            }

            rb.MovePosition(rb.position + move);
            pushed += delta;
            yield return new WaitForFixedUpdate();
        }
    }
}
