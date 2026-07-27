using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCrawler : MonoBehaviour
{
    public GameObject crawler;
    int width = 100;
    int depth = 100;
    Vector3Int crawlerPos;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int dx = Random.Range(-1, 2);
        int dz = Random.Range(-1, 2);
        if (Random.Range(0, 2) == 0)
        {
            if (crawlerPos.z + dz * 20 > depth || crawlerPos.z + dz * 20 < 0) dz *= -1;
            crawlerPos += new Vector3Int(0, 0, dz * 20);
        }
        else
        {
            if (crawlerPos.x + dx * 20 > width || crawlerPos.x + dx * 20 < 0) dx *= -1;
                crawlerPos += new Vector3Int(dx * 20, 0, 0);
        }
        crawler.transform.position = crawlerPos;
    }
}
