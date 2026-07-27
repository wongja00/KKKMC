using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "FindNearTarget", story: "[Self] Find [NearTarget]", category: "Action", id: "0e2bbaf008d74dbd2825a92ad712ce43")]
public partial class FindNearTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> NearTarget;

    CharacterBase characterBase;
    protected override Status OnStart()
    {
        characterBase = Self.Value.GetComponent<CharacterBase>();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(GetNearestPlayer() == null)
            return Status.Failure;



        NearTarget.Value = GetNearestPlayer().gameObject;

        return Status.Success;
    }

    protected override void OnEnd()
    {
    }

    Transform GetNearestPlayer()
    {
        Transform nearest = null;
        float minDist = float.MaxValue;

        var players = PlayerRegistry.Players;

        switch(characterBase.characterTeam)
        {
            case Team.Player:
                players = EnemyRegistry.Enemies;


                break;

            case Team.Enemy:



                break;

            default:


                break;
        }


        if(players.Count == 0) return null;

        foreach (var p in players)
        {
            if (p == null) continue;

            float d = Vector3.Distance(Self.Value.transform.position, p.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = p.transform;
            }
        }
        return nearest;
    }
}

