using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "ItemDatasContaner", menuName = "Scriptable Objects/ItemDatasContaner")]
public class ItemDatasContaner : ScriptableObject
{
    public ScriptableItemData[] itemDatas;
}
