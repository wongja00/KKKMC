using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Globalization;


public class Dialogue : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI dialogueText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] float typingSpeed = 0.05f;
    [SerializeField] float scaleDuration = 0.15f;
    [SerializeField] AnimationCurve scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);

    Coroutine typingCoroutine;


    public void SetDialogue(string name, string dialogue)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            dialogueText.text = dialogue;

            return;
        }

        nameText.text = name;
        StartDialogue(dialogue);
    }

    void StartDialogue(string dialogue)
    {
        typingCoroutine = StartCoroutine(TypingText(dialogue));
    }

    IEnumerator TypingText(string dialogue)
    {
        dialogueText.text = dialogue;
        dialogueText.ForceMeshUpdate();

        TMP_TextInfo tTinfo = dialogueText.textInfo;
        int totalcharacters = tTinfo.characterCount;

        float[] charProgress = new float[totalcharacters];

        TMP_MeshInfo[] cachedMeshInfo = tTinfo.CopyMeshInfoVertexData();

        SetAllCharactersScaleZero(tTinfo, cachedMeshInfo);

        int currentRevealIndex = 0;
        float timer = 0f;

        while(currentRevealIndex < totalcharacters || IsAnyCharacterScaling(charProgress))
        {
            timer += Time.deltaTime;

            if(timer >= typingSpeed && currentRevealIndex < totalcharacters)
            {
                currentRevealIndex++;
                timer = 0f;
            }

            for(int i = 0; i < currentRevealIndex; i++)
            {
                if (charProgress[i] < 1f)
                {
                    charProgress[i] += Time.deltaTime / scaleDuration;
                    if (charProgress[i] > 1f) charProgress[i] = 1f;
                }
            }

            AnimateVertices(tTinfo, cachedMeshInfo, charProgress, currentRevealIndex);
            yield return null;
        }

        typingCoroutine = null;
    }

    private void SetAllCharactersScaleZero(TMP_TextInfo info, TMP_MeshInfo[] cachedMeshInfo)
    {
        for(int i = 0; i < info.characterCount; i++)
        {
            if (!info.characterInfo[i].isVisible) continue;

            int materialIndex = info.characterInfo[i].materialReferenceIndex;
            int vertexIndex = info.characterInfo[i].vertexIndex;

            Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;
            Vector3[] destinationVertices = info.meshInfo[materialIndex].vertices;

            Vector3 center = (sourceVertices[vertexIndex] + sourceVertices[vertexIndex + 2]) * 0.5f;
        
            for(int j = 0; j < 4; j++)
            {
                destinationVertices[vertexIndex + j] = center;
            }
        }
        dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private void AnimateVertices(TMP_TextInfo info, TMP_MeshInfo[] cachedMeshInfo, float[] charProgress, int maxindex)
    {
        for(int i = 0; i < maxindex; i++)
        {
            TMP_CharacterInfo charInfo = info.characterInfo[i];
            if(!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;
            Vector3[] destinationVertices = info.meshInfo[materialIndex].vertices;

            Vector3 center = (sourceVertices[vertexIndex] + sourceVertices[vertexIndex + 2]) * 0.5f;

            float currentScale = scaleCurve.Evaluate(charProgress[i]);

            for(int j = 0; j < 4; j++)
            {
                Vector3 originPos = sourceVertices[vertexIndex + j];
                destinationVertices[vertexIndex + j] = Vector3.Lerp(center, originPos, currentScale);
            }
        }

        for(int i = 0; i < info.meshInfo.Length; i++)
        {
            info.meshInfo[i].mesh.vertices = info.meshInfo[i].vertices;
            dialogueText.UpdateGeometry(info.meshInfo[i].mesh, i);
        }
    }

    private bool IsAnyCharacterScaling(float[] charProgress)
    {
        foreach(float progress in charProgress)
        {
            if (progress < 1f) return true;
        }
        return false;
    }

    public bool IsTyping()
    {
        return typingCoroutine != null;
    }
}


