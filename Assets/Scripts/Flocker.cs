// Jared White
// November 22, 2016

using UnityEngine;
using System.Collections;

/// <summary>
/// A moving object/vehicle that Coheres, Aligns, and Separates
/// itself from othe vehicles of its type within its flock.
/// </summary>
public class Flocker : VehicleMovement
{
    // Fields
    #region Fields
    public FlockManager flock;
    public float awareRadius = 3f;
    public float distFromLeader = 3f;

    private Vector3 separationForce;
    private Vector3 followForce;
    #endregion

    // Properties
    #region Properties
    /// <summary>
    /// The awareness radius of this Flocker
    /// </summary>
    public float AwareRadius
    {
        get { return awareRadius / awareRadius; }
        set { awareRadius = value * value; }
    }
    #endregion

    // Use this for initialization
    protected override void Start()
    {
        base.Start();
        awareRadius *= awareRadius;
        separationForce = Vector3.zero;

        // If not already assigned to a FlockManager, make it this Flocker's
        // parent object.
        if (flock == null && transform.parent != null
            && transform.GetComponentInParent<FlockManager>() != null)
        {
            flock = transform.GetComponentInParent<FlockManager>();
        }
	}

    /// <summary>
    /// Calculate the summation of forces to steer in specified behaviors
    /// </summary>
    protected override void CalculateSteeringForces()
    {
        // FLOCKING
        if (flock.currentState == FlockManager.BehaviorState.Flocking)
        {
            Wander();
            Flock();
        }

        // LEADER FOLLOWING
        else if (flock.leader != null
            && flock.currentState == FlockManager.BehaviorState.LeaderFollowing)
        {
            // If this Flocker is the leader, then Wander
            if (flock.leader == this && !(flock.leader is ZombieBehavior))
            {
                Wander();
            }

            // Else, follow the leader & stay away from other Flockers on the
            // path
            else if (flock.leader != this)
            {
                steeringForces += FollowLeader() * flock.followWeight;
                steeringForces += Separate() * flock.separationWeight;
            }
        }

        // PATH FOLLOWING
        else if (path != null && flock.currentState == FlockManager.BehaviorState.PathFollowing)
        {
            steeringForces += FollowPath() * flock.pathWeight;
            steeringForces += Separate() * flock.separationWeight;
        }

        // OBSTACLE AVOIDANCE
        steeringForces += AvoidObstacles() * 50;
    }


    // *********************************************************************************************************************
    //                                                      Flocking
    // *********************************************************************************************************************

    #region Flocking
    /// <summary>
    /// Follow flocking behaviors - Align, Cohere, Separate, and leader follow.
    /// </summary>
    protected void Flock()
    {
        steeringForces += Align() * flock.alignmentWeight;
        steeringForces += Cohere() * flock.cohesionWeight;
        steeringForces += Separate() * flock.separationWeight;
    }


    /// <summary>11
    /// Seek the average direction of this Flocker's flock
    /// </summary>
    /// <returns>Vector3 steering force to align this Flocker towards the
    /// direction that all Flockers in its flock are facing.</returns>
    protected Vector3 Align()
    {
        // Normalize the sum of all the Flockers' directions, then multiply
        // by the max speed to get the desired velocity. Then, seek it.
        return (flock.AverageDirection * maxSpeed) - velocity;
    }

    /// <summary>
    /// Seek the average center of the flock's location
    /// </summary>
    /// <returns>Vector3 steering force to cohere this Flocker to the center
    /// of the flock</returns>
    protected Vector3 Cohere()
    {
        return GetSeek(flock.FlockPosition);
    }

    /// <summary>
    /// Flee other nearby Flockers in this flock, at a force inversely
    /// proportional to each Flocker's distance from each other.
    /// </summary>
    /// <returns></returns>
    protected Vector3 Separate()
    {
        separationForce = Vector3.zero;

        foreach (Flocker flocker in flock.Flock)
        {
            if (flocker != this)
            {
                if ((flocker.transform.position - position).sqrMagnitude
                    <= awareRadius)
                {
                    separationForce += GetFlee(flocker.transform.position);
                }
            }
        }

        return separationForce;
    }
    #endregion


    // *********************************************************************************************************************
    //                                                 LEADER FOLLOWING
    // *********************************************************************************************************************

    #region Leader Following
    /// <summary>
    /// Get the steering force to follow a leader
    /// </summary>
    /// <returns>Vector3 steering force to separate and arrive to a leader</returns>
    protected Vector3 FollowLeader()
    {
        // Seek and arrive to a point behind the leader
        followForce = Arrive(flock.leader.transform.position
            + (flock.leader.transform.forward * -distFromLeader),
            awareRadius);


        if ((flock.leader.transform.position - transform.position).sqrMagnitude
            < Mathf.Pow(flock.leader.GetComponent<Flocker>().distFromLeader, 2))
        {
            // Determine which direction to scramble in
            // Steer right if leader is on left side
            if (Vector3.Dot(flock.leader.transform.position - transform.position,
                flock.leader.transform.right) > 0)
            {
                followForce += GetSeek(transform.position
                        + (flock.leader.transform.right * -distFromLeader))
                    * Mathf.Pow(flock.followWeight, 10);
            }
            // Steer left if leader is on right side
            else
            {
                followForce += GetSeek(transform.position
                        + (flock.leader.transform.right * distFromLeader))
                    * Mathf.Pow(flock.followWeight, 10);
            }
        }

        return followForce;
    }

    /// <summary>
    /// Draw Gizmos in the inspector scene
    /// </summary>
    void OnDrawGizmos()
    {
        if (flock != null && flock.currentState
            == FlockManager.BehaviorState.LeaderFollowing && flock.leader == this)
            Gizmos.DrawWireSphere(position, distFromLeader);
    }
    #endregion
}
