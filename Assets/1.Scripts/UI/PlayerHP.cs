using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{
    [SerializeField] Image hpBarImage;
    [SerializeField] TextMeshProUGUI hpText;

    public void SetHPImage(float curHp, float maxHp)
    {
        hpBarImage.fillAmount = curHp / maxHp;
        hpText.text = $"{curHp} / {maxHp}";
    }
}
