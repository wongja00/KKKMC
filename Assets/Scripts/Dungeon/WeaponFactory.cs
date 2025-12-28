using UnityEngine;
using System.Collections.Generic;
using Mirror;

public static class WeaponFactory
{
    static Dictionary<GunType, List<GameObject>> weaponPrefabs;

    public static void Init( Dictionary<GunType, List<GameObject>> prefabs)
    {
        if (weaponPrefabs == null)
            weaponPrefabs = new Dictionary<GunType, List<GameObject>>();

        foreach (var pair in prefabs)
        {
            if (weaponPrefabs.ContainsKey(pair.Key))
            {
                weaponPrefabs[pair.Key].AddRange(pair.Value);
            }
            else
            {
                weaponPrefabs[pair.Key] = new List<GameObject>(pair.Value);
            }
        }
    }

    public static GameObject SpawnRandomWeaponByType(GunType type, Vector3 pos)
    {
        List<GameObject> weapons = new();
        bool isweapon = weaponPrefabs.TryGetValue(type,out weapons);

        if(!isweapon) return null;

        if(weapons.Count <= 0)
        {
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, weapons.Count);
        GameObject weapon = weapons[randomIndex];

        GameObject go = UnityEngine.Object.Instantiate(weapon, pos, Quaternion.identity);

        if(NetworkServer.active)
            NetworkServer.Spawn(go);

        return go;
    }
}
