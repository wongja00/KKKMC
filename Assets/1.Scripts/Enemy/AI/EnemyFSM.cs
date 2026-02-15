using System.Linq;
using UnityEngine;
using Unity.Behavior;
using UnityEngine.AI;
using Mirror;

public class EnemyFSM : NetworkBehaviour
{
    private Transform target;

    [SerializeField]
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private BehaviorGraphAgent behaviorGraphAgent;


    void Awake()
    {
        NetworkIdentity identity = GetComponent<NetworkIdentity>();
        if (identity != null && !identity.isServer)
        {
            //behaviorGraphAgent.enabled = false; // BT 통째로 서버 전용
            
        }
    }

    void Update()
    {

    }

    public void StopGraph()
    {
        behaviorGraphAgent.enabled = false;
    }

    public void SetHP(float hp)
    {
        behaviorGraphAgent.SetVariableValue("curHP", hp);
    }
    public void SetAttackDistance(float dist)
    {
        if(behaviorGraphAgent != null)
        {
            //behaviorGraphAgent.SetVariableValue("patrolPoints", wayPoints.ToList());
            behaviorGraphAgent.SetVariableValue("attackDistance", dist);
        }
    }

    public void SetUp(Transform target, GameObject[] wayPoints, bool isDugeon = false, bool isServer = false)
    {
        this.target = target;

        if(navMeshAgent != null)
        {
            //navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
        }

        if(behaviorGraphAgent != null)
        {
            //behaviorGraphAgent.SetVariableValue("patrolPoints", wayPoints.ToList());
            behaviorGraphAgent.SetVariableValue("isInDungeon", isDugeon);
            behaviorGraphAgent.SetVariableValue("Target", target.gameObject);
            behaviorGraphAgent.SetVariableValue("isServer", isServer);

            behaviorGraphAgent.Start();
        }

    }

    
}
