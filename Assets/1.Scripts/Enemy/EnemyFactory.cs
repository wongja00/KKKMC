using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.AI;
using Unity.VisualScripting;
using Mirror;

public enum EnemyType
{
    Melee,
    Ranged,
    Elite
}

public static class EnemyFactory
{
    static Dictionary<EnemyType, GameObject> enemyPrefaps;

    public static void Init(Dictionary<EnemyType, GameObject> prefaps)
    {
        enemyPrefaps = prefaps;
    }



    public static GameObject Spawn(EnemyType type, Vector3 pos)
    {
        if(!enemyPrefaps.ContainsKey(type)) return null;

        GameObject e = Object.Instantiate(enemyPrefaps[type], pos, Quaternion.identity);

        //Debug.Log("적 생성");

        if(NavMesh.SamplePosition(pos, out NavMeshHit hit, 50f, NavMesh.AllAreas))
        {
            e.transform.position = hit.position;
        }
        else Debug.LogWarning("NavMesh Sample 실패");

        NetworkServer.Spawn(e);
        return e;
    }
}
