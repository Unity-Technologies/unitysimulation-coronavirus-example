# Infection Model

## Configurable parameters
- *Exposure Distance Meters* - controls the maximum distance (in meters) that healthy shoppers are potentially exposed; 
exposure is only checked when they are within this distance of a infectious shopper. Defaults to 1.8288m (6 feet).
- *Exposure Probability At Zero Distance* - controls the probability of a healthy shopper becoming exposed when they are 
0 meters from a infectious shopper.
- *Exposure Probability At Max Distance* - controls the probability of a healthy shopper becoming exposed when they are 
`Exposure Distance Meters` meters from a infectious shopper.

Note that the probability values are expressed as the probability of infection per second, instead of per step. This is
to keep the overall chance of infection independent of the simulation frame rate.  

## Exposure Updates
During each `Update()` step of the simulation, we perform the following steps in `StoreSimulation.UpdateExposure()`:
- For each infectious shopper, find the healthy shoppers within `Exposure Distance Meters` of them.
- For each healthy shopper, determine the probability of infection. This is a function of the distance between
the shoppers, the Exposure Probability values, and the timestep (Time.deltaTime). The probability is linearly 
interpolated between `Exposure Probability At Zero Distance` and `Exposure Probability At Max Distance` based on the 
distance, then transformed to account for the timestep.

