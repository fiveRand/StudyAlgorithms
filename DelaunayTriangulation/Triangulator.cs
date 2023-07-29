using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyTriangulation;
using MyDataStructure;
# if UNITY_EDITOR
using UnityEditor;
#endif
public class Triangulator : MonoBehaviour
{
    public Vector2 gridSize = new Vector2(50, 50);
    public float radius = 4;
    public List<Vector2> points = new List<Vector2>();
    public List<Vector2> constraints = new List<Vector2>();
    public HalfEdgeData data;
    public HalfEdge curEdge;
    public HalfEdgeFace curFace;
    public List<HalfEdgeFace> faces = new List<HalfEdgeFace>();
    public List<HalfEdge> halfedges = new List<HalfEdge>();
    public List<HalfEdgeVertex> vertices = new List<HalfEdgeVertex>();


    Mesh mesh;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireMesh(mesh);
        foreach (var point in points)
        {
            Gizmos.DrawWireSphere(point, 0.5f);
        }


        Gizmos.color = Color.red;
        if (constraints.Count > 0)
        {
            foreach (var constraint in constraints)
            {
                Gizmos.DrawWireSphere(constraint, 0.5f);
            }
        }
        Gizmos.color = Color.blue;
        if (curFace != null)
        {
            var e = curFace.edge;
            Gizmos.DrawLine(e.v.position, e.nextEdge.v.position);
            Gizmos.DrawLine(e.nextEdge.v.position, e.prevEdge.v.position);
            Gizmos.DrawLine(e.prevEdge.v.position, e.v.position);
        }

        Gizmos.color = Color.black;
        /*
        if(halfedges.Count > 0)
        {
            foreach(var edge in halfedges)
            {
                Gizmos.DrawLine(edge.v.position, edge.nextEdge.v.position);
            }
        }
        */
        
        if(faces != null)
        {
            foreach(var face in faces)
            {
                var e = face.edge;
                Gizmos.DrawLine(e.v.position, e.nextEdge.v.position);
                Gizmos.DrawLine(e.nextEdge.v.position, e.prevEdge.v.position);
                Gizmos.DrawLine(e.prevEdge.v.position, e.v.position);
            }
        }
        if(vertices.Count > 0)
        {
            foreach(var vertex in vertices)
            {
                Gizmos.DrawWireSphere(vertex.position, 0.5f);
            }
        }
        

        Gizmos.color = Color.cyan;
        if (curEdge != null)
        {
            Gizmos.DrawWireSphere(curEdge.v.position, 0.5f);
            Gizmos.DrawLine(curEdge.v.position, curEdge.nextEdge.v.position);
        }
    }

    private void Update() 
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            PrevEdge();
        }
        if(Input.GetKeyDown(KeyCode.W))
        {
            TwinEdge();
        }
        if(Input.GetKeyDown(KeyCode.D))
        {
            NextEdge();
        }
    }

    void Trangulate()
    {
        mesh = BoyerWatson.GenerateDelaunayMesh(points,constraints,out data);
        // curEdge = data.edges[0];
        faces = new List<HalfEdgeFace>();
        halfedges = new List<HalfEdge>();
        vertices = new List<HalfEdgeVertex>();
    }

    void Test()
    {
        
        faces = BoyerWatson.GetTrianglesInsideOfConstraint(data, constraints,out halfedges);
        faces = BoyerWatson.FloodFillInsideConstraint(data, faces,halfedges);
        //vertices = data.vertices;
    }
    void NextEdge()
    {
        curEdge = curEdge.nextEdge;
    }

    void PrevEdge()
    {
        curEdge = curEdge.prevEdge;
    }

    void TwinEdge()
    {
        curEdge = curEdge.oppositeEdge;
    }


    void CreateRandom()
    {
        points = PoissonDiscSamping.GeneratePoint(transform.position, gridSize, radius);
    }

    private List<Vector2> ExtractPointsFromCollider(PolygonCollider2D collider)
    {
        List<Vector2> result = new List<Vector2>();
        int pathCount = collider.pathCount;

        for (int i = 0; i < pathCount; ++i)
        {
            List<Vector2> pathPoints = new List<Vector2>();
            collider.GetPath(i, pathPoints);
            result.AddRange(pathPoints);
        }
        return result;
    }

    


#if UNITY_EDITOR

    [CustomEditor(typeof(Triangulator))]
    public class TriangulationEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var script = (Triangulator)target;
            if (GUILayout.Button("CreateRandom"))
            {
                script.CreateRandom();
            }
            if (GUILayout.Button("Triangulate"))
            {
                script.Trangulate();
            }

            if (GUILayout.Button("Next"))
            {
                script.NextEdge();
            }
            if (GUILayout.Button("Prev"))
            {
                script.PrevEdge();
            }
            if (GUILayout.Button("GetTwin"))
            {
                script.TwinEdge();
            }

            if (GUILayout.Button("Test"))
            {
                script.Test();
            }



        }
    }

#endif
}
