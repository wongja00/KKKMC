using Mirror;
using UnityEngine;

public class PlayerBuffSystem : NetworkBehaviour
{
    [SerializeField] private CharacterBase character;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Command]
    public void CharacterStatUp(int id)
    {
        BuffDataObject buf = BuffManager.Instance.buffDataDic[id];

        if(buf == null)
        {
            Debug.Log("버프 없당 으헤헤");
            return;
        }

        character.ApplyBuff(buf.buffType, buf.buffWeight);
    }

    
}
