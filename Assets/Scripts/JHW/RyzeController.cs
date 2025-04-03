using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RyzeController : PlayerController
{
    int runeCount = 0;
    [SerializeField] Animator animator;
    [SerializeField] GameObject AAObj;
    [SerializeField] GameObject QObj;
    [SerializeField] GameObject WObj;
    [SerializeField] GameObject EObj;

    public override void Update()
    {
        base.Update();
        if (agent.remainingDistance <= 0.1f)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Run") || animator.GetCurrentAnimatorStateInfo(0).IsName("RunFast"))
            {
                animator.SetTrigger("Stop");
            }
        }
        else
        {
            if (character.MoveSpeed >= 400)
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("RunFast"))
                {
                    animator.SetTrigger("RunFast");
                }
            }
            else
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
                {
                    animator.SetTrigger("Run");
                }
            }
        }
    }

    public override void Stop()
    {
        base.Stop();
        animator.SetTrigger("Stop");
    }

    public override void Move(Vector3 pos)
    {
        base.Move(pos);
    }

    public override void AutoAttack(PlayerController target)
    {
        //StartCoroutine(AAHandle());
        Vector3 look = target.transform.position;
        look.y = transform.position.y;
        transform.LookAt(look, transform.forward);
        Debug.Log("AA " + target.name);
        //평타
        float damage = character.ATK;

        if (character.CritChance >= Random.Range(0f, 1f)) //치명타
        {
            damage *= character.CritDamage;
        }
        animator.SetTrigger("AA");
        GameObject tempAA = Instantiate(AAObj, transform.position, Quaternion.identity);
        tempAA.GetComponent<RyzeAAScript>().GetDmg(target.transform, damage, character.Lethality, character.ArmorPenetration, enemyTag, character);

    }

    public override void SkillQ(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurQCool <= 0)
        {
            //대충 스킬쓰기 ?? : 그러니까 못맞추지
            animator.SetTrigger("UseQ");
            character.SetQCooldown();
            Vector3 look = location;
            look.y = transform.position.y;
            transform.LookAt(look, transform.forward);
            GameObject tempQ = Instantiate(QObj, transform.position, Quaternion.identity);
            tempQ.GetComponent<RyzeQScript>().GetDmg(155 + (0.55f * character.ATK), character.Lethality, character.ArmorPenetration, enemyTag, character);
            tempQ.transform.LookAt(location);
            tempQ.transform.rotation = Quaternion.Euler(0, tempQ.transform.rotation.eulerAngles.y, tempQ.transform.rotation.eulerAngles.z);
            UseRuneCount();
        }
        else
        {
            //못쓴다하기

            //진짜 못한다.
            Debug.Log("Q사용불가. 쿨타임: " + character.CurQCool);
        }
    }
    public override void SkillW(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.transform.position) <= 5.5f)
            {
                if (character.CurWCool <= 0)
                {
                    if (target != null && target.CompareTag(enemyTag))
                    {
                        animator.SetTrigger("UseW");
                        GameObject tempW = Instantiate(WObj, target.transform);
                        Destroy(tempW, 1.5f);
                        Vector3 look = target.transform.position;
                        look.y = transform.position.y;
                        transform.LookAt(look, transform.forward);
                        //대충 스킬쓰기

                        character.SetWCooldown();
                        character.CurQCool = 0;

                        target.character.TakeDamage(character, 200 + (0.7f * character.ATK), false, character.Lethality, character.ArmorPenetration);
                        StartCoroutine(WCCCor(target.character, target?.GetComponentInChildren<RyzeEScrpit>())); //표식 여부 확인
                        AddRuneCount();
                        Debug.Log("W사용");
                    }
                }
                else
                {
                    //못쓴다하기
                    Debug.Log("W사용불가. 쿨타임: " + character.CurWCool);
                }

            }
        }
    }

    IEnumerator WCCCor(Character target, RyzeEScrpit marked)
    {
        if (marked != null) //표식 있으면
        {
            Destroy(marked.gameObject);
            target.SetState(State.Root, 1.5f);
        }
        else
        {
            int tempSpeed = (int)(target.MoveSpeed * 0.5f);
            target.SetState(State.Slow, 1.5f);
            target.AdjustMoveSpeed(-tempSpeed);
            yield return new WaitForSeconds(1.5f);
            target.AdjustMoveSpeed(tempSpeed);
        }
    }

    public override void SkillE(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.transform.position) <= 5.5f)
            {
                if (character.CurECool <= 0)
                {
                    if (target != null && target.CompareTag(enemyTag))
                    {
                        animator.SetTrigger("UseE");
                        Vector3 look = target.transform.position;
                        look.y = transform.position.y;
                        transform.LookAt(look, transform.forward);
                        RyzeEScrpit preE = target?.GetComponentInChildren<RyzeEScrpit>();
                        if (preE != null)
                        {
                            Destroy(preE.gameObject);
                        }
                        //대충 스킬쓰기
                        target.character.TakeDamage(character, 180 + (character.ATK * 0.5f), false, character.Lethality, character.ArmorPenetration);

                        GameObject tempE = Instantiate(EObj, target.transform);
                        tempE.GetComponent<RyzeEScrpit>().SpreadE(enemyTag, EObj);
                        Destroy(tempE, 4f);

                        character.SetECooldown();
                        character.CurQCool = 0;
                        AddRuneCount();
                        Debug.Log("E사용");
                    }
                }
                else
                {
                    //못쓴다하기
                    Debug.Log("E사용불가. 쿨타임: " + character.CurECool);
                }
            }

        }
    }
    public override void SkillR(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurRCool <= 0)
        {
            //대충 스킬쓰기
            StartCoroutine(RyzeRCor());
            character.SetRCooldown();
            Debug.Log("R사용 ");
        }
        else
        {
            //못쓴다하기
            Debug.Log("R사용불가. 쿨타임: " + character.CurRCool);
        }
    }

    IEnumerator RyzeRCor()
    {
        character.AdjustLifeSteal(0.25f);
        character.AdjustAbilityHaste(80);
        character.AdjustMoveSpeed(80);
        yield return new WaitForSeconds(10f);
        character.AdjustLifeSteal(-0.25f);
        character.AdjustAbilityHaste(-80);
        character.AdjustMoveSpeed(-80);
    }

    void AddRuneCount()
    {
        if (runeCount < 2)
        {
            runeCount++;
        }
    }

    void UseRuneCount()
    {
        if (runeCount == 2)
        {
            StartCoroutine(RuneRun());
        }
        runeCount = 0;
    }

    IEnumerator RuneRun()
    {
        int tempSpeed = (int)(character.MoveSpeed * 0.44f);
        character.AdjustMoveSpeed(tempSpeed);
        yield return new WaitForSeconds(2f);
        character.AdjustMoveSpeed(-tempSpeed);
    }

    public override void Death()
    {
        base.Death();
        animator.SetTrigger("Die");

    }
}