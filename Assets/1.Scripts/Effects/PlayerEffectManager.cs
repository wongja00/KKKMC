using Mirror;
using UnityEngine;

public class PlayerEffectManager : NetworkBehaviour
{
    [SerializeField] CombatSystem combatSystem;
    [SerializeField] CharacterEffectHandler characterEffectHandler;

    void Awake()
    {
        combatSystem.OnEffectFind += AddCharacterEffect;
    }

    //[Command]
    void AddCharacterEffect(GameObject prefab, EffectSocketPart part, string effectName)
    {
        characterEffectHandler.EffectAdd(part, effectName, prefab);
    }
}
