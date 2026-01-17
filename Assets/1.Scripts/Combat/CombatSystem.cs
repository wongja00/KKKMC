using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class CombatSystem : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    public Animator animator;
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
    private float currentAttackTimer = 0f;
    private bool isAttacking = false;
    private bool canReceiveInput = false;

    [Header("입력 버퍼")]
    private Queue<AttackInputType> inputBuffer = new Queue<AttackInputType>();
    private float inputBufferTime = 0.2f;
    private float lastInputTime = 0f;

    [Header("콤보 관리")]
    private int comboCount = 0;
    private float comboResetTime = 2f;
    private float lastHitTime = 0f;

    private Dictionary<int, AttackData> attackDictionary = new Dictionary<int, AttackData>();
    private Coroutine currentAttackCoroutine;

    public event Action<AttackEvent> OnCustomEvent;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        if(!isAttacking)
        {
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
        Debug.Log("공격");
        }
        
        //강공격
        else if(Input.GetButtonDown("Fire1"))
        {
            TryStartAttack(AttackInputType.Light);
        }
        
        //특공격
        else if(Input.GetButtonDown("Fire1"))
        {
            TryStartAttack(AttackInputType.Light);
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
            StartAttack(attack);
        }
    }

    void StartAttack(AttackData attack)
    {
        if(isAttacking) return;

        currentAttack = attack;
        isAttacking = true;
        currentAttackTimer = 0f;
        canReceiveInput = false;

        //애니메이션 재생
        if(animator != null && attack.animationClip != null)
        {
            //animator.speed = attack.animationSpeed;
            //animator.Play(attack.animationClip.name);

            PlayAttackAnimation(attack);
        }

        //공격 코루틴 시작
        if(currentAttackCoroutine != null)
            StopCoroutine(currentAttackCoroutine);

        currentAttackCoroutine = StartCoroutine(AttackCoroutine(attack));
    }

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

    IEnumerator AttackCoroutine(AttackData attack)
    {
        //선딜
        yield return new WaitForSeconds(attack.startupTime);

        //입력 가능 시간 시작
        canReceiveInput = true;

        //판정 시작
        StartCoroutine(DamageWindowCoroutine(attack));

        //이벤트 처리
        StartCoroutine(ProcessAttackEvents(attack));

        //후딜
        yield return new WaitForSeconds(attack.activeTime + attack.recoveryTime);

        //공격 종료
        EndAttack();
    }

    IEnumerator DamageWindowCoroutine(AttackData attack)
    {
        float elapsed = 0f;

        while(elapsed < attack.activeTime)
        {
            CheckHit(attack);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void CheckHit(AttackData attack)
    {
        Vector3 hitboxPos = hitboxOrigin.position + hitboxOrigin.TransformDirection(attack.hitboxOffset);
        Collider[] hits = Physics.OverlapBox(hitboxPos, attack.hitboxSize / 2f, 
        hitboxOrigin.rotation, attack.hitLayerMask);

        foreach(var hit in hits)
        {
            ICharacter target = hit.GetComponent<ICharacter>();
            if((target != null) && !ReferenceEquals(target, character))
            {
                //데미지 처리 
                target.TakeDamage((int)attack.damage);

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
                    //일반 체인
                    EndAttack();
                    StartAttack(nextAttack);
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
            StartAttack(combo.steps[currentComboStep].attackData);
        }
    }

    void EndAttack()
    {
        isAttacking = false;
        canReceiveInput = false;
        currentAttack = null;
        currentAttackTimer = 0f;

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
