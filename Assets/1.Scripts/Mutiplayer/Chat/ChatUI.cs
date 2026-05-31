using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;

public class ChatUI : MonoBehaviour
{
    public TMP_InputField chatInputField;
    public TextMeshProUGUI chatOutputText;

    public Toggle chatToggle;
    public Toggle systemToggle;
    private Scrollbar scrollbar;

    public GameObject chatPanel;

    private void Awake()
    {
        chatPanel = transform.GetChild(0).gameObject;
    }

    //public string mode = "Chat"; // "Chat" or "System"

    public void CmdSendMessage(string message)
    {
        //if (string.IsNullOrEmpty(message)) return;
            //RpcSendMessage(message);
    }

    public void RpcSendMessage(string mode)
    {
        //if (string.IsNullOrEmpty(message)) return;
        chatOutputText.text = ChatManager.Instance.GetTexts(mode).ToString();
        chatInputField.text = string.Empty;
    }

    public void SetText(StringBuilder text)
    {
        chatOutputText.text = text.ToString();
    }
}
