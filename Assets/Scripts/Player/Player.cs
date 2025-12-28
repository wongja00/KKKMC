using Mirror;
using UnityEngine;

public class Player : CharacterBase
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!isLocalPlayer) return;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        PlayerRegistry.Register(transform);
    }

    void OnDisable()
    {
        PlayerRegistry.Unregister(transform);
    }
}
