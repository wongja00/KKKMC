using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "QueueAttack", story: "Choice [QueueAttackID] [AttackRange] by [Combatsystem]", category: "Action", id: "9402521d1edf63b18332e161f0a81106")]
public partial class QueueAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<int> QueueAttackID;
    [SerializeReference] public BlackboardVariable<float> AttackRange;
    [SerializeReference] public BlackboardVariable<EnemyCombatSystem> Combatsystem;

    protected override Status OnStart()
    {
        Combatsystem.Value.QueueRandomAttack();

        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

