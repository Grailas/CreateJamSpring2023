using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipDetector : MonoBehaviour
{
    private Ship ship;
    private SphereCollider col;

    // Start is called before the first frame update
    void Start()
    {
        ship = transform.parent.GetComponent<Ship>();
        col = GetComponent<SphereCollider>();
        col.radius = ship.detectionRange;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        Ship otherShip = other.GetComponent<Ship>();
        if (!otherShip)
        { return; }

        ship.AddNearbyShip(otherShip);
    }

    private void OnTriggerExit(Collider other)
    {
        Ship otherShip = other.GetComponent<Ship>();
        if (!otherShip || otherShip == ship)
        { return; }

        ship.RemoveNearbyShip(otherShip);
    }
}
