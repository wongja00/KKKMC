using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class NPCDialogueController : MonoBehaviour
{
    public LLMChat llm;
    public ElevenLabsTTS tts;

    //간단 히스토리(너무 길어지면 느려짐: 최근 6 ~ 10개만 유지 추천)
    private readonly List<LLMChat.Msg> history = new();

    private void Awake()
    {
        history.Clear();
        history.Add(new LLMChat.Msg("system",
                        "너는 게임의 안내자 NPC(중년 남성)다. 한국어로 자연스러운 구어체로, 1~2문장만 말해. " +
            "설명/번역투/메타발언 금지. 친절하고 차분하게."));
    }

    public void PlayerSays(string playerText)
    {
        StartCoroutine(TalkRoutine(playerText));
    }
    IEnumerator TalkRoutine(string playerText)
    {
        history.Add(new LLMChat.Msg("user", playerText));

        //히스토리 과식 방지: system + 최근 8개만 남기기
        TrimHistoryKeepLast(9);

        string reply = null;
        string err = null;

        yield return llm.Chat(history,
            onDone: (r) => reply = r,
            onError: (e) => err = e);



        if(!string.IsNullOrEmpty(err))
        {
            Debug.Log("LLM error: " + err);

            yield break;
        }

        history.Add(new LLMChat.Msg("assistant", reply));

        //TTS는 비용/속도/ 있으니 너무 긴 답은 자르기 추천
        string speakText = reply.Length > 120 ? reply.Substring(0, 120) : reply;

        Debug.Log($"[NPC] Player: {playerText}");
        
        if (!string.IsNullOrEmpty(err)) { Debug.LogError("[NPC] LLM error: " + err); yield break; }

        Debug.Log($"[NPC] NPC Reply: {reply}");
        Debug.Log($"[NPC] Speak Text(len={speakText.Length}): {speakText}");

        yield return tts.Speak(speakText,
            onDone: () => { },
            onError: (e) => Debug.LogError("TTS Error: " + e));
    }

     void TrimHistoryKeepLast(int keepCountIncludingSystem)
    {
        //ststem 1개는 유지
        if(history.Count <= keepCountIncludingSystem) return;

        var system = history[0];
        int keep = keepCountIncludingSystem - 1;

        var tail = history.GetRange(history.Count - keep, keep);
        history.Clear();
        history.Add(system);
        history.AddRange(tail);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayerSays("처음인데, 뭘 하면 되죠?");
        }
    }
}
