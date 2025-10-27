using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button makeRoomButon;
    [SerializeField] private Button searchRoomButton;
    [SerializeField] private Button gameExitButton;

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject makeRoomPanel;
    [SerializeField] private GameObject searchRoomPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        makeRoomButon.onClick.AddListener(() => {OpenPanel(makeRoomPanel); });

        searchRoomButton.onClick.AddListener(() => OpenPanel(searchRoomPanel));

        gameExitButton.onClick.AddListener(() => Application.Quit());
    }

    private void OpenPanel(GameObject panel)
    {
        panel.SetActive(true);

        mainMenuPanel.SetActive(false);
    }
}
