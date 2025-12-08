using UnityEngine;

public interface Equable
{
    public bool GetIsEquipped();
    public void SetIsEquipped(bool isEquipped);

    public KeyCode GetKeyCode();

    public void UseItem();

}
