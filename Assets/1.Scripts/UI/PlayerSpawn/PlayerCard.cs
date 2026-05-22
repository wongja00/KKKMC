using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerCard : MonoBehaviour
{
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI status;
    public TextMeshProUGUI desc;
    public Image thumnail;
    public Button choiceButton;
    public Button statusButton;
    public GameObject statusView;

    void Awake()
    {
        statusButton.onClick.AddListener(ToggleStatusView);
    }

    public void SetCardData(CharacterInfo info)
    {
        characterName.text = info.characterName;
        desc.text = info.desc;

        thumnail.sprite = info.thumnail;

        status.text = 
        $"힘: {info.status.strength}\n방어력: {info.status.defense}\n민첩성: {info.status.agility}\n지능: {info.status.intelligence}\n공속: {info.status.attackSpeed}\n치확: {info.status.critChance}\n치피: {info.status.critDamage}";

        choiceButton.onClick.AddListener(
            () => {
                var player = NetworkClient.localPlayer ? NetworkClient.localPlayer.GetComponent<Player>() : null;
                if (player != null)
                {
                    player.CmdReplaceCharacter(info.ID);
                }
                else
                {
                    
                }
            }
            );
    }

    void ToggleStatusView()
    {
        statusView.SetActive(!statusView.activeSelf);
    }
}
