using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    public Transform frontSail;
    int health = 2;
    public float detectionRange = 10f;

    static HashSet<Ship> nearbyShips = new HashSet<Ship>();
    static HashSet<Ship> activeShips = new HashSet<Ship>();
    //TODO: Add detector

    public int windCrew;
    public int oarCrew;
    public int separationCrew;
    public int avoidCrew;
    public int cohesionLocalCrew;
    public int cohesionGlobalCrew;
    public int alignmentCrew;
    public int directCrew;

    private Rigidbody rb;
    public SteeringOutput currentSteering = new();
    public SteeringOutput targetSteering = new();

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        activeShips.Add(this);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ApplySteering();
    }

    public Ship[] GetNearbyShips()
    {
        Ship[] result = new Ship[nearbyShips.Count];
        nearbyShips.CopyTo(result);
        return result;
    }

    public Ship[] GetActiveShips()
    {
        Ship[] result = new Ship[activeShips.Count];
        activeShips.CopyTo(result);
        return result;
    }

    private void ApplySteering()
    {
        SteeringOutput combined = new();
        combined += Steering.Wind(this, windCrew);
        combined += Steering.Oars(this, oarCrew);
        combined += Steering.Separation(this, separationCrew);
        combined += Steering.CohesionLocal(this, cohesionLocalCrew);
        combined += Steering.CohesionGlobal(this, cohesionGlobalCrew);
        combined += Steering.Alignment(this, alignmentCrew);
        combined += Steering.Direct(this, directCrew);

        combined += Steering.Avoidance(this, avoidCrew, combined);

        targetSteering = combined*Steering.GLOBAL_CONTROL_POWER;

        currentSteering = SteeringOutput.Lerp(currentSteering, targetSteering, 0.05f);

        rb.velocity = currentSteering.linear;
        rb.angularVelocity = new Vector3(0, Mathf.Clamp(currentSteering.angular, -Steering.MAX_ANGULAR, Steering.MAX_ANGULAR), 0);

        //rb.MoveRotation(Quaternion.LookRotation(rb.velocity));

        frontSail.rotation = Quaternion.LookRotation(targetSteering.linear, Vector3.up);
    }

    public void AddNearbyShip(Ship ship)
    {
        nearbyShips.Add(ship);
    }

    public void RemoveNearbyShip(Ship ship)
    {
        nearbyShips.Remove(ship);
    }

    private void OnDrawGizmosSelected()
    {
        if (currentSteering != null)
        {
            Debug.DrawRay(transform.position, currentSteering.linear, Color.green);
        }
        if (targetSteering != null)
        {
            Debug.DrawRay(transform.position, targetSteering.linear, Color.red);
        }
    }
}
