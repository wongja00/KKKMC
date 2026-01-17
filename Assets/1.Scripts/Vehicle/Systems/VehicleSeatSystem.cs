using System.Collections.Generic;
using UnityEngine;

public enum ESeatType
{
    Driver = 0x00,
    Passenger = 0x01
}

public class VehicleSeatSystem : MonoBehaviour
{
    
    [SerializeField] private Transform driverSeatTransformParent;

    [SerializeField] private List<VehicleSeat> seats = new List<VehicleSeat>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeSeats();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitializeSeats()
    {
        seats.Clear();

        if(driverSeatTransformParent == null) return;

        foreach(Transform child in driverSeatTransformParent)
        {
            if(child.TryGetComponent(out VehicleSeat seat))
            {
                seats.Add(seat);
            }
        }
    }

    public VehicleSeat GetSeat(ESeatType seatType)
    {
        return seats.Find(seat => seat.GetSeatType() == seatType);
    }

    public void EnterSeat(ESeatType seatType, Transform enterTransform)
    {
        VehicleSeat seat = GetSeat(seatType);

        if(seat == null) return;

        Debug.Log("Enter Seat: " + seat.name);
        
        enterTransform.SetParent(seat.transform);
        enterTransform.localPosition = Vector3.zero;
        enterTransform.localRotation = Quaternion.identity;
        enterTransform.localScale = Vector3.one;
    }

    public void ExitSeat(Transform exitTransform)
    {
        //캐릭터와 자동차가 충돌하지 않게 캐릭터 크기와 자동차 크기를 더한 만큼 떨어진 곳에서 캐릭터 위치를 초기화 한다.
        exitTransform.SetParent(null);
        exitTransform.position = exitTransform.position + exitTransform.forward * (exitTransform.localScale.y + transform.localScale.y)*2;
        exitTransform.localRotation = Quaternion.identity;
    }
}
