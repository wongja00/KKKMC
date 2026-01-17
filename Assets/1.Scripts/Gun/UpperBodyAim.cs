using UnityEngine;
using UnityEngine.UIElements;

public class UpperBodyAim : MonoBehaviour
{
    public Transform spineBone;
    public Transform aimTarget;
    public float roateSpeed = 8f;
    public float maxAngle = 60f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LateUpdate()
    {
        if(aimTarget == null || spineBone == null) return;

        //조준 방향 벡터
        Vector3 dir = aimTarget.position - spineBone.position;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        //현재 로컬 회전 기준
        Quaternion localTarget = Quaternion.Inverse(spineBone.parent.rotation);

        //각도제한
        Vector3 euler = localTarget.eulerAngles;
        if(euler.y > 180) euler.y -= 360;
        euler.y = Mathf.Clamp(euler.y, -maxAngle, maxAngle);
        localTarget = Quaternion.Euler(0, euler.y, 0);

        //부드럽게 회전
        spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, localTarget, Time.deltaTime * roateSpeed);
    }
}
