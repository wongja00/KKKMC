using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class DialogLoder : MonoBehaviour
{
    [SerializeField] private string GoogleSheetURL= "https://docs.google.com/spreadsheets/d/e/2PACX-1vR1GWclg1hAK7AYbZp7ggY7wychXjY-rsU38EXvxP6dTjizTIKQv1qOEi-Jr6OaqAw6ulyPBHkrWr7V/pub?gid=0&single=true&output=csv";
    [SerializeField] private string GoogleSheetIDURL= "https://docs.google.com/spreadsheets/d/e/2PACX-1vR1GWclg1hAK7AYbZp7ggY7wychXjY-rsU38EXvxP6dTjizTIKQv1qOEi-Jr6OaqAw6ulyPBHkrWr7V/pub?gid=1621988845&single=true&output=csv";
    [SerializeField] private StoryScriptHandler storyScriptHandler;

    private string localFilePath;
    private string versionKey = "DialogVersion";

    private void Awake()
    {
        localFilePath = Path.Combine(Application.persistentDataPath, "dialog.csv");
        InitDialogueSystem();
    }

    public void InitDialogueSystem()
    {
        StartCoroutine(LoadDialogueRoutine());
    }

    private IEnumerator LoadDialogueRoutine()
    {
        bool hasLocalFile = File.Exists(localFilePath);
        bool shouldDownload = false;

        Debug.Log($"로컬 파일 존재여부 {hasLocalFile}");

        //인터넷 연결 확인
        if(Application.internetReachability!= NetworkReachability.NotReachable)
        {
            Debug.Log("인터넷 연결 확인 버전체크");

            using(UnityWebRequest versionRequest = UnityWebRequest.Get(GoogleSheetIDURL))
            {
                yield return versionRequest.SendWebRequest();

                if(versionRequest.result == UnityWebRequest.Result.Success)
                {
                    string onlineVersion = versionRequest.downloadHandler.text.Trim();
                    string localVersion = PlayerPrefs.GetString(versionKey, "0");

                    Debug.Log($"온라인 버전: {onlineVersion}, 로컬 버전: {localVersion}");
                    if(!hasLocalFile || onlineVersion != localVersion)
                    {
                        shouldDownload = true;
                        PlayerPrefs.SetString(versionKey, onlineVersion);
                    }
                }
                else
                {
                    Debug.LogError("버전 정보 가져오기 실패: " + versionRequest.error);
                }
            }
        }
        else
        {
            Debug.Log("인터넷 연결 없음, 로컬 파일 사용");

            storyScriptHandler.InitScripts(localFilePath);
        }

        if(shouldDownload)
        {
            Debug.Log("변경사항 감지 최신 데이터 다운");
           yield return StartCoroutine(DownloadDialogue());
        }

        if(File.Exists(localFilePath))
        {
            //string dialogueData = File.ReadAllText(localFilePath);
            storyScriptHandler.InitScripts(localFilePath);
        }
        else
        {
            Debug.LogError("대화 데이터 파일을 찾을 수 없습니다.");
        }
    }

    IEnumerator DownloadDialogue()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(GoogleSheetURL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(localFilePath, request.downloadHandler.text, System.Text.Encoding.UTF8);
                PlayerPrefs.Save();
                Debug.Log("대화 데이터 다운로드 및 저장 완료");

                storyScriptHandler.InitScripts(localFilePath);
            }
            else
            {
                Debug.LogError("대화 데이터 다운로드 실패: " + request.error);

                PlayerPrefs.DeleteKey(versionKey);
            }
        }
    }
}
