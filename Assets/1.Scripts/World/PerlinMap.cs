using JetBrains.Annotations;
using System.Collections;   
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PerlinMap : MonoBehaviour
{
    [Range(1, 8)]
    public int octaves = 2;

    [Range(0, 1000)]
    public int xOffset = 0;
    [Range(0, 1000)]
    public int yOffset = 0;

    [Range(0.001f, 0.01f)]
    public float xScale = 0.003f;
    [Range(0.001f, 0.01f)]
    public float yScale = 0.003f;

    [Range(0.0f, 1.0f)]
    public float greencutoff = 0.3f;
    
    [Range(0.0f, 1.0f)]
    public float bluecutoff = 0.6f;
    
    [Range(0.0f, 1.0f)]
    public float yellowcutoff = 0.9f;


    [Range(0.0f, 1.0f)]
    public float lowcutoff = 0.0f;

    [Range(0.0f, 1.0f)]
    public float mediumcutoff = 0.0f;

    [Range(0.0f, 1.0f)]
    public float mediumhighcutoff = 0.0f;

    [Range(0.0f, 1.0f)]
    public float mediumlowcutoff = 0.0f;

    [Range(0.0f, 1.0f)]
    public float highcutoff = 0.0f;





    private void OnValidate2()
    {
        Texture2D texture = new Texture2D(1024, 1024);
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;

        float perlinr;
        float perlinc;
        float perlini;
        Color color = Color.black;

        for(int y = 0; y < texture.height; y++)
        {
            for(int x = 0; x < texture.width; x++)
            {
                perlinr = fBM((x + xOffset) * xScale, (y + yOffset) * yScale, octaves);
                perlinc = fBM((x + xOffset + 100) * xScale, (y + yOffset + 100) * yScale, octaves);
                perlini = fBM((x + xOffset + 5000) * xScale, (y + yOffset + 5000) * yScale, octaves);

                if(perlinr < greencutoff)
                {
                    color = Color.green;
                }
                if(perlinc < bluecutoff)
                {
                    color = Color.blue;
                }
                if(perlini < yellowcutoff)
                {
                    color = Color.yellow;
                }

                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }
    private void OnValidate3()
    {
        Texture2D texture = new Texture2D(1024, 1024);
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;

        float perlin;
        Color color = Color.white;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                perlin = fBM((x + xOffset) * xScale, (y + yOffset) * yScale, octaves);

                if (perlin < greencutoff)
                {
                    color = Color.green;
                }
                else if (perlin < bluecutoff)
                {
                    color = Color.blue;
                }
                else if (perlin < yellowcutoff)
                {
                    color = Color.yellow;
                }

                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }

    private void OnValidate()
    {
        Texture2D texture = new Texture2D(1024, 1024);
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;

        float perlin;
        Color color = Color.black;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                perlin = fBM((x + xOffset) * xScale, (y + yOffset) * yScale, octaves);

                if (perlin < lowcutoff) color = new Color(0, 0, 0);
                else if (perlin < mediumlowcutoff) color = new Color(0.4f, 0.4f, 0.4f);
                else if (perlin < mediumcutoff) color = new Color(0.6f, 0.6f, 0.6f);
                else if (perlin < mediumhighcutoff) color = new Color(0.8f, 0.8f, 0.8f);
                else if (perlin < highcutoff) color = new Color(1, 1, 1);

                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }

    public float fBM(float x, float y, int octaves)
    {
        float total = 0;
        float frequency = 1;

        for(int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency);
            
            frequency *= 2;
        }
        return total / (float)octaves;
    }
}
