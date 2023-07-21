// Random-waypoint Patrol Script v2 - By DJ Arghlex
// Made by special request from YOR on the SE Discord https://discord.gg/0hIE7GirODUqhfIg

// FEATURES
// Patrols [argument] meters from its starting position along a randomly generated route (with a scaling minimum distance between each point)
// When the turret detects something, it'll freeze in place and let the turrets attack the target.
// it'll then wait some seconds and resume patrols.

// INSTALLATION
// install a turret, setting it to fire on whatever you need it to fire on.
// install a remote control, setting it to have Collision Avoidance on, and patrol mode can't be "One Way"
// create a group called "ALERT" (with a beacon or something) that turns on when the ship finds an enemy. Don't put your turrets in this.
// Run this script once a second with the run argument (inside the timer block!) your GPS coordinates of whatever you want patrolled.
// get it from your GPS menu, hit Copy to Clipboard. Should look like this:
//   GPS:MyWaypointCoordinates:1533.43:3454.34:-2867.76:

// CONFIG
int secondsUntilPatrolResume = 15;		// resume patrol after this many times the script's run that the turret doesn't detect something
int maximumPoints = 10;					// generate and patrol this many points. Don't set this too high.
int patrolRadius = 50;					// max distance from patrol waypoint we should go
float distanceModifier = 0.5F;			// needs to be more than 0.0 and less than 2.0. 

// internal variables, don't fool with these
List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
List<Vector3D> patrolPoints = new List<Vector3D>();
IMyLargeTurretBase weapon;
Vector3D origin = new Vector3D(0,0,0);	// DO NOT CHANGE THIS! SERIOUSLY!
float lastAzimuth=0;
float lastElevation=0;
int turretClock=0;
bool restoreRoute = false;

//load state (or create a new one if it doesn't exist)
public Program() {
	if (this.Storage != "") {
		string[] storagesplit = new string[3];
		storagesplit = this.Storage.Split(',');
		turretClock=Convert.ToInt32(storagesplit[0]);
		lastAzimuth=(float)Convert.ToDouble(storagesplit[1]);
		lastElevation=(float)Convert.ToDouble(storagesplit[2]);
		Echo("ready!");
	} else {
		lastAzimuth=0;
		lastElevation=0;
		turretClock=0;
		Echo("RESET!");
		patrolPoints = new List<Vector3D>();
		restoreRoute=true;
	}
}

//save state
public void Save() {
	Storage = turretClock+","+lastAzimuth+","+lastElevation;
}

//toggle a group on or off
void toggleGroup(string groupName, bool state) {
	var group = this.GridTerminalSystem.GetBlockGroupWithName(groupName);
	if (group != null) {
		var groupBlocks = new List<IMyTerminalBlock>();
		group.GetBlocks(groupBlocks);
		for(int i = 0; i < groupBlocks.Count; i++){
			IMyTerminalBlock block = groupBlocks[i];
			if (state==true){
				block.GetActionWithName("OnOff_On").Apply(block);
			}
			else if(state==false){
				block.GetActionWithName("OnOff_Off").Apply(block);
			}
		}
		Echo("G/"+groupName+":"+state);
	} else {
		//throw new Exception("Group named "+groupName+" not found!");
		Echo("Group named "+groupName+" not found!");
	}
}

void Main(string argument) {
	if (argument == "reset") {
		lastAzimuth=0;
		lastElevation=0;
		turretClock=0;
		patrolPoints = new List<Vector3D>();
		restoreRoute=true;
		Storage = turretClock+","+lastAzimuth+","+lastElevation;
		Echo("MANUAL RESET!");
	} else {
		GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);
		if (list.Count > 0) {
			var remote = list[0] as IMyRemoteControl;
			bool gotoOrigin = false;
			GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(list);
			
			Echo("Parsing patrol center");
			string[] argumentsplit = new string[6];
			argumentsplit = argument.Split(':');
			origin.X=Convert.ToDouble(argumentsplit[2]);
			origin.Y=Convert.ToDouble(argumentsplit[3]);
			origin.Z=Convert.ToDouble(argumentsplit[4]);
			Echo("Patrolling "+patrolRadius+"m from: "+argumentsplit[1]);
			
			if (list.Count == 0) {
				gotoOrigin = true;
				Echo("ERROR: No guns installed! Returning to origin @ "+origin.ToString());
			} else {
				bool hasUsableGun = false;
				for (int i = 0; i < list.Count; ++i) {
					weapon = list[i] as IMyLargeTurretBase;
					if (!weapon.IsFunctional) continue;
					//if (weapon.HasInventory() && !weapon.GetInventory(0).IsItemAt(0)) continue;
					hasUsableGun = true;
				}
	
				if (!hasUsableGun) {
					gotoOrigin = true;
					Echo("ERROR: No usable guns found! Returning to origin @ "+origin.ToString());
				}
			}
			
			if (patrolPoints.Count == 0) {
				Random r = new Random();
				for (int i=0; i < maximumPoints; ++i) {
					bool generateNewPoint=true;
					Vector3D patrolPoint = new Vector3D(0,0,0);
					while (generateNewPoint == true) {
						patrolPoint.X = r.Next(Convert.ToInt32(Math.Floor(origin.X))-patrolRadius,Convert.ToInt32(Math.Floor(origin.X))+patrolRadius);
						patrolPoint.Y = r.Next(Convert.ToInt32(Math.Floor(origin.Y))-patrolRadius,Convert.ToInt32(Math.Floor(origin.Y))+patrolRadius);
						patrolPoint.Z = r.Next(Convert.ToInt32(Math.Floor(origin.Z))-patrolRadius,Convert.ToInt32(Math.Floor(origin.Z))+patrolRadius);
						if ( i != 0 ) { // make sure we're not trying to compare the first point to a point that doesn't exist (cuz that'd be ba-a-a-a-ad)
							if (Vector3D.DistanceSquared(patrolPoint, patrolPoints[i-1]) > (patrolRadius*distanceModifier)*(patrolRadius*distanceModifier) ) { 
								generateNewPoint=false;
							} else {
								generateNewPoint=true;
							}
						} else {
							generateNewPoint = false;
						}
					}
					patrolPoints.Add(patrolPoint);
				}
				Echo("Generated new patrol route");
				restoreRoute=true;
			}
			
			if (restoreRoute) {
				remote.ClearWaypoints();
				for (int i=0; i < patrolPoints.Count; ++i) {
					Echo("Added Point #"+(i+1)+": "+patrolPoints[i].ToString());
					remote.AddWaypoint(patrolPoints[i],"Patrol Point #"+(i+1));
				}
				Echo("Restored patrol route from save");
				restoreRoute=false;
			}
			
			if (gotoOrigin) {
				remote.ClearWaypoints();
				remote.AddWaypoint(origin, "Origin1");
				remote.AddWaypoint(origin, "Origin2");
				remote.AddWaypoint(origin, "Origin3");
				remote.SetAutoPilotEnabled(true);
				Echo("Going home.");
				restoreRoute=true;
			} else {
				Echo("Patrolling.");
				
				bool freakout = false;
				//normal operation. check for turret angle changes.
				if (lastAzimuth != weapon.Azimuth && lastElevation != weapon.Elevation ) {
					Echo("ENEMY DETECTED!");
					turretClock = 0;
					freakout = true;
				} else {
					turretClock=turretClock+1;
					if ( turretClock >= secondsUntilPatrolResume ) {
						Echo("All clear.");
						freakout=false;
						turretClock=secondsUntilPatrolResume;
					} else {
						Echo("Enemy was detected. cooldown: "+turretClock+"/"+secondsUntilPatrolResume);
						freakout=true;
					}
				}
				//save our angles and stuff to Storage
				lastAzimuth=weapon.Azimuth;
				lastElevation=weapon.Elevation;
				this.Storage = turretClock+","+lastAzimuth+","+lastElevation;
				Echo ("Saved angles.");
				
				if (freakout) {
					// something has caused the turret to move. FREAK OUT.
					remote.SetAutoPilotEnabled(false);
					toggleGroup("ALERT",true);
				} else {
					if (!remote.IsAutoPilotEnabled) { remote.SetAutoPilotEnabled(true); }
					toggleGroup("ALERT",false);
				}
			}
		} else {
			throw new Exception("ERROR: No remotes found!");
		}
	}
}