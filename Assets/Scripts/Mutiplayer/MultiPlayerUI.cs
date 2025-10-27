using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Mirror;

public class MultiPlayerUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TextMeshProUGUI statusText;

    private NetworkManager networkManager;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();

        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

     public void StartHost()
    {
        networkManager.StartHost();
        statusText.text = "호스트 시작됨";

    }
     
     private void StartClient()
     {
        networkManager.networkAddress = ipInputField.text;
        networkManager.StartClient();
        statusText.text = "클라이언트 연결 중...";
     }

     private void MatchList()
     {
        
     }
}
