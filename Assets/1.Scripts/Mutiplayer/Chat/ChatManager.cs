using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine.SceneManagement;

public class ChatManager : NetworkBehaviour
{
    //√§∆√, Ω√Ω∫≈€ µÒº≈≥ ∏Æ
    Dictionary<string, StringBuilder> chatHistory = new Dictionary<string, StringBuilder>();

    public static ChatManager Instance { get; private set; }
    private ChatUI chatUI;

    [SerializeField] KeyCode onChatKey = KeyCode.T;
    [SerializeField] KeyCode offChatKey = KeyCode.Escape;
    [SerializeField] KeyCode sendChatKey = KeyCode.Return;

    [SerializeField] Toggle chatToggle;
    [SerializeField] Toggle systemToggle;

    const int MaxSoftLimit = 15000;
    const int DeleteChunkSize = 1000;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        chatHistory["Chat"] = new StringBuilder(16384);

        chatHistory["System"] = new StringBuilder(16384);

        SceneManager.sceneLoaded += FindChatUI;
    }
    private void Start()
    {
        chatUI = InteractUIManager.Instance.chatUI;
        chatToggle = chatUI.chatToggle;
        systemToggle = chatUI.systemToggle;

        chatToggle.onValueChanged.AddListener(isOn => ChangeChatMode(isOn));
        systemToggle.onValueChanged.AddListener(isOn => ChangeSystemMode(isOn));
    }

    void FindChatUI(Scene scene, LoadSceneMode mode)
    {
        chatUI = InteractUIManager.Instance.chatUI;

        chatToggle = chatUI.chatToggle;
        systemToggle = chatUI.systemToggle;

        chatToggle.onValueChanged.AddListener(isOn => ChangeChatMode(isOn));
        systemToggle.onValueChanged.AddListener(isOn => ChangeSystemMode(isOn));

        chatUI.SetText(chatHistory.ContainsKey("Chat") ? chatHistory["Chat"] : new StringBuilder());
    }

    private void Update()
    {
        ToggleChat();
        SendMessage();
    }

    void ToggleChat()
    {
        if(Input.GetKeyDown(onChatKey))
        {
            if (chatUI == null) return;
            {
                chatUI.chatPanel.SetActive(true);
                if(chatUI.chatPanel.activeSelf)
                {
                    chatUI.chatInputField.ActivateInputField();
                    chatUI.chatInputField.Select();
                }
            }
        }
        else if(Input.GetKeyDown(offChatKey))
        {
            if (chatUI == null) return;
                chatUI.chatPanel.SetActive(false);
        }

    }

    void SendMessage()
    {
        if(Input.GetKeyDown(sendChatKey) && chatUI.gameObject.activeSelf)
        {
            string message = chatUI.chatInputField.text;
            AddChatMessage(message);
            //chatUI.CmdSendMessage(message);
        }
    }

    private void ChangeChatMode(bool isChat)
    {
        chatUI.SetText(chatHistory.ContainsKey("Chat") ? chatHistory["Chat"] : new StringBuilder());
    }

    private void ChangeSystemMode(bool isSystem)
    {
        chatUI.SetText(chatHistory.ContainsKey("System") ? chatHistory["System"] : new StringBuilder());
    }

    public StringBuilder GetTexts(string mode)
    {
        if (chatHistory.ContainsKey(mode))
        {
            return chatHistory[mode];
        }
        return new StringBuilder();
    }

    [ClientRpc]
    void AddText(string mode, string message)
    {
        if (!chatHistory.ContainsKey(mode))
        {
            chatHistory[mode] = new StringBuilder();
        }

        //HH.MM.SS
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");

        chatHistory[mode].AppendLine($"[{timestamp}] {message}");

        if(chatHistory[mode].Length > MaxSoftLimit)
        {
            int targetIndex = DeleteChunkSize;

            string currentText = chatHistory[mode].ToString();
            int nextLineBreak = currentText.IndexOf('\n', targetIndex);

            if(nextLineBreak != -1)
            {
                chatHistory[mode].Remove(0, nextLineBreak + 1);
            }
            else
            {
                chatHistory[mode].Remove(0, targetIndex);
            }
        }

        chatUI.RpcSendMessage(mode);


    }


    [Command(requiresAuthority = false)]
    public void AddChatMessage(string message)
    {
        AddText("Chat", message);
    }


    [Command(requiresAuthority = false)]
    public void AddSystemMessage(string message)
    {
        AddText("System", message);
    }
}
