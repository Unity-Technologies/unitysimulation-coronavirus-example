Contact information: See #ai-idm-covid19-simulation in slack.

# Grocery Store Simulation:
![Grocery Simulation](docs/images/Grocery.png "Grocery Simulation")

## Waypoints
Waypoints are nodes on graph. The edges are determined at the start of the simulation. Entrance waypoints will make an outgoing edge to any waypoint that they have line of sight to.
Other waypoints will only check for edges in the cardinal directions. This is a heuristic that we can adjust later.

## Shoppers
Shoppers spawn randomly from one of the entrance waypoints. The total number of shoppers and number of contagious shoppers are controllable from the StoreSimulation script.
Shoppers follow the path between two nodes at a constant speed. The default speed is set in the Shopper prefab and is modulated for each shopper when they're spawned. When the shopper
reaches its target waypoint, it picks a new one randomly, biased towards nodes that are in the direction it's currently moving. Left and right turns are less likely, and U-turns are very unlikely.
When a shopper reaches an exit waypoint, they are respawned at a random entrance.

## Infection
Shoppers are colored according to their health status:
* Blue shoppers are healthy (suceptible).
* Red shoppers are contagious.
* Yellow shoppers have been exposed by a contagious shopper, but are not themselves contagious.
At each update, contagious shoppers will infect healthy ones that are within 2 units (roughly 6 feet).


# Future directions
## Track exposure rate
We could run the simulation for a longer time and track the percentage of shoppers that are exposed as a function of the number of total and contagious shoppers.

## Better transmission model
Currently susceptible shoppers are infected 100% of the time that they pass with 2m of a contagious one. We could fine tune this to account for distance and timestep.

## Handle large timesteps better
Some artifacts may appear at larger timesteps.
* Shoppers will stop at their target waypoint for the frame, even if could go "further" in that step. A better model would be to pick a new target and continue moving to that target depending on how far they've already moved that frame.
* The transmission model only looks at the current position of both shoppers. For very high timesteps, this could cause "tunneling", where an infected shopper and a suceptible one pass each other with no interaction. The way to solve this is to track both the current and previous positions of the shoppers, and look at the closest point between their segments. This also doesn't account for turning corners.

## Better movement model
The current model is very primitive. Shoppers don't account for each other, only follow set paths, and don't do any realistic shopping behavior. Some ideas:
* Navmesh for movement instead of waypoints
* Avoidance - shoppers should try to avoid each other if they're on a collision course.
* Path planning - they should head to areas of the store they haven't already been, and shouldn't leave until they've visited a certain number of unique waypoints.
* Queueing - they should queue to get into the building and at the register. This is hard to simulate but would be good to show that distancing reduces exposure.

## Better art
"Art" is used loosely here.