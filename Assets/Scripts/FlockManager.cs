// Jared White
// November 22, 2016

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A script to manage a group of Flocker objects.
/// Will automatically generate Flockers if not filled prior to starting script.
/// </summary>
public class FlockManager : MonoBehaviour
{
    // Fields & Instances
    #region Fields
    // Flock instances
    public List<GameObject> _flockerPrefabs;
    public GameObject _leaderPrefab;
    public GameObject _avgPositionBlockPrefab;

    public List<Flocker> flockers;
    public VehicleMovement leader;
    public Path path;
    public BehaviorState currentState;
    

    public int numFlockers = 3;
    public float spawnWidth = 3;


    // Weights
    public float alignmentWeight = 1;
    public float cohesionWeight = 1;
    public float separationWeight = 1;
    public float followWeight = 1.1f;
    public float pathWeight = 1;

    // Average flock values
    protected Vector3 avgDirection;
    protected Vector3 avgPosition;
    protected GameObject block;

    private int randomNum;


    public enum BehaviorState
    {
        Flocking = 1,
        LeaderFollowing = 2,
        PathFollowing = 3
    }
    #endregion


    // Properties
    #region Properties
    /// <summary>
    /// The average direction that each Flocker in this flock is facing
    /// </summary>
    public Vector3 AverageDirection
    {
        get { return avgDirection; }
    }

    /// <summary>
    /// The average position of each Flocker in this flock
    /// </summary>
    public Vector3 FlockPosition
    {
        get { return avgPosition; }
    }

    /// <summary>
    /// Get the Flockers
    /// </summary>
    public List<Flocker> Flock
    {
        get { return flockers; }
        set { flockers = value; }
    }

    /// <summary>
    /// The block representing the centroid position of this Flock
    /// </summary>
    public GameObject Block
    {
        get
        {
            if (_avgPositionBlockPrefab != null)
            {
                block = Instantiate(_avgPositionBlockPrefab as GameObject);
                block.transform.SetParent(transform);
                block.name = "Flock Block";
            }

            return block;
        }
        set
        {
            block = value;

            // Enable/Disable debug blocks
            if (SceneManager.debugEnabled)
            {
                block.GetComponent<MeshRenderer>().enabled = true;
            }
            else if (!SceneManager.debugEnabled)
            {
                block.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }
    #endregion


    // Use this for initialization
    protected virtual void Start()
    {
        // Create the debug block representing the flock's average position
        if (_avgPositionBlockPrefab != null && block == null)
        {
            block = Instantiate(_avgPositionBlockPrefab as GameObject);
            block.transform.SetParent(transform);
            block.name = "Flock Block";
        }

        // If the Flock isn't already populated, instantiate flockers
        if (flockers.Count == 0 && _flockerPrefabs.Count > 0)
        {
            // Validate public references before utilizing them
            if (numFlockers < 0)
            {
                numFlockers *= -1;
            }
            if (spawnWidth < 0)
            {
                spawnWidth *= -1;
            }

            // Instance variables to assist with instantiating flocks
            float spawnX;
            float spawnZ;
            float spawnY;
            Vector3 offset = transform.position;

            // Spawn flockers
            for (int i = 0; i < numFlockers; i++)
            {
                // Take a random prefab from the list of Flocker prefabs
                // and spawn it within a square boundary of the FlockManager
                randomNum = Random.Range(0, _flockerPrefabs.Count);

                if (_flockerPrefabs[randomNum].GetComponent<Flocker>() != null)
                {
                    // Acquire random values and sample the terrain at this location
                    spawnX = transform.position.x + Random.Range(-spawnWidth, spawnWidth);
                    spawnZ = transform.position.z + Random.Range(-spawnWidth, spawnWidth);
                    offset.x = spawnX;
                    offset.z = spawnZ;
                    spawnY = Terrain.activeTerrain.SampleHeight(offset);
                    offset.y = spawnY;

                    // Instantiate the flocker
                    flockers.Add((Instantiate(
                            _flockerPrefabs[randomNum],
                            offset,
                            transform.localRotation) as GameObject)
                        .GetComponent<Flocker>());

                    // Set the newly instantiated Flocker as a child of this FlockManager
                    flockers[i].transform.SetParent(transform);

                    if (path != null)
                    {
                        flockers[i].path = path;
                    }
                }

                else
                {
                    Debug.LogError("Flocker Prefab does not have a Flocker script");
                }
            }

            // Enable/Disable debug blocks
            block.GetComponent<MeshRenderer>().enabled = false;
        }

        // If populated already, readjust references
        else
        {
            numFlockers = flockers.Count;
        }

        // Create a leader, if not done so already
        if (_leaderPrefab != null && leader == null)
        {
            leader = (Instantiate(
                _leaderPrefab,
                transform.position,
                transform.rotation) as GameObject).GetComponent<VehicleMovement>();
            leader.transform.SetParent(transform);
            leader.name = "LEADER";
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDirection();
        UpdatePosition();

        // Enable/Disable debug blocks
        if (SceneManager.debugEnabled)
        {
            block.GetComponent<MeshRenderer>().enabled = true;
        }
        else if (!SceneManager.debugEnabled)
        {
            block.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    
    /// <summary>
    /// Update the average direction of the flock
    /// </summary>
    void UpdateDirection()
    {
        if (flockers.Count > 0)
        {
            avgDirection = Vector3.zero;
            foreach (Flocker flocker in flockers)
            {
                avgDirection += flocker.transform.forward;
            }

            avgDirection.Normalize();
            block.transform.forward = avgDirection;
        }
        else
        {
            avgDirection = transform.forward;
        }
    }

    /// <summary>
    /// Update the location of the centroid of the flock
    /// </summary>
    void UpdatePosition()
    {
        if (flockers.Count > 0)
        {
            avgPosition = Vector3.zero;
            foreach (Flocker flocker in flockers)
            {
                avgPosition += flocker.transform.position;
            }
            
            avgPosition /= flockers.Count;
            block.transform.position = avgPosition;
        }
    }

    /// <summary>
    /// Draw debug lines if they are enabled in the scene
    /// </summary>
    void OnRenderObject()
    {
        if (SceneManager.debugEnabled)
        {
            DrawDefaultLines();
        }
    }

    /// <summary>
    /// Draw the default forward and right debug lines
    /// </summary>
    void DrawDefaultLines()
    {
        // Forward Debug Line
        SceneManager.forwardMat.SetPass(0);          // Set line material
        GL.Begin(GL.LINES);                          // Begin to draw lines
        GL.Vertex(avgPosition);                      // First endpoint of this line
        GL.Vertex(avgPosition + avgDirection + avgDirection + avgDirection + avgDirection);       // Second endpoint of this line
        GL.End();                                    // Finish drawing the line
    }
}
