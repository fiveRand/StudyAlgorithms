using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyDataStructure
{

    public class Edge
    {
        public Vector2 v1;
        public Vector2 v2;

        public bool isIntersecting = false;

        public Edge(Vector2 v1, Vector2 v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }
    }

    public struct Triangle
    {
        public Vector2 v1;
        public Vector2 v2;
        public Vector2 v3;

        public Triangle(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;

            SetOrientation();
        }
        /// <summary>
        /// Switch clockwise = counter-clockwise 
        /// </summary>
        public void ChangeOrientation()
        {
            Vector2 temp = v1;
            v1 = v2;
            v2 = temp;
        }

        public float MinX()
        {
            return Mathf.Min(v1.x, Mathf.Min(v2.x, v3.x));
        }
        public float MaxX()
        {
            return Mathf.Max(v1.x, Mathf.Max(v2.x, v3.x));
        }
        public float MinY()
        {
            return Mathf.Min(v1.y, Mathf.Min(v2.y, v3.y));
        }
        public float MaxY()
        {
            return Mathf.Max(v1.y, Mathf.Max(v2.y, v3.y));
        }

        public void SetOrientation()
        {
            if (!MathUtility.isTriangleOrientedClockWise(v1, v2, v3))
            {
                ChangeOrientation();
            }
        }


    }

    public class HalfEdge
    {
        //The vertex it points to
        public HalfEdgeVertex v;

        //The face it belongs to
        public HalfEdgeFace face;

        //The next half-edge inside the face (ordered clockwise)
        //The document says counter-clockwise but clockwise is easier because that's how Unity is displaying triangles
        public HalfEdge nextEdge;

        //The opposite half-edge belonging to the neighbor
        public HalfEdge oppositeEdge;

        //(optionally) the previous halfedge in the face
        //If we assume the face is closed, then we could identify this edge by walking forward
        //until we reach it
        public HalfEdge prevEdge;



        public HalfEdge(HalfEdgeVertex v)
        {
            this.v = v;
        }

    }

    public class HalfEdgeFace
    {
        public HalfEdge edge;

        public float circumRadius;
        public Vector2 circumCenter;

        public HalfEdgeFace(HalfEdge edge)
        {
            this.edge = edge;

            Vector2 a = edge.v.position;
            Vector2 b = edge.nextEdge.v.position;
            Vector2 c = edge.prevEdge.v.position;

            LinearEquation lineAB = new LinearEquation(a, b);
            LinearEquation lineBC = new LinearEquation(b, c);
            var perpendicularAB = lineAB.PerpendicularLineAt(Vector2.Lerp(a, b, .5f));
            var perpendicularBC = lineBC.PerpendicularLineAt(Vector2.Lerp(b, c, .5f));

            Vector2 circumCenter = GetCrossingPoint(perpendicularAB, perpendicularBC);

            this.circumRadius = Vector2.Distance(circumCenter, a);
            this.circumCenter = circumCenter;
        }

        static Vector2 GetCrossingPoint(LinearEquation line1, LinearEquation line2)
        {
            float A1 = line1._A;
            float A2 = line2._A;
            float B1 = line1._B;
            float B2 = line2._B;
            float C1 = line1._C;
            float C2 = line2._C;

            //Cramer's rule
            float Determinant = A1 * B2 - A2 * B1;
            float DeterminantX = C1 * B2 - C2 * B1;
            float DeterminantY = A1 * C2 - A2 * C1;

            float x = DeterminantX / Determinant;
            float y = DeterminantY / Determinant;

            return new Vector2(x, y);
        }
    }

    [System.Serializable]
    public class LinearEquation
    {
        public float _A;
        public float _B;
        public float _C;

        public LinearEquation() { }

        //Ax+By=C
        public LinearEquation(Vector2 pointA, Vector2 pointB)
        {
            float deltaX = pointB.x - pointA.x;
            float deltaY = pointB.y - pointA.y;
            _A = deltaY; //y2-y1
            _B = -deltaX; //x1-x2
            _C = _A * pointA.x + _B * pointA.y;
        }

        public LinearEquation PerpendicularLineAt(Vector3 point)
        {
            LinearEquation newLine = new LinearEquation();

            newLine._A = -_B;
            newLine._B = _A;
            newLine._C = newLine._A * point.x + newLine._B * point.y;

            return newLine;
        }
    }

    public class HalfEdgeVertex
    {
        public Vector2 position;

        // 이 edge의 시작점은 이 vertex다.
        public HalfEdge edge;

        public HalfEdgeVertex(Vector2 position_)
        {
            position = position_;
        }
    }

    public class HalfEdgeData
    {
        public List<HalfEdgeVertex> vertices;

        public List<HalfEdgeFace> faces;

        public List<HalfEdge> edges;



        public HalfEdgeData()
        {
            this.vertices = new List<HalfEdgeVertex>();

            this.faces = new List<HalfEdgeFace>();

            this.edges = new List<HalfEdge>();
        }

        public void Update(List<Triangle> triangles)
        {
            foreach (var tri in triangles)
            {
                HalfEdgeVertex v1 = new HalfEdgeVertex(tri.v1);
                HalfEdgeVertex v2 = new HalfEdgeVertex(tri.v2);
                HalfEdgeVertex v3 = new HalfEdgeVertex(tri.v3);

                HalfEdge he1 = new HalfEdge(v1);
                HalfEdge he2 = new HalfEdge(v2);
                HalfEdge he3 = new HalfEdge(v3);

                he1.nextEdge = he2;
                he2.nextEdge = he3;
                he3.nextEdge = he1;

                he1.prevEdge = he3;
                he2.prevEdge = he1;
                he3.prevEdge = he2;

                v1.edge = he2;
                v2.edge = he3;
                v3.edge = he1;

                HalfEdgeFace face = new HalfEdgeFace(he1);

                // he1.face = he2.face = he3.face = face;
                he1.face = face;
                he2.face = face;
                he3.face = face;

                edges.Add(he1);
                edges.Add(he2);
                edges.Add(he3);

                faces.Add(face);

                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);
            }

            SetOpposite();
        }

        public void Update(HashSet<Triangle> triangles)
        {
            List<Triangle> listTriangles = new List<Triangle>(triangles.Count);
            foreach(var triangle in triangles)
            {
                listTriangles.Add(triangle);
            }
            Update(listTriangles);

        }

        public HalfEdgeData(List<Triangle> triangles)
        {
            this.vertices = new List<HalfEdgeVertex>();
            this.faces = new List<HalfEdgeFace>();
            this.edges = new List<HalfEdge>();

            foreach (var tri in triangles)
            {
                HalfEdgeVertex v1 = new HalfEdgeVertex(tri.v1);
                HalfEdgeVertex v2 = new HalfEdgeVertex(tri.v2);
                HalfEdgeVertex v3 = new HalfEdgeVertex(tri.v3);

                HalfEdge he1 = new HalfEdge(v1);
                HalfEdge he2 = new HalfEdge(v2);
                HalfEdge he3 = new HalfEdge(v3);

                he1.nextEdge = he2;
                he2.nextEdge = he3;
                he3.nextEdge = he1;

                he1.prevEdge = he3;
                he2.prevEdge = he1;
                he3.prevEdge = he2;

                v1.edge = he2;
                v2.edge = he3;
                v3.edge = he1;

                HalfEdgeFace face = new HalfEdgeFace(he1);

                // he1.face = he2.face = he3.face = face;
                he1.face = face;
                he2.face = face;
                he3.face = face;

                edges.Add(he1);
                edges.Add(he2);
                edges.Add(he3);

                faces.Add(face);

                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);
            }
            
            foreach (var e in edges)
            {
                var curEdgeVertex = e.v;
                var nextEdgeVertex = e.nextEdge.v;

                foreach (HalfEdge other in edges)
                {
                    if (e == other) // 해당 edge의 opposite edge를 찾는거니 자기 자신을 참조할 필요는 없다
                    {
                        continue;
                    }
                    if (curEdgeVertex.position.Equals(other.nextEdge.v.position) && nextEdgeVertex.position.Equals(other.v.position))
                    {
                        e.oppositeEdge = other;
                        break;
                    }
                }
            }
            

        }
        public void SetOpposite()
        {
            foreach (var e in edges)
            {
                var curEdgeVertex = e.v;
                var nextEdgeVertex = e.nextEdge.v;

                foreach (HalfEdge other in edges)
                {
                    if (e == other) // 해당 edge의 opposite edge를 찾는거니 자기 자신을 참조할 필요는 없다
                    {
                        continue;
                    }
                    if (curEdgeVertex.position.Equals(other.nextEdge.v.position) && nextEdgeVertex.position.Equals(other.v.position))
                    {
                        e.oppositeEdge = other;
                        break;
                    }
                }
            }
        }

        public HashSet<Triangle> HalfEdge2Triangle()
        {
            HashSet<Triangle> triangles = new HashSet<Triangle>();

            foreach (var face in faces)
            {
                var v1 = face.edge.v.position;
                var v2 = face.edge.nextEdge.v.position;
                var v3 = face.edge.prevEdge.v.position;

                Triangle tri = new Triangle(v1, v2, v3);
                triangles.Add(tri);
            }
            return triangles;
        }

        //Get a list with unique edges
        //Currently we have two half-edges for each edge, making it time consuming
        //So this method is not always needed, but can be useful
        //But be careful because it takes time to generate this list as well, so measure that the algorithm is faster by using this list
        public HashSet<HalfEdge> GetUniqueEdges()
        {
            HashSet<HalfEdge> uniqueEdges = new HashSet<HalfEdge>();

            foreach (HalfEdge e in edges)
            {
                var p1 = e.v.position;
                var p2 = e.prevEdge.v.position;

                bool isInList = false;

                //TODO: Put these in a lookup dictionary to improve performance
                foreach (HalfEdge eUnique in uniqueEdges)
                {
                    var p1_test = eUnique.v.position;
                    var p2_test = eUnique.prevEdge.v.position;

                    if ((p1.Equals(p1_test) && p2.Equals(p2_test)) || (p2.Equals(p1_test) && p1.Equals(p2_test)))
                    {
                        isInList = true;

                        break;
                    }
                }

                if (!isInList)
                {
                    uniqueEdges.Add(e);
                }
            }

            return uniqueEdges;
        }
    }
    public class Normalizer2
    {
        float dMax;
        AABB2 boundingBox;
        public Normalizer2(List<Vector2> points)
        {
            this.boundingBox = new AABB2(points);
            this.dMax = CalculateDMax(boundingBox);
        }
        public float CalculateDMax(AABB2 boundingBox)
        {
            float dX = boundingBox.max.x - boundingBox.min.x;
            float dY = boundingBox.max.y - boundingBox.min.y;
            float dMax = Mathf.Max(dX, dY);
            return dMax;
        }

        public Vector2 Normalize(Vector2 point)
        {
            float x = (point.x - boundingBox.min.x) / dMax;
            float y = (point.y - boundingBox.min.y) / dMax;

            return new Vector2(x, y);
        }

        public Vector2 UnNormalize(Vector2 point)
        {
            float x = (point.x * dMax) + boundingBox.min.x;
            float y = (point.y * dMax) + boundingBox.min.y;

            return new Vector2(x, y);
        }

        public List<Vector2> Normalize(List<Vector2> points)
        {
            List<Vector2> result = new List<Vector2>();
            foreach (var p in points)
            {
                result.Add(Normalize(p));
            }
            return result;
        }


        public void UnNormalize(ref HalfEdgeData data)
        {
            foreach (var v in data.vertices)
            {
                Vector2 unNormalize = UnNormalize(v.position);
                v.position = unNormalize;
            }
        }
    }

    public struct AABB2
    {
        public Vector2 min;
        public Vector2 max;

        public AABB2(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public AABB2(List<Vector2> points)
        {

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point.x < minX)
                {
                    minX = point.x;
                }
                else if (point.x > maxX)
                {
                    maxX = point.x;
                }

                if (point.y < minY)
                {
                    minY = point.y;
                }
                else if (point.y > maxY)
                {
                    maxY = point.y;
                }
            }
            this.min = new Vector2(minX, minY);
            this.max = new Vector2(maxX, maxY);
        }
    }
}
