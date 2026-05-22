using UnityEngine;
using System.Collections.Generic;
using System;


public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    [SerializeField] StoryScriptHandler scriptHandler;
    [SerializeField] Dialogue dialogue;
    [SerializeField] GameObject dialoguePanel;

    [SerializeField] KeyCode showKey = KeyCode.Space;


    [SerializeField] List<string> curScripts = new List<string>();
    int curScriptIndex = 0;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

    }

    private void Update()
    {
        UpdateScript();
    }

    void UpdateScript()
    {
        if(curScripts == null)
        {
            return;
        }

        if (curScripts.Count == 0)
        {
            return;
        }

        if (Input.GetKeyDown(showKey) && curScripts.Count > 0)
        {
            curScriptIndex++;
            OnScript(curScriptIndex);
        }
    }

    public void ShowDialogue(string name, string dialogue)
    {
        dialoguePanel.SetActive(true);


        this.dialogue.SetDialogue(name, dialogue);
    }

    public void HideDialogue()
    {
        dialogue.SetDialogue("", "");

        dialoguePanel.SetActive(false);
        curScriptIndex = 0;
        curScripts = null;
    }

    public List<string> GetScript(string scriptName)
    {
        return scriptHandler.GetScript(scriptName);
    }

    public void OnScriptEvent(string ID)
    {
        curScripts = GetScript(ID);

        OnScript(0);
    }

    public void OnScript(int i)
    {
        if(curScripts == null || i >= curScripts.Count)
        {
            HideDialogue();
            return;
        }

        string sName = curScripts[i].Split(',')[0];
        string sDialogue = curScripts[i].Split(',')[1];

        ShowDialogue(sName, sDialogue);
    }

    public Dictionary<string, List<string>> GetAllScript()
    {
        return scriptHandler.GetAllScripts();
    }
}
