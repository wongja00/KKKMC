using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerChoiceUI : MonoBehaviour
{
    [SerializeField] private PlayerCard cardPrefab;
    [SerializeField] private Transform choiceParent;

    //캐릭터 선택창에서 캐릭터들 정보 
    public void SetCards(CharacterInfo[] infos)
    {
        foreach(CharacterInfo player in infos)
        {
            PlayerCard card =  Instantiate(cardPrefab, choiceParent);
            card.SetCardData(player);
        }
    }
}
