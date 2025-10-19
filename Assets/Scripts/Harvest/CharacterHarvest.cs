using Unity.VisualScripting;
using System;
using UnityEngine;

//플레이어 채칩/채굴 컨트롤러및 시스템
public class CharacterHarvest : MonoBehaviour
{
    private ToolItem curToolItem = null;

    [SerializeField]
    private KeyCode harvestKey = KeyCode.Mouse0;

    private LayerMask nodeLayer;

    [SerializeField] private GameObject harvestUI;

    //자원 채취 이벤튼
    public event Action<GameObject, int> OnHarvest;

    void Awake()
    {

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nodeLayer = LayerMask.GetMask("Node");
    }

    // Update is called once per frame
    void Update()
    {
        if(ShowAndCheckNode(out Node outNode))
        {
            if(Input.GetKeyDown(harvestKey))
            {
                DoHarvest(outNode);
            }
        }

    }

    public void SetCurItem(Item inItem)
    {
        if (inItem is ToolItem tool)
        {
            curToolItem = tool;
            return;
        }

        Debug.Log("채집/채굴 도구가 아이다 마!");
    }

    //채집기능
    private void DoHarvest(Node InNode)
    {
        if(curToolItem == null || InNode == null)
            return;

        var items = InNode.GetItemObjectArray();

        if (items != null && items.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, items.Length);
            int randomAmount = UnityEngine.Random.Range(1, 3);
            var selectedItem = items[randomIndex];


            OnHarvest?.Invoke(selectedItem, randomAmount);
        }
    }

    private bool ShowAndCheckNode(out Node OutNode)
    {
        if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 3f, nodeLayer))
        {
            if(curToolItem == null)
            {
                OutNode = null;
                return false;
            }

            if(harvestUI != null)
            {
                harvestUI.SetActive(true);                
            }

            if(hit.transform.TryGetComponent<Node>(out Node outNode))
            {                
                OutNode = outNode;
            } 
            else
            {
                OutNode = null;
            }

            return true;
        }       
        else
        {
            if(harvestUI != null)
                harvestUI.SetActive(false);
            
            OutNode = null;
            return false;
        }
    }
}
