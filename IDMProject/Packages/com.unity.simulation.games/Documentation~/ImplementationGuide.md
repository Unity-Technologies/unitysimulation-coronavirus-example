# Implementation Guide
## Requirements
Requirements to implement and use Unity Game Simulation
* Unity Services enabled in your project. See instructions [here](https://docs.unity3d.com/Manual/SettingUpProjectServices.html)
* Using Unity 2018.3 or later
* Compiled for Linux (i.e. you need to be able to build for Linux from the editor)
* Using OpenGL
* Configured to auto-run on open (i.e. it contains a bot or playthrough script)
* Must call Application.Quit() when gameplay is finished during runtime

Additionally, make sure you have the following design and experimentation questions:
* A list of all parameters you would like to evaluate
* A list of all metrics you would like to measure which will show up on the Unity Game Simulation results page in the Web UI. Note: we only support metrics stored as type Long

## Step 1. Install the Unity Game Simulation Package
1. Download the Game Simulation package by adding the following line to your project's dependencies in your manifest.json file: `"com.unity.simulation.games": "0.4.0-preview",`

## Step 2. Create Parameters in Game Simulation for each Grid Search Parameter
1. From the editor, open the Game Simulation window (Window > Game Simulation)
2. Add an entry for each parameter that you would like to be able to test with Unity Game Simulation. Choose a name, type, and default value for each. Note: If you’ve already done this from remote config, you should see the parameters you created in this window and do not need to re-create them.
3. Press “Save” when done.
Note: Unity Game Simulation will update the values for these parameters in each simulation instance based on the options specified in the Unity Game Simulation web UI. If your build is run outside of Unity Game Simulation, these parameters will default to the value in the “Default Value” field. Therefore, put an appropriate default value for each parameter.

## Step 3. Load Parameters for Grid Search
Before each run of your simulation, Unity Game Simulation decides on a set of parameter values to evaluate. At run time, your game needs to retrieve the set of parameter values for evaluation and then set variables in your game to those values. Unity Game Simulation requires that parameter values are fetched by calling `GameSimManager.Instance.FetchConfig(Action<GameSimConfigResponse>)`, which is included in the Unity Game Simulation package. 

To load parameters into your game
1. Fetch this run’s set of parameter values with GameSimManager.Instance’s FetchConfig method at game start. This will store this run’s parameter values in a GameSimConfigResponse object.
2. Set game variables to the values now stored in in the GameSimConfigResponse object. Access the variables stored in GameSimConfigResponse with
GameSimConfigResponse.Get[variable type]("key name");

## Step 4. Enable Tracking of Metrics
Unity Game Simulation uses a Counter in order to track metrics throughout each run of your game. At the end of the simulation, these metrics will be available for download in both raw and aggregated from the Unity Game Simulation web UI.

### Example Implementation
In this example racing game, we want to track lap count and finishing time.

1. Call `IncrementCounter` or `SetCounter` to update counter values. If a counter with the `name` supplied is not found, it will be created, initialized to 0, then either incremented or set as appropriate.
```
   void OnLapFinish()
   {        
      GameSimManager.Instance.IncrementCounter("lapCount", 1);
   }


   void OnFinish()
   {        
      GameSimManager.Instance.SetCounter("finishingTime", GetFinishingTime());
   }
```

2. Call Application.Quit()
```
   void OnFinish()
   {        
      GameSimManager.Instance.Reset("finishingTime", getFinishingTime());
      Application.Quit()
   }
```

## Step 5. Verify it all Works
1. Build the game targeted to your operating system with the Unity Game Simulation SDK implemented.
2. Run the executable and verify your gameplay script or bot plays through the game with no external input and quits on its own.
3. Verify that there is a file called counters_0.json in your system’s default `Application.persistentDataPath`.  If you are using a mac, this is likely ~/Library/Application Support/Unity Technologies/.

## Step 6. Upload Your Build to Unity Game Simulation
Before you can start upload your builds to Game Simulation, you will must be whitelisted. Please reach out to [gamesimulation@unity3d.com](mailto:gamesimulation@unity3d.com) if you have not been whitelisted.

1. Open up the Build Settings window (File > Build Settings …) and make sure all scenes you would like to include in your build appear in the “Scenes in Build” section. If not, open them up in the editor and then click “Add Open Scenes” in the Build Settings window.
2. Close the Build Settings window
3. Click on the build upload tab in the Game Simulation window (Window > Game Simulation)
4. Select the scenes to include in your build
5. Name your build
6. Click “Build and Upload”
7. Click on “Create Simulation” (or navigate directly to the [Dashboard](https://gamesimulation.unity3d.com)), to run a simulation from the Web UI. 