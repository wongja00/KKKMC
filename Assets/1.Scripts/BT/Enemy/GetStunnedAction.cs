using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "GetStunned", story: "[Agent] [Animator] Stun [StunDuration]", category: "Action", id: "e54d29851f1dd15d4f3929b00d210dd8")]
public partial class GetStunnedAction : Action
{
    [SerializeReference] public BlackboardVariable<CharacterBase> Agent;
    [SerializeReference] public BlackboardVariable<Animator> Animator;
    [SerializeReference] public BlackboardVariable<float> StunDuration;

    protected override Status OnStart()
    {
        Agent.Value.HitStun(StunDuration.Value);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        //Animator.Value.SetTrigger("Stun");




        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

