using System.Collections;
using System.Security;
using UnityEngine;
using UnityEngine.UI;

public class ReloadCoolTImeUI : MonoBehaviour
{
    [SerializeField] private Image cooTimeImage;
    private Coroutine reloadCoroutine;

    public void RunCoolTime(float time)
    {
        reloadCoroutine = StartCoroutine(CoolTime(time));
    }

    IEnumerator CoolTime(float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;
            cooTimeImage.fillAmount = Mathf.Clamp01(1f - (elapsed / time));
            yield return null; // 매 프레임 부드럽게 반영
        }
        cooTimeImage.fillAmount = 0f; // 장전 끝나면 완전히 비움
        reloadCoroutine = null;
    }

    public void CancleReload()
    {
        if(reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            cooTimeImage.fillAmount = 0f;
            reloadCoroutine = null;
        }
    }
}
