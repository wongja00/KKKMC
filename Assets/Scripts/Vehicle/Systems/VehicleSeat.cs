using UnityEngine;

public class VehicleSeat : MonoBehaviour
{

    [SerializeField] private ESeatType seatType;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(seatType == ESeatType.Driver)
        {
            InitializeDriverSeat();
        }
        else if(seatType == ESeatType.Passenger)
        {
            InitializePassengerSeat();
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitializeDriverSeat()
    {

    }

    private void InitializePassengerSeat()
    {

    }

    public ESeatType GetSeatType()
    {
        return seatType;
    }
}
