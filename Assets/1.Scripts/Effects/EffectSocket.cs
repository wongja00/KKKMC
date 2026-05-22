using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class EffectSocket : NetworkBehaviour
{
    public Transform socketTransfrom;
    public EffectSocketPart part;
    Dictionary<string, VisualEffect> visualEffectDic = new Dictionary<string, VisualEffect>();

    [SerializeField] VisualEffect hitEffectPrefab;

    //[ClientRpc]
    public void PlayEffect(string effectName)
    {
        if(visualEffectDic.TryGetValue(effectName, out VisualEffect effect))
        {
            effect.Play();
        }
    }

    //[Server]
    public void AddEffect(string effectName, GameObject obj)
    {
        GameObject prefab = Instantiate(obj);
        //NetworkServer.Spawn(prefab);

        VisualEffect effect = prefab.GetComponentInChildren<VisualEffect>(); 
        visualEffectDic.TryAdd(effectName, effect);
        hitEffectPrefab = effect;

        SetVFXTransform(prefab);
        
    }

    public void RemoveEffect(string effectName)
    {
        if(visualEffectDic.TryGetValue(effectName, out VisualEffect effect))
        {
            NetworkServer.Destroy(effect.gameObject);
       
            visualEffectDic.Remove(effectName);
        }

    }

    //[ClientRpc]
    void SetVFXTransform(GameObject effect)
    {
        effect.transform.SetParent(socketTransfrom);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
    }

}
