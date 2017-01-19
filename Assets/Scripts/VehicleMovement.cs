// Jared White
// November 6, 2016

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]

/// <summary>
/// Simulates behaviors of a moving vehicle that utilizes steering forces to
/// make choices about how to navigate the world it is in
/// </summary>
public abstract class VehicleMovement : MonoBehaviour
{
    // Fields
    #region Fields
    // Vectors for force movement
    public Vector3 position;
    public Vector3 direction;
    public Vector3 velocity;
    public Vector3 acceleration;

    protected Vector3 steeringForces;
    protected Vector3 wanderDisplace;
    public float radius = 1f;

    public bool grounded = true;
    public float floatDistance = 2.5f;


    // Floats for force movement
    public float mass = 1;
    public float maxSpeed = 5;
    private float arriveSpeed;
    public float maxWanderSpeed = 4f;
    public float maxForce = 10;
    public float wanderAngle = 0;
    public float arrivalSqrDistance = 1f;


    // Instance variables for path following
    public Path path;
    public int targetNodeIndex;
    private float shortestSqrDistance;
    private float testDistance;
    private Vector3 testVector;
    private Vector3 futurePos;
    private Vector3 pathForces;
    private Vector3 futureProjection;
    private Vector3 currentProjection;
    private bool backTracking = false;

    // Closest point method vectors
    private Vector3 projectionVector;
    private Vector3 futurePos_projection;


    // Materials for drawing debug lines
    public static Material forwardMat;
    public static Material rightMat;
    #endregion


    // Use this for initialization
    protected virtual void Start()
    {
        if (mass == 0)
        {
            mass = 1;
        }
        else if (mass < 0)
        {
            mass *= -1;
        }
        if (arrivalSqrDistance < 0)
        {
            arrivalSqrDistance *= -1;
        }

        position = transform.position;
        direction = transform.forward;

        // If path exists, determine the closest target index
        if (path != null)
        {
            futurePos = velocity * 1.5f;
            targetNodeIndex = (path.GetClosestSegmentIndex(position + futurePos) + 1) % path.Count;
        }

        if (arrivalSqrDistance == 0)
        {
            arrivalSqrDistance = 1f;
        }
    }

    // Update is called once per frame
    protected void Update()
    {
        // Reset local variables for this update cycle
        position = transform.position;
        steeringForces = Vector3.zero;

        CalculateSteeringForces();
        steeringForces = Vector3.ClampMagnitude(steeringForces, maxForce);

        // Keep the vehicle within the park grounds regardless of what the max
        // forces for this vehicle are.
        SteerInBounds();
        
        if (grounded) steeringForces.y = 0;

        ApplyForce(steeringForces);
        UpdatePosition();

        SetTransform();
    }


    /// <summary>
    /// UpdatePosition
    /// Calculate the velocity and resulting position of a vehicle
    /// based on any forces
    /// </summary>
    void UpdatePosition()
    {
        // Accumulation of acceleration changes the velocity.
        // Clamp it to a maximum speed.
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // Update the local position of the object after movement.
        position += velocity * Time.deltaTime;
        direction = velocity.normalized;

        // Reset acceleration
        acceleration = Vector3.zero;
    }


    /// <summary>
    /// Calculate and apply all steering forces acting upon this object, thus
    /// affecting the overall acceleration of the object.
    /// </summary>
    protected abstract void CalculateSteeringForces();


    #region Seek & Flee
    /// <summary>
    /// Acquire a force to have this object attracted towards a location
    /// </summary>
    /// <param name="targetPosition">Location to be attracted to</param>
    /// <returns>Vector3 force of seeking force</returns>
    protected Vector3 GetSeek(Vector3 targetPosition)
    {
        return ((targetPosition - position).normalized * maxSpeed) - velocity;
    }


    /// <summary>
    /// Acquire a force to make this object move away from a target location
    /// </summary>
    /// <param name="targetPosition">Target to move away from</param>
    /// <returns>Vector3 force to flee with</returns>
    protected Vector3 GetFlee(Vector3 targetPosition)
    {
        return ((position - targetPosition).normalized * maxSpeed) - velocity;
    }
    #endregion

    /// <summary>
    /// Acquire the future location of a moving target
    /// </summary>
    /// <param name="target">Moving target</param>
    /// <param name="deltaTime">Time, in seconds, into the future to acquire a
    /// future position from.</param>
    /// <returns>Vector3 location of a moving target's future location</returns>
    protected Vector3 GetFutureLocation(VehicleMovement target, float deltaTime)
    {
        return ((target.velocity * deltaTime) + target.gameObject.transform.position);
    }

    #region Pursue & Evade
    /// <summary>
    /// Move away from the future location of a moving target
    /// </summary>
    /// <param name="target">Moving target</param>
    /// <param name="deltaTime">Time, in seconds, into the future to acquire a
    /// future position from.</param>
    /// <returns>Vector3 force to evade with</returns>
    protected Vector3 GetEvade(VehicleMovement target, float deltaTime)
    {
        return (GetFlee(GetFutureLocation(target, deltaTime)));
    }

    /// <summary>
    /// Move towards from the future location of a moving target
    /// </summary>
    /// <param name="target">Moving target</param>
    /// <param name="deltaTime">Time, in seconds, into the future to acquire a
    /// future position from.</param>
    /// <returns>Vector3 force to pursue with</returns>
    protected Vector3 GetPursue(VehicleMovement target, float deltaTime)
    {
        return (GetSeek(GetFutureLocation(target, deltaTime)));
    }
    #endregion


    /// <summary>
    /// Apply a smooth variation in movement to the steering forces
    /// </summary>
    protected void Wander()
    {
        // Project a circle some distance ahead
        wanderDisplace = position + (transform.forward * 1.3f);

        // Get a random angle to determine a displacement vector
        wanderAngle += Random.Range(-6f, 6f);
        if (wanderAngle > 90)
        {
            wanderAngle = wanderAngle - 180;
        }
        else if (wanderAngle < -90)
        {
            wanderAngle = 180 - wanderAngle;
        }
        wanderDisplace += Quaternion.Euler(0, wanderAngle, 0) * transform.forward;

        // Seek this displacement vector
        steeringForces += ((wanderDisplace - position).normalized * maxWanderSpeed) - velocity;
    }


    /// <summary>
    /// Applies any Vector3 force to the acceleration vector (a = F/m)
    /// </summary>
    /// <param name="force">Force to apply</param>
    void ApplyForce(Vector3 force)
    {
        acceleration += force / mass;
    }

    /// <summary>
    /// Sets the transform component to the local positon
    /// </summary>
    void SetTransform()
    {
        // Set the "forward" vector to this vehicle's direction
        //direction.y = 0;
        if (!grounded)
        {
            if (direction.y > .9f)
            {
                direction.y = .9f;
            }
            else if (direction.y < -.9f)
            {
                direction.y = -.9f;
            }
            //direction.y = 0;
        }
        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }

        //velocity.y = 0;
        gameObject.GetComponent<CharacterController>().Move(velocity * Time.deltaTime);

        if (grounded)
        {
            position = transform.position;
            position.y = Terrain.activeTerrain.SampleHeight(position) + floatDistance;
            transform.position = position;
        }

    }

    /// <summary>
    /// If approaching the outer bounds of the park, steer the vehicle
    /// so it stays within the bounds of the park.
    /// </summary>
    void SteerInBounds()
    {
        // If outside the buffer zone for the park bounds, steer into park
        if (position.x > SceneManager.MaxX - SceneManager.BorderBuffer
            || position.x < SceneManager.MinX + SceneManager.BorderBuffer
            || position.z > SceneManager.MaxZ - SceneManager.BorderBuffer
            || position.z < SceneManager.MinZ + SceneManager.BorderBuffer)
        {
            steeringForces += ((SceneManager.Center - position).normalized
                * maxSpeed * maxSpeed);
        }
    }


    /// <summary>
    /// Obstacle avoidance - Return a Vector3 to steer away from obstacles in
    /// the path of this vehicle
    /// </summary>
    protected Vector3 AvoidObstacles()
    {
        // Step 1: Project a "safe zone" some distance away from the vehicle w/
        // the same radius as the vehicle
        if (SceneManager.obstacles.Count > 0)
        {
            foreach (Obstacle obstacle in SceneManager.obstacles)
            {
                // Step 2: Eliminate any obstacles outside the safe zone's distance
                if ((obstacle.gameObject.transform.position - position).sqrMagnitude
                - Mathf.Pow(obstacle.radius, 2) - (radius * radius) < (velocity).sqrMagnitude * 1)
                {
                    // Step 3: In front or behind? Dot difference to determine where
                    // it is. Only use if in front.
                    if (Vector3.Dot(
                        obstacle.gameObject.transform.position - position,
                        transform.forward) > 0)
                    {
                        // Step 4: Determine if radius of obstacle is in projected
                        // safe zone by using dot product
                        if (Mathf.Pow(obstacle.radius, 2) + (radius * radius)
                            >= Vector3.Dot(
                                obstacle.gameObject.transform.position - position,
                                transform.right))
                        {
                            // Step 5: Steer left or right away from obstacle
                            if (Vector3.Dot(
                                obstacle.gameObject.transform.position - position,
                                transform.right) >= 0)
                            {
                                return ((-transform.right * maxSpeed)
                                    - velocity) * (velocity.sqrMagnitude
                                        / (obstacle.gameObject.transform.position
                                        - position).sqrMagnitude);
                            }
                            else
                            {
                                return ((transform.right * maxSpeed)
                                    - velocity) * (velocity.sqrMagnitude
                                        / (obstacle.gameObject.transform.position
                                        - position).sqrMagnitude);
                            }
                        }
                    }
                }
            }
        }

        // If no obstacles in the way, don't steer away from anything
        return Vector3.zero;
    }


    // *********************************************************************************************************************
    //                                                  PATH FOLLOWING
    // *********************************************************************************************************************

    #region Path Following
    /// <summary>
    /// Follow the current path
    /// </summary>
    /// <returns>Vector3 force to stay on the path and go to the next node
    /// </returns>
    /// <param name="timeInFuture">Time in future of the vehicle's position
    /// to determine if it will be on the path this far ahead into the future
    /// (in seconds)</param>
    protected Vector3 FollowPath(float timeInFuture = .5f)
    {
        if (path != null)
        {
            // Reset the path forces
            pathForces = Vector3.zero;

            // Step 1: Find the vehicle's future position
            futurePos = position + (velocity * timeInFuture);

            // Step 2: Determine which node to chase after
            #region STEP 2: Determine Target Node
            #region Proximity-Based Path Node Selection
            if (!path.pointBased)
            {
                // Target the node of the endpoint of the closest path segment every frame
                targetNodeIndex = (path.GetClosestSegmentIndex(futurePos) + 1) % path.Count;
            }
            #endregion
            

            // Project the current and future positions of this vehicle onto the
            // closest point of each to the path's spine
            currentProjection = GetClosestFuturePosition(
                path[(path.Count + (targetNodeIndex - 1)) % path.Count],
                path.GetSegment((path.Count + (targetNodeIndex - 1)) % path.Count));

            // Project the future position of this vehicle onto the path's spine.
            futureProjection = GetClosestFuturePosition(
                path[(path.Count + (targetNodeIndex - 1)) % path.Count],
                path.GetSegment((path.Count + (targetNodeIndex - 1)) % path.Count),
                timeInFuture);

            
            // Determine whether the next node should be targeted:
            // Has the node been passed if proximity-based, or has the node been
            // reached if node-based?
            if ((path.pointBased && (path[targetNodeIndex] - position).sqrMagnitude
                        <= path.pathNodeProximity * path.pathNodeProximity
                    || !path.pointBased && (path[targetNodeIndex] - position).sqrMagnitude
                        <= path.pathRadius * path.PathRadius)
                || (!path.pointBased
                    && ((currentProjection - path[(path.Count + (targetNodeIndex - 1)) % path.Count]).sqrMagnitude
                        >= path.GetSegment((path.Count + (targetNodeIndex - 1)) % path.Count).sqrMagnitude)))
            {
                AdvancePathNode();

                // If the path nodes are very close together even after advancement,
                // advance one node ahead even further.
                // Re-project the current and future positions of this vehicle onto the
                // closest point of each to the path's spine
                currentProjection = GetClosestFuturePosition(
                    path[(path.Count + (targetNodeIndex - 1)) % path.Count],
                    path.GetSegment((path.Count + (targetNodeIndex - 1)) % path.Count));

                // Re-project the future position of this vehicle onto the path's spine.
                futureProjection = GetClosestFuturePosition(
                    path[(path.Count + (targetNodeIndex - 1)) % path.Count],
                    path.GetSegment((path.Count + (targetNodeIndex - 1)) % path.Count),
                    timeInFuture);
            }
            #endregion

            #region STEP 3: Staying on the path
            // If point-based path, seek the target node if within radius but not within
            // required distance. OR, if point-based but the point has been passed, seek
            // the node immediately.
            if ((path.pointBased && (path[targetNodeIndex] - position).sqrMagnitude <= path.pathRadius * path.PathRadius
                    && (path[targetNodeIndex] - position).sqrMagnitude > path.pathNodeProximity * path.pathNodeProximity)
                || (path.pointBased && ((currentProjection - path[(path.Count + (targetNodeIndex - 1)) % path.Count]).sqrMagnitude
                        >= path.GetSegment((path.Count + (targetNodeIndex - 1)) % path.Count).sqrMagnitude + path.PathRadius * path.PathRadius)
                    && Vector3.Dot(
                        currentProjection - path[(path.Count + (targetNodeIndex - 1)) % path.Count],
                        path.GetSegment((path.Count + (targetNodeIndex - 1)) % path.Count).normalized)
                        > 0))
            {
                // Allow the path follower to fight against the flow of the path
                // to seek the node
                backTracking = true;
                pathForces += GetSeek(path[targetNodeIndex]);
            }
            else
            {
                backTracking = false;
            }


            #region CHECK: Am I behind the start of the path segment?
            // Ensure that the projection is not in a direction opposite that
            // from the segment's direction. If so, reverse the future projection's
            // direction from the current projection.
            if (Vector3.Dot(
                    path[(path.Count + (targetNodeIndex - 1)) % path.Count] - futureProjection,
                    path.GetSegment((path.Count + (targetNodeIndex - 1)) % path.Count).normalized)
                < 0)
            {
                // If the future position is outside of the path's radius, seek the projection.
                if ((futureProjection - futurePos).sqrMagnitude
                        > path.PathRadius * path.PathRadius)
                {
                    pathForces += GetSeek(
                        (currentProjection - futureProjection) + currentProjection);
                }

                // Step 4: Align to the direction of the reversed path segment
                else if (!backTracking)
                {
                    pathForces += path.GetSegment(
                        (path.Count + (targetNodeIndex - 1)) % path.Count) - velocity;
                }
            }
            #endregion


            // If following the normal path direction, continue as normal.
            else
            {
                // If the future position is outside of the path's radius, seek the projection.
                if ((futureProjection - futurePos).sqrMagnitude
                        > path.PathRadius * path.PathRadius)
                {
                    pathForces += GetSeek(futureProjection);
                }

                // Step 4: Align to the direction of the current path segment
                // if on the path
                else if (!backTracking)
                {
                    pathForces += path.GetSegment(
                        (path.Count + (targetNodeIndex - 1)) % path.Count) - velocity;
                }
            }
            #endregion
            

            // Return all the accumulated forces
            return pathForces;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Get the closest position from this vehicle to a vector segment 
    /// </summary>
    /// <param name="origin">The origin point of the segment</param>
    /// <param name="segment">The segment to find the closest point on</param>
    /// <param name="time">The amount of time in the future to look at where this vehicle
    /// will be. Get the closest point of that position. Default to current position.</param>
    /// <returns>Vector3 position of the closest point on the segment to this
    /// vehicle</returns>
    protected Vector3 GetClosestFuturePosition(Vector3 origin, Vector3 segment, float time = 0)
    {
        futurePos_projection = position + (velocity * time);
        projectionVector = origin + Vector3.Dot(futurePos_projection - origin, segment.normalized) * segment.normalized;
        return projectionVector;
    }

    /// <summary>
    /// Start targeting the next node in the path
    /// </summary>
    protected void AdvancePathNode()
    {
        targetNodeIndex = (targetNodeIndex + 1) % path.Count;
    }
    #endregion


    // *********************************************************************************************************************
    //                                                    ARRIVING
    // *********************************************************************************************************************
    
    /// <summary>
    /// Slow down as a target is approached
    /// </summary>
    /// <param name="target">Target to arrive at</param>
    /// <param name="sqrArriveDistance">Distance from target to begin
    /// arriving/slowing</param>
    /// <returns>Vector3 steering force to arrive to a target</returns>
    protected Vector3 Arrive(Vector3 target, float sqrArriveDistance)
    {
        if (sqrArriveDistance != 0 && (target - position).sqrMagnitude <= sqrArriveDistance)
        {
            if ((target - position).sqrMagnitude / sqrArriveDistance > .1f)
            {
                return GetSeek(target) * ((target - position).sqrMagnitude / sqrArriveDistance);
            }
            else
            {
                return Vector3.zero;
            }
        }

        return GetSeek(target);
    }
}