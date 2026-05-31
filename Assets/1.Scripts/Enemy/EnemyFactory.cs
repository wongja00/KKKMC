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
    Elite,
    Boss
}

public static class EnemyFactory
{
    static Dictionary<EnemyType, List<GameObject>> enemyPrefaps;

    public static void Init(Dictionary<EnemyType, List<GameObject>> prefaps)
    {
        enemyPrefaps = prefaps;
    }

    public static void Init(Dictionary<string, GameObject> prefaps)
    {

    }

    public static GameObject Spawn(EnemyType type, Vector3 pos)
    {
        if(!enemyPrefaps.ContainsKey(type)) return null;

        GameObject e = Object.Instantiate(enemyPrefaps[type][Random.Range(0, enemyPrefaps[type].Count)], pos, Quaternion.identity);

        if(NavMesh.SamplePosition(pos, out NavMeshHit hit, 50f, NavMesh.AllAreas))
        {
            e.transform.position = hit.position;
        }
        else Debug.LogWarning("NavMesh Sample 실패");

        Debug.Log("적 스폰");

        NetworkServer.Spawn(e);
        return e;
    }
}
