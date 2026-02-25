using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Search;
using Mirror;

public class BuffUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI buffSelectCountText;
    [SerializeField] TextMeshProUGUI buffCountText;

    int buffSelectCount = 0;
    int buffCount = 0;

    private void UpdateBuffCountText()
    {
        buffCountText.text = $"현재 버프수: {buffCount}";
    }

    private void UpdateBuffSelectCountText()
    {
        buffSelectCountText.text = $"선택가능 버프수: {buffSelectCount}";
    }

    public void SetBuffCount(int count)
    {
        buffCount = count;
        UpdateBuffCountText();
    }

    public void AddBuffCount()
    {
        buffCount++;
        UpdateBuffCountText();
    }
    
    public void SetBuffSelectCount(int count)
    {
        buffSelectCount = count;
        UpdateBuffSelectCountText();
    }

    public void AddBuffSelectCount()
    {
        buffSelectCount++;
        UpdateBuffSelectCountText();
    }

    public void DecreaseBuffSelectCount()
    {
        buffSelectCount = Mathf.Max(0, buffSelectCount - 1);
        UpdateBuffSelectCountText();
    }

    
}
