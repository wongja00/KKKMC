using Mirror;
using UnityEngine;

public class CustomNetworkAnimator : NetworkAnimator
{
    protected override void Awake()
    {
        if(animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        base.Awake();
    }
}
