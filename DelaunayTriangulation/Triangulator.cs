using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyTriangulation;
# if UNITY_EDITOR
using UnityEditor;
#endif
public class Triangulator : MonoBehaviour
{
    public Vector2 gridSize = new Vector2(50, 50);
    public float radius = 4;
    public List<Vector2> points = new List<Vector2>();
    public List<Vector2> constraints = new List<Vector2>();


    HashSet<HaraborDataStructure.Triangle> triangles = new HashSet<HaraborDataStructure.Triangle>();
    Mesh mesh;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;


        Gizmos.DrawWireMesh(mesh);

        Gizmos.color = Color.cyan;
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
    }

    void Trangulate()
    {
        mesh = BoyerWatson.GenerateDelaunayMesh(points);


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

            if (GUILayout.Button("Triangulate"))
            {
                script.Trangulate();
            }
            if (GUILayout.Button("CreateRandom"))
            {
                script.CreateRandom();
            }

        }
    }

#endif
}
