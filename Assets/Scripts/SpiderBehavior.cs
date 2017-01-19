// Jared White
// November 2, 2016

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// Vehicle that flees from Zombie objects when it is within a certain range
/// </summary>
public class SpiderBehavior : Flocker
{
    // Fields
    #region Fields
    // Flee targets
    public List<FlockManager> evadeFlocks;
    public float evadeWeight = 10f;
    public float timeInFuture = 3f;
    protected bool panicMode = false;

    // Other fields
    protected float defaultAlignmentWeight;
    protected float defaultCohesionWeight;
    protected float defaultSeparationWeight;
    #endregion


    // Use this for initialization
    protected override void Start()
    {
        base.Start();

        // Assign base default values for weights
        defaultAlignmentWeight = flock.alignmentWeight;
        defaultCohesionWeight = flock.cohesionWeight;
        defaultSeparationWeight = flock.separationWeight;

        evadeFlocks = new List<FlockManager>();
        foreach (GameObject obj in FindObjectsOfType(typeof(GameObject)))
        {
            if (obj.CompareTag("Spider Evade Flock") && obj.GetComponent<FlockManager>() != null)
            {
                evadeFlocks.Add(obj.GetComponent<FlockManager>());
            }
        }
    }


    /// <summary>
    /// Calculate and apply weighted steering behavior forces.
    /// (Flee from the closest threats when threatened)
    /// </summary>
    protected override void CalculateSteeringForces()
    {
        // Flee the threats if too close/within Spider's awareness radius
        if (flock.currentState == FlockManager.BehaviorState.Flocking && evadeFlocks != null)
        {
            for (int f = 0; f < evadeFlocks.Count; f++)
            {
                foreach (Flocker evadeTarget in evadeFlocks[f].flockers)
                {
                    // If a thing to flee from is too close to this Spider, apply a flee force
                    if ((evadeTarget.transform.position - transform.position).sqrMagnitude < awareRadius)
                    {
                        steeringForces += GetFlee(evadeTarget.transform.position) * evadeWeight;
                        panicMode = true;
                    }
                }

                if ((evadeFlocks[f].leader.transform.position - transform.position).sqrMagnitude < awareRadius)
                {
                    steeringForces += GetFlee(evadeFlocks[f].leader.transform.position) * evadeWeight;
                    panicMode = true;
                }
                
            }

            // If it's time to panic, don't care about staying in a flock anymore.
            // Just run away.
            if (panicMode)
            {
                flock.alignmentWeight = defaultAlignmentWeight * .2f;
                flock.cohesionWeight = defaultCohesionWeight * .7f;
                flock.separationWeight = defaultSeparationWeight * 2;
            }
            else
            {
                flock.alignmentWeight = defaultAlignmentWeight;
                flock.cohesionWeight = defaultCohesionWeight;
                flock.separationWeight = defaultSeparationWeight;
            }
        }

        base.CalculateSteeringForces();
        panicMode = false;
    }
}