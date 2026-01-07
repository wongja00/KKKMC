using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dialogueOutput;

    [SerializeField] private TextMeshProUGUI inputText;

    [SerializeField] public Button sendButton;

    public UnityEvent OnSend;

    public string sendText { get; private set; } = "";

    private void Awake()
    {
        sendButton.onClick.RemoveAllListeners();

        sendButton.onClick.AddListener(SendMessage);
    }

    public void SendMessage()
    {
        sendText = inputText.text;

        OnSend?.Invoke();
    }

    public void SetDialogue(string msg)
    {
        dialogueOutput.text = msg;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
