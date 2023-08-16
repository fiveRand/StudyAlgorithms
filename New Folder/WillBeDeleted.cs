using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WillBeDeleted : MonoBehaviour
{
    public Transform lineStart;
    public Transform lineEnd;
    public Transform point;
    public Vector2 closestPoint;

    private void OnDrawGizmos() {
        Gizmos.color = Color.white;
        if(lineStart!=null && lineEnd != null)
        {
            Gizmos.DrawLine(lineStart.position, lineEnd.position);
        }

        if(point != null)
        {
            Gizmos.DrawWireSphere(point.position, 0.5f);
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(closestPoint, 0.5f);
    }

    void Update()
    {
        closestPoint = GetClosestPointOnLine(lineStart.position, lineEnd.position, point.position,true);
    }

    public Vector3 GetClosestPointOnLine(Vector3 a, Vector3 b, Vector3 point, bool withinSegment)
    {

        //Assume the line goes from a to b
        Vector3 ab = b - a;
        //Vector from start of the line to the point outside of line
        Vector3 ap = point - a;

        //The normalized "distance" from a to the closest point, so [0,1] if we are within the line segment
        float distance = Vector3.Dot(ap, ab) / Vector3.SqrMagnitude(ab);


        ///This point may not be on the line segment, if so return one of the end points
        float epsilon = MathUtility.EPSILON;

        if (withinSegment && distance < 0f - epsilon)
        {
            return a;
        }
        else if (withinSegment && distance > 1f + epsilon)
        {
            return b;
        }
        else
        {
            //This works because a_b is not normalized and distance is [0,1] if distance is within ab
            return a + ab * distance;
        }
    }

    Vector2 GetclosestPoint(Vector2 a,Vector2 b, Vector2 point)
    {
        Vector2 result = new Vector2();
        if(a.x == b.x)
        {
            result = new Vector2(a.x, point.y);
            return result;
        }
        if(a.y == b.y)
        {
            result = new Vector2(point.x, a.y);
            return result;
        }

        var m1 = (b.y - a.y) / (b.x - a.x);
        var m2 = -1 / m1;
        var x = (m1 * a.x - m2 * point.x + point.y - a.y) / (m1 - m2);
        var y = m2 * (x - point.x) + point.y;
        result = new Vector2(x, y);

        return result;
    }
}
