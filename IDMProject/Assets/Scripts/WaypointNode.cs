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

    public WaypointNode GetRandomNeighbor(WaypointNode exclude = null)
    {
        // TODO handle no edges
        var index = Random.Range(0, Edges.Count);
        if (Edges[index] == exclude)
        {
            // Picked the one we were trying to avoid
            // Add a random offset (mod the length) to
            // get a new node with uniform distribution
            var offset = Random.Range(1, Edges.Count);
            index = (index + offset) % Edges.Count;
        }

        return Edges[index];
    }

    void OnDrawGizmos()
    {
        DrawEdges();
    }

    void DrawEdges()
    {
        foreach (var neighbor in Edges)
        {
            if (neighbor == null)
            {
                continue;
            }
            var drawColor = Color.cyan;
            drawColor.a = .5f;
            Gizmos.color = drawColor;
            Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }
}


