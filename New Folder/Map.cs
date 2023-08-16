using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MyDataStructure;
# if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyPathfinding
{
    public class Map : MonoBehaviour
    {
        public int nodeCount;

        public List<Vector2> points = new List<Vector2>();
        public HalfEdgeData data;
        Mesh mesh;
        public List<Transform> constraintParent = new List<Transform>();
        public List<Node> nodes = new List<Node>();

        public List<Vector3> nodeCenterPoints = new List<Vector3>();
        public List<Vector2> paths = new List<Vector2>();
        public List<HalfEdge> edges = new List<HalfEdge>();

        public Transform start;
        public Transform end;
        public Transform point;
        Node test;


        private void OnDrawGizmos()
        {

            Gizmos.color = Color.white;
            Gizmos.DrawWireMesh(mesh);
            foreach (var point in points)
            {
                Gizmos.DrawWireSphere(point, 0.5f);
            }

            Gizmos.color = Color.red;
            if (constraintParent.Count > 0)
            {
                for (int i = 0; i < constraintParent.Count; i++)
                {
                    var parent = constraintParent[i];
                    var col = parent.GetComponentInChildren<PolygonCollider2D>();
                    if (col != null)
                    {
                        List<Vector2> constraint = ExtractPointsFromCollider(col);

                        for (int j = 0; j < constraint.Count; j++)
                        {
                            var child = constraint[j];
                            Gizmos.DrawWireSphere(child, 0.5f);
                        }
                    }
                }
            }
            Gizmos.color = Color.cyan;

            if(paths.Count > 0)
            {
                foreach(var path in paths)
                {
                    Gizmos.DrawWireSphere(path, 0.5f);
                }
            }

            if (test != null)
            {
                var pos = test.face.centerPosition;
                Gizmos.DrawWireSphere(pos, 0.5f);
            }
            if (start != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(start.position, 0.5f);
            }
            if (end != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(end.position, 0.5f);
            }
            if (point != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(point.position, 0.5f);
            }

            if (edges.Count > 0)
            {
                Gizmos.color = Color.magenta;
                foreach (var edge in edges)
                {
                    Gizmos.DrawLine(edge.v.position, edge.nextEdge.v.position);
                }
            }
        }
        public void Test()
        {
            float a = Mathf.Pow(81, 1f / 4f);
            Debug.Log(a);
        }

        public List<Vector2> GetPath(Vector2 start,Vector2 end,float radius = 0)
        {
            var edges = AStar.StartPathfinding(this, start, end);
            List<Vector2> path = SimpleStupidFunnel.Algorithm(start, end, edges,radius);
            return path;
        }
        public void DoPathfind()
        {
            nodeCenterPoints.Clear();
            this.paths.Clear();

            var paths = AStar.StartPathfinding(this, start.position, end.position);

            SimpleStupidFunnel.Algorithm(start.position, end.position, paths);
            if(paths != null)
            {
                this.edges = paths;
                // this.paths = paths;

                /*
                foreach(Node node in paths)
                {
                    nodeCenterPoints.Add(node.face.centerPosition);
                }
                */

            }
        }



        public List<Node> GetNeighbor(Node node)
        {
            List<Node> result = new List<Node>(3);
            var curEdge = node.face.edge;

            if(curEdge.oppositeEdge != null)
            {
                Node neighbor = GetNode(curEdge.oppositeEdge.face.centerPosition);
                result.Add(neighbor);
            }
            curEdge = curEdge.nextEdge;
            if (curEdge.oppositeEdge != null)
            {
                Node neighbor = GetNode(curEdge.oppositeEdge.face.centerPosition);
                result.Add(neighbor);
            }
            curEdge = curEdge.nextEdge;
            if (curEdge.oppositeEdge != null)
            {
                Node neighbor = GetNode(curEdge.oppositeEdge.face.centerPosition);
                result.Add(neighbor);
            }

            return result;
        }

        public Node GetNode(Vector3 position)
        {
            foreach(var node in nodes)
            {
                if(node.face.IsInside(position))
                {
                    return node;
                }
            }
            return null;
        }

        public void GenerateDelaunay()
        {
            List<List<Vector2>> holesPoint = new List<List<Vector2>>();

            for (int i = 0; i < constraintParent.Count; i++)
            {
                var parent = constraintParent[i];
                var col = parent.GetComponentInChildren<PolygonCollider2D>();
                List<Vector2> constraint = ExtractPointsFromCollider(col);
                holesPoint.Add(constraint);

            }


            mesh = BoyerWatson.GenerateDelaunayMesh(points, holesPoint, out data);



            nodeCount = data.faces.Count;
            nodes = new List<Node>(data.faces.Count);
            foreach (var face in data.faces)
            {
                Node newNode = new Node(face);
                nodes.Add(newNode);
            }

            foreach(var node in nodes)
            {
                var neighbors = GetNeighbor(node);
                node.neighbors = neighbors;
            }
        }


        private List<Vector2> ExtractPointsFromCollider(PolygonCollider2D collider)
        {
            List<Vector2> result = new List<Vector2>();

            for (int i = 0; i < collider.pathCount; ++i)
            {
                List<Vector2> pathPoints = new List<Vector2>();
                collider.GetPath(i, pathPoints);
                for (int j = 0; j < pathPoints.Count; j++)
                {
                    pathPoints[j] = pathPoints[j] + (Vector2)collider.transform.position;
                }
                result.AddRange(pathPoints);
            }
            return result;
        }

        public void SortAllParentClockWise()
        {
            for (int i = 0; i < constraintParent.Count; i++)
            {
                Transform parent = constraintParent[i];
                var col = parent.GetComponentInChildren<PolygonCollider2D>();
                Vector2[] paths = col.GetPath(0);

                var orderedPath = SortParentClockwise(paths);
                col.SetPath(0, orderedPath);
            }
        }
        Vector2[] SortParentClockwise(Vector2[] paths)
        {
            var points = paths;
            var midX = points.Average(t => t.x);
            var midY = points.Average(t => t.y);
            Vector2[] orderedPoints = paths.OrderBy(t => -Mathf.Atan2(midY - t.y, midX - t.x)).ToArray<Vector2>();
            return orderedPoints;
        }
    }



#if UNITY_EDITOR

    [CustomEditor(typeof(Map))]
    public class MapEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var script = (Map)target;

            if (GUILayout.Button("SortAllParentClockWise"))
            {
                script.SortAllParentClockWise();
            }

            if (GUILayout.Button("Triangulate"))
            {
                script.GenerateDelaunay();
            }

            if (GUILayout.Button("PathFind"))
            {
                script.DoPathfind();
            }

            if (GUILayout.Button("Test"))
            {
                script.Test();
            }



        }
    }

}
#endif