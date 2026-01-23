using System.Linq;
using UnityEngine;
using Unity.AI;
using Unity.Behavior;
using UnityEngine.AI;

public class EnemyFSM : MonoBehaviour
{
    private Transform target;

    [SerializeField]
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private BehaviorGraphAgent behaviorGraphAgent;

    public void SetUp(Transform target, GameObject[] wayPoints)
    {
        this.target = target;

        if(navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
        }

        if(behaviorGraphAgent != null)
        {
            behaviorGraphAgent.SetVariableValue("patrolPoints", wayPoints.ToList());
        }
    }

    
}
