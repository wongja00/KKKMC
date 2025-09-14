using UnityEngine;

//모듈 인터페이스
public interface ModuleSlot
{
    public ModuleType GetModuleType();

}

public enum ModuleType
{
    None,
    WheelModule,
    WeaponModule,
    DefenseModule
}
