using Mirror;
using UnityEngine;

public class DoorInteractor : NetworkBehaviour, Interactable
{
    [SerializeField]
    Animator myAnim;

    public bool inZone = false;

    [SyncVar(hook =nameof(OpenDoorAnim))]
    public bool isOpen = false;

    [SyncVar]
    public bool canControl = true;

    [SerializeField] KeyCode openDoorKey = KeyCode.E;

    void OpenDoorAnim(bool oldValue, bool newValue)
    {
         myAnim.SetBool("isOpen", newValue);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
            inZone = true; 
    }

    void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player")
            inZone = false;
    }

    [Command(requiresAuthority = false)]
    virtual public void Interact()
    {
        if(canControl == false) return;

        isOpen = !isOpen;
    }
}
