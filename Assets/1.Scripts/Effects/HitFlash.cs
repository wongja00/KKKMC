using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] Material characterMat;
    [SerializeField] CharacterBase character;

    [Header("Flash Settings")]
    [SerializeField] float flashDuration = 0.25f;
    [SerializeField] Color flashColor = Color.white;
    [SerializeField] string colorProperty = "_HitColor";

    Renderer targetRenderer;
    Material instanceMat;
    MaterialPropertyBlock propBlock;
    Coroutine flashCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        character.OnHpDecrease += characterHitFlash;

        // 렌더러 및 머티리얼 준비
        targetRenderer = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if(characterMat == null)
        {
            characterMat = targetRenderer.material;
        }

        if (characterMat != null)
        {
            // 지정된 머티리얼을 인스턴스화하여 다른 오브젝트에 영향 안주도록 함
            instanceMat = new Material(characterMat);
            if (targetRenderer != null)
            {
                targetRenderer.material = instanceMat;
            }
        }
        else if (targetRenderer != null)
        {
            // 렌더러의 material 프로퍼티는 인스턴스이므로 바로 사용 가능
            instanceMat = targetRenderer.material;
        }
    }
    void OnDestroy()
    {
        if (character != null)
            character.OnHpDecrease -= characterHitFlash;
    }

    void characterHitFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    IEnumerator FlashCoroutine()
    {
        float elapsed = 0f;

        // 즉시 색상 설정(쉐이더가 해당 프로퍼티를 가지고 있을 때만 적용)
        if (targetRenderer != null)
        {
            propBlock.Clear();
            if (HasColorProperty(targetRenderer, colorProperty))
                propBlock.SetColor(colorProperty, flashColor);

            targetRenderer.SetPropertyBlock(propBlock);
        }
        else if (instanceMat != null)
        {
            if (instanceMat.HasProperty(colorProperty))
                instanceMat.SetColor(colorProperty, flashColor);
        }

        // 페이드아웃
        while (elapsed < flashDuration)
        {
            float t = Mathf.Clamp01(elapsed / flashDuration);
            float value = Mathf.Lerp(1f, 0f, t);

            if (targetRenderer != null)
            {
                propBlock.Clear();
                if (HasColorProperty(targetRenderer, colorProperty))
                    propBlock.SetColor(colorProperty, flashColor);
                targetRenderer.SetPropertyBlock(propBlock);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 안전하게 0으로 초기화
        if (targetRenderer != null)
        {
            propBlock.Clear();
            if (HasColorProperty(targetRenderer, colorProperty))
                propBlock.SetColor(colorProperty, Color.clear);
            targetRenderer.SetPropertyBlock(propBlock);
        }
        else if (instanceMat != null)
        {
            if (instanceMat.HasProperty(colorProperty))
                instanceMat.SetColor(colorProperty, Color.clear);
        }

        flashCoroutine = null;
    }

    // Renderer의 머티리얼에서 컬러 프로퍼티가 존재하는지 안전하게 검사
    bool HasColorProperty(Renderer rend, string propName)
    {
        if (rend == null) return false;
        var mats = rend.sharedMaterials;
        foreach (var m in mats)
        {
            if (m != null && m.HasProperty(propName)) return true;
        }
        return false;
    }
}
