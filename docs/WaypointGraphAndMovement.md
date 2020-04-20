# Waypoint Graph

Waypoints are used to represent nodes in a directed graph. The edges are determined procedurally; each pair of nodes
is examined, and an edge is created based on a few criteria:

* Entrance nodes can only have outgoing edges, and exit nodes can only have incoming edges.
* The destination node must be within a angle threshold of the origin node's forward, left, right, and backwards 
directions.
  * If the StoreSimulation is enforcing one-way aisles, the backwards direction is not considered
  * If a node is marked as `passthrough`, only its forward direction is considered.
* A raycast between the nodes must not hit anything besides the destination node.  

# Movement
When shoppers spawn, by default they pick a random (but plausible) path through the store:
* Pick a certain number of intermediate goals, and a random entrance and exit.
* Reorder the intermediate goals to try to reduce the total distance (although this might not actually be the best 
ordering).
* Find the shortest path between the intermediate goals using Dijkstra's algorithm, and combine these sub-paths into
the full path.

Shoppers then follow that path at constant speed until they reach a [checkout queue](QueueingModel.md).

If something goes wrong with the path generation, shoppers will wander around the store at random.
