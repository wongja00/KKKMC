using UnityEngine;
using System.Collections.Generic;

public class RoomNode : MonoBehaviour
{
    RoomType type;
    int depth;          // 시작점으로부터 거리
    List<RoomNode> next;
}

public enum RoomType {
    Start,
    Combat,
    Elite,
    Event,
    Treasure,
    MiniBoss,
    Boss
}
