using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusCard : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI statText;

    int Value = 0;
    string statName = "";

    public void SetCard(string text, int Value)
    {
        statName = text;
        this.Value = Value;

        UpdateText();
    }

    public void UpdateText()
    {
        statText.text = $"{statName}: {Value}";
    }

    public void SetValue(int Value)
    {
        this.Value = Value;

       UpdateText();
    }
}
