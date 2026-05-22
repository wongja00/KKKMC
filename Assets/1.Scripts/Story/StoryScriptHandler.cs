using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class StoryScriptHandler : MonoBehaviour
{

    [SerializeField] Dictionary<string, List<string>> storyScripts = new Dictionary<string, List<string>>();

    private void Awake()
    {
    }

    public List<string> GetScript(string scriptName)
    {
        if (storyScripts.TryGetValue(scriptName, out List<string> script))
        {
            return script;
        }
        else
        {
            Debug.LogWarning($"Script '{scriptName}' not found.");
            return null;
        }
    }

    public Dictionary<string, List<string>> GetAllScripts()
    {
        return storyScripts;
    }

    public void InitScripts(string csvFilePath)
    {
        storyScripts.Clear();

        //CSV 파일에서 스크립트 로드
        string csvFile = File.ReadAllText(csvFilePath, System.Text.Encoding.UTF8);

        if (csvFile == null)
        {
            Debug.LogError($"CSV파일 없음: {csvFilePath}");
            return;
        }

        Debug.Log(csvFile);

        string[] lines = csvFile.Split(new[] { '\n' }, System.StringSplitOptions.None);

        string tempID = "";

        foreach (string line in lines)
        {
            string[] parts = line.Split(new[] { ',' });

            if (parts.Length < 1)
            {
                Debug.LogWarning($"Invalid line in CSV: {line}");
                continue;
            }

            string scriptID = parts[0].Trim();

            if (!string.IsNullOrEmpty(scriptID))
            {
                tempID = scriptID;
                Debug.Log($"새 스크립트 ID 감지: {tempID}");

                List<string> scriptLines = new List<string>();

                storyScripts[tempID] = scriptLines;
                
            }

            storyScripts[tempID].Add(parts[1] + "," + parts[2]);

        }
    }

}
