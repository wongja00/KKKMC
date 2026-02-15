using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    private int curRound = 0;

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
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
