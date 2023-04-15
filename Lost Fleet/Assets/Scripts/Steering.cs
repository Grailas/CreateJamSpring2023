using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Steering
{
    public const float MAX_ANGULAR = 1f;
    public const float GLOBAL_CONTROL_POWER = 0.5f;

    public const float OAR_FORCE_PER_CREW = 1f;

    public const float SEPARATION_RANGE = 5f;
    public const float SEPARATION_LINEAR_PER_CREW = 1f;
    public const float SEPARATION_ANGULAR_PER_CREW = 1f / 10f;
    public const float SEPARATION_DECAY = 10;

    public const float AVOIDANCE_BREAK_RATIO_PER_CREW = 0.75f / 10f;

    public const float COHESION_LOCAL_RANGE = 5f;
    public const float COHESION_LOCAL_FORCE_PER_CREW = 0.5f;
    public const float COHESION_LOCAL_ANGULAR_PER_CREW = 1f / 10f;

    public const float COHESION_GLOBAL_FORCE_PER_CREW = 0.25f;
    public const float COHESION_GLOBAL_ANGULAR_PER_CREW = 1f / 10f;

    public const float ALIGN_RANGE = 5f;
    public const float ALIGN_LINEAR_PER_CREW = 0.5f;
    public const float ALIGN_ANGULAR_PER_CREW = 1f / 10f;

    public const float DIRECT_LINEAR_PER_CREW = 0.5f;
    public const float DIRECT_ANGULAR_PER_CREW = 1f/10f;

    public static Vector3 directVector = new();

    //- Wind(use sails to move forward, strength based on alignment with wind)
    //- Oars(brute force forward, exhausting)
    //- Separation(steer ships away from each other)
    //- Avoidance(steer ships away from hazards straight ahead)
    //- Cohesion(keep the fleet together)
    //- Alignment(maintain same heading as the rest of the fleet)
    //- Direct(orient the ships towards a specific direction)

    //In additon, there is always:
    //- Undercurrent
    //- Waves

    public static SteeringOutput Wind(Ship ship, int crew)
    {
        SteeringOutput result = new SteeringOutput();

        //Sample wind 
        //TODO: static for now, make wind system and sample
        Vector3 localWind = new Vector3(1f, 0f, 1f).normalized;
        Vector3 totalWind = localWind + Weather.Instance.globalWind;

        //Get level of alignment (dot product)
        float alignment = Vector3.Dot(ship.transform.forward, totalWind);

        //crew * alignment * forward * readiness
        result.linear = crew * alignment * ship.transform.forward * Crewing.Readiness;
        //TODO: mult wind strength

        return result;
    }

    public static SteeringOutput Oars(Ship ship, int crew)
    {
        SteeringOutput result = new();
        result.linear = ship.transform.forward * crew * OAR_FORCE_PER_CREW * Crewing.Readiness;
        return result;
    }

    public static SteeringOutput Separation(Ship ship, int crew)
    {
        SteeringOutput result = new SteeringOutput();

        //Get ships to separate from
        Ship[] neighbors = ship.GetNearbyShips();

        //If none, do nothing
        if (neighbors.Length == 0)
        {
            return result;
        }

        float maxAcceleration = crew * SEPARATION_LINEAR_PER_CREW;
        float maxAngular = crew * SEPARATION_ANGULAR_PER_CREW;

        //For each ship, calculate repulsive force
        foreach (Ship neighbor in neighbors)
        {
            //Get direction
            Vector3 direction = ship.transform.position - neighbor.transform.position;
            //Get square distance
            float sqrDistance = direction.sqrMagnitude;

            //Filter by distance
            if (sqrDistance > SEPARATION_RANGE * SEPARATION_RANGE)
            { continue; }

            //Get separation strength by inverse square law
            //float strength = Mathf.Min(SEPARATION_DECAY / sqrDistance, maxAcceleration);
            //float strength = SEPARATION_DECAY / sqrDistance;

            //Get separation strength by modified inverse square law
            //float strength = Mathf.Min(SEPARATION_DECAY*crew / sqrDistance*crew, maxAcceleration);


            //Separation by linear function
            float strength = Mathf.Max(
                maxAcceleration * ((SEPARATION_RANGE - Mathf.Sqrt(sqrDistance)) / SEPARATION_RANGE),
                0);


            //Normalize direction
            direction.Normalize();
            //Add to result
            result.linear += strength * direction;


            //Get degree that neighbor is to one of the sides ("rotated" dot product), right is positive
            float rightness = Vector3.Dot(ship.transform.right, direction);

            //calculate amount to steer, add to result
            result.angular += rightness * maxAngular;
        }

        //Clamp results, apply readiness
        result.linear = Vector3.Min(result.linear, Vector3.one * maxAcceleration) * Crewing.Readiness;
        result.angular = Mathf.Clamp(result.angular, -maxAngular, maxAngular) * Crewing.Readiness;

        return result;
    }

    public static SteeringOutput Avoidance(Ship ship, int crew, SteeringOutput steeringToTweak)
    {

        //boxcast ahead, if hit, check if most to left or right
        RaycastHit[] hits = Physics.BoxCastAll(
            ship.transform.position, ship.transform.localScale * 1.25f, ship.transform.forward,
             ship.transform.rotation, ship.detectionRange, ~LayerMask.NameToLayer("Ship"), QueryTriggerInteraction.Ignore);
        if (hits.Length > 1)
        {
            foreach (RaycastHit hit in hits)
            {
                if (hit.point == Vector3.zero)
                {
                    continue;
                }
                Debug.Log(hit.transform.name);
                Debug.DrawLine(ship.transform.position, hit.point, Color.blue);
            }

            steeringToTweak.linear -= steeringToTweak.linear * (crew * AVOIDANCE_BREAK_RATIO_PER_CREW);
        }

        return steeringToTweak;
    }

    public static SteeringOutput CohesionLocal(Ship ship, int crew)
    {
        SteeringOutput result = new();

        //Get ships to stick together with
        Ship[] neighbors = ship.GetNearbyShips();

        //Calculate center of surrounding fleet
        Vector3 center = new();
        int count = 0;
        foreach (Ship neighbor in neighbors)
        {
            //Filter by distance
            if (Vector3.SqrMagnitude(neighbor.transform.position - ship.transform.position) < COHESION_LOCAL_RANGE)
            {
                continue;
            }

            center += neighbor.transform.position;
            count++;
        }
        if (count == 0)
        { return result; }

        center /= count;

        //Find direction to center
        Vector3 direction = (center - ship.transform.position).normalized;

        //Get degree that center is to one of the sides ("rotated" dot product), right is positive
        float rightness = Vector3.Dot(ship.transform.right, direction);

        //Calculate result
        result.linear = direction * crew * COHESION_LOCAL_FORCE_PER_CREW * Crewing.Readiness;
        result.angular = rightness * crew * COHESION_LOCAL_ANGULAR_PER_CREW * Crewing.Readiness;


        return result;
    }

    public static SteeringOutput CohesionGlobal(Ship ship, int crew)
    {
        SteeringOutput result = new();

        //Get ships to stick together with
        Ship[] neighbors = ship.GetActiveShips();

        //Calculate center of surrounding fleet
        Vector3 center = new();
        int count = 0;
        foreach (Ship neighbor in neighbors)
        {
            center += neighbor.transform.position;
            count++;
        }
        if (count == 0)
        { return result; }

        center /= count;

        //Find direction to center
        Vector3 direction = (center - ship.transform.position).normalized;

        //Get degree that center is to one of the sides ("rotated" dot product), right is positive
        float rightness = Vector3.Dot(ship.transform.right, direction);

        //Calculate result
        result.linear = direction * crew * COHESION_GLOBAL_FORCE_PER_CREW * Crewing.Readiness;
        result.angular = rightness * crew * COHESION_GLOBAL_ANGULAR_PER_CREW * Crewing.Readiness;

        return result;
    }

    public static SteeringOutput Alignment(Ship ship, int crew)
    {
        SteeringOutput result = new SteeringOutput();

        //Get ships to align with
        Ship[] neighbors = ship.GetNearbyShips();

        //Calculate average direction
        Vector3 direction = new();
        int count = 0;
        foreach (Ship neighbor in neighbors)
        {
            //Filter by distance
            if (Vector3.SqrMagnitude(neighbor.transform.position - ship.transform.position) < ALIGN_RANGE)
            {
                continue;
            }

            direction += neighbor.transform.forward;
            count++;
        }
        if (count == 0)
        { return result; }

        direction.Normalize();

        //Calculate difference in angle
        float angleDif = Vector3.SignedAngle(direction, ship.transform.forward, ship.transform.up);

        //Calculate result
        //result.linear = direction * crew * ALIGN_LINEAR_PER_CREW * Crewing.Readiness;
        result.angular = Mathf.Clamp((angleDif / 180) * crew * ALIGN_ANGULAR_PER_CREW, -MAX_ANGULAR, MAX_ANGULAR) * Crewing.Readiness;


        return result;
    }

    public static SteeringOutput Direct(Ship ship, int crew)
    {
        SteeringOutput result = new();

        //result.linear = directVector * DIRECT_FORCE_PER_CREW * crew * Crewing.Readiness;

        //Calculate difference in angle
        float angleDif = Vector3.SignedAngle(directVector, ship.transform.forward, ship.transform.up);

        //Calculate result
        result.angular = Mathf.Clamp((angleDif / 180) * crew * DIRECT_ANGULAR_PER_CREW, -MAX_ANGULAR, MAX_ANGULAR) * Crewing.Readiness;


        return result;
    }
}

[System.Serializable]
public class SteeringOutput
{
    public Vector3 linear = Vector3.zero;
    public float angular;

    public SteeringOutput()
    {
    }

    public SteeringOutput(Vector3 linear, float angular)
    {
        this.linear = linear;
        this.angular = angular;
    }

    public static SteeringOutput operator +(SteeringOutput so1, SteeringOutput so2)
    {
        return new SteeringOutput(so1.linear + so2.linear, so1.angular + so2.angular);
    }
    public static SteeringOutput operator -(SteeringOutput so1, SteeringOutput so2)
    {
        return new SteeringOutput(so1.linear - so2.linear, so1.angular - so2.angular);
    }
    public static SteeringOutput operator *(SteeringOutput so, int n)
    {
        return new SteeringOutput(so.linear * n, so.angular * n);
    }
    public static SteeringOutput operator *(SteeringOutput so, float f)
    {
        return new SteeringOutput(so.linear * f, so.angular * f);
    }
    public static SteeringOutput operator /(SteeringOutput so, int n)
    {
        return new SteeringOutput(so.linear / n, so.angular / n);
    }
    public static SteeringOutput operator /(SteeringOutput so, float f)
    {
        return new SteeringOutput(so.linear / f, so.angular / f);
    }

    public static SteeringOutput Lerp(SteeringOutput so1, SteeringOutput so2, float t)
    {
        return new SteeringOutput(Vector3.Lerp(so1.linear, so2.linear, t), Mathf.Lerp(so1.angular, so2.angular, t));
    }
}