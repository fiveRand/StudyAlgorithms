using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyPathfinding;
public class Unit : MonoBehaviour
{

    public Map map;

    public float speed = 10;
    public float radius = 0.5f;

    List<Vector2> path = new List<Vector2>();
    Coroutine coroutine;

    private void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
    private void Update() 
    {
        if(Input.GetMouseButtonDown(1))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            GetPath(transform.position, mousePos);
        }
    }

    void GetPath(Vector2 start,Vector2 end)
    {

        path = map.GetPath(start, end);

        if(path.Count > 0)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            coroutine = StartCoroutine(FollowPath(path));
        }
        else
        {
            Debug.Log("PATH EMPTY");
        }
    }

    IEnumerator FollowPath(List<Vector2> path)
    {
        int pathIndex = 0;
        while (true)
        {
            Vector3 destination = path[pathIndex];

            Vector3 direction = (destination - transform.position).normalized;
            float sqrDist = (destination - transform.position).sqrMagnitude;

            Vector3 velocity = direction * speed;
            transform.position += velocity * Time.fixedDeltaTime;


            if(sqrDist <= 0.1f)
            {
                pathIndex++;

                if(pathIndex == path.Count)
                {
                    Debug.Log("REACH");
                    break;
                }
            }
            

            yield return new WaitForFixedUpdate();
        }
    }
}
