using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connector : MonoBehaviour
{
    public Vector2 size = Vector2.one * 4;
    public bool isConnected = false;

    public bool isPlaying;

    void Start()
    {
        isPlaying = true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isConnected ? Color.green : Color.red;
        if(isPlaying == false) Gizmos.color = Color.cyan;
        
        Vector2 halfSize = size * 0.5f;
        Vector3 offset = transform.position + transform.up * halfSize.y;
        Gizmos.DrawLine(offset,offset + transform.forward);
        //위, 옆 벡터
        Vector3 top = transform.up * size.y;
        Vector3 side = transform.right * halfSize.x;
        //코너 벡터
        Vector3 topRight = transform.position + top + side;
        Vector3 topLeft =  transform.position + top - side;

        Vector3 botRight = transform.position + side;
        Vector3 botLeft = transform.position -side;

        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, botLeft);
        Gizmos.DrawLine(botLeft, botRight);
        Gizmos.DrawLine(botRight, topRight);

        //대각선 그리기
        Gizmos.color *= 0.3f;
        Gizmos.DrawLine(topRight, offset);
        Gizmos.DrawLine(topLeft, offset);
        Gizmos.DrawLine(botRight, offset);
        Gizmos.DrawLine(botLeft, offset);

    }
}
