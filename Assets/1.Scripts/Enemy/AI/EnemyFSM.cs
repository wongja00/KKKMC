using System.Linq;
using UnityEngine;
using Unity.AI;
using Unity.Behavior;
using UnityEngine.AI;
using Unity.VisualScripting;

public class EnemyFSM : MonoBehaviour
{
    private Transform target;

    [SerializeField]
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private BehaviorGraphAgent behaviorGraphAgent;

    public void SetUp(Transform target, GameObject[] wayPoints, bool isDugeon = false)
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

            behaviorGraphAgent.Start();
        }

    }

    
}
