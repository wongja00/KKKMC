using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Mirror.BouncyCastle.Asn1;



public class LLMChat : MonoBehaviour
{
    [Header("LM Studio")]
    public string baseUrl = "http://127.0.0.1:1234/v1";//내 PC
    public string model = "gemma-3-1b-it-qat";

    [Header("Generation")]
    [Range(0f, 2f)] public float temperature = 0.7f;
    public int maxTokens = 120;

    [Header("Debug")]
    public bool debugLog = true;
    public bool debugLogRawResponse = true;
    public bool debugLogHistory = false; // 너무 길면 콘솔 폭발하니까 옵션

    [Serializable]
    public class Msg { 
        public string role; 
        public string content; 
        public Msg(string r, string c) { role = r; content = c; } 
    }

    [Serializable]
    class ChatRequest
    {
        public string model;
        public List<Msg> messages;
        public float temperature;
        public int max_tokens;
        public bool stream;
    }

    [Serializable]
    class ChatResponse
    {
        public Choice[] choices;
    }

    [Serializable]
    public class Choice
    {
        public ChoiceMessage message;
    }

    [Serializable]
    public class ChoiceMessage
    {
        public string role;
        public string content;
    }

    public IEnumerator Chat(List<Msg> messages, Action<string> onDone, Action<string> onError = null)
    {
        var reqObj = new ChatRequest
        {
            model = model,
            messages = messages,
            temperature = temperature,
            max_tokens = maxTokens,
            stream = false
        };

        string url = $"{baseUrl}/chat/completions";
        string json = JsonUtility.ToJson(reqObj);

        if (debugLog)
        {
            Debug.Log($"[LLM][REQ] POST {url}\n{json}");
            Debug.Log($"[LLM][REQ] messages={messages.Count}, max_tokens={maxTokens}, temp={temperature}");

            if (debugLogHistory)
            {
                for (int i = 0; i < messages.Count; i++)
                    Debug.Log($"[LLM][HIST {i}] {messages[i].role}: {messages[i].content}");
            }
        }

        var body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 30;

        yield return req.SendWebRequest();

        string raw = req.downloadHandler.text;
        if (debugLog && debugLogRawResponse)
            Debug.Log($"[LLM][RAW] HTTP {(long)req.responseCode}\n{raw}");

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"HTTP {(long)req.responseCode} {req.error}\nRAW:\n{raw}");
    
                yield break;
        }

        try
        {
            // JsonUtility는 배열/중첩에 약해서 가끔 실패함 -> 간단 파서로 처리할 수도 있지만,
            // LM Studio 응답이 표준이면 아래가 잘 되는 편. 
            var resq = JsonUtility.FromJson<ChatResponse>(req.downloadHandler.text);
            var content = resq?.choices?[0]?.message?.content?.Trim();
            if (string.IsNullOrWhiteSpace(content)) content = "빈 응답";

            if (debugLog)
                Debug.Log($"[LLM][OUT] {content}");

            onDone?.Invoke(content);
        }
        catch (Exception e)
        {
            onError?.Invoke("Parse error: " + e.Message + "/nRaw: " + req.downloadHandler.text);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
