using Mirror;
using UnityEngine;

public class DoorInteractor : NetworkBehaviour, Interactable
{
    [SerializeField]
    Animator myAnim;

    public bool inZone = false;

    [SyncVar(hook =nameof(OpenDoorAnim))]
    public bool isOpen = false;

    [SerializeField] KeyCode openDoorKey = KeyCode.E;


    void Start()
    {
        //myAnim = GetComponent<Animator>();
    }

    void Update()
    {
        if(inZone == true && Input.GetKeyDown(openDoorKey))
        {
            //OpenDoor();
        }
    }    
    
    void OpenDoor()
    {
        //if(inZone == true)
            
        Debug.Log("열려고 함");
    }

    void OpenDoorAnim(bool oldValue, bool newValue)
    {
         myAnim.SetBool("isOpen", newValue);

         Debug.Log("문");
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

    //[Command(requiresAuthority = false)]
    virtual public void Interact()
    {
        
        Debug.Log("상호작용");
        OpenDoor();
        isOpen = !isOpen;
    }
}
