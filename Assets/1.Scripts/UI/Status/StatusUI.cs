using System.Collections.Generic;
using UnityEngine;

public class StatusUI : MonoBehaviour
{
    [SerializeField] KeyCode statusUIKey = KeyCode.P;

    [SerializeField] GameObject StatusWindow;
    [SerializeField] StatusCard card;
    [SerializeField] Transform startParent;

    public Dictionary<string, StatusCard> statDic = new Dictionary<string, StatusCard>();


    void Update()
    {
        if(Input.GetKeyDown(statusUIKey))
        {
            if(StatusWindow.activeSelf)
            {
                StatusWindow.SetActive(false);
            }
            else
            {
                StatusWindow.SetActive(true);
            }
        }
    }

    public void AddCards(string statName, int Value)
    {
        StatusCard stat = Instantiate(card, startParent);
        stat.gameObject.SetActive(true); 
        
        stat.SetCard(statName, Value);

        statDic.Add(statName, stat);
    }

    public void DisableOriginCard()
    {
        card.gameObject.SetActive(false);
    }
}
