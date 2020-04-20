# Simulation Queuing Model
The queuing model allows you to simulate the checkout queue at the store. 

## Configurable parameters
- *Number of Open Counters* (In real world scenario, this might become a bottle neck if the number of counters open are 
less as compared to the traffic of the shoppers).
- *Max Purchasing Time*: This is variable and can range between 1 and max time set.

Queuing behavior ties up with the waypoint system initialized in the beginning. Certain waypoints have access to 
registers that are tagged with "Register" tag. As the shopper approaches the register, indicating willingness to 
checkout, the queuing system checks if any open counter's queue, accessible from the shopper's waypoint is available. 
Shopper gets queued accordingly. In case if there are no queues available, the waypoint nodes for the shopper are 
updated simulating the shopper roaming around until the queues become available.

## Limitations/Improvements
- The current size of the queue is fixed (set to 4) considering the distance between the register and the waypoint 
access node. It can be configurable based upon the max safe distance set (Compute the max queue size based upon max 
safe distance)
- Doesn't account for 15 items or less counter.
- Currently the queuing system is inherent to the store simulation and only accounts for checkout queues. Ideally it 
can be designed to be independent system to handle different types of queues in addition to register queues such as 
queue outside the store etc..
