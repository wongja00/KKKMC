using UnityEngine;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviour
{

    [SerializeField] private Button button;
    [SerializeField] private GameObject mainMenuPanel; 
    [SerializeField] private GameObject closePanel; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button.onClick.AddListener(OpenMainMenupanel);
    }

    void OpenMainMenupanel()
    {
        mainMenuPanel.SetActive(true);
        closePanel.SetActive(false);
    }
}
