using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Mirror;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChaseAction", story: "[Self] Navigate To [Target]", category: "Action", id: "177198fa2956caa18572b596b50c5490")]
public partial class ChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private NavMeshAgent agent;
    private CharacterBase character;
    private Animator animator;
    private NetworkIdentity identity;
    private Vector2 animVelocity;

    protected override Status OnStart()
    {        
        if (agent == null)
        {
            agent = Self.Value.GetComponent<NavMeshAgent>();
            identity = Self.Value.GetComponent<NetworkIdentity>();
            animator = Self.Value.GetComponent<Enemy>().GetAnimator();
            character = Self.Value.GetComponent<CharacterBase>();
        }
        
        //if(identity != null && identity.isServer == false) return Status.Success; //서버가 아니면(클라면) 스킵

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        //if(identity != null && identity.isServer == false) return Status.Running; //서버가 아니면(클라면) 스킵

        if (agent != null && Target.Value != null && Self.Value.activeSelf == true)
        {
            if(character.isHitStun)
            {
                agent.speed = 0;
                animVelocity.y = 0;
                animator.SetFloat("velocityZ", animVelocity.y);
                return Status.Running;
            }

            float distanceToPlayer = Vector3.Distance(Self.Value.transform.position, Target.Value.transform.position);

            if (animator != null)
            {
                if (agent.velocity.z < 0)
                {
                    animVelocity.y = -1;
                }

                if (distanceToPlayer > 5f)
                {
                    agent.speed = 3; // 속도 증가
                    
                    //animVelocity.x = 2;
                    animVelocity.y = 2;
                }
                else if(distanceToPlayer <= 5f)
                {
                    agent.speed = 1.5f; // 속도 감소
                    
                    //animVelocity.x = 2;
                    animVelocity.y = 1;
                }
                else
                {
                    animVelocity.y = 0;
                    return Status.Success;//도착
                }

                //animator.SetFloat("velocityX", animVelocity.x);
                animator.SetFloat("velocityZ", animVelocity.y);
            }

            if(agent.isOnNavMesh == true)
                agent.SetDestination(Target.Value.transform.position);
        }
        else
        {
             return Status.Failure;
        }

        return Status.Running;
    }
}

