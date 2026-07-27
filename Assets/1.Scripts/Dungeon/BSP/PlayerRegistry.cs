using UnityEngine;
using System.Collections.Generic;

public class PlayerRegistry
{
    private static readonly List<Transform> players = new List<Transform>();

    public static IReadOnlyList<Transform> Players => players;

    public static void Register(Transform player)
    {
        if(player == null) return;
        if(!players.Contains(player))
            players.Add(player);
    }

    public static void Unregister(Transform player)
    {
        if(player == null) return;

        if (players.Contains(player))
            players.Remove(player);
    }
    
}

public class EnemyRegistry
{
    private static readonly List<Transform> enemies = new List<Transform>();

    public static IReadOnlyList<Transform> Enemies => enemies;

    public static void Register(Transform enemy)
    {
        if (enemy == null) return;
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public static void Unregister(Transform enemy)
    {
        if (enemy == null) return;

        if (enemies.Contains(enemy))
            enemies.Remove(enemy);
    }
}
