using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Mirror;

public class CombatSystem : NetworkBehaviour
{
    [Header("참조 컴포넌트")]
    public Animator animator;
    public Transform playerTransform;
    public PlayerMovement playerMovement;
    public HandHeld handHeld;
    public PlayableGraph playableGraph;
    public AnimationPlayableOutput playableOutput;
    public AnimationClipPlayable curPlayable;
    public CharacterBase character;
    public Transform hitboxOrigin; //히트박스 기준점

    [Header("콤보 데이터")]
    public List<AttackData> availableAttacks = new List<AttackData>();
    public List<ComboChain> availableCombos = new List<ComboChain>();

    [Header("상태")]
    private AttackData currentAttack;
    private ComboChain currentCombo;
    private int currentComboStep = 0;
    private float attackNormalTime = 0f;
    public bool isAttacking{get; private set;} = false;
    private bool canReceiveInput = false;
    public bool canMoveDuringAttack{private set; get;} = true;//공격하면서 움직일수 있는지
    public bool canRotateDuringAttack{private set; get;} = true;//공격하면서 회전할수 있는지

    [Header("입력 버퍼")]
    private Queue<AttackInputType> inputBuffer = new Queue<AttackInputType>();
    private float inputBufferTime = 0.2f;
    private float lastInputTime = 0f;

    [Header("콤보 관리")]
    private int comboCount = 0;
    private float comboResetTime = 2f;
    private float lastHitTime = 0f;

    private Dictionary<int, AttackData> attackDictionary = new Dictionary<int, AttackData>();
    Dictionary<float, bool> hitFired = new Dictionary<float, bool>();
    private Coroutine currentAttackCoroutine;

    public event Action<AttackEvent> OnCustomEvent;

    private HashSet<CharacterBase> hitEnemies = new HashSet<CharacterBase>();
    HashSet<int> firedHitWindows = new HashSet<int>();
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer) return;

        //공격 데이터를 딕셔너리로 변환(빠른 검색)
        foreach(var attack in availableAttacks)
        {
            if(attack != null)
                attackDictionary[attack.attackID] = attack;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!isLocalPlayer) return;

        if(!isAttacking)
        {
            if(handHeld.curObjectItem == null)
                HandleInput();
        }

        UpdateComboTimer();
        ProcessInputBuffer();
    }

    void LateUpdate()
    {
        if(isAttacking && playableGraph.IsValid())
        {
            if(curPlayable.GetTime() >= curPlayable.GetDuration())
            {
                EndAttack();
            }
        }
    }

    void HandleInput()
    {
        //경공격
        if(Input.GetButtonDown("Fire1"))
        {
            TryStartAttack(AttackInputType.Light);
        }        
        //강공격
        else if(Input.GetButtonDown("Fire2"))
        {
            TryStartAttack(AttackInputType.Heavy);
        }        
        //특공격
        else if(Input.GetButtonDown("Fire3"))
        {
            TryStartAttack(AttackInputType.Special);
        }

    }

    void TryStartAttack(AttackInputType inputType)
    {
        //콤보 중이면 다음 단계 시도
        if(isAttacking && currentCombo != null)
        {
            TryChainCombo(inputType);

            return;
        }

        //새공격 시작
        AttackData attack = FindAttackByInput(inputType);
        if(attack != null)
        {
            StartAttack(attack.attackID);
        }
    }

    [Command]
    void StartAttack(int ID)
    {
        firedHitWindows.Clear();
        hitEnemies.Clear();

        Server_ExecuteAttack(attackDictionary[ID]);
    }

    [Server]
    void Server_ExecuteAttack(AttackData attack)
    {
        if(isAttacking) return;

        currentAttack = attack;
        isAttacking = true;
        canReceiveInput = false;
        canMoveDuringAttack = attack.canMoveDuringAttack;
        canRotateDuringAttack = attack.canRotateDuringAttack;

        //애니메이션 재생
        if(animator != null && attack.animationClip != null)
        {
            PlayAttackAnimation(attack.attackID);
        }
        
        // 공격 코루틴 시작
        if(currentAttackCoroutine != null)
            StopCoroutine(currentAttackCoroutine);

        currentAttackCoroutine = StartCoroutine(AttackCoroutine(attack));
    }



    [ClientRpc]
    void PlayAttackAnimation(AttackData attack)
    {
        if(playableGraph.IsValid())
            playableGraph.Destroy();

        playableGraph = PlayableGraph.Create("AttackGraph");
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        curPlayable = AnimationClipPlayable.Create(playableGraph, attack.animationClip);
        curPlayable.SetSpeed(attack.animationSpeed);

        playableOutput = AnimationPlayableOutput.Create(playableGraph, "Anim", animator);
        playableOutput.SetSourcePlayable(curPlayable);

        playableGraph.Play();
    }

    [ClientRpc]
    void PlayAttackAnimation(int attackID)
    {
        AttackData attack = attackDictionary[attackID];

        if(playableGraph.IsValid())
        playableGraph.Destroy();

        playableGraph = PlayableGraph.Create("AttackGraph");
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        curPlayable = AnimationClipPlayable.Create(playableGraph, attack.animationClip);
        curPlayable.SetSpeed(attack.animationSpeed);

        playableOutput = AnimationPlayableOutput.Create(playableGraph, "Anim", animator);
        playableOutput.SetSourcePlayable(curPlayable);

        playableGraph.Play();
    }

    [Server]
    IEnumerator AttackCoroutine(AttackData attack)
    {
        //선딜
        yield return new WaitForSeconds(attack.startupTime);

        //입력 가능 시간 시작
        canReceiveInput = true;

        //판정 시작
        StartCoroutine(DamageWindowCoroutine(attack.attackID));

        //이벤트 처리
        StartCoroutine(ProcessAttackEvents(attack));

        //후딜
        yield return new WaitForSeconds(attack.activeTime + attack.recoveryTime);

        //공격 종료
        EndAttack();
    }

    [Server]
    IEnumerator DamageWindowCoroutine(int attackID)
    {
        float elapsed = 0f;

        AttackData attack = attackDictionary[attackID];

        while(elapsed <= attack.totalDuration)
        {
            float normalTime = elapsed / attack.totalDuration;

            for(int i = 0; i< attack.hitBoxTimes.Count; i++)
            {
                var window = attack.hitBoxTimes[i];

                if(normalTime >= window.start && window.end >= normalTime)
                {
                    if(firedHitWindows.Contains(i)) continue;

                    CheckHit(attack.attackID);
                    firedHitWindows.Add(i);
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        hitEnemies.Clear();
    }
    void OnDrawGizmos()
    {
        foreach(CharacterBase en in hitEnemies)
            Gizmos.DrawLine(transform.position, en.transform.position);       
    }

    [Server]
    void CheckHit(int attackID)
    {
        AttackData attack = attackDictionary[attackID];

        Vector3 hitboxPos = transform.position + hitboxOrigin.TransformDirection(attack.hitboxOffset);

        Collider[] hits = Physics.OverlapSphere(playerTransform.position, attack.distance, attack.hitLayerMask);

        float closest = float.MaxValue;

        foreach(var hit in hits)
        {
            float dist = Vector3.Distance(playerTransform.position, hit.transform.position);

            if(closest > dist)
                closest = dist;
        }

        foreach(var hit in hits)
        {
            // 공격대상(hit)이 내 앞에 있을 때만 판정 (transform.forward 기준)
            Vector3 toTarget = (hit.transform.position - playerTransform.position).normalized;
            float forwardDot = Vector3.Dot(playerTransform.forward, toTarget);
            if(forwardDot < 0.3f)
            {
                continue; // 앞에 있지 않으면 맞지 않음
            }
            CharacterBase target = hit.GetComponentInParent<CharacterBase>();

            

            if(!hitEnemies.Add(target)) continue;

            if((target != null) && !ReferenceEquals(target, character))
            {
                //데미지 처리 
                target.TakeDamage((int)attack.damage);
                Debug.Log("공격");

                //넉백
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if(rb != null)
                {
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    rb.AddForce(direction * attack.knockbackForce, ForceMode.Impulse);
                }

                //콤보 카운트 증가
                OnHitEnemy();
            }
        }
    }

    IEnumerator ProcessAttackEvents(AttackData attack)
    {
        foreach(var evt in attack.events)
        {
            yield return new WaitForSeconds(evt.triggerTime);
            TriggerEvent(evt);
        }
    }

    void TriggerEvent(AttackEvent evt)
    {
        switch(evt.eventType)
        {
            case AttackEventType.DamageStart:
            //데미지 판정 시작(이미 처리됨)
            break;
            case AttackEventType.SpawnEffect:
            //이펙트 생성
            break;
            case AttackEventType.PlaySound:
            //사운드 재생
            break;
            case AttackEventType.ScreenShake:
            break;
            case AttackEventType.Custom:
            //커스텀 이벤트
            OnCustomEvent?.Invoke(evt);            
            break;
        }
    }

    void TryChainCombo(AttackInputType inputType)
    {
        if(!canReceiveInput) return;

        //현재 공격에서 연결 가능한 공격 찾기
        if(currentAttack != null)
        {
            AttackData nextAttack = FindChainableAttack(currentAttack, inputType);
            if(nextAttack != null)
            {
                //콤보 체인 찾기
                ComboChain combo = FindComboChain(currentAttack, nextAttack);
                if(combo != null)
                {
                    StartCombo(combo);
                }
                else
                {
                    //일반 공격이거나 다음콤보로
                    EndAttack();
                    StartAttack(nextAttack.attackID);
                }
            }
        }
    }


    AttackData FindChainableAttack(AttackData from, AttackInputType inputType)
    {
        AttackData nextAttack = FindAttackByInput(inputType);

        if(nextAttack != null && from.canChainTo.Contains(nextAttack.attackID))
        {
            return nextAttack;
        }

        return null;
    }

    ComboChain FindComboChain(AttackData from, AttackData to)
    {
        foreach(var combo in availableCombos)
        {
            if(combo.steps.Count > currentComboStep + 1)
            {
                if(combo.steps[currentComboStep].attackData == from &&
                   combo.steps[currentComboStep + 1].attackData == to)
                   {
                        return combo;
                   }
            }
        }
        return null;
    }

    void StartCombo(ComboChain combo)
    {
        currentCombo = combo;
        currentComboStep = 0;

        //다음단께로 진행
        currentComboStep++;
        if(currentComboStep < combo.steps.Count)
        {
            EndAttack();
            StartAttack(combo.steps[currentComboStep].attackData.attackID);
        }
    }

    void EndAttack()
    {
        isAttacking = false;
        canMoveDuringAttack = true;//공격 끝나면 무조건 움직일수 있게
        canRotateDuringAttack = true;
        canReceiveInput = false;
        currentAttack = null;

        StopAnimation();

        if(animator != null)
        {
            animator.speed = 1.0f;
            animator.Rebind();
            animator.Update(0f);
        }
    }

    void StopAnimation()
    {
        if(playableGraph.IsValid())
        {
            playableGraph.Stop();
            playableGraph.Destroy();
        }
    }

    void UpdateComboTimer()
    {
        if(Time.time - lastHitTime > comboResetTime)
        {
            comboCount = 0;
            currentCombo = null;
            currentComboStep = 0;
        }
    }

    void OnHitEnemy()
    {
        comboCount++;
        lastHitTime = Time.time;
    }

    AttackData FindAttackByInput(AttackInputType inputType)
    {
        foreach(var attack in availableAttacks)
        {
            if(attack != null && attack.inputType == inputType)
            {
                return attack;
            }
        }

        return null;
    }

    void ProcessInputBuffer()
    {
        //입력 버퍼 처리
    }
}
