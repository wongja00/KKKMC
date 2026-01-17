using UnityEngine;

[CreateAssetMenu(fileName = "HarvestToolData", menuName = "Scriptable Objects/HarvestToolData")]
public class HarvestToolData : ScriptableObject
{
        [Header("채집/채광 장비 기본 정보")]
    public string toolID = "0";
    public string toolName = "Unknown Tool";
    [TextArea(3, 5)]
    public ToolType toolType = ToolType.None;
    public int maxDuration = 0;
    public int harvestAmount = 1;

}
