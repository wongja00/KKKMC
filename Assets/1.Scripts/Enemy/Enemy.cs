using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;
using UnityEngine.UI;
using Mirror;

public class Enemy : CharacterBase
{
    [SerializeField]
    private Animator animator; 

    public float detectRange = 20f;
    public float stopDistance = 1.5f;

    private NavMeshAgent agent;
    Transform player;
    bool isRun;
    public bool isDugeon = false;

    public event Action OnDeath;
    
    public event Action OnTakeDamage; 

    private Vector2 animVelocity;

    readonly int rimEnable = Shader.PropertyToID("_Enable");
    readonly int rimColor = Shader.PropertyToID("_RimColor");
    readonly int rimIntensity = Shader.PropertyToID("_RimIntensity");
    readonly int rimPower = Shader.PropertyToID("_RimPower");
    [SerializeField] SkinnedMeshRenderer skinRenderer;
    [SerializeField] EnemyFSM enemyFSM;
    [SerializeField] EnemyCombatSystem enemyCombatSystem;
    [SerializeField] NetworkIdentity identity;
    MaterialPropertyBlock mpb;

    [SerializeField]
    Image hpBar;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stopDistance;

        mpb = new MaterialPropertyBlock();


    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(isServer)
        {
        }
        else
        {
            enemyFSM.StopGraph();
        }

        player = GetNearestPlayer();

        if(enemyFSM != null)
        {
            enemyFSM.SetUp(player, null, isDugeon, identity.isServer);
            OnDeath += enemyFSM.StopGraph;
            OnDeath += ()=>{enemyCombatSystem.isdead = true;};
            OnDeath += enemyCombatSystem.StopAnimation;
            enemyCombatSystem.OnAttackDistInfo += enemyFSM.SetAttackDistance;             
        }

        CurHP = MaxHP;
        OnHpChanged += UpadteHpUI;
        isDead = false;
        isRun = false;

        animVelocity = new Vector2(0,0);
    }

    // Update is called once per frame
    void Update()
    {
        if(isDead || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        //TrackingPlayer();
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    private void LowHP()
    {
                    // 체력이 일정 이하면 도망
        float hpThreshold = MaxHP * 0.3f; // 30% 이하로 떨어지면 도망
        if (CurHP <= hpThreshold)
        {
            isRun = true;
            SetRim(true, Color.red, 2f, 3f);

            FleeFromNearestPlayer();

            return;
        }
    }

    [Server]
    override public void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        OnTakeDamage?.Invoke();
        
        enemyFSM.SetHP(CurHP);
        RpcUpadteHpUI();

        hpBar.fillAmount = CurHP/MaxHP;

        if(CurHP <= 0 && isDead == false)
        {
            Die();
        }
    }

    [ClientRpc]
    private void RpcUpadteHpUI()
    {
        UpadteHpUI();
    }

    private void UpadteHpUI()
    {
        hpBar.fillAmount = CurHP/MaxHP;
    }




    public void SetIsDead(bool IsDead)
    {  
        isDead = IsDead;

        animVelocity = new Vector2(0,0);

        if(isDead == true)
        {
            Die();
        }
    }

    public void Die()
    {
        if(isDead) return;
        isDead = true;

        animator.SetBool("isDead", true);
        Debug.Log("사망");
        OnDeath?.Invoke();
        agent.enabled = false;

        StartCoroutine(DisappearAfterDie());
    }

    void SetRim(bool on, Color color, float intensity = 2f, float power = 3f)
    {
        if(skinRenderer == null) return;

        skinRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(rimEnable, on ? 1f : 0f);
        mpb.SetColor(rimColor, color);
        mpb.SetFloat(rimIntensity, intensity);
        mpb.SetFloat(rimPower, power);
        
        skinRenderer.SetPropertyBlock(mpb);
    }

    Transform GetNearestPlayer()
    {
        Transform nearest = null;
        float minDist = float.MaxValue;

        var players = PlayerRegistry.Players;
        if(players.Count == 0) return null;

        foreach (var p in players)
        {
            if (p == null) continue;

            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = p.transform;
            }
        }
        return nearest;
    }

    IEnumerator DisappearAfterDie()
    {
        yield return new WaitForSecondsRealtime(7f);

        NetworkManager.Destroy(this.gameObject);
    }

    public void FleeFromNearestPlayer()
    {
        // 플레이어 반대 방향으로 이동한다
        Vector3 awayDir = (transform.position - player.position).normalized;
        Vector3 runDestination = transform.position + awayDir * 10f; // 10m 뒤로 도망
        agent.speed = 7f; // 도망 속도
        //animVelocity.y = -2; // 도망 애니메이션 (예: 음수로 Treat as 도망)

        agent.SetDestination(runDestination);
    }
}
