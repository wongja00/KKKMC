using UnityEngine;
using UnityEngine.AI;
using System;

public class Enemy : CharacterBase
{
    [SerializeField] private Animator animator; 

    public float detectRange = 20f;
    public float stopDistance = 1.5f;

    private NavMeshAgent agent;
    Transform player;

    public event Action OnDeath;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stopDistance;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GetNearestPlayer();

        CurHP = MaxHP;
        isDead = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(isDead || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        player = GetNearestPlayer();
        
        if(player == null) return;

        agent.SetDestination(player.position);
        Debug.Log($"추적 {player.name}");
        
    }

    override public void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        if(CurHP <= 0 && isDead == false)
        {
            Die();
        }

    }


    public void SetIsDead(bool IsDead)
    {  
        isDead = IsDead;

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
        OnDeath?.Invoke();
        agent.enabled = false;
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
}
