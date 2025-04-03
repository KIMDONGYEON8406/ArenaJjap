using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System;
using Photon.Realtime;
using UnityEngine.SocialPlatforms;

[Serializable]
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour, IPunObservable
{
    public Character character;
    public string enemyTag;
    protected NavMeshAgent agent;
    public PhotonView pv;

    [Header("Q")]
    public int qRange;
    public float qDelay;
    public bool qTarget;
    public bool qChannel;

    [Header("W")]
    public int wRange;
    public float wDelay;
    public bool wTarget;
    public bool wChannel;

    [Header("E")]
    public int eRange;
    public float eDelay;
    public bool eTarget;
    public bool eChannel;

    [Header("R")]
    public int rRange;
    public float rDelay;
    public bool rTarget;
    public bool rChannel;


    protected Queue<CommandBase> toExecute;
    protected CommandBase curCommand;
    protected Coroutine curCommandCor;

    Ray ray;
    RaycastHit hit;
    protected bool canAA = true;

    protected bool canCancel = true;

    // Start is called before the first frame update
    public virtual void Start()
    {
        toExecute = new Queue<CommandBase>();
        curCommand = new CommandBase(this, 0);
        Character temp = character;
        character = ScriptableObject.CreateInstance<Character>();
        character.InitCharacter(temp);
        pv = GetComponent<PhotonView>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = character.MoveSpeed * 0.01f;
        agent.angularSpeed = 100000;
        agent.acceleration = 100000;
        character.OnMoveSpeedChanged += ApplyMoveSpeed;
        character.OnTakeDamage += ApplyDamage;
        character.OnHeal += ApplyHeal;
        character.OnStateChanged += ApplyState;
        character.OnDie += Death;

        //Cursor.SetCursor(cursorTexture, new Vector2(0.5f, 0.5f), CursorMode.Auto);
        //StartCoroutine(HpRegen());
        StartCoroutine(Execution());
        if (pv.IsMine)
        {
            Camera.main.GetComponent<InGameCamera>().player = gameObject;
            Camera.main.GetComponent<InGameCamera>().Init(gameObject);
            PhotonNetwork.LocalPlayer.TagObject = this.gameObject;
        }
    }

    [PunRPC]
    public void SetMyTag(int tag)
    {
        if (tag == 1)
        {
            gameObject.tag = "Red";
            enemyTag = "Blue";
            gameObject.layer = LayerMask.NameToLayer("Red");
        }
        else if (tag == 2)
        {
            gameObject.tag = "Blue";
            enemyTag = "Red";
            gameObject.layer = LayerMask.NameToLayer("Blue");
        }
    }


    private void OnDestroy()
    {
        character.OnMoveSpeedChanged -= ApplyMoveSpeed;
        character.OnTakeDamage -= ApplyDamage;
        character.OnHeal -= ApplyHeal;
        character.OnStateChanged -= ApplyState;
        character.OnDie -= Death;
    }

    IEnumerator Execution()
    {
        while (true)
        {
            yield return new WaitUntil(() => canCancel);
            yield return new WaitUntil(() => toExecute.Count > 0);
            Debug.Log("tete: " + toExecute.Count);
            if (curCommandCor != null)
            {
                StopCoroutine(curCommandCor);
                curCommandCor = null;
            }
            if (curCommand != null)
            {
                curCommand.Cancel();
            }
            if (toExecute.Count > 0)
            {
                curCommand = toExecute?.Dequeue();
            }
            if (curCommand != null)
            {
                if (curCommand is SKillCommands)
                {
                    SKillCommands tempCommand = (SKillCommands)curCommand;
                    if (tempCommand.isTargeting == true)
                    {
                        curCommandCor = StartCoroutine(MoveToUse(false, tempCommand.range * 0.01f, tempCommand.GetTargetTransform()));
                    }
                    else
                    {
                        pv.RPC("Executer", RpcTarget.All);
                        if (curCommand.Delay > 0)
                        {
                            StartCoroutine(Delay(curCommand.Delay));
                        }
                    }
                }
                else if (curCommand is AutoAttackCommand)
                {
                    curCommandCor = StartCoroutine(AACoroutine(((AutoAttackCommand)curCommand).GetTargetTransform(), character.Range * 0.01f));
                }
                else
                {
                    //curCommand.Execute();
                    pv.RPC("Executer", RpcTarget.All);
                    if (curCommand.Delay > 0)
                    {
                        StartCoroutine(Delay(curCommand.Delay));
                    }
                }
            }
        }
    }

    protected IEnumerator MoveToUse(bool loop, float range, Transform target)
    {
        while (Vector3.Distance(transform.position, target.position) > range)
        {
            yield return null;
            agent.SetDestination(target.position);
        }
        agent.ResetPath();
        pv.RPC("Executer", RpcTarget.All);
        if (curCommand.Delay > 0)
        {
            StartCoroutine(Delay(curCommand.Delay));
        }
    }

    protected IEnumerator AACoroutine(Transform target, float range)
    {
        while (true)
        {
            yield return null;
            if (Vector3.Distance(transform.position, target.position) > range)
            {
                agent.SetDestination(target.position);
            }
            else if (canAA)
            {
                StartCoroutine(AAHandle());
                agent.ResetPath();
                pv.RPC("Executer", RpcTarget.All);
                if (curCommand.Delay > 0)
                {
                    StartCoroutine(Delay(curCommand.Delay));
                }
            }
        }
    }

    public void ApplyDamage(float value, bool trueDamage, int lethal, float armorPen)
    {
        pv.RPC("DamageRPC", RpcTarget.OthersBuffered, value, trueDamage, lethal, armorPen);
    }

    [PunRPC]
    public void DamageRPC(float value, bool trueDamage, int lethal, float armorPen)
    {
        character.TakeDamage(null, value, trueDamage, lethal, armorPen);
    }

    public void ApplyHeal(float value)
    {
        pv.RPC("HealRPC", RpcTarget.OthersBuffered, value);
    }

    [PunRPC]
    public void HealRPC(float value)
    {
        character.Heal(value);
    }

    public void ApplyState()
    {
        if(character.CurState != State.Neutral)
        {
            pv.RPC("StateRPC", RpcTarget.OthersBuffered, character.CurState, character.stateDict[character.CurState]);
        }
        else
        {
            pv.RPC("StateRPC", RpcTarget.OthersBuffered, character.CurState, 0);
        }
    }

    [PunRPC]
    public void StateRPC(State state, float time)
    {
        character.SetState(state, time);
    }

    [PunRPC]
    public void Executer()
    {
        //CommandBase command = DeserializeCommandInfo(stream);
        //command.Execute();
        curCommand?.Execute();
    }

    [PunRPC]
    public void AAEnququer(int targetId)
    {
        toExecute.Enqueue(new AutoAttackCommand(this, 0, targetId));
    }

    [PunRPC]
    public void SkillEnqueuer(int type, float delay, bool isTarget, bool isChannel, int viewId, Vector3 point)
    {
        switch (type) //1: q, 2: w, 3: e, 4:r
        {
            case 1:
                toExecute.Enqueue(new SkillQCommand(this, delay, isTarget, isChannel, qRange, viewId, point));
                break;
            case 2:
                toExecute.Enqueue(new SkillWCommand(this, delay, isTarget, isChannel, wRange, viewId, point));
                break;
            case 3:
                toExecute.Enqueue(new SkillECommand(this, delay, isTarget, isChannel, eRange, viewId, point));
                break;
            case 4:
                toExecute.Enqueue(new SkillRCommand(this, delay, isTarget, isChannel, rRange, viewId, point));
                break;
            default:
                break;
        }
    }

    IEnumerator Delay(float time)
    {
        canCancel = false;
        yield return new WaitForSeconds(time);
        canCancel = true;
    }

    // Update is called once per frame
    public virtual void Update()
    {
        if (pv.IsMine)
        {
            if ((character.CurState == State.Stun || character.CurState == State.Airborne) == false)
            {
                if (Input.GetMouseButton(1))
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.transform.gameObject.CompareTag(enemyTag) && (curCommand is AutoAttackCommand) == false)
                        {
                            pv.RPC("AAEnququer", RpcTarget.All, hit.transform.GetComponent<PhotonView>().ViewID);
                            //toExecute.Enqueue(new AutoAttackCommand(this, 0, hit.transform.GetComponent<PhotonView>().ViewID));
                        }
                        else
                        {
                            //toExecute.Enqueue(new MoveCommand(this, 0, hit.point));
                            Move(hit.point);
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        PhotonView enemyTemp = hit.transform.GetComponent<PhotonView>();
                        if (qTarget)
                        {
                            if (enemyTemp.CompareTag(enemyTag))
                            {
                                pv.RPC("SkillEnqueuer", RpcTarget.All, 1, qDelay, qTarget, qChannel, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point);
                            }
                        }
                        else
                        {
                            pv.RPC("SkillEnqueuer", RpcTarget.All, 1, qDelay, qTarget, qChannel, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point);
                        }
                        //SkillEnqueuer(1, 0.2f, false, false, hit.transform.GetComponent<PhotonView>().ViewID, hit.point);
                        //toExecute.Enqueue(new SkillQCommand(this, 0.2f, false, false, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point));
                    }
                }
                if (Input.GetKeyDown(KeyCode.W))
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        PhotonView enemyTemp = hit.transform.GetComponent<PhotonView>();
                        if (wTarget)
                        {
                            if (enemyTemp.CompareTag(enemyTag))
                            {
                                pv.RPC("SkillEnqueuer", RpcTarget.All, 2, wDelay, wTarget, wChannel, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point);
                            }
                        }
                        else
                        {
                            pv.RPC("SkillEnqueuer", RpcTarget.All, 2, wDelay, wTarget, wChannel, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point);
                        }
                        //SkillEnqueuer(2, 0.1f, false, false, hit.transform.GetComponent<PhotonView>().ViewID, hit.point);
                        //toExecute.Enqueue(new SkillWCommand(this, 0.1f, true, false, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point));
                    }
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        PhotonView enemyTemp = hit.transform.GetComponent<PhotonView>();
                        if (eTarget)
                        {
                            if (enemyTemp.CompareTag(enemyTag))
                            {
                                pv.RPC("SkillEnqueuer", RpcTarget.All, 3, eDelay, eTarget, eChannel, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point);
                            }
                        }
                        else
                        {
                            pv.RPC("SkillEnqueuer", RpcTarget.All, 3, eDelay, eTarget, eChannel, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point);
                        }
                        //SkillEnqueuer(3, 0.1f, false, false, hit.transform.GetComponent<PhotonView>().ViewID, hit.point);
                        //toExecute.Enqueue(new SkillECommand(this, 0.1f, true, false, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point));
                    }
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        PhotonView enemyTemp = hit.transform.GetComponent<PhotonView>();
                        if (rTarget)
                        {
                            if (enemyTemp.CompareTag(enemyTag))
                            {
                                pv.RPC("SkillEnqueuer", RpcTarget.All, 4, rDelay, rTarget, rChannel, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point);
                            }
                        }
                        else
                        {
                            pv.RPC("SkillEnqueuer", RpcTarget.All, 4, rDelay, rTarget, rChannel, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point);
                        }
                        //SkillEnqueuer(4, 0, false, false, hit.transform.GetComponent<PhotonView>().ViewID, hit.point);
                        //toExecute.Enqueue(new SkillRCommand(this, 0, false, false, enemyTemp != null ? enemyTemp.ViewID : 0, hit.point));
                    }
                }
                if (Input.GetKeyDown(KeyCode.D))
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        toExecute.Enqueue(new RushCommand(this, 0));
                    }
                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        toExecute.Enqueue(new FlashCommmand(this, 0, hit.point));
                    }
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    Stop();
                    toExecute.Clear();
                }
            }
            else if (character.CurState == State.Root)
            {
                agent.ResetPath();
            }
        }
    }

    IEnumerator HpRegen()
    {
        while (character.CurHP > 0)
        {
            yield return new WaitForSeconds(1f);
            character.Heal(character.HpRegen);
        }
    }

    public IEnumerator AAHandle()
    {
        canAA = false;
        yield return new WaitForSeconds(1f / character.AttackSpeed);
        canAA = true;
    }

    private void FixedUpdate()
    {
        if (character.CurQCool >= 0)
        {
            character.CurQCool -= Time.deltaTime;
            if (character.CurQCool <= 0)
            {
                character.CurQCool = 0;
            }
        }
        if (character.CurWCool >= 0)
        {
            character.CurWCool -= Time.deltaTime;
            if (character.CurWCool <= 0)
            {
                character.CurWCool = 0;
            }
        }
        if (character.CurECool >= 0)
        {
            character.CurECool -= Time.deltaTime;
            if (character.CurECool <= 0)
            {
                character.CurECool = 0;
            }
        }
        if (character.CurRCool >= 0)
        {
            character.CurRCool -= Time.deltaTime;
            if (character.CurRCool <= 0)
            {
                character.CurRCool = 0;
            }
        }
        if (character.stateDict != null)
        {
            for (int i = 1; i < (int)State.End; i++)
            {
                State tempState = (State)i;
                if (character.stateDict.ContainsKey(tempState) && character.stateDict[tempState] > 0)
                {
                    character.stateDict[tempState] -= Time.deltaTime;
                    if (character.stateDict[tempState] <= 0)
                    {
                        character.stateDict[tempState] = 0;
                    }
                }
            }
        }
        character.StateChecker();
    }


    public virtual void Stop()
    {
        if (curCommandCor != null)
        {
            StopCoroutine(curCommandCor);
            curCommandCor = null;
        }
        agent.ResetPath();
    }

    public void ApplyMoveSpeed()
    {
        agent.speed = character.MoveSpeed * 0.01f;
    }

    public virtual void Move(Vector3 pos)
    {
        //움직이다
        toExecute.Clear();
        if (curCommand != null)
        {
            curCommand = null;
        }
        if (curCommandCor != null)
        {
            StopCoroutine(curCommandCor);
            curCommandCor = null;
        }

        if (character.CurState != State.Root)
        {
            agent.SetDestination(pos);
        }
    }

    public virtual void AutoAttack(PlayerController target)
    {
        //StartCoroutine(AAHandle());
        Debug.Log("AA " + target.name);
        //평타
        float damage = character.ATK;

        if (character.CritChance >= UnityEngine.Random.Range(0f, 1f)) //치명타
        {
            damage *= character.CritDamage;
        }

        target.character.TakeDamage(character, damage, false, character.Lethality, character.ArmorPenetration);
    }

    public virtual void SkillQ(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurQCool <= 0)
        {
            //대충 스킬쓰기
            character.SetQCooldown();
            Debug.Log("Q사용");
        }
        else
        {
            //못쓴다하기
            Debug.Log("Q사용불가. 쿨타임: " + character.CurQCool);
        }
    }

    public virtual void SkillW(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurWCool <= 0)
        {
            //대충 스킬쓰기
            character.SetWCooldown();
            Debug.Log("W사용");
        }
        else
        {
            //못쓴다하기
            Debug.Log("W사용불가. 쿨타임: " + character.CurWCool);
        }
    }

    public virtual void SkillE(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurECool <= 0)
        {
            //대충 스킬쓰기
            character.SetECooldown();
            Debug.Log("E사용");
        }
        else
        {
            //못쓴다하기
            Debug.Log("E사용불가. 쿨타임: " + character.CurECool);
        }
    }

    public virtual void SkillR(bool isTargeting, bool isChanneling, PlayerController target, Vector3 location)
    {
        if (character.CurRCool <= 0)
        {
            //대충 스킬쓰기
            character.SetRCooldown();
            Debug.Log("R사용");
        }
        else
        {
            //못쓴다하기
            Debug.Log("R사용불가. 쿨타임: " + character.CurRCool);
        }
    }

    public void Rush()
    {
        //도주
        if (character.CanRush)
        {
            StartCoroutine(DoRush());
        }
        else
        {
            //못쓴다고 하기
        }
    }

    IEnumerator DoRush()
    {
        character.SetCanRush(false);
        int tempSpeed = (int)(character.MoveSpeed * 0.4f);
        character.AdjustMoveSpeed(tempSpeed);
        yield return new WaitForSeconds(2f);
        character.AdjustMoveSpeed(-tempSpeed);
    }

    public void Flash(Vector3 pos)
    {
        //점멸
        if (character.CanFlash)
        {
            character.SetCanFlash(false);
            pos.y = transform.position.y;
            pos = transform.position + Vector3.ClampMagnitude(pos - transform.position, 4);
            agent.ResetPath();
            if (NavMesh.SamplePosition(pos, out NavMeshHit navhit, 3, NavMesh.AllAreas))
            {
                Debug.Log($"pos: {pos} | nHit: {navhit.position}");
                agent.SetDestination(navhit.position);
                transform.position = navhit.position;
            }
            //transform.position += Vector3.ClampMagnitude(pos - transform.position, 4);
        }
        else
        {
            //못쓴다고 하기
        }
    }

    public void ResetCharacter()
    {
        character.ResetState();
        StartCoroutine(HpRegen());
    }

    public void ApplySlow(float percent, float time)
    {
        StartCoroutine(SlowDown(percent, time));
    }

    IEnumerator SlowDown(float percent, float time)
    {
        int tempSpeed = (int)(character.MoveSpeed * percent);
        character.AdjustMoveSpeed(-tempSpeed);
        yield return new WaitForSeconds(time);
        character.AdjustMoveSpeed(tempSpeed);
    }

    public virtual void Death()
    {
        Debug.Log(pv.ViewID + " 사망");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDeath(pv.ControllerActorNr);
        }
        agent.enabled = false;
        this.enabled = false;
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }
}