using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class RoomCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomName;
    [SerializeField] private TextMeshProUGUI hostName;
    [SerializeField] private TextMeshProUGUI userCount;
    
    [SerializeField] private Button connectButton; 


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    public void SetTexts(string roomname, string hostname, int curuser, int maxuer)
    {
        roomName.text = roomname;
        hostName.text = hostname;
        userCount.text = $"{curuser} / {maxuer}";
    }

    public void OnAddButtonEvent(UnityAction connectEvent)
    {
        connectButton.onClick.AddListener(connectEvent);
    }

}
