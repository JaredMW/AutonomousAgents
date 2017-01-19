// Jared White
// November 9, 2016

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages base-level scene functions and behavior. Does not contain
/// references to instantiated agents.
/// </summary>
public class SceneManager : MonoBehaviour
{
    // Fields
    #region Fields
    // Environment
    public GameObject _prefabObstacle;
    public FlockManager mainFlock;
    public Terrain terrain;
    //public static Terrain terrainObj;

    public float buffer = 1.5f;
    private static float borderBuffer;

    // Park boundaries
    public static float baseHeight = .01f;
    private static Vector3 minBounds;
    private static Vector3 maxBounds;
    private static Vector3 center;

    // Debug line materials
    public static bool debugEnabled = false;
    public Material fwdMat;
    public static Material forwardMat;

    // Internal references to instantiated objects
    public int numObstacles;
    public static List<Obstacle> obstacles;
    #endregion


    // Properties
    #region Properties
    /// <summary>
    /// Minimum X value of the park grounds
    /// </summary>
    public static float MinX
    {
        get { return minBounds.x; }
    }

    /// <summary>
    /// Maximum X value of the park grounds
    /// </summary>
    public static float MaxX
    {
        get { return maxBounds.x; }
    }

    /// <summary>
    /// Minimum Z value of the park grounds
    /// </summary>
    public static float MinZ
    {
        get { return minBounds.z; }
    }

    /// <summary>
    /// Maximum Z value of the park grounds
    /// </summary>
    public static float MaxZ
    {
        get { return maxBounds.z; }
    }

    /// <summary>
    /// Get the center position of the park
    /// </summary>
    public static Vector3 Center
    {
        get { return center; }
    }

    /// <summary>
    /// The inwards distance from the edge of the park grounds that a vehicle
    /// should stay in
    /// </summary>
    public static float BorderBuffer
    {
        get { return borderBuffer; }
        set { borderBuffer = value; }
    }
    #endregion


    // Use this for initialization
    void Start()
    {
        // Hide the mouse cursor
        Cursor.visible = false;

        // Set the border buffer size
        borderBuffer = buffer;

        // Initialize values for the environmental fields
        minBounds = terrain.GetComponent<TerrainCollider>().bounds.min;
        maxBounds = terrain.GetComponent<TerrainCollider>().bounds.max;
        center = terrain.GetComponent<TerrainCollider>().bounds.center;
        center.y = baseHeight;

        //terrainObj = terrain;

        // Setup the obstacles
        if (numObstacles < 0)
        {
            numObstacles = 0;
        }

        obstacles = new List<Obstacle>();
        GameObject obstacleObj = new GameObject();
        obstacleObj.name = "Obstacles";

        float randX;
        float randZ; 
        for (int o = 0; o < numObstacles; o++)
        {
            randX = Random.Range(MinX + BorderBuffer, MaxX - BorderBuffer);
            randZ = Random.Range(MinZ + BorderBuffer, MaxZ - BorderBuffer);
            while (terrain.SampleHeight(new Vector3(randX, 0, randZ))
                < terrain.GetComponent<TerrainGenerator>().waterHeightPercentage * terrain.terrainData.size.y
                    + terrain.GetComponent<TerrainGenerator>().maxWaterBob)
            {
                randX = Random.Range(MinX + BorderBuffer, MaxX - BorderBuffer);
                randZ = Random.Range(MinZ + BorderBuffer, MaxZ - BorderBuffer);
            }

            obstacles.Add((Instantiate(
                _prefabObstacle,
                new Vector3(
                    randX,
                    terrain.SampleHeight(new Vector3(randX, 0, randZ)),
                    randZ),
                Quaternion.Euler(
                    0,
                    Random.Range(-180f, 180f),
                    0)) as GameObject).GetComponent<Obstacle>());
            obstacles[o].transform.SetParent(obstacleObj.transform);
            obstacles[o].name = _prefabObstacle.name;
            if (obstacles[o].GetComponent<Obstacle>() == null)
            {
                obstacles[o].gameObject.AddComponent<Obstacle>();
            }
        }

        // Setup debug line materials
        #region Setup debug lines
        debugEnabled = false;
        forwardMat = fwdMat;
        #endregion
    }
    
    // Update is called once per frame
    void Update()
    {
        // On the press of the D key, enable/disable debug lines
	    if (Input.GetKeyDown(KeyCode.E))
        {
            debugEnabled = !debugEnabled;
            if (debugEnabled)
            {
                Cursor.visible = true;
            }
            else
            {
                Cursor.visible = false;
            }
        }
    }


    /// <summary>
    /// Determine if 2 circular vehicles are colliding
    /// </summary>
    /// <param name="a">First vehicle</param>
    /// <param name="b">Second vehicle</param>
    /// <returns>True if both are colliding/overlapping</returns>
    bool CircleCollision(VehicleMovement a, VehicleMovement b)
    {
        return ((b.transform.position - a.transform.position).sqrMagnitude
            < b.radius * b.radius + a.radius * a.radius);
    }
}
