using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Mirror;
using UnityEngine.AI;

public class EnemyCombatSystem : NetworkBehaviour
{
    [Header("참조 컴포넌트")]
    public Animator animator;
    public Transform playerTransform;
    public NetworkIdentity identity;
    
    public PlayableGraph playableGraph;
    public AnimationPlayableOutput playableOutput;
    public AnimationClipPlayable curPlayable;
    public NavMeshAgent agent;
    public CharacterBase character;
    public Transform hitboxOrigin; //히트박스 기준점

    [Header("콤보 데이터")]
    public List<AttackData> availableAttacks = new List<AttackData>();

    [Header("상태")]
    [SyncVar] private int currentAttackID = -1;
    [SyncVar] private int queueAttackID = -1;
    [SyncVar] private int bbqueueAttackID = -1;
    private AttackData currentAttack;
    private ComboChain currentCombo;
    private int currentComboStep = 0;
    private float attackNormalTime = 0f;
    
    [SyncVar] public bool isAttacking = false;

    [Range(0,1)]
    private float curPlayableDuration = 1;

    private Dictionary<int, AttackData> attackDictionary = new Dictionary<int, AttackData>();
    
    private Coroutine currentAttackCoroutine;
    private Coroutine currentDamageCoroutine;
    HashSet<int> firedHitWindows = new HashSet<int>();

    Dictionary<float, bool> hitFired = new Dictionary<float, bool>();
    private HashSet<CharacterBase> hitEnemies = new HashSet<CharacterBase>();
    
    public event Action<AttackEvent> OnCustomEvent;

    public bool isdead = false;
    public event Action<float> OnAttackDistInfo;
    public float attackAngle = 0.6f;

    void Awake()
    {
        //if(identity.isServer == false) return;
        foreach(var attack in availableAttacks)
        {
            if(attack != null)
            {
                attackDictionary[attack.attackID] = attack;
            }
        }

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //if(identity.isServer == false) return;
        
        //공격 데이터를 딕셔너리로 변환(빠른 검색)

        agent = GetComponent<NavMeshAgent>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isAttacking)
        {
            agent.speed = 0;
        }
        
    }

    // 공격 랜덤 예약
    [Server]
    public void QueueRandomAttack()
    {
        int randomId = GetRandomID();

        if (randomId != -1)
        {
            bbqueueAttackID = randomId;

            AttackData data = attackDictionary[bbqueueAttackID];

            OnAttackDistInfo?.Invoke(data.distance);
        }
    }


    [Server]
    public void RequestAttack(AttackInputType inputType)
    {
        if(isAttacking) return;
        
        //새공격 시작
        AttackData attack = FindAttackBT(inputType);
        if(attack != null)
        {
            if(attackDictionary[bbqueueAttackID] != null)
            {
                StartAttack(bbqueueAttackID);
                return;
            }

            StartAttack(attack.attackID);
        }
    }

    [Server]
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

    public bool GetIsAttack()
    {
        return isAttacking;
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
        //판정 시작
        currentDamageCoroutine = StartCoroutine(DamageWindowCoroutine(attack.attackID));

        //이벤트 처리
        SetDurationTime(attack.attackID);
        float duration = curPlayableDuration;
        
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

        Collider[] hits = Physics.OverlapSphere(transform.position, attack.distance, attack.hitLayerMask);

        float closest = float.MaxValue;

        foreach(var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);

            if(closest > dist)
                closest = dist;
        }

        foreach(var hit in hits)
        {
            // 공격대상(hit)이 내 앞에 있을 때만 판정 (playerTransform.forward 기준)
            Vector3 toTarget = (hit.transform.position - transform.position).normalized;
            float forwardDot = Vector3.Dot(transform.forward, toTarget);
            if(forwardDot < attackAngle)
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
        }
    }
    
    [Server]
    void EndAttack()
    {
        if(isdead) return;

        isAttacking = false;
        currentAttack = null;
        currentAttackID = -1;
        queueAttackID = -1;

        //agent.speed = character.speed;

        StopAnimation();

        if(animator != null)
        {
            animator.speed = 1.0f;
            animator.Rebind();
            animator.Update(0f);
        }

    }

    
    [ClientRpc]
    public void StopAnimation()
    {
        if(playableGraph.IsValid())
        {
            playableGraph.Stop();
            playableGraph.Destroy();
        }
    }

        AttackData FindAttackBT(AttackInputType tpye)
    {
        foreach(var attack in availableAttacks)
        {
            if(attack != null && attack.inputType == tpye)
            {
                return attack;
            }
        }

        return null;
    }

    public int GetRandomID()
    {
        // attackDictionary에 있는 키 중 랜덤으로 리턴
        if (attackDictionary.Count == 0)
            return -1;

        List<int> keys = new List<int>(attackDictionary.Keys);
        int randomIndex = UnityEngine.Random.Range(0, keys.Count);
        return keys[randomIndex];
    }

}
