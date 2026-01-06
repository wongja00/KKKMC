using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

public class ElevenLabsTTS : MonoBehaviour
{
    [Header("ElevenLabs")]
    public string apiKey = "sk_9dcb61d77525a8ae439a2ed7e0e2bf65b0d22dde67ad019c";
    public string voiceId = "c42b80edeca320bb354dce79fed7da9a9748740fdcdbac7dc8f5f1e8fe79e145";

    [Header("Voice Settings")]
    [Range(0f, 1f)] public float stability = 0.5f;
    [Range(0f, 1f)] public float similarityBoost= 0.7f;

    [Header("Output")]
    public AudioSource audioSource;

    [Serializable]
    class TtsRequest
    {
        public string text;
        public string model_id = "eleven_multilingual_v2";
        public VoiceSettings voice_settings;
    }

    [Serializable]
    class VoiceSettings
    {
        public float stability;
        public float similarity_boost;
    }

    public IEnumerator Speak(string text, Action onDone = null, Action<string> onError = null)
    {
        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

        Debug.Log("[TTS] URL = " + url);
        Debug.Log("[TTS] voiceId = " + voiceId);

        var reqObj = new TtsRequest
        {
            text = text,
            model_id = "eleven_multilingual_v2",
            voice_settings = new VoiceSettings
            {
                stability = stability,
                similarity_boost = similarityBoost
            }
        };

        string json = JsonUtility.ToJson(reqObj);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);

        //post에서도 오디오로 바로 받게 설정
        req.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("xi-api-key", apiKey);
        req.SetRequestHeader("accept", "audio/mpeg");
        req.timeout = 60;

        yield return req.SendWebRequest();

        if(req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }

        try
        {
            var clip = DownloadHandlerAudioClip.GetContent(req);

            if(clip == null)
            {
                onError?.Invoke("AudioClip is null");
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();
            onDone?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke($"Error: {ex.Message}");
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
