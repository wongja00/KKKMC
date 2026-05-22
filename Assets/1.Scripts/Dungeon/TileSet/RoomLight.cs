using UnityEngine;

public class RoomLight : MonoBehaviour
{
    [SerializeField] private Light[] roomLights;
    [SerializeField] private Color lightColor;
    private Color previousColor;

    void Awake()
    {
        roomLights = GetComponentsInChildren<Light>();
    }


    void Start()
    {
        previousColor = lightColor;
        ChangeLightColor(lightColor);
    }

    void Update()
    {
        if (lightColor != previousColor)
        {
            ChangeLightColor(lightColor);
            previousColor = lightColor;
        }
    }

    public void ChangeLightColor(Color color)
    {
        foreach (Light light in roomLights)
        {
            if (light.color != color)
                light.color = color;
        }
    }
}
