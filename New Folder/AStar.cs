using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyDataStructure;
using Priority_Queue;



namespace MyPathfinding
{

    public static class AStar
    {

        public static List<HalfEdge> StartPathfinding(Map map,Vector2 start,Vector2 end)
        {

            SimplePriorityQueue<Node> openSet = new SimplePriorityQueue<Node>();
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>(map.nodeCount);

            foreach (Node node in map.nodes)
            {
                node.Gcost = int.MaxValue;
                node.Hcost = int.MaxValue;
            }

            var startNode = map.GetNode(start);
            var endNode = map.GetNode(end);

            startNode.Gcost = 0;
            startNode.Hcost = GetDistance(startNode, endNode);

            openSet.Enqueue(startNode, startNode.Fcost);
            

            while(openSet.Count > 0)
            {

                var curNode = openSet.Dequeue();

                if(curNode == endNode)
                {

                    return RetracePath(cameFrom, curNode,startNode);
                }

                foreach(var neighbor in curNode.neighbors)
                {
                    if(neighbor == null)
                    {
                        continue;
                    }
                    float neighborGcost = curNode.Gcost + GetDistance(curNode, neighbor) + neighbor.weight;
                    if(neighborGcost < neighbor.Gcost)
                    {
                        neighbor.Gcost = neighborGcost;
                        neighbor.Hcost = GetDistance(neighbor, endNode);
                        cameFrom[neighbor] = curNode;
                        if(!openSet.Contains(neighbor))
                        {
                            openSet.Enqueue(neighbor,neighbor.Fcost);
                        }
                    }


                }
            }

            return null;

        }

        public static List<HalfEdge> RetracePath(Dictionary<Node, Node> cameFrom,Node curNode,Node startNode)
        {
            List<HalfEdge> testResult = new List<HalfEdge>(cameFrom.Count);

            List<Node> result = new List<Node>(cameFrom.Count);
            result.Add(curNode);
            while(curNode != startNode)
            {
                
                curNode = cameFrom[curNode];
                result.Add(curNode);
            }
            result.Reverse();
            // return result;


            for (int i = 0; i < result.Count - 1; i++)
            {
                var curEdge = result[i].face.edge;
                while (true) // Find Adjacent Edge from next face
                {

                    if (curEdge.oppositeEdge != null && curEdge.oppositeEdge.face == result[i + 1].face)
                    {
                        curEdge = curEdge.oppositeEdge;
                        testResult.Add(curEdge);
                        break;
                    }

                    curEdge = curEdge.nextEdge;
                }

            }



            return testResult;
            
        }

        public static float GetAngle(Vector3 p1,Vector3 p2,Vector3 fixedPoint)
        {

            var angle1 = Mathf.Atan2(fixedPoint.y - p1.y, fixedPoint.x - p1.x);
            var angle2 = Mathf.Atan2(fixedPoint.y - p2.y, fixedPoint.x - p2.x);

            return Mathf.Rad2Deg * (angle1 - angle2);
        }

        static bool isSame(Vector2 aVec, Vector2 bVec, float toleranceRange = 0.00001f)
        {
            return Vector3.SqrMagnitude(aVec - bVec) < toleranceRange;
        }

        public static float GetDistance(Node a,Node b)
        {

            return Vector2.SqrMagnitude(a.face.centerPosition - b.face.centerPosition);
        }

        public static bool AreLineIntersecting(Vector2 p1v1, Vector2 p1v2, Vector2 p2v1, Vector2 p2v2, bool includeEndPoints)
        {
            bool result = false;

            if (isSame(p1v1, p2v1) || isSame(p1v1, p2v2) || isSame(p1v2, p2v1) || isSame(p1v2, p2v2))
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

    }

    public class Node : FastPriorityQueueNode
    {
        public HalfEdgeFace face;
        public List<Node> neighbors;
        public int weight;
        public float Gcost;
        public float Hcost;
        public float Fcost
        {
            get
            {
                return Gcost + Hcost;
            }
        }

        public Node(HalfEdgeFace face)
        {
            this.face = face;
            weight = 0;
            Gcost = float.MaxValue;
            Hcost = float.MaxValue;
        }


    }

    /// <summary>
    /// http://jceipek.com/Olin-Coding-Tutorials/pathing.html#funnel-algorithm
    /// </summary>
    public class Funnel
    {
        public Vector3 edgeStart;
        public Vector3 edgeEnd;
        public Vector3 centerPoint;
        public float lastAngle;

        public Funnel(Vector3 edgeStart, Vector3 edgeEnd, Vector3 centerPoint)
        {
            this.edgeStart = edgeStart;
            this.edgeEnd = edgeEnd;
            this.centerPoint = centerPoint;
            UpdateAngle();
        }
        public void UpdateAngle()
        {
            lastAngle = GetAngle(edgeStart, edgeEnd, centerPoint);
        }

        public float GetAngle(Vector3 p1, Vector3 p2, Vector3 fixedPoint)
        {

            var angle1 = Mathf.Atan2(fixedPoint.y - p1.y, fixedPoint.x - p1.x);
            var angle2 = Mathf.Atan2(fixedPoint.y - p2.y, fixedPoint.x - p2.x);

            return Mathf.Rad2Deg * (angle1 - angle2);
        }

        public bool isNarrowerThanLastAngle(Vector2 left,Vector2 right)
        {
            float newAngle = GetAngle(left, right, centerPoint);

            return newAngle < lastAngle;
        }

        public bool isWrongSide(Vector2 tail,Vector2 head, Vector2 point,bool isRight)
        {

            bool result = Algo(tail, head, point);

            if(isRight)
            {
                return !result;
            }
            else
            {
                return result;
            }
            
        }

        /// <summary>
        /// if vector direction is  down, then 9 o'clock is true
        /// else , which is up, then 3 o'clock is true.
        /// if vector direction is right, then 6 o'clock is true,
        /// else, which is left, then 12 o'clock is true.
        /// </summary>
        /// <param name="tail"></param>
        /// <param name="head"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Algo(Vector2 tail, Vector2 head, Vector2 point)
        {
            float x1 = tail.x - point.x;
            float y1 = tail.y - point.y;
            float x2 = head.x - point.x;
            float y2 = head.y - point.y;

            float determinant = x1 * y2 - y1 * x2;

            float EPSILON = 0.00001f;

            return determinant < -EPSILON;
        }


        /*
        void FailedSimpleStupidFunnel()
        {


            var faceCenter = result[0].face.centerPosition;
            var curEdge = result[0].face.edge;
            var nextFaceCurEdge = result[1].face.edge;
            while (true) // Find Adjacent Edge from next face
            {
                if (curEdge.oppositeEdge != null && curEdge.oppositeEdge.face == result[1].face)
                {
                    curEdge = curEdge.oppositeEdge;
                    break;
                }
                
                if (isSame(curEdge.v.position, nextFaceCurEdge.nextEdge.v.position) && isSame(curEdge.nextEdge.v.position, nextFaceCurEdge.v.position))
                {
                    curEdge = nextFaceCurEdge;
                    break;
                }
                else if (isSame(curEdge.v.position, nextFaceCurEdge.prevEdge.v.position) && isSame(curEdge.nextEdge.v.position, nextFaceCurEdge.nextEdge.v.position))
                {
                    curEdge = nextFaceCurEdge.nextEdge;
                    break;
                }
                else if (isSame(curEdge.v.position, nextFaceCurEdge.v.position) && isSame(curEdge.nextEdge.v.position, nextFaceCurEdge.prevEdge.v.position))
                {
                    curEdge = nextFaceCurEdge.prevEdge;
                    break;
                }
                

                curEdge = curEdge.nextEdge;
            }


            Funnel funnel = new Funnel(curEdge.v.position, curEdge.nextEdge.v.position, result[0].face.centerPosition);

            Debug.Log($"EdgeStart : {curEdge.v.position} , EdgeEnd : {curEdge.nextEdge.v.position}");

            for (int i = 1; i < result.Count - 1; i++)
            {
                curEdge = result[i].face.edge;
                nextFaceCurEdge = result[i + 1].face.edge;
                while (true) // Find Adjacent Edge from next face
                {

                    if (curEdge.oppositeEdge != null && curEdge.oppositeEdge.face == result[i + 1].face)
                    {
                        curEdge = curEdge.oppositeEdge;
                        break;
                    }
                    

                    if(isSame(curEdge.v.position,nextFaceCurEdge.v.position))
                    {
                        curEdge = nextFaceCurEdge.prevEdge;
                        break;
                    }
                    else if(isSame(curEdge.v.position, nextFaceCurEdge.nextEdge.v.position))
                    {
                        curEdge = nextFaceCurEdge;
                        break;
                    }
                    else if(isSame(curEdge.v.position,nextFaceCurEdge.prevEdge.v.position))
                    {
                        curEdge = nextFaceCurEdge.nextEdge;
                        break;
                    }
                    

                    curEdge = curEdge.nextEdge;
                }
                Debug.Log($"FunnelStart : {funnel.edgeStart} , FunnelEnd : {funnel.edgeEnd}, FunnelPoint : {funnel.centerPoint}");
                Debug.Log($"EdgeStart : {curEdge.v.position} , EdgeEnd : {curEdge.nextEdge.v.position}");
                if (!funnel.Algo(funnel.edgeStart, funnel.centerPoint, curEdge.nextEdge.v.position))
                {
                    Debug.Log($" Hit : {curEdge.nextEdge.v.position}");
                    funnel.UpdateAngle();
                    testResult.Add(funnel.edgeStart);
                }


                // search right first.
                

                if (funnel.isWrongSide(funnel.left, curEdge.v.position,true))
                {
                    funnel.centerPoint = funnel.right;
                    testResult.Add(funnel.centerPoint);
                    funnel.UpdateAngle();
                    continue;
                }

                if (funnel.isNarrowerThanLastAngle(funnel.left,curEdge.v.position))
                {
                    funnel.right = curEdge.v.position;
                    funnel.UpdateAngle();
                }
                if (funnel.isWrongSide(funnel.right, curEdge.nextEdge.v.position, false))
                {
                    funnel.centerPoint = funnel.left;
                    testResult.Add(funnel.centerPoint);
                    funnel.UpdateAngle();
                    continue;
                }
                
                if (funnel.isNarrowerThanLastAngle(funnel.right, curEdge.nextEdge.v.position))
                {
                    funnel.left = curEdge.nextEdge.v.position;
                    funnel.UpdateAngle();
                }
                
            }
        }
*/
    }
}
