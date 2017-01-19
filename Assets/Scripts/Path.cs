// Jared White
// December 6, 2016

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A system of nodes linking to each other represented physically as a path
/// </summary>
public class Path : MonoBehaviour
{
    // Fields and References
    #region Fields
    public List<GameObject> nodes;
    public float pathRadius;
    public float pathNodeProximity = 2f; // Precision radius to a path node
    public bool pointBased;
    public bool displayGizmos = false;

    // For determining the closest segment to a position
    private Vector3 testVector;
    private Vector3 projectionVector;
    private float testDistance;
    private float shortestSqrDistance;
    private int closestIndex;
    #endregion

    // Properties
    #region Properties
    /// <summary>
    /// Get the position of this a node in the path
    /// </summary>
    /// <param name="nodeNumber">Index of node</param>
    /// <returns>Vector3 location of node</returns>
    public Vector3 this[int nodeNumber]
    {
        get { return nodes[nodeNumber].transform.position; }
        set
        {
            if (nodeNumber < nodes.Count && nodeNumber >= 0)
            {
                nodes[nodeNumber].transform.position = value;
            }
        }
    }

    /// <summary>
    /// Number of nodes in this Path
    /// </summary>
    public int Count
    {
        get { return nodes.Count; }
    }

    /// <summary>
    /// Get or set the radius of the path to follow.
    /// If a negative value is given, radius will be set to zero.
    /// </summary>
    public float PathRadius
    {
        get { return pathRadius; }
        set
        {
            if (value < 0)
            {
                pathRadius = 0;
            }
            else
            {
                pathRadius = value;
            }
        }
    }
    #endregion
    
    // Use this for initialization
    void Start()
    {
        // Validate approximated values
        if (PathRadius < 0)
        {
            PathRadius *= -1;
        }

        if (pathNodeProximity < 0)
        {
            pathNodeProximity *= -1;
        }
        else if (pathNodeProximity == 0)
        {
            pathNodeProximity = 2f;
        }
	}
	
	// Update is called once per frame
	void Update()
    {

	}


    /// <summary>
    /// Add a node to the end of the Path
    /// </summary>
    /// <param name="node">GameObject representing the node location</param>
    public void AddNode(GameObject node)
    {
        // Add a reference to the new node
        nodes.Add(node);
        nodes[nodes.Count - 1].transform.SetParent(transform);
    }

    /// <summary>
    /// Add a node to the end of the Path
    /// </summary>
    /// <param name="location">Location of node to add</param>
    public void AddNode(Vector3 location)
    {
        // Create a new node GameObject & track a reference to it
        nodes.Add(new GameObject());
        nodes[nodes.Count - 1].transform.SetParent(transform);
        nodes[nodes.Count - 1].transform.position = location;
        nodes[nodes.Count - 1].name = name + " Node #" + nodes.Count;
    }

    /// <summary>
    /// Draw Gizmo lines from nodes in the scene
    /// </summary>
    void OnDrawGizmos()
    {
        if (displayGizmos && nodes.Count > 1)
        {
            //Gizmos.color = Color.white;
            for (int i = 0; i < nodes.Count; i++)
            {
                Gizmos.color = new Color(1, 1f - ((float)i / (nodes.Count - 1)), 1f - ((float)i / (nodes.Count - 1)));
                Gizmos.DrawSphere(nodes[i].transform.position, pathNodeProximity);
                Gizmos.DrawLine(nodes[i].transform.position,
                    nodes[(i + 1) % nodes.Count].transform.position);

                Gizmos.DrawWireSphere(nodes[i].transform.position, pathRadius);
            }
        }
    }

    /// <summary>
    /// Get the closest Path segment from a position
    /// </summary>
    /// <param name="position">Position to check distance from path segments</param>
    /// <returns>Segment that's closest to the position</returns>
    public Vector3 GetClosestSegment(Vector3 position)
    {
        if (nodes.Count > 1)
        {
            // If more than one segment, determine which is the closest.
            if (nodes.Count > 2)
            {
                // Set the initial closest segment to the first segment's projection
                closestIndex = 0;
                projectionVector = this[0] + Vector3.Dot(this[0] - position, GetSegment(0).normalized) * GetSegment(0).normalized;
                shortestSqrDistance = (projectionVector - position).sqrMagnitude;

                for (int i = 1; i < nodes.Count; i++)
                {
                    // If any of the other segments are closer, that's the new
                    // closest distance
                    // Projection = B + (|BE| * (BFP dot |BE|))
                    testVector = this[i] + Vector3.Dot(this[i] - position, GetSegment(i).normalized) * GetSegment(i).normalized;
                    testDistance = (testVector - position).sqrMagnitude;

                    // If the distance between the position's projection onto
                    // the segment is shorter than the initial shortest distance
                    // then this segment's is now the current closest segment.
                    if (testDistance <= shortestSqrDistance)
                    {
                        shortestSqrDistance = testDistance;
                        projectionVector = testVector;
                        closestIndex = i;
                    }
                }
                
                return GetSegment(closestIndex);
            }
            
            // If only one segment, then that is the closest segment.
            return GetSegment(0);
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Get the closest Path segment index from a position
    /// </summary>
    /// <param name="position">Position to check distance from path segments</param>
    /// <returns>Segment index that's closest to the position</returns>
    public int GetClosestSegmentIndex(Vector3 position)
    {
        if (nodes.Count > 1)
        {
            // If more than one segment, determine which is the closest.
            if (nodes.Count > 2)
            {
                // Set the initial closest segment to the first segment's projection
                closestIndex = 0;
                projectionVector = this[0] + Vector3.Dot(this[0] - position, GetSegment(0).normalized) * GetSegment(0).normalized;
                shortestSqrDistance = (projectionVector - position).sqrMagnitude;

                for (int i = 1; i < nodes.Count; i++)
                {
                    // If any of the other segments are closer, that's the new
                    // closest distance Projection = B + (|BE| * (BFP dot |BE|))
                    testVector = this[i] + Vector3.Dot(this[i] - position, GetSegment(i).normalized) * GetSegment(i).normalized;
                    testDistance = (testVector - position).sqrMagnitude;

                    // If the distance between the position's projection onto
                    // the segment is shorter than the initial shortest distance
                    // then this segment's is now the current closest segment.
                    if (testDistance < shortestSqrDistance)
                    {
                        shortestSqrDistance = testDistance;
                        projectionVector = testVector;
                        closestIndex = i;
                    }
                }
                
                return closestIndex;
            }

            // If only one segment, then that is the closest segment.
            return 0;
        }

        if (nodes.Count == 1)
        {
            return 0;
        }

        return -1;
    }


    /// <summary>
    /// Get dynamic segments from the nodes' current positions
    /// </summary>
    /// <param name="nodeIndex">Index of the note of the segment's origin</param>
    /// <returns>Segment vector from one node to the next</returns>
    public Vector3 GetSegment(int nodeIndex)
    {
        if (nodes.Count > 1)
        {
            return (nodes[(nodeIndex + 1) % nodes.Count].transform.position - nodes[nodeIndex].transform.position);
        }
        
        return Vector3.zero;
    }
}
