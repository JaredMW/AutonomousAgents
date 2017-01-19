// Jared White
// November 2, 2016

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// Vehicle that chases a target as the leader of a flock
/// </summary>
public class ZombieBehavior : Flocker
{
    // Fields
    #region Fields
    // Zombie targets
    public GameObject zombieTarget;
    public float distFromTarget = 1f;

    protected float seekWeight;
    #endregion


    #region Properties
    /// <summary>
    /// The weight on the Seek steering force
    /// </summary>
    public float SeekWeight
    {
        get { return seekWeight; }
        set { seekWeight = value; }
    }
    #endregion


    // Use this for initialization
    protected override void Start()
    {
        base.Start();
        if (flock != null)
        {
            flock = flock as ZombieFlock;
        }
    }


    /// <summary>
    /// Calculate and apply weighted steering behavior forces.
    /// (Arrive after a target)
    /// </summary>
    protected override void CalculateSteeringForces()
    {
        // Seek the targets if leader following until I get really close to it
        if (zombieTarget != null
            && flock.currentState == FlockManager.BehaviorState.LeaderFollowing
            && flock.leader == this)
        {
            if ((transform.position - (zombieTarget.transform.position
                    + zombieTarget.transform.forward * distFromTarget)).sqrMagnitude
                > 1f)
            {
                steeringForces += Arrive(
                    zombieTarget.transform.position
                        + zombieTarget.transform.forward * distFromTarget,
                    distFromTarget) * seekWeight;
            }
        }

        base.CalculateSteeringForces();
    }
}