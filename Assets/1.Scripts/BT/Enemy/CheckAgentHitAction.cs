using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Check Agent Hit ", story: "Check [Agent] Hit And[StunDuration] [IsStun]", category: "Action", id: "55db6f996866c4eac869258e392d5ce9")]
public partial class CheckAgentHitAction : Action
{
    [SerializeReference] public BlackboardVariable<Enemy> Agent;
    [SerializeReference] public BlackboardVariable<float> StunDuration;
    [SerializeReference] public BlackboardVariable<bool> IsStun;

    protected override Status OnStart()
    {

        return Status.Running;
    }

    private void Value_OnStun(float obj)
    {
        throw new NotImplementedException();
    }

    protected override Status OnUpdate()
    {
        if(Agent.Value == null)
            return Status.Failure;

        IsStun.Value = Agent.Value.isHitStun;

        if(IsStun.Value && Agent.Value.stunDuration > 0)
            StunDuration.Value = Agent.Value.stunDuration;

        return Status.Success;
    }

    protected override void OnEnd()
    {
    }

    void OnStun(float stunDuration)
    {
        StunDuration.Value = stunDuration;
        IsStun.Value = true;
    }
}

