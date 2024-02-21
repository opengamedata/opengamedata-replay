# opengamedata-replay
A Unity package for replay of games via log data

This is a light-weight package for creating replays from the OpenGameData log files.

### Ussage:

#### Parsing the Log File
Add the `Parser.cs` to your project. 

Use the dropdown menu to choose a logfile
![](menu.jpg)

This script will parse these files and then convert individual sessions into binary files ready for playback.

#### Replay
Add the `Replay.cs` to your project.
![](replayObject.jpg)

You will need to set the following:

* `Replay Filepath` the path to the binary replay file
* `Gaze Object` the gameobject associated with the players view
* `Right Hand Object` the gameobject associated with the right hand
* `Left Hand Object` the gameobject associated with the left hand

The other objects are currently made public for interactive analysis. 

With this all set, simply start the application and the playback should happen automatically.

### Log File Assumptions:
* Log files are constructed of tab-seperated values
* Columns exist for `session_id`, `event_name`, `event_data`, and `game_state` 
* Game State is formated as json with the following format:

> 		public class GameState
    {
        public float posX;
        public float posY;
        public float posZ;
        public float rotW;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float seconds_from_launch;
    };
	 
* Events for gaze are under the name, `viewport_data` and event data is packed under the name `gaze_data_package`
* Events for right hand data is under the name, `right_hand_data` and event data is packed under the name `right_hand_data_package`
*  Events for left hand data is under the name, `left_hand_data` and event data is packed under the name `left_hand_data_package`
*  Packages are constructed as a list of Vector3 `pos` and Vector4 `rot`