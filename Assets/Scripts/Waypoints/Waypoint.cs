// SerialID: [77a855b2-f53d-4b80-9c94-c40562952b74]
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [SerializeField] private int index = 0;
    public int Index {
        get { return index; }
        private set { index = value; }
    }

    [SerializeField] private bool isLast = false;
    public bool IsLast {
        get { return isLast; }
        private set { isLast = value; }
    }

    [SerializeField] private Vector3 nextDirection;
    /// <summary>
    /// 次のWaypointの方向
    /// </summary>
    public Vector3 NextDirection {
        get { return nextDirection; }
        private set { nextDirection = value; }
    }

    [Header("Layer Settings"), SerializeField] private string layerWall = "Wall";
    private string LayerWall => layerWall;

    [SerializeField] private string layerRoad = "Road";
    private string LayerRoad => layerRoad;

    public void SetPosition(int index, bool isLast) {
        Index = index;
        IsLast = isLast;

        transform.localScale = Vector3.one;

        RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.down, out hit, float.MaxValue, LayerMask.GetMask(LayerRoad))) {
            transform.position = hit.point + Vector3.up;
            var rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            transform.rotation = rotation * transform.rotation;
        }
    }

    public (Vector3, Vector3) SetScaleAndRotation() {
        var left = Vector3.zero;
        var right = Vector3.zero;

        RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.down, out hit, float.MaxValue, LayerMask.GetMask(LayerRoad))) {
            transform.position = hit.point + Vector3.up;
            var rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            transform.rotation = rotation * transform.rotation;
        }

        if(Physics.Raycast(transform.position + transform.up, -transform.right, out hit, float.MaxValue, LayerMask.GetMask(LayerWall))) {
            left = hit.point;
        }

        if(Physics.Raycast(transform.position + transform.up, transform.right, out hit, float.MaxValue, LayerMask.GetMask(LayerWall))) {
            right = hit.point;
        }

        var distance = Vector3.Distance(left, right);
        var position = (left + right) / 2;

        transform.position = new Vector3(position.x, transform.position.y, position.z);
        transform.localScale = new Vector3(distance, 2, 1);

        return (left, right);
    }
    // private Waypoint nextWaypoint;
    // /// <summary>
    // /// コース上の次のウェイポイント
    // /// </summary>
    // public Waypoint NextWaypoint {
    //     get { return nextWaypoint; }
    //     private set { nextWaypoint = value; }
    // }

    public void SetNextDirection(Vector3 nextPosition) { // ★引数を元に戻す★
        NextDirection = Vector3.Normalize(nextPosition - transform.position);
    }

    void OnDrawGizmosSelected() {
        /*
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position - transform.right * 20);
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 20);
        */
    }
}
