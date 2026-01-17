using UnityEngine;

public interface ToolBase
{
    public ToolType GetToolType();

    public int GetDurability();

    public int GetHarvestAmount();

}

public enum ToolType
{
    None,
    Axe,
    Pickaxe
}
