using System.Collections.Generic;
using UnityEngine;

public class NodeManager : MonoBehaviour
{
    [Header("자원데이터 컨테이너")]
    public NodeDataContaner[] Contaners;

    public static NodeManager instance;

    private Dictionary<string, NodeData> nodeDataDic = new Dictionary<string, NodeData>();

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        
        foreach(NodeDataContaner contaner in Contaners)
        {
            foreach(NodeData datas in contaner.nodeDatas)
            {
                nodeDataDic.Add(datas.NodeID, datas);
            }
        }
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public NodeData GetNodeData(string ID)
    {
        nodeDataDic.TryGetValue(ID, out NodeData data);

        if(data == null)
        {
            return new NodeData();
        }

        return data;
    }
}
