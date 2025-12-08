using UnityEngine;

public class Enemy : CharacterBase
{
    [SerializeField] private Animator animator; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CurHP = MaxHP;
        isDead = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    override public void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        Debug.Log($"데미지: {CurHP}");

        if(CurHP <= 0 && isDead == false)
        {
            Die();
        }

    }


    public void SetIsDead(bool IsDead)
    {  
        isDead = IsDead;

        if(isDead == true)
        {
            Die();
        }
    }

    public void Die()
    {
        if(isDead) return;
        isDead = true;

        animator.SetBool("isDead", true);
        
            Debug.Log($"사망");
    }
}
