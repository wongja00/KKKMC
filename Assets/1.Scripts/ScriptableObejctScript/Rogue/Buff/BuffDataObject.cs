using UnityEngine;

[CreateAssetMenu(fileName = "BuffDataObject", menuName = "RogueLite/BuffDataObject")]
public class BuffDataObject : ScriptableObject
{
    public int buffID = 0;
    public string buffName = "";
     [TextArea(3, 5)]
    public string buffDesc = "";

    public BuffType buffType = BuffType.None;

    //버프 가중치
    public float buffWeight = 0f;

    //버프 지속 시간 
    public float buffTime = 9999f;

}
