# Running the Grocery Store Simulation on Unity Game Simulation

## Pre-Setup

1. Check out the [`sim-integration`](https://github.com/Unity-Technologies/unitysimulation-coronavirus-example/tree/sim-integration) branch.
This branch includes code that integrates with the Game Simulation package, which is also included in the branch.
2. Get whitelisted for Unity Game Simulation by using this [form](https://create.unity3d.com/unity-game-simulation-beta-sign-up). In the section that asks "In a few words, what interests you about Unity Game Simulation" please put "Unity Game Simulation for Coronavirus", the team will reach out to you via email once you've been granted access. Game Simulation is currently in beta, and requires your Unity Organization to be whitelisted to use it.

## Step One - Enable Services

1. In order to use Unity Game Simulation, you must enable services in the Unity project.  Click on **Window** -> **General** -> **Services** to open the services window.
2. Select your organization.
3. Click on the settings tab and make sure there is a project ID.

## Step Two - Set up Game Simulation Parameters

Normally, you would use the Game Simulation Editor Window to set up your simulation parameters, but for convenience, we've included a `.csv` file will the parameters we used to get you started.

1. Open the Game Simulation window by going to **Window** -> **Game Simulation**. Click on the tab called **Parameter Set Up**. This will create the environment in Remote Config that Game Simulation will use when running in the cloud. Close the Game Simulation Editor window.
2. Find the `.csv` [file](link to file) in the `sim-integration` branch.
3. Go to the [Remote Config Dashboard](https://app.remote-config.unity3d.com/).
4. Find your project in the project list and click on it.
5. You should see a table with your **GameSim** config. Click the **View** button.
6. Click **Default Config**.
7. At the top of the config table, to the right of the search bar, click the three dots, and select **Upload CSV**, and select the `.csv` file from the first step.
8. In Unity, open the Game Simulation window again, and verify that the parameters from the `.csv` file are visibile. If the window is already open, close and reopen the window to see the parameters.

## Step Three - Create a Build

1. In the Game Simulation Editor window, click the **Build Upload** tab.
2. Click the checkbox next to the "UnityFarmsMarket" scene.
3. Give your build a name in the build name text box.
4. Click **Build and Upload**.

## Step Four - Run a Simulation

1. Go to the [Game Simulation Dashboard](https://gamesimulation.unity3d.com/), and find and click on your project.
2. Click the **Create Simulation** button.
3. Give your simulation a name.
4. Select the build you just uploaded from the dropdown. You may notice your build says "Verification in Progress", if that's the case, wait about 5-10 minutes for the verification to be completed before proceeding. This ensures that your build is properly configured for Game Simulation. If you see "Verification Failed" please contact us at gamesimulation@unity3d.com.
5. In the parameters table, enter the different values for the parameters that you'd like to run. You can enter multiple values to run by using a comma to separate them.
6. Set the number of times you'd like each parameter combination to run.
7. Click **Run**.

## Step Five - Download the Results

With the default value for the `SimulationTimeInSeconds` parameter, the simulation will run for about five minutes. Check back in about 10 minutes to see if your simulation is complete. Once the simulation is complete, you can download the results from the simulation overview page.

## Step Six - Make it Your Own!

Play around with different values for the parameters and see how the results change.

You can also add new parameters by using the Game Simulation Editor window. Make sure to instrument the new parameters in the `StoreSimulation` class.

## Reference Links

* [Game Simulation Package Documentation](https://docs.unity3d.com/Packages/com.unity.simulation.games@0.4/manual/index.html)
* [Game Simulation Dashboard Documentation](https://unity-technologies.github.io/gamesimulation/Docs/dashboard.html)
* [Unity Simulation Documentation](https://github.com/Unity-Technologies/Unity-Simulation-Docs)
* [Unity Remote Config Documentation](https://docs.unity3d.com/Packages/com.unity.remote-config@1.2/manual/index.html)