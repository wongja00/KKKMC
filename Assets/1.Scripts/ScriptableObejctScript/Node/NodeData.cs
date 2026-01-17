using System;
using System.Collections.Generic;
using UnityEngine;

//필드상에 있는 자원(나무 광석등)에 대한 스크립터블 오브젝트
[CreateAssetMenu(fileName = "NodeData", menuName = "Scriptable Objects/NodeData")]
public class NodeData : ScriptableObject
{
    [Header("자원 기본 정보")]
    public string NodeID = "0";
    public string NodeName = "Unknown Node";
    [TextArea(3, 5)]
    public string description = "Unknown Node";
    public NodeType nodeType = NodeType.None;
    public int maxAmount = 0;
    public float coolTime = 1.0f;

    [Header("얻을 수 있는 아이템템")]
    public GameObject[] GetItemArray;
}
