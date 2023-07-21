// DJ Arghlex's Turret-Guided Torpedo, v4

// FEATURES
// - Target acquisition by way of an on-board turret. Once fired, completely independent.
// - precise aiming
// - minimal involvement on the torpedo, so as to minimize interference with other systems.

// INSTALLATION
// - install a turret and a gyroscope so they both face the same direction, and upwards is the same direction for them.
// - configure the rest of your torpedo's launch mechanisms (forward thrusters, dampener-thrusters, battery, auto-destruct timer, launch sequencing)
// - turn off Idle Movement on the turret. this is important.
// - make sure this script is being run with a timer block set to "Trigger Now" itself (or using Digi's Loop Computers to set it to "run every tick") when it's launched

// CONFIG
string turretName = "BEAM";	// Name your turret this.
string gyroName = "GYRO";	// Name your gyroscope this.
float precision = 0.07F;	// Don't set too low or the torpedo will wiggle. Too high and it won't hit what it's trying to hit.
int interval = 3;			// Run the script every this many ticks. Don't raise this unless you have to.

// DO NOT TOUCH ANYTHING BELOW THIS LINE.

// internal variables
IMyLargeTurretBase b;
IMyTerminalBlock g;
float lastAzimuth = 0;
float lastElevation = 0;

//moved all our block acquisitions so as to lighten engine load
public Program() {
	b = (IMyLargeTurretBase)GridTerminalSystem.GetBlockWithName(turretName);
	g = GridTerminalSystem.GetBlockWithName(gyroName) as IMyTerminalBlock;
	if (b == null){
		Echo("ERROR! Name your guidance turret "+turretName);
	}
	if (g == null) {
		Echo("ERROR! Name your gyroscope "+gyroName);
	}
}


void setGyro(string t, int v) {
	if (((IMyGyro)g).GyroOverride != true) {
		g.GetActionWithName("Override").Apply(g);
	}
	float av=((float)v)*40F; //hardcoded because it stopped making sense to horse around with different values.
	g.SetValueFloat(t,av);
	Echo(gyroName+"/"+t+": "+v);
}

void AimAtTarget() {
	Echo(b.Azimuth+"/"+b.Elevation);
	if ( lastAzimuth != b.Azimuth || lastElevation != b.Elevation ) {
		// our turret's angles changed. point us at the target.
		if (b.Azimuth >= precision) {
			setGyro("Yaw",-1);
		} else if (b.Azimuth <= (float)(-1*precision)) {
			setGyro("Yaw",1);
		} else {
			setGyro("Yaw",0);
		}
		if (b.Elevation >= precision) {
			setGyro("Pitch",1);
		} else if (b.Elevation <= (float)(-1*precision)) {
			setGyro("Pitch",-1);
		} else{
			setGyro("Pitch",0);
		}
		//setGyro("Roll",1); // does not work. completely breaks turret's targetting. do not reenable
	} else {
		//turret angles didn't change. lock out our gyro so we don't whirl around.
		setGyro("Pitch",0);
		setGyro("Roll",0);
		setGyro("Yaw",0);
	}
	lastAzimuth=b.Azimuth;
	lastElevation=b.Elevation;
}

//decides wether we want to run the program every tick or not.
int clock=0;
void Main() {
	if (clock % interval == 0) {
		AimAtTarget();
		clock=0;
	}
	clock=clock+1;
}