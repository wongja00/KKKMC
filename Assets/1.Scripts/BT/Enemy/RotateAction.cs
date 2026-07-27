using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Rotate", story: "[Self] Rotate To [Target]", category: "Action", id: "1ac1395cf51b7f33bd0d6587b4fccad6")]
public partial class RotateAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyCombatSystem> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnStart()
    {



        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null) return Status.Failure;


        Vector3 targetPosition = Target.Value.transform.position;
        Vector3 selfPosition = Self.Value.transform.position;
        targetPosition.y = selfPosition.y;
        Self.Value.transform.LookAt(targetPosition);

        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

