using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI roundCountText;
    int roundCount = 0;
    public void SetRoundCount(int count)
    {
        roundCount = count;
        UpdateRoundCountText();
    }

    public void AddRoundCount()
    {
        roundCount++;
        UpdateRoundCountText();
    }

    private void UpdateRoundCountText()
    {
        roundCountText.text = $"현재 라운드: {roundCount}";
    }
}
