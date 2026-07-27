using UnityEngine;
using System.Collections.Generic;


public class VoronoiMap : MonoBehaviour
{
    [Range(1, 10)]
    public int locationCount = 5;

    private void OnValidate()
    {
        Texture2D texture = new Texture2D(1024, 1024);
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
        Color color = Color.white;
        Dictionary<Vector2Int, Color> locations = new Dictionary<Vector2Int, Color>();

        while (locations.Count < locationCount)
        {
            int x = Random.Range(0, texture.width);
            int y = Random.Range(0, texture.height);
            color = new Color(Random.value, Random.value, Random.value);

            if(!locations.ContainsKey(new Vector2Int(x, y)))
            {
                locations.Add(new Vector2Int(x, y), color);
                texture.SetPixel(x, y, Color.black);

            }

        }

        for(int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float distace = Mathf.Infinity;
                color = Color.white;

                foreach(KeyValuePair<Vector2Int, Color> val in locations)
                {
                    float distTo = Vector2Int.Distance(val.Key, new Vector2Int(x, y));

                    if(distTo < distace)
                    {
                        color = val.Value;
                        distace = distTo;
                    }
                }
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
    }
}
