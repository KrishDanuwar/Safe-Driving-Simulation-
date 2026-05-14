using UnityEngine;

public class TurnZone : MonoBehaviour
{
    public float turnAngle = 90f; // Set to 90 for Right turn, -90 for Left turn
    
    private void OnTriggerEnter(Collider other)
    {
        SimpleTrafficCar car = other.GetComponent<SimpleTrafficCar>();
        if (car != null)
        {
            // Rotate the car to face the new road
            car.transform.Rotate(0, turnAngle, 0);
            
            // Move the car slightly forward so it doesn't get stuck in the trigger
            car.transform.position += car.transform.forward * 2f;
        }
    }
}
