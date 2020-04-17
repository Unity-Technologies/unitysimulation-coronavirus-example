# Future directions

## Handle large timesteps better
Some artifacts may appear at larger timesteps.
* Shoppers will stop at their target waypoint for the frame, even if could go "further" in that step. A better model would be to pick a new target and continue moving to that target depending on how far they've already moved that frame.
* The transmission model only looks at the current position of both shoppers. For very high timesteps, this could cause "tunneling", where an infected shopper and a suceptible one pass each other with no interaction. The way to solve this is to track both the current and previous positions of the shoppers, and look at the closest point between their segments. This also doesn't account for turning corners.

## Better movement model
The current model is very primitive. Shoppers don't account for each other, only follow set paths, and don't do any realistic shopping behavior. Some ideas:
* Navmesh for movement instead of waypoints
* Avoidance - shoppers should try to avoid each other if they're on a collision course.

