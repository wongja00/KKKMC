using UnityEngine;

[CreateAssetMenu(fileName = "VehicleItemData", menuName = "VehicleItemData")]
public class VehicleItemData : ScriptableObject
{
   [Header("자동차 부품 정보")]
    public ItemType vehiclePartType = ItemType.VehicleFrame;

    [Header("자동차 부품 속성")]
    public float dirability = 100f;
    public float weight = 1f;

    [Header("3D 모델")]
    public GameObject model;

}
