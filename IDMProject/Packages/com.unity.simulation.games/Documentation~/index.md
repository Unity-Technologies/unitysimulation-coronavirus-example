# Unity Game Simulation

## How It Works
The first step is to implement the Game Simulation package.  The package exists to help you create a build of your game to be used with the Unity Game Simulation service.  At a high level, the package:
1. Fetches parameter values for simulation and update class values accordingly before they are required in game
2. Updates counters after each event to be tracked
3. Calls `Application.Quit()` at the end of gameplay

Once this build is uploaded to Unity Game Simulation, a designer or other user at your studio can run simulations from the Unity Game Simulation dashboard.

Please reach out to [gamesimulation@unity3d.com](mailto:gamesimulation@unity3d.com) if you have any issues with implementation.

## Getting Whitelisted
Before you can start upload your builds to Game Simulation, you will must be whitelisted. Please reach out to [gamesimulation@unity3d.com](mailto:gamesimulation@unity3d.com) if you have not been whitelisted.