using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [SerializeField] GameObject damageTextParent;
    
    List<DamageTextEffect> damageTextEffects = new();

    private void Awake()
    {
        foreach (Transform child in damageTextParent.transform)
        {
            DamageTextEffect damageTextEffect = child.GetComponent<DamageTextEffect>();
            if (damageTextEffect != null)
            {
                damageTextEffects.Add(damageTextEffect);
                child.gameObject.SetActive(false);
            }
        }

        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void ShowDamageText(Vector3 pos, float damage)
    {
        foreach (DamageTextEffect damageTextEffect in damageTextEffects)
        {
            if (!damageTextEffect.gameObject.activeInHierarchy)
            {
                damageTextEffect.transform.position = pos;
                damageTextEffect.gameObject.SetActive(true);
                damageTextEffect.SetDamage(damage);
                break;
            }
        }
    }
}
