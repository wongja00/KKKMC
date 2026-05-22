using UnityEngine;
using Mirror;

public class PlayerModel : MonoBehaviour
{
    public Animator animator;
    public SkinnedMeshRenderer meshRenderer;
    
    void Awake()
    {
        animator = GetComponent<Animator>();

        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        
        if(transform.parent.GetComponent<NetworkAnimator>().animator == null) transform.parent.GetComponent<NetworkAnimator>().animator = animator;
    }
}
