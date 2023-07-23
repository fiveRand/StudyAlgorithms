using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyDataStructure;

public static class BoyerWatson
{

    public static Mesh GenerateDelaunayMesh(List<Vector2> points)
    {
        var triangles = GenerateDelaunayTriangle(points);
        var mesh = Triangle2Mesh(triangles);
        return mesh;
    }
    /// <summary>
    /// https://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm
    /// </summary>
    /// <param name="startPoints"></param>
    /// <returns></returns>
    static HashSet<Triangle> GenerateDelaunayTriangle(List<Vector2> startPoints)
    {
        var bound = SetBoundary(startPoints);
        var normalizedPoint = NormalizePoints(startPoints, bound);

        Triangle superTriangle = new Triangle(new Vector2(-100, -100), new Vector2(0, 100), new Vector2(100, -100));
        List<Triangle> tempTriangles = new List<Triangle> { superTriangle };
        HalfEdgeData triangulationData = new HalfEdgeData();
        triangulationData.Update(tempTriangles);

        for (int i = 0; i < normalizedPoint.Count; i++)
        {
            var point = normalizedPoint[i];
            OnInsertPoint(point, triangulationData);
        }
        RemoveSuperTriangle(superTriangle, triangulationData);
        UnNormalizePoints(ref triangulationData, bound);
        HashSet<Triangle> results = triangulationData.HalfEdge2Triangle();
        return results;
    }

    public static Mesh GenerateDelaunayMesh(List<Vector2> points, List<Vector2> constraints)
    {
        var triangles = GenerateDelaunayTriangle(points, constraints);
        var mesh = Triangle2Mesh(triangles);
        return mesh;
    }


    static HashSet<Triangle> GenerateDelaunayTriangle(List<Vector2> startPoints, List<Vector2> constraints)
    {
        var bound = SetBoundary(startPoints);
        var normalizedPoint = NormalizePoints(startPoints, bound);
        var constraintedNormalizedPoint = NormalizePoints(constraints, bound);

        Triangle superTriangle = new Triangle(new Vector2(-100, -100), new Vector2(0, 100), new Vector2(100, -100));
        List<Triangle> tempTriangles = new List<Triangle> { superTriangle };
        HalfEdgeData triangulationData = new HalfEdgeData();
        triangulationData.Update(tempTriangles);

        for (int i = 0; i < normalizedPoint.Count; i++)
        {
            var point = normalizedPoint[i];
            OnInsertPoint(point, triangulationData);
        }


        RemoveSuperTriangle(superTriangle, triangulationData);
        UnNormalizePoints(ref triangulationData, bound);
        HashSet<Triangle> results = triangulationData.HalfEdge2Triangle();
        return results;
    }

    static void OnInsertPoint(Vector2 point,HalfEdgeData triangulationData)
    {
        List<HalfEdgeFace> badTriangles = new List<HalfEdgeFace>();
        foreach (var triangle in triangulationData.faces)
        {
            if (isPointInsideCircumCircle(triangle, point))
            {
                badTriangles.Add(triangle);
            }
        }
        List<HalfEdge> NotSharedEdges = new List<HalfEdge>();
        foreach (var triangle in badTriangles)
        {
            var e1 = triangle.edge;
            var e2 = triangle.edge.nextEdge;
            var e3 = triangle.edge.prevEdge;



            if (!isEdgeSharedByOtherTriangles(e1, badTriangles))
            {
                NotSharedEdges.Add(e1);
            }
            if (!isEdgeSharedByOtherTriangles(e2, badTriangles))
            {
                NotSharedEdges.Add(e2);
            }
            if (!isEdgeSharedByOtherTriangles(e3, badTriangles))
            {
                NotSharedEdges.Add(e3);
            }

        }

        foreach (var triangle in badTriangles)
        {
            DeleteTriangleFace(triangle, triangulationData, true);
        }

        foreach (var halfedge in NotSharedEdges)
        {
            CreateNewFace(halfedge, point, triangulationData);
        }
    }

    static void RemoveSuperTriangle(Triangle superTriangle, HalfEdgeData data)
    {
        HashSet<HalfEdgeFace> trianglesToDelete = new HashSet<HalfEdgeFace>();

        foreach (var v in data.vertices)
        {
            if (trianglesToDelete.Contains(v.edge.face))
            {
                continue;
            }

            var v1 = v.position;

            if (v1.Equals(superTriangle.v1) || v1.Equals(superTriangle.v2) || v1.Equals(superTriangle.v3))
            {
                trianglesToDelete.Add(v.edge.face);
            }
        }

        foreach (var face in trianglesToDelete)
        {
            DeleteTriangleFace(face, data, shouldSetOppositeNull: true);
        }
    }


    static void CreateNewFace(HalfEdge eOld, Vector2 point, HalfEdgeData data)
    {
        // fix this face
        var vOrigin = new HalfEdgeVertex(eOld.v.position);
        var vNext = new HalfEdgeVertex(eOld.nextEdge.v.position);
        var vSplit = new HalfEdgeVertex(point);

        HalfEdge e1 = new HalfEdge(vOrigin);
        HalfEdge e2 = new HalfEdge(vNext);
        HalfEdge e3 = new HalfEdge(vSplit);
        // create Connection
        // Update new Edge connection.
        // But if opposite edge needs a new reference to this edge if it's not a border
        e1.oppositeEdge = eOld.oppositeEdge;
        if (e1.oppositeEdge != null)
        {
            eOld.oppositeEdge.oppositeEdge = e1;
        }

        // Creating connection each other
        e1.nextEdge = e2;
        e1.prevEdge = e3;

        e2.nextEdge = e3;
        e2.prevEdge = e1;

        e3.nextEdge = e1;
        e3.prevEdge = e2;
        // Update face
        // add Edge to vertex
        vSplit.edge = e3;
        vNext.edge = e2;
        vOrigin.edge = e1;


        HalfEdgeFace f = new HalfEdgeFace(e1);
        e1.face = e2.face = e3.face = f;

        data.faces.Add(f);

        data.edges.Add(e1);
        data.edges.Add(e2);
        data.edges.Add(e3);

        data.vertices.Add(vOrigin);
        data.vertices.Add(vNext);
        data.vertices.Add(vSplit);

    }

    public static void DeleteTriangleFace(HalfEdgeFace face, HalfEdgeData data, bool shouldSetOppositeNull)
    {
        var e1 = face.edge;
        var e2 = e1.nextEdge;
        var e3 = e2.nextEdge;

        if (shouldSetOppositeNull)
        {
            if (e1.oppositeEdge != null)
            {
                e1.oppositeEdge.oppositeEdge = null;
            }
            if (e2.oppositeEdge != null)
            {
                e2.oppositeEdge.oppositeEdge = null;
            }
            if (e3.oppositeEdge != null)
            {
                e3.oppositeEdge.oppositeEdge = null;
            }
        }

        data.faces.Remove(face);

        data.edges.Remove(e1);
        data.edges.Remove(e2);
        data.edges.Remove(e3);

        data.vertices.Remove(e1.v);
        data.vertices.Remove(e2.v);
        data.vertices.Remove(e3.v);
    }
    static bool isEdgeSharedByOtherTriangles(HalfEdge edge,List<HalfEdgeFace> badTriangles)
    {
        foreach(var triangle in badTriangles)
        {
            if(isEdgeOpposite(edge,triangle.edge))
            {
                return true;
            }
            if(isEdgeOpposite(edge, triangle.edge.nextEdge))
            {
                return true;
            }
            if(isEdgeOpposite(edge, triangle.edge.prevEdge))
            {
                return true;
            }
        }
        return false;
    }

    static bool isEdgeOpposite(HalfEdge edge,HalfEdge otherEdge)
    {
        return edge.v.position.Equals(otherEdge.nextEdge.v.position) && edge.nextEdge.v.position.Equals(otherEdge.v.position);
    }

    public static bool isPointInsideCircumCircle(HalfEdgeFace face,Vector2 point)
    {
        float dist = Vector2.Distance(point, face.circumCenter);

        if(dist < face.circumRadius)
        {
            return true;
        }
        return false;
    }


    /// <summary>
    /// IsQuadrilateralConvex? from 
    /// https://www.newcastle.edu.au/__data/assets/pdf_file/0019/22519/23_A-fast-algortithm-for-generating-constrained-Delaunay-triangulations.pdf
    /// 
    /// 'stable' mean it's numerically stable. get more info from the above link 
    /// 
    /// v1,v2,v3 is counter-clockwise
    /// v1,v2 is edge that we want to flip.
    /// so, 
    /// </summary>
    /// <param name="flipEdgeV1"></param>
    /// <param name="flipEdgeV2"></param>
    /// <param name="v3"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static bool ShouldFlipEdgesStable(Vector2 flipEdgeV1, Vector2 flipEdgeV2, Vector2 v3, Vector2 point)
    {
        float x13 = flipEdgeV1.x - v3.x;
        float x23 = flipEdgeV2.x - v3.x;
        float x1p = flipEdgeV1.x - point.x;
        float x2p = flipEdgeV2.x - point.x;

        float y13 = flipEdgeV1.y - v3.y;
        float y23 = flipEdgeV2.y - v3.y;
        float y1p = flipEdgeV1.y - point.y;
        float y2p = flipEdgeV2.y - point.y;

        float cosA = x13 * x23 + y13 * y23;
        float cosB = x2p * x1p + y2p * y1p;

        if (cosA >= 0f && cosB >= 0f)
        {
            return false;
        }
        if (cosA < 0f && cosB < 0)
        {
            return true;
        }

        float sinAB = (x13 * y23 - x23 * y13) * cosB + (x2p * y1p - x1p * y2p) * cosA;

        if (sinAB < 0)
        {
            return true;
        }

        return false;
    }

    static List<Vector2> NormalizePoints(List<Vector2> points, Bounds bound)
    {
        List<Vector2> result = new List<Vector2>(points.Count);
        float dMax = CalculateDMax(bound);

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];

            float x = (p.x - bound.min.x) / dMax;
            float y = (p.y - bound.min.y) / dMax;

            result.Add(new Vector2(x, y));
        }
        return result;
    }

    static void UnNormalizePoints(ref HalfEdgeData data, Bounds bound)
    {
        float dMax = CalculateDMax(bound);
        foreach (var v in data.vertices)
        {
            float x = (v.position.x * dMax) + bound.min.x;
            float y = (v.position.y * dMax) + bound.min.y;
            v.position = new Vector2(x, y);
        }
    }
    static float EPSILON = 0.00001f;

    /// <summary>
    /// if a is start, b is end line
    ///  negative = point at South and West /
    ///  positive = point at North and East 
    ///  
    ///        +                        -
    ///  a ----------> b         a <--------- b
    ///        -                        +
    ///        
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static int IsPointAtRightOfLine(Vector3 a, Vector3 b, Vector3 point)
    {


        var relationValue = MathUtility.GetPointInRelationToVector(a, b, point);
        if (relationValue > EPSILON)
        {
            return 1;
        }
        else if (relationValue < -EPSILON)
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/
    /// </summary>
    /// <param name="p1v1"></param>
    /// <param name="p1v2"></param>
    /// <param name="p2v1"></param>
    /// <param name="p2v2"></param>
    /// <param name="includeEndPoints"></param>
    /// <returns></returns>
    public static bool AreLineIntersecting(Vector2 p1v1, Vector2 p1v2, Vector2 p2v1, Vector2 p2v2, bool includeEndPoints)
    {
        bool result = false;
        float denominator = (p2v2.y - p2v1.y) * (p1v2.x - p1v1.x) - (p2v2.x - p2v1.x) * (p1v2.y - p1v1.y);

        if (denominator != 0f)
        {
            float a = ((p2v2.x - p2v1.x) * (p1v1.y - p2v1.y) - (p2v2.y - p2v1.y) * (p1v1.x - p2v1.x)) / denominator;
            float b = ((p1v2.x - p1v1.x) * (p1v1.y - p2v1.y) - (p1v2.y - p1v1.y) * (p1v1.x - p2v1.x)) / denominator;

            if (includeEndPoints)
            {
                if (a >= 0f && a <= 1f && b >= 0f && b <= 1f)
                {
                    result = true;
                }
            }
            else
            {
                if (a > 0f && a < 1f && b > 0f && b < 1f)
                {
                    result = true;
                }
            }
        }
        return result;
    }

    static float CalculateDMax(Bounds bound)
    {
        float dX = bound.max.x - bound.min.x;
        float dY = bound.max.y - bound.min.y;
        float dMax = Mathf.Max(dX, dY);
        return dMax;
    }

    static Bounds SetBoundary(List<Vector2> points)
    {
        Vector2 newMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 newMax = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < points.Count; ++i)
        {
            if (points[i].x > newMax.x)
            {
                newMax.x = points[i].x;
            }

            if (points[i].y > newMax.y)
            {
                newMax.y = points[i].y;
            }

            if (points[i].x < newMin.x)
            {
                newMin.x = points[i].x;
            }

            if (points[i].y < newMin.y)
            {
                newMin.y = points[i].y;
            }
        }

        Vector2 size = new Vector2(Mathf.Abs(newMax.x - newMin.x), Mathf.Abs(newMax.y - newMin.y));

        return new Bounds(newMin + (size * 0.5f), size);
    }

    static Mesh Triangle2Mesh(HashSet<Triangle> triangles)
    {
        if (triangles == null)
        {
            return null;
        }

        Vector3[] triVertices = new Vector3[triangles.Count * 3];
        int[] triOrder = new int[triangles.Count * 3];
        int i = 0;
        foreach (var tri in triangles)
        {
            int triIndex = i * 3;
            int i1 = triIndex;
            int i2 = triIndex + 1;
            int i3 = triIndex + 2;

            // Debug.Log($"v1 : {tri.v1} , v2 : {tri.v2} , v3 : {tri.v3}");

            triVertices[i1] = tri.v1;
            triVertices[i2] = tri.v2;
            triVertices[i3] = tri.v3;

            triOrder[i1] = i1;
            triOrder[i2] = i2;
            triOrder[i3] = i3;
            i++;

        }

        Mesh mesh = new Mesh();

        mesh.vertices = triVertices;
        mesh.normals = triVertices;
        mesh.triangles = triOrder;

        return mesh;

    }
}
