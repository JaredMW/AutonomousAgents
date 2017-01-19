// Jared White
// December 11, 2016

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;

/// <summary>
/// Flock that leader follows with a leader seeking the player's position
/// </summary>
public class ZombieFlock : FlockManager
{
    // Fields
    #region Fields
    public FirstPersonController fps;
    public float zombieTargetSeekWeight = 1.5f;
    #endregion


    // Use this for initialization
    protected override void Start()
    {
        currentState = BehaviorState.LeaderFollowing;
        base.Start();
        
        if (leader.GetComponent<ZombieBehavior>() != null)
        {
            leader.GetComponent<ZombieBehavior>().zombieTarget = fps.gameObject;
            leader.GetComponent<ZombieBehavior>().SeekWeight = zombieTargetSeekWeight;
        }
        else
        {
            Debug.LogError("Zombie Flock leader does not have a ZombieBehavior script");
        }
    }
}