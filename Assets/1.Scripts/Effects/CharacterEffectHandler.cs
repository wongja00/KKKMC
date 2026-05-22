using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public enum EffectSocketPart
{
    LeftHand,
    RightHand,
    LeftFoot,
    RightFoot,
    ETC
}

public class CharacterEffectHandler : NetworkBehaviour
{
    //해시용 리스트(일단 인스펙터에 넣고 딕셔너리로 변환할거임)
    public List<EffectSocket> effectSocketList = new List<EffectSocket>();

    private Dictionary<EffectSocketPart, EffectSocket> effectSocketDict = new Dictionary<EffectSocketPart, EffectSocket>();

    private void Awake()
    {
        foreach(EffectSocket effectSocket in effectSocketList)
        {
            if (!effectSocketDict.ContainsKey(effectSocket.part))
            {
                effectSocketDict.Add(effectSocket.part, effectSocket);
            }
        }
    }

    // 부위명으로 소켓을 가져오는 메소드
    public EffectSocket GetEffectSocket(EffectSocketPart part)
    {
        effectSocketDict.TryGetValue(part, out var socket);
        return socket;
    }

    public void EffectPlay(EffectSocketPart part, string effectName)
    {
        if(effectSocketDict.TryGetValue(part, out EffectSocket socket))
        {
            socket.PlayEffect(effectName);
        }
    }

    public void EffectAdd(EffectSocketPart part, string effectName, GameObject effectPrefab)
    {
        if(effectSocketDict.TryGetValue(part, out EffectSocket socket))
        {
            socket.AddEffect(effectName, effectPrefab);
        }
    }

    public void EffectRemove(EffectSocketPart part, string effectName)
    {
        if(effectSocketDict.TryGetValue(part, out EffectSocket socket))
        {
            socket.RemoveEffect(effectName);
        }
    }
}
