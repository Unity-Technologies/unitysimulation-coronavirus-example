using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointNode : MonoBehaviour
{
    public List<WaypointNode> Edges = new List<WaypointNode>();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public WaypointNode GetRandomNeighbor()
    {
        // TODO handle no edges
        return Edges[Random.Range(0, Edges.Count)];
    }

    void OnDrawGizmos()
    {
        DrawEdges();
    }

    void DrawEdges()
    {
        foreach (var neighbor in Edges)
        {
            var drawColor = Color.cyan;
            drawColor.a = .5f;
            Gizmos.color = drawColor;
            Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }
}


