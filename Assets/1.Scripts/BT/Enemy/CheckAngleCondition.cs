using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CheckAngle", story: "[Self] check Angle [Target]", category: "Conditions", id: "618a91ef66a7e654d303756fbe9812de")]
public partial class CheckAngleCondition : Condition
{
    [SerializeReference] public BlackboardVariable<EnemyCombatSystem> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    public override bool IsTrue()
    {
        Vector3 toTarget = (Target.Value.transform.position - Self.Value.transform.position).normalized;

        float dot = Vector3.Dot(Self.Value.transform.forward, toTarget);

        if(dot > Self.Value.attackAngle)
        {
            return true;
        }

        return false;
    }
}
