using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyDataStructure;
/// <summary>
/// http://jceipek.com/Olin-Coding-Tutorials/pathing.html#funnel-algorithm
/// 
/// 
/// First, all edges should be ordered, clockwise/ counter-clockwise are both fine
/// if, clockwise then you can skip below.
/// else if counter-clockwise should set -
/// </summary>
public static class SimpleStupidFunnel
{
    public static List<Vector2> Algorithm(Vector2 start,Vector2 end, List<HalfEdge> edges,float radius = 0)
    {
        List<Vector2> result = new List<Vector2>(edges.Count);
        // edges = sortEdge(edges, start, end);
        int i = 0;
        for( i =0; i < edges.Count; i++)
        {
            var edge = edges[i];

            Vector2 direction = (edge.nextEdge.v.position - edge.v.position).normalized;

            edge.nextEdge.v.position -= direction * radius;
            edge.v.position += direction * radius;

            
            Debug.Log($"index : {i} start : {edge.v.position} end : {edge.nextEdge.v.position}");
        }
        i = 0;
        Vector2 apexPoint = start;
        Vector2 Epoint = edges[i].v.position;
        Vector2 EVpoint = edges[i].nextEdge.v.position;

        int EIndex = 0;
        int EVIndex = 0;

        int safety = 0;
        for (i = 1; i < edges.Count; i++)
        {
            safety++;

            if(safety > 10000)
            {
                break;
            }
            var edge = edges[i];

            var canTurnE = WherePointLocatedOnTheLine(edge.v.position,apexPoint, Epoint) <= 0;

            if(canTurnE)
            {
                var isEpointInEVLine = WherePointLocatedOnTheLine(edge.v.position, apexPoint, EVpoint) >= 0;

                
                if (isEpointInEVLine) // 만약 Epoint가 funnel 안에 있다면...
                {
                    Epoint = edge.v.position;
                    EIndex = i;
                }
                else
                {
                    apexPoint = EVpoint;
                    result.Add(apexPoint);

                    EVpoint = edges[EVIndex].nextEdge.v.position;
                    Epoint = edges[EVIndex].v.position;
                    i = EVIndex;

                    continue;
                }
            }


            var canTurnEV = WherePointLocatedOnTheLine(edge.nextEdge.v.position, apexPoint, EVpoint) >= 0;

            if(canTurnEV) // EVPoint 가 funnel 안에 있다면
            {
                var isEpointInELine = WherePointLocatedOnTheLine(edge.nextEdge.v.position, apexPoint, Epoint) <= 0;


                if( isEpointInELine)
                {
                    EVpoint = edge.nextEdge.v.position;
                    EVIndex = i;
                }
                else
                {
                    apexPoint = Epoint;

                    result.Add(apexPoint);

                    Epoint = edges[EIndex].v.position;
                    EVpoint = edges[EIndex].nextEdge.v.position;
                    i = EIndex;
                    continue;
                }
            }

            if(i == edges.Count - 1)
            {
                if (WherePointLocatedOnTheLine(end, apexPoint, Epoint) > 0) // end가 funnel밖에 있다
                {
                    apexPoint = Epoint;
                    result.Add(Epoint);

                    Epoint = edges[EIndex].v.position;
                    EVpoint = edges[EIndex].nextEdge.v.position;
                    i = EIndex;

                }
                else if (WherePointLocatedOnTheLine(end, apexPoint, EVpoint) < 0)
                {
                    apexPoint = EVpoint;
                    result.Add(apexPoint);

                    EVpoint = edges[EVIndex].nextEdge.v.position;
                    Epoint = edges[EVIndex].v.position;
                    i = EVIndex;
                }
            }

        }

        result.Add(end);

        foreach (var point in result)
        {
            Debug.Log(point);
        }
        
        return result;
    }




    /// <summary>
    /// if directed to 3 o'clock and point is below become +
    /// also, 6 o'clock direction and point is placed at left become +.
    /// on tail --> head
    /// < 0 =  right direction
    /// = 0 = on the line
    /// > 0 = left direction
    /// 시계방향이면 +값이 삼각형의 안에 있다
    /// 반대시계방향이면 -값이 삼각형의 안에 있다

    /// </summary>
    /// <param name="point"></param>
    /// <param name="tail"></param>
    /// <param name="head"></param>
    /// <returns></returns>
    static float WherePointLocatedOnTheLine(Vector2 point, Vector2 tail, Vector2 head)
    {
        var ax = tail.x - point.x;
        var ay = tail.y - point.y;
        var bx = head.x - point.x;
        var by = head.y - point.y;
        return bx * ay - ax * by;
    }
}
