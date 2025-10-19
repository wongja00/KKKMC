using UnityEngine;

public class TreeNode : MonoBehaviour, Node
{
    [SerializeField] string nodeID;

    private NodeData nodeData;

    private int curAmount = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NodeData temp = NodeManager.instance.GetNodeData(nodeID);
        nodeData = temp;
        curAmount = temp.maxAmount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public NodeData GetNodeData()
    {
        return nodeData;
    }

    public void SetNodeData(ref NodeData data)
    {
        nodeData = data;
    }

    public string GetNodeID()
    {
        return nodeData.NodeID;
    }

    public string GetNodeName()
    {
        return nodeData.NodeName;
    }

    public int GetNodeAmount()
    {
        return curAmount;
    }

    public void SetNodeAmount(int amount)
    {
        curAmount = amount;
    }

    public NodeType GetNodeType()
    {
        return nodeData.nodeType;
    }

    public GameObject[] GetItemObjectArray()
    {
        return nodeData.GetItemArray;
    }
}
