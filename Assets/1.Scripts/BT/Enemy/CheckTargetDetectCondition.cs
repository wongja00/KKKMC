using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckTargetDetect", story: "Compare Values of [CurDistance] and [ChaseDistance]", category: "Conditions", id: "e94fb1482e1295f0c705dc04b921a7f3")]
public partial class CheckTargetDetectCondition : Condition
{
    [SerializeReference] public BlackboardVariable<float> CurDistance;
    [SerializeReference] public BlackboardVariable<float> ChaseDistance;

    public override bool IsTrue()
    {
        if(CurDistance < ChaseDistance)
            return true;
            else return false;
    }
}
