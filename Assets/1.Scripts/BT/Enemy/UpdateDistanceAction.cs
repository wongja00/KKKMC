using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "UpdateDistance", story: "Update [Self] and [Target] [curDistance]", category: "Action", id: "43478725c6c34d2ffdf9a14c7e161a3c")]
public partial class UpdateDistanceAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> CurDistance;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        CurDistance.Value = Vector3.Distance(Self.Value.transform.position, Target.Value.transform.position);  
        
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

