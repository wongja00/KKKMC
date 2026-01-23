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
    [SyncVar] private int currentAttackID = -1;
    [SyncVar] private int queueAttackID = -1;
    private AttackData currentAttack;
    private ComboChain currentCombo;
    private int currentComboStep = 0;
    private float attackNormalTime = 0f;
    
    [SyncVar] public bool isAttacking = false;

    [SerializeField]
    [SyncVar]private bool canReceiveInput = false;
    [SyncVar] public bool canMoveDuringAttack = true;//공격하면서 움직일수 있는지
    [SyncVar] public bool canRotateDuringAttack = true;//공격하면서 회전할수 있는지
    
    [Range(0,1)]
    private float curPlayableDuration = 1;

    [Header("입력 버퍼")]
    private Queue<AttackInputType> inputBuffer = new Queue<AttackInputType>();
    private float INPUT_BUFFER_WINDOW = 1f;
    private float lastInputTime = 0f;

    [Header("콤보 관리")]
    private int comboCount = 0;
    private float comboResetTime = 2f;
    private float lastHitTime = 0f;

    private Dictionary<int, AttackData> attackDictionary = new Dictionary<int, AttackData>();
    Dictionary<float, bool> hitFired = new Dictionary<float, bool>();
    private Coroutine currentAttackCoroutine;
    private Coroutine currentDamageCoroutine;
    HashSet<int> firedHitWindows = new HashSet<int>();

    private HashSet<CharacterBase> hitEnemies = new HashSet<CharacterBase>();
    
    public event Action<AttackEvent> OnCustomEvent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer) return;

        //공격 데이터를 딕셔너리로 변환(빠른 검색)
        foreach(var attack in availableAttacks)
        {
            if(attack != null)
            {
                attackDictionary[attack.attackID] = attack;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!isLocalPlayer) return;

        //if(!isAttacking)
        {
            if(handHeld.curObjectItem == null)
                HandleInput();
        }

        UpdateComboTimer();
        ProcessInputBuffer();
    }

    void HandleInput()
    {
        AttackInputType? input = null;

        //경공격
        if(Input.GetButtonDown("Fire1"))
        {
            input = AttackInputType.Light;
            //TryStartAttack(AttackInputType.Light);
        }        
        //강공격
        else if(Input.GetButtonDown("Fire2"))
        {
            input = AttackInputType.Heavy;
            //TryStartAttack(AttackInputType.Heavy);
        }        
        //특공격
        else if(Input.GetButtonDown("Fire3"))
        {
            input = AttackInputType.Special;
            //TryStartAttack(AttackInputType.Special);
        }

        if(input.HasValue)
        {
            BufferInput(input.Value);
        }

    }

    void BufferInput(AttackInputType inputType)
    {
        //버퍼 크기 제한
        if(inputBuffer.Count >= 3)
            inputBuffer.Dequeue();

        inputBuffer.Enqueue(inputType);
        lastInputTime = Time.time;
    }

    void ProcessInputBuffer()
    {
        if(inputBuffer.Count == 0) return;

        //입력 버퍼 윈도우 체크
        if(Time.time - lastInputTime > INPUT_BUFFER_WINDOW)
        {
            inputBuffer.Clear();
            return;
        }
        //공격중이고 입력가능하면 콤보시도
        if(isAttacking && canReceiveInput)
        {
            var inputType = inputBuffer.Dequeue();
            TryChainCombo(inputType);
        }
        //대기중이면 새 공격 시작
        else if(!isAttacking && inputBuffer.Count > 0)
        {
            var inputType = inputBuffer.Dequeue();
            TryStartAttack(inputType);
        }
    }

    void TryStartAttack(AttackInputType inputType)
    {
        //콤보 중이면 다음 단계 시도
        if(isAttacking && currentCombo != null)
        {
            //TryChainCombo(inputType);

            //return;
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
        if(!attackDictionary.ContainsKey(ID))
        {
            return;
        }

        if(isAttacking && currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
        }


        Server_ExecuteAttack(attackDictionary[ID]);
    }

    [Server]
    void Server_ExecuteAttack(AttackData attack)
    {
        //if(isAttacking) return;

        firedHitWindows.Clear();
        hitEnemies.Clear();
        
        currentAttackID = attack.attackID;
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
    void PlayAttackAnimation(int attackID)
    {
        AttackData attack = attackDictionary[attackID];

        if(playableGraph.IsValid())
        playableGraph.Destroy();

        playableGraph = PlayableGraph.Create("AttackGraph");
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        curPlayable = AnimationClipPlayable.Create(playableGraph, attack.animationClip);
        curPlayable.SetSpeed(attack.animationSpeed);
        //curPlayableDuration = attack.animationClip.length / attack.animationSpeed;

        playableOutput = AnimationPlayableOutput.Create(playableGraph, "Anim", animator);
        playableOutput.SetSourcePlayable(curPlayable);

        playableGraph.Play();
    }

    [Server]
    void SetDurationTime(int attackID)
    {
        AttackData attack = attackDictionary[attackID]; 
        curPlayableDuration = attack.animationClip.length / attack.animationSpeed;
    }

    [Server]
    IEnumerator AttackCoroutine(AttackData attack)
    {
        //선딜
        //yield return new WaitForSeconds(attack.startupTime);

        //입력 가능 시간 시작
        canReceiveInput = true;

        //판정 시작
        currentDamageCoroutine = StartCoroutine(DamageWindowCoroutine(attack.attackID));

        //이벤트 처리
        StartCoroutine(ProcessAttackEvents(attack));
        SetDurationTime(attack.attackID);
        float duration = curPlayableDuration;
        Debug.Log($"시간{duration}");
        yield return new WaitForSeconds(duration);

        if(queueAttackID != -1)
        {
            int nextID = queueAttackID;
            queueAttackID = -1;
            Server_ExecuteAttack(attackDictionary[nextID]);
            yield break;
        }
        //공격 종료
        EndAttack();
    }

    double GetPlayableDuration()
    {
        return curPlayable.GetDuration();
    }

    double GetPlayableGetTime()
    {
        return curPlayable.GetTime();
    }

    [Server]
    IEnumerator DamageWindowCoroutine(int attackID)
    {
        float elapsed = 0f;

        AttackData attack = attackDictionary[attackID];

        while(elapsed <= curPlayableDuration)
        {
            double normalTime = elapsed / curPlayableDuration;

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
            // 공격대상(hit)이 내 앞에 있을 때만 판정 (playerTransform.forward 기준)
            Vector3 toTarget = (hit.transform.position - playerTransform.position).normalized;
            float forwardDot = Vector3.Dot(playerTransform.forward, toTarget);
            if(forwardDot < 0.3f)
            {
                continue; // 앞에 있지 않으면 맞지 않음
            }
            CharacterBase target = hit.GetComponentInParent<CharacterBase>();

            if(target == null) continue;
            if(ReferenceEquals(target, character)) continue;
            if(!hitEnemies.Add(target)) continue;

            //데미지 처리 
            target.TakeDamage((int)attack.damage);
            Debug.Log($"피해자{target.name}, 공격{attack.attackName}");

            //넉백
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if(rb != null)
            {
                Vector3 direction = (hit.transform.position - transform.position).normalized;
                rb.AddForce(direction * attack.knockbackForce, ForceMode.Impulse);
            }

            OnHitEnemy();
        }
    }

    [Server]
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
        //if(!canReceiveInput) return;
        if(currentAttack == null) return;

        //현재 공격에서 연결 가능한 공격 찾기
        //if(currentAttack != null)
        
        CmdQueueCombo(inputType);
    }

    [Command]
    void CmdQueueCombo(AttackInputType input)
    {
        if(currentAttack == null) return;

        AttackData nextAttack = FindChainableAttack(currentAttack, input);
        if(nextAttack == null) return;

        queueAttackID = nextAttack.attackID;
    }


    AttackData FindChainableAttack(AttackData from, AttackInputType inputType)
    {
        //return null;
        AttackData nextAttack = FindNextAttackByInput(from.attackID, inputType);
        //AttackData nextAttack = FindAttackByInput(inputType);

        if(nextAttack != null)
        {
            return nextAttack;
        }

        return null;
    }

    AttackData FindNextAttackByInput(int attackID, AttackInputType inputType)
    {
        if(attackDictionary[attackID].canChainTo == null) return null;

        foreach(int ID in attackDictionary[attackID].canChainTo)
        {
            if(attackDictionary[ID].inputType == inputType)
            {
                return attackDictionary[ID];
            }
        }
        return null;
    }

    [Command]
    void EndAttack()
    {
        isAttacking = false;
        canMoveDuringAttack = true;//공격 끝나면 무조건 움직일수 있게
        canRotateDuringAttack = true;
        canReceiveInput = false;
        currentAttack = null;
        currentAttackID = -1;
        queueAttackID = -1;

        StopAnimation();

        if(animator != null)
        {
            animator.speed = 1.0f;
            animator.Rebind();
            animator.Update(0f);
        }
    }

    [ClientRpc]
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


}
