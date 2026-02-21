using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AttackAction", story: "[Self][Combat] Attack To [Target]", category: "Action", id: "05f71dec607029d6c84fbd266022312b")]
public partial class AttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<EnemyCombatSystem> Combat;
    private Enemy enemy;

    protected override Status OnStart()
    {
        enemy = Self.Value.GetComponent<Enemy>();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(Combat.Value.GetIsAttack() == true)
        {
            return Status.Running;
        }

        if(Combat.Value.GetIsAttack() == false && enemy.CurHP > 0)
        {
            Combat.Value.RequestAttack(AttackInputType.Light);
        }
        if(enemy.CurHP <= 0)
        {
            return Status.Failure;
        }
        
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }

    void Attack()
    {
        
    }
}

