using UnityEngine;

[CreateAssetMenu(fileName = "NewGunData", menuName = "Scriptable Objects/GunDataScriptableObject")]
public class GunDataScriptableObject : ScriptableObject
{
    public GunType type = GunType.AssultRifle;

    //데미지
    public float damage = 10f;
    //연사 속도
    public float fireRate = 0.2f;
    //탄창 용량
    public int maxAmmo = 30;
    //장전 시간
    public float reloadTime = 1.5f;

    // 연발 가능 여부
    public bool isAutomatic = false;
}

public enum GunType
{
    AssultRifle,
    Pistol,
    Rifle,
    ShotGun
}

//총기에 따라 레이어가 달라지므로 일단 코드를 여기에 둠
public enum AnimLayer
{
    Base, // 
    Rifle
}