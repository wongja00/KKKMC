using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//나무 광석등 자원 오브젝트에 쓰일 인터페이스 
public interface Node
{    
    NodeData GetNodeData();
    void SetNodeData(ref NodeData data);
    string GetNodeID();
    string GetNodeName();
    int GetNodeAmount();
    void SetNodeAmount(int amount);

    GameObject[] GetItemObjectArray();

    NodeType GetNodeType();
}

public enum NodeType
{
    None,
    Tree,
    Ore,
}
