using UnityEngine;
using TMPro;

public class IngreDientText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI IngreName;
    [SerializeField] TextMeshProUGUI IngreAmount;

    private int max = 0;

    private int cur = 0;

    public void SetNameText(string name)
    {
        IngreName.text = name;
    }

    public void SetAmountText(int amount)
    {
        max = amount;
        IngreAmount.text = "<color=red>" + cur.ToString() + "</color>" + "/" + amount;
    }

    public void SetCurAmountText(int amount)
    {
        cur = amount;

        string colorTag = cur >= max ? "<color=green>" : "<color=red>";

        IngreAmount.text = colorTag + cur.ToString() + "</color>/" + max;
    }

    public void OnEnable()
    {
        string colorTag = cur >= max ? "<color=green>" : "<color=red>";

        IngreAmount.text = colorTag + cur.ToString() + "</color>/" + max;
    }
}
