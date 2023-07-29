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
        UnNormalizePoints(triangulationData, bound);
        HashSet<Triangle> results = triangulationData.HalfEdge2Triangle();
        return results;
    }

    public static Mesh GenerateDelaunayMesh(List<Vector2> points, List<Vector2> constraints,out HalfEdgeData data)
    {
        var triangles = GenerateDelaunayTriangle(points, constraints,out data);
        var mesh = Triangle2Mesh(triangles);
        return mesh;
    }


    static HashSet<Triangle> GenerateDelaunayTriangle(List<Vector2> startPoints, List<Vector2> constraints,out HalfEdgeData data)
    {
        var bound = SetBoundary(startPoints);
        var normalizedPoint = NormalizePoints(startPoints, bound);
        var constraintedNormalizedPoint = NormalizePoints(constraints, bound);
        normalizedPoint.AddRange(constraintedNormalizedPoint);

        Triangle superTriangle = new Triangle(new Vector2(-100, -100), new Vector2(0, 100), new Vector2(100, -100));
        List<Triangle> tempTriangles = new List<Triangle> { superTriangle };
        data = new HalfEdgeData();
        data.Update(tempTriangles);

        for (int i = 0; i < normalizedPoint.Count; i++)
        {
            var point = normalizedPoint[i];
            OnInsertPoint(point, data);
        }
        OnConstraints(data, constraintedNormalizedPoint);

        RemoveSuperTriangle(superTriangle, data);
        UnNormalizePoints(data, bound);
        HashSet<Triangle> results = data.HalfEdge2Triangle();
        return results;
    }

    static void OnConstraints(HalfEdgeData data, List<Vector2> constraints)
    {
        for (int i = 0; i < constraints.Count; i++)
        {

            var edgeStart = constraints[i];
            var edgeEnd = (i == constraints.Count - 1) ? constraints[0] : constraints[i + 1];

            var face = FindpointTriangle(data.faces, edgeStart);

            var intersectingEdges = FindIntersectingEdges(data.faces,face,edgeStart,edgeEnd);

            var FlippedEdges = RemoveIntersectingEdges(data,intersectingEdges, edgeStart, edgeEnd);
            RestoreDelaunayTriangle(data,FlippedEdges, edgeStart, edgeEnd);
        }
        RemoveConstraint(data,constraints);
    }

    static void RemoveConstraint(HalfEdgeData data,List<Vector2> constraints)
    {
        var outLineFaces = GetTrianglesInsideOfConstraint(data, constraints,out List<HalfEdge> outlineEdges);

        var willBeDeletedFaces = FloodFillInsideConstraint(data, outLineFaces,outlineEdges);

        foreach(var face in willBeDeletedFaces)
        {
            DeleteTriangleFace(face, data, true);
        }

    }

    public static List<HalfEdgeFace> FloodFillInsideConstraint(HalfEdgeData data,List<HalfEdgeFace> outLineFaces,List<HalfEdge> outlineEdge)
    {
        Queue<HalfEdgeFace> searchQ = new Queue<HalfEdgeFace>(outLineFaces);
        List<HalfEdgeFace> result = new List<HalfEdgeFace>(outLineFaces);


        while(searchQ.Count > 0)
        {
            HalfEdgeFace curFace = searchQ.Dequeue();
            HalfEdge curEdge = curFace.edge;
            HalfEdge nextEdge = curEdge.nextEdge;
            HalfEdge prevEdge = curEdge.prevEdge;




            if (!outlineEdge.Contains(curEdge) && !result.Contains(curEdge.oppositeEdge.face))
            {
                searchQ.Enqueue(curEdge.oppositeEdge.face);
                result.Add(curEdge.oppositeEdge.face);
            }
            if (!outlineEdge.Contains(nextEdge) && !result.Contains(nextEdge.oppositeEdge.face))
            {
                searchQ.Enqueue(nextEdge.oppositeEdge.face);
                result.Add(nextEdge.oppositeEdge.face);
            }
            if (!outlineEdge.Contains(prevEdge) && !result.Contains(prevEdge.oppositeEdge.face))
            {
                searchQ.Enqueue(prevEdge.oppositeEdge.face);
                result.Add(prevEdge.oppositeEdge.face);
            }
        }
        return result;
    }

    public static List<HalfEdgeFace> GetTrianglesInsideOfConstraint(HalfEdgeData data,List<Vector2> constraints,out List<HalfEdge> outlineEdges)
    {
        List<HalfEdgeFace> result = new List<HalfEdgeFace>(constraints.Count);
        outlineEdges = new List<HalfEdge>();
        int index = 0;
        var edgeStart = constraints[index];
        var edgeEnd = constraints[index+1];
        HalfEdge curEdge = null;
        HalfEdgeFace curFace = null;

        foreach(var vertex in data.vertices)
        {
            if(vertex.position == edgeStart)
            {
                curEdge = vertex.edge;
                curFace = curEdge.face;
                break;
            }
        }

        while(true)
        {
            if(curEdge.v.position == edgeStart)
            {
                break;
            }
            curEdge = curEdge.nextEdge;
        }

        int safety = 0;
        
        while(true)
        {
            safety++;

            if(safety > 1000)
            {
                Debug.Log("Hit Infinity");
                break;
            }

            if(isSame(curEdge.v.position,edgeStart) && isSame(curEdge.nextEdge.v.position,edgeEnd))
            {
                outlineEdges.Add(curEdge);
                curEdge = curEdge.nextEdge;
                break;
            }
            else if(isSame(curEdge.nextEdge.v.position, edgeStart) && isSame(curEdge.prevEdge.v.position, edgeEnd))
            {
                outlineEdges.Add(curEdge);
                curEdge = curEdge.prevEdge;
                break;
            }
            else if(isSame(curEdge.prevEdge.v.position, edgeStart) && isSame(curEdge.v.position, edgeEnd))
            {
                outlineEdges.Add(curEdge);
                break;
            }
            curEdge =curEdge.prevEdge.oppositeEdge;
            curFace = curEdge.face;

        }

        result.Add(curEdge.face);
        index++;
        safety = 0;
        
        while(index != constraints.Count)
        {
            edgeStart = constraints[index];
            edgeEnd = (index == constraints.Count - 1) ? constraints[0] : constraints[index + 1];
            safety++;

            if (safety > 1000)
            {
                Debug.Log("Hit Infinity");
                break;
            }

            var a = curEdge;
            var b = a.nextEdge;
            var c = a.prevEdge;

            if (isSame(a.v.position, edgeStart) && isSame(b.v.position, edgeEnd))
            {
                if (!result.Contains(curFace))
                {
                    result.Add(curFace);
                }
                outlineEdges.Add(curEdge);
                curEdge = b;
                index++;
                continue;
            }
            if (isSame(b.v.position, edgeStart) && isSame(c.v.position, edgeEnd))
            {
                if (!result.Contains(curFace))
                {
                    result.Add(curFace);
                }
                outlineEdges.Add(curEdge);
                curEdge = c;

                index++;
                continue;
            }
            if (isSame(c.v.position, edgeStart) && isSame(a.v.position, edgeEnd))
            {
                if (!result.Contains(curFace))
                {
                    result.Add(curFace);
                }
                outlineEdges.Add(curEdge);
                curEdge = a;

                index++;
                continue;
            }
            curEdge = curEdge.oppositeEdge.nextEdge;
            curFace = curEdge.face;


        }
        
        
        
        
        return result;

    }


    static bool isSame(Vector2 aVec,Vector2 bVec,float toleranceRange = 0.00001f)
    {
        return Vector3.SqrMagnitude(aVec - bVec) < toleranceRange;
    }

    static void FindConstraintEdges(HalfEdgeData data,List<Vector2> constraints)
    {

    }
    static void RestoreDelaunayTriangle(HalfEdgeData data,List<HalfEdge> newEdges,Vector2 edgeStart,Vector2 edgeEnd)
    {
        int safety = 0;

        while(newEdges.Count > 0)
        {
            safety++;
            if(safety > 100000)
            {
                Debug.LogError("Hit Infinity while flipping restoring triangle");
                break;
            }

            bool hasFlipped = false;

            for (int i = 0; i < newEdges.Count; i++)
            {
                var curEdge = newEdges[i];

                var a = curEdge.v.position;
                var b = curEdge.nextEdge.v.position;
                var c = curEdge.prevEdge.v.position;
                var d = curEdge.oppositeEdge.prevEdge.v.position;

                if ((a.Equals(edgeStart) && b.Equals(edgeEnd)) || (b.Equals(edgeStart) && a.Equals(edgeEnd)))
                {
                    continue;
                }

                if (ShouldFlipEdge(curEdge.face,d))
                {
                    hasFlipped = true;
                    TestFlipEdge(curEdge,ref data);
                }
            }

            if(!hasFlipped)
            {
                break;
            }
        }
    }


    public static Queue<HalfEdge> FindIntersectingEdges(List<HalfEdgeFace> faces,HalfEdgeFace startTriangle ,Vector2 edgeStart,Vector2 edgeEnd)
    {
        Queue<HalfEdge> result = new Queue<HalfEdge>(faces.Count);
        int safety = 0;
        var curEdge = startTriangle.edge;
        bool isLeft = false;

        while(!curEdge.v.position.Equals(edgeStart))
        {
            curEdge = curEdge.nextEdge;
        }

        // 해당 버텍스에 충돌하면 선분이 나올때까지 버텍스를 지점으로 우측방향으로 이동 
        // 거대한 삼각형 덕에 충돌처리가 잘못될리가 없지만 직접 만든 Mesh로서는 에러가 나올 수 있다
        // 그래서 우측이 비워져 있다면 좌측방향으로도 살펴보게 만든다
        while(true)
        {
            safety++;
            if (safety > 10)
            {
                Debug.Log("Endless loop while finding triangle to walk");
                break;
            }

            var e1 = curEdge;
            var e2 = curEdge.nextEdge;
            var e3 = curEdge.prevEdge;
            if (AreLineIntersecting(curEdge.v.position, curEdge.nextEdge.v.position, edgeStart, edgeEnd, false))
            {
                curEdge = curEdge.oppositeEdge;
                break;
            }
            else if (AreLineIntersecting(curEdge.nextEdge.v.position, curEdge.prevEdge.v.position, edgeStart, edgeEnd, false))
            {
                curEdge = curEdge.nextEdge.oppositeEdge;
                break;
            }
            else if (AreLineIntersecting(curEdge.prevEdge.v.position, curEdge.v.position, edgeStart, edgeEnd, false))
            {
                curEdge = curEdge.prevEdge.oppositeEdge;
                break;
            }
            else
            {
                
                if (isLeft && curEdge.oppositeEdge.nextEdge == null)
                {

                    Debug.LogError("this vertex is empty");
                    return null;
                }
                if(curEdge.prevEdge.oppositeEdge == null)
                {
                    isLeft = true;
                }
                
                curEdge = (isLeft) ? curEdge.oppositeEdge.nextEdge : curEdge.prevEdge.oppositeEdge;


            }

        }

        result.Enqueue(curEdge);

        safety = 0;
        
        while (true)
        {
            safety += 1;

            if (safety > 100)
            {
                Debug.Log("Endless loop while finding triangle to walk");
                break;
            }

            var nextEdge = curEdge.nextEdge;
            var prevEdge = curEdge.prevEdge;

            if(AreLineIntersecting(nextEdge.v.position,nextEdge.nextEdge.v.position, edgeStart, edgeEnd, false))
            {
                curEdge = nextEdge.oppositeEdge;
            }
            else if(AreLineIntersecting(prevEdge.v.position, prevEdge.nextEdge.v.position, edgeStart, edgeEnd, false))
            {
                curEdge = prevEdge.oppositeEdge;
            }
            else
            {
                break;
            }
            result.Enqueue(curEdge);
        }
        


        return result;
    }

    public static List<HalfEdge> RemoveIntersectingEdges(HalfEdgeData data,Queue<HalfEdge> intersectingEdges,Vector2 edgeStart, Vector2 edgeEnd)
    {
        List<HalfEdge> result = new List<HalfEdge>(intersectingEdges.Count);
        int safety = 0;

        while(intersectingEdges.Count > 0)
        {
            
            safety++;
            if(safety > 10000)
            {
                Debug.LogError("Hit infinity while removing intersecting edges");
                break;
            }
            var curEdge = intersectingEdges.Dequeue();
            var a = curEdge.v.position;
            var b = curEdge.nextEdge.v.position;
            var c = curEdge.prevEdge.v.position;
            var d = curEdge.oppositeEdge.prevEdge.v.position;

            if(!IsQuadrilateralConvex(a,b,c,d))
            {
                intersectingEdges.Enqueue(curEdge);
                continue;
            }
            else
            {
                TestFlipEdge(curEdge,ref data);

                a = curEdge.v.position;
                b = curEdge.nextEdge.v.position;

                if(AreLineIntersecting(edgeStart,edgeEnd,a,b,false))
                {
                    intersectingEdges.Enqueue(curEdge);
                }
                else
                {
                    result.Add(curEdge);
                }
            }
        }
        return result;
    }

    public static void TestFlipEdge(HalfEdge e,ref HalfEdgeData data)
    {

        var e1 = e;
        var e2 = e.nextEdge;
        var e3 = e.prevEdge;

        HalfEdge e4 = e.oppositeEdge;
        var e5 = e4.nextEdge;
        var e6 = e4.prevEdge;

        // DeleteTriangleFace(e.face, data, false);
        // DeleteTriangleFace(e4.face, data, false);

        Vector2 aPos = e.v.position;
        Vector2 bPos = e.nextEdge.v.position;
        Vector2 cPos = e.prevEdge.v.position;
        Vector2 dPos = e6.v.position;

        var aOld = e1.v;
        var aOppositeOld = e5.v;
        var bOld = e2.v;
        var bOppositeOld = e4.v;
        var cOld = e3.v;
        var dOld = e6.v;

        var d = dOld;
        var c = cOld;
        var a = aOld;

        var dOpposite = aOppositeOld;
        dOpposite.position = dPos;
        var cOpposite = bOppositeOld;
        bOppositeOld.position = cPos;
        var b = bOld;

        // update half-edge connections.

        var ef = e1.face;
        var of = e4.face;

        e1.face = ef;
        e3.face = ef;
        e5.face = ef;



        e2.face = of;
        e4.face = of;
        e6.face = of;

        ef.edge = e1;
        of.edge = e4;

        e1.nextEdge = e3;
        e1.prevEdge = e5;

        e2.nextEdge = e4;
        e2.prevEdge = e6;

        e3.nextEdge = e5;
        e3.prevEdge = e1;

        e4.nextEdge = e6;
        e4.prevEdge = e2;

        e5.nextEdge = e1;
        e5.prevEdge = e3;

        e6.nextEdge = e2;
        e6.prevEdge = e4;

        // update half edge vertex

        e1.v = d;
        e3.v = c;
        e5.v = a;
        e2.v = b;
        e4.v = cOpposite;
        e6.v = dOpposite;

        e1.v.edge = e3;
        e3.v.edge = e5;
        e5.v.edge = e1;

        e2.v.edge = e4;
        e4.v.edge = e6;
        e6.v.edge = e2;


        d.edge = e1;
        c.edge = e3;
        a.edge = e5;

        cOpposite.edge = e4;
        dOpposite.edge = e6;
        b.edge = e2;

        // update Face
    }

    public static HalfEdgeFace FindpointTriangle(List<HalfEdgeFace> faces, Vector2 point)
    {
        HalfEdgeFace result = null;
        foreach(var face in faces)
        {
            var curEdge = face.edge;
            var nextEdge = curEdge.nextEdge;
            var prevEdge = curEdge.prevEdge;

            if(curEdge.v.position.Equals(point) || nextEdge.v.position.Equals(point) || prevEdge.v.position.Equals(point))
            {
                result = face;
            }
        }
        return result;
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
            DeleteTriangleFace(triangle, triangulationData, false);
        }

        foreach (var halfedge in NotSharedEdges)
        {
            CreateNewFace(halfedge, point, triangulationData);
        }

        foreach (var e in triangulationData.edges)
        {
            if(e.oppositeEdge != null)
            {
                continue;
            }
            var curEdgeVertex = e.v;
            var nextEdgeVertex = e.nextEdge.v;

            foreach (HalfEdge other in triangulationData.edges)
            {
                if (e == other || other.oppositeEdge != null)
                {
                    continue;
                }

                if (curEdgeVertex.position.Equals(other.nextEdge.v.position) && nextEdgeVertex.position.Equals(other.v.position))
                {
                    e.oppositeEdge = other;
                    other.oppositeEdge = e;
                    break;
                }
            }
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

            var vPos = v.position;

            if (vPos.Equals(superTriangle.v1) || vPos.Equals(superTriangle.v2) || vPos.Equals(superTriangle.v3))
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
        var vPrev = new HalfEdgeVertex(point);

        HalfEdge e1 = new HalfEdge(vOrigin);
        HalfEdge e2 = new HalfEdge(vNext);
        HalfEdge e3 = new HalfEdge(vPrev);
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

        HalfEdgeFace f = new HalfEdgeFace(e1);
        e1.face = f;
        e2.face = f;
        e3.face = f;

        vPrev.edge = e1;
        vNext.edge = e3;
        vOrigin.edge = e2;


        data.faces.Add(f);

        data.edges.Add(e1);
        data.edges.Add(e2);
        data.edges.Add(e3);

        data.vertices.Add(vOrigin);
        data.vertices.Add(vNext);
        data.vertices.Add(vPrev);

    }

    static void CreateNewFace(HalfEdgeFace completedFace, HalfEdgeData data)
    {
        var e1 = completedFace.edge;
        var e2 = e1.nextEdge;
        var e3 = e1.prevEdge;

        data.faces.Add(completedFace);

        data.edges.Add(e1);
        data.edges.Add(e2);
        data.edges.Add(e3);

        data.vertices.Add(e1.v);
        data.vertices.Add(e2.v);
        data.vertices.Add(e3.v);

    }

    public static void DeleteTriangleFace(HalfEdgeFace face, HalfEdgeData data, bool shouldSetOppositeNull)
    {
        var e1 = face.edge;
        var e2 = e1.nextEdge;
        var e3 = e1.prevEdge;

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
            var otherEdge = triangle.edge;
            if(isEdgeOpposite(edge,otherEdge))
            {
                return true;
            }
            if(isEdgeOpposite(edge, otherEdge.nextEdge))
            {
                return true;
            }
            if(isEdgeOpposite(edge, otherEdge.prevEdge))
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

    public static bool ShouldFlipEdge(HalfEdgeFace face, Vector2 point)
    {
        bool shouldFlipEdge = false;

        if(isPointInsideCircumCircle(face,point))
        {
            var curEdge = face.edge;
            var a = curEdge.v.position;
            var b = curEdge.nextEdge.v.position;
            var c = curEdge.prevEdge.v.position;
            if(IsQuadrilateralConvex(a,b,c,point))
            {
                shouldFlipEdge = true;
            }
        }
        return shouldFlipEdge;
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

    static void UnNormalizePoints(HalfEdgeData data, Bounds bound)
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
        if (p1v1.Equals(p2v1) || p1v1.Equals(p2v2) || p1v2.Equals(p2v1) || p1v2.Equals(p2v2))
        {
            return false;
        }
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

    public static bool IsQuadrilateralConvex(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        bool isConvex = false;

        bool abc = IsTriangleOrientedClockwise(a, b, c);
        bool abd = IsTriangleOrientedClockwise(a, b, d);
        bool bcd = IsTriangleOrientedClockwise(b, c, d);
        bool cad = IsTriangleOrientedClockwise(c, a, d);

        if (abc && abd && bcd & !cad)
        {
            isConvex = true;
        }
        else if (abc && abd && !bcd & cad)
        {
            isConvex = true;
        }
        else if (abc && !abd && bcd & cad)
        {
            isConvex = true;
        }
        //The opposite sign, which makes everything inverted
        else if (!abc && !abd && !bcd & cad)
        {
            isConvex = true;
        }
        else if (!abc && !abd && bcd & !cad)
        {
            isConvex = true;
        }
        else if (!abc && abd && !bcd & !cad)
        {
            isConvex = true;
        }


        return isConvex;
    }

    public static bool IsTriangleOrientedClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        bool isClockWise = true;

        float determinant = p1.x * p2.y + p3.x * p1.y + p2.x * p3.y - p1.x * p3.y - p3.x * p2.y - p2.x * p1.y;

        if (determinant > 0f)
        {
            isClockWise = false;
        }

        return isClockWise;
    }

}
