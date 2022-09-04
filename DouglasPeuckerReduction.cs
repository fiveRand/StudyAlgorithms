using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DouglasPeuckerReduction
{
    /// <summary>
    /// prefer tolerance is 0 ~ 0.3f
    ///  check this site for example.
    ///  https://cartography-playground.gitlab.io/playgrounds/douglas-peucker-algorithm/
    /// </summary>
    /// <param name="convertList"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static List<Vector3> Reduct(List<Vector3> convertList, float tolerance)
    {
        if (convertList == null || convertList.Count < 3)
        {
            Debug.LogError("Nodes are null or too less to reduct");
            return convertList;
        }
        int first = 0;
        int last = convertList.Count - 1;

        List<int> tempList = new List<int>();
        tempList.Add(first);
        tempList.Add(last);

        while (convertList[first] == convertList[last])
        {
            last--;
        }

        Reducting(convertList, first, last, tolerance, ref tempList);
        List<Vector3> result = new List<Vector3>(tempList.Count);
        tempList.Sort();
        foreach (var i in tempList)
        {
            result.Add(convertList[i]);
        }
        return result;
    }

    static void Reducting(List<Vector3> nodes, int first, int last, float tolerance, ref List<int> tempList)
    {
        float maxDist = 0;
        int iFarthest = 0;

        for (int i = first; i < last; i++)
        {
            float distance = PerpendicularDistance(nodes[first], nodes[last], nodes[i]);

            if (distance > maxDist)
            {
                maxDist = distance;
                iFarthest = i;
            }
        }

        if (maxDist > tolerance && iFarthest != 0)
        {
            tempList.Add(iFarthest);
            Reducting(nodes, first, iFarthest, tolerance, ref tempList);
            Reducting(nodes, iFarthest, last, tolerance, ref tempList);
        }
    }

    static float PerpendicularDistance(Vector3 a, Vector3 b, Vector3 c)
    {
        float area = Mathf.Abs(.5f * (a.x * b.y + b.x * c.y + c.x * a.y - b.x * a.y - c.x * b.y - a.x * c.y));
        float bottom = Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2)) + Mathf.Pow(a.y - b.y, 2);
        float height = area / bottom * 2;
        return height;
    }
}
