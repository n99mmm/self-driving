// SerialID: [77a855b2-f53d-4b80-9c94-c40562952b74]
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class WaypointsController : MonoBehaviour
{
    [Header("Waypoint Settings"), SerializeField, Range(1, 40)] private float distance = 1;
    public float Distance => distance;

    [SerializeField] private float height = 10.0f;
    public float Height => height;

    [SerializeField] private int size = 100;
    public int Size => size;

    [SerializeField] private Waypoint prefab = null;
    public Waypoint Prefab => prefab;

    [Header("Obstacle Settings"), SerializeField] private int obstacleRate = 10;
    public int ObstacleRate => obstacleRate;

    [SerializeField] private int obstacleFirePosition = -8;
    public int ObstacleFirePosition => obstacleFirePosition;

    [SerializeField] private Obstacle obstaclePrefab = null;
    public Obstacle ObstaclePrefab => obstaclePrefab;

    [SerializeField] private float minForce = 100.0f;
    public float MinForce => minForce;

    [SerializeField] private float maxForce = 300.0f;
    public float MaxForce => maxForce;

    [SerializeField] private float minScale = 0.5f;
    public float MinScale { get { return minScale; } set { minScale = value; } }

    [SerializeField] private float maxScale = 1.5f;
    public float MaxScale { get { return maxScale; } set { maxScale = value; } }
    public void Start() {
        SetNextDirections();
    }

    // private List<Waypoint> allWaypoints;
    /// <summary>
    /// コース上の全てのウェイポイント（Index順）
    /// </summary>
    // public List<Waypoint> AllWaypoints => allWaypoints; // ★追加：外部からアクセスできるようにする★

    public void SetNextDirections() {
        // ウェイポイントのリストを AllWaypoints に格納する
        // allWaypoints = GetComponentsInChildren<Waypoint>() // ★allWaypoints = ... に変更★
        //                     .Where(x => x.transform != Prefab.transform)
        //                     .Where(x => x.transform != transform)
        //                     .OrderBy(x => x.Index)
        //                     .ToList();
        
        List<Waypoint> waypoints = GetComponentsInChildren<Waypoint>()
                                     .Where(x => x.transform != Prefab.transform)
                                     .Where(x => x.transform != transform)
                                     .OrderBy(x => x.Index)
                                     .ToList();

        for (int i = 0; i < waypoints.Count; i++) {
                // Waypoint.cs の SetNextDirection(Vector3 nextPosition) に戻す
                waypoints[i].SetNextDirection(waypoints[(i + 1) % waypoints.Count].transform.position);
        }
    }

    public Vector3 GetFirstNextDirection()
    {
        return GetComponentsInChildren<Waypoint>()
        .Where(x => x.transform != Prefab.transform)
        .Where(x => x.transform != transform)
        .OrderBy(x => x.Index)
        .ToList().Last().NextDirection;
    }
}
