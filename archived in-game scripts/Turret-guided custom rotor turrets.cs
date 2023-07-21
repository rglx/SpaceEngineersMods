// DJ Arghlex's Turret-Guided Custom Rotor Turret Script, version 3

// HISTORY
// v1: most basic implementation
// v2: added turret zeroing, and LCD-based state saving
// v3: tossed the LCD in favor of storing to the programmable block

// INSTALLATION
// - Azimuth and Elevation rotors making sure their default positions are both at 0 degrees
// - Turret on the same grid as the guns themselves, with its barrel lining up with theirs
// - Guns need to be grouped to a group named below, with their power OFF and their Shoot toggle ON
// - Adjust precisions and intervals to your liking.

// TODO
// - Optimize block-fetching code to do it only when the script is first ran
// - Come up with, and implement a way to make the turrets not fire their guns
//	  for the first 'idletickmax' ticks when they're pasted in or the game is loaded
// - future rumored LiDAR support????

// IGNORE THIS CRAP
#if DEBUG
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers
{
	public sealed class Program : MyGridProgram
	{
#endif



		// CONFIG
		// block names, prefixed with contents of Argument Box (an EXACT prefix, so include
		//	any underscores and dashes in your naming scheme in the Argument Box!)
		string blockPrefix = "Turret_";     // prefix all the names of your blocks with this.
		string argumentOverride = null;
		// for instance, your turret's inverted pitch rotor would be named "Turret1_Pitch_Inv"
		string targetBeamName = "Beam";         // turret that points forward on turret barrel
		string rotorPitchName = "Pitch";        // up-down pivoting rotor
		string rotorYawName = "Yaw";            // left-right pivoting rotor
		string fireGroupName = "Fire";          // group of things to turn on and off
		//string fireSoundName = "FireSound";         // sound block to play and stop when the guns are firing
		string aimingGroupName = "Aiming";          // block group turn on and off when the turret is trying to aim
		string rotorInvertedString = "_Inv";        // suffix to invert a companion rotor's rotation angles (still requires a normally-aligned rotor)

		// numbers
		float precision = 0.02F;            // how precise do you want the turrets (in radians) (don't set it too low or your guns will wiggle and never fire!)
		float fireMult = 1.5F;              // multiplier for when we actually fire the guns
		int interval = 5;                   // run the main body of the script every X ticks. if you use Digi's Loop Computers, set to 1 and use the slider
		int idletickmax = 10;               // if beam holds still for this long*interval, rotate the turrets to 0a,0e
		int rotorMult = 1;                  // Number of times to apply rotor velocity action 
		float zeroPrecision = 0.04F;        // what's considered zeroed in terms of the rotor's angles in Radians

		// DO NOT MODIFY PAST THIS POINT
		// mostly because it's really kludgy and horrible

		//required stuff
		IMyMotorStator pitchRotor = null;
		IMyMotorStator yawRotor = null;
		IMyLargeTurretBase targetBeam = null;
		List<IMyTerminalBlock> fireGroupBlocks = new List<IMyTerminalBlock>();

		//optional stuff
		IMyMotorStator pitchRotorInv = null;
		IMyMotorStator yawRotorInv = null;
		List<IMyTerminalBlock> aimingGroupBlocks = new List<IMyTerminalBlock>();
		//IMySoundBlock fireSoundBlock = null; // until we find a way to determine wether sound is playing on a block, this won't work at all.

		int clock = 0;
		int turretClock = 0;
		float lastAzimuth = 0;
		float lastElevation = 0;
		float firePrecision = 0;

		public Program()
		{
			// storage allocation/setup
			if (this.Storage != "")
			{ // we have *something* in our block's storage...
				string[] storagesplit = new string[3];
				storagesplit = this.Storage.Split(',');
				turretClock = Convert.ToInt32(storagesplit[0]);
				lastAzimuth = (float)Convert.ToDouble(storagesplit[1]);
				lastElevation = (float)Convert.ToDouble(storagesplit[2]);
				Echo("Notice: loaded saved values!");
			}
			else
			{ // storage is null or empty string, reset everything!
				lastAzimuth = 0;
				lastElevation = 0;
				turretClock = 0;
				this.Storage = "0,0,0";
				Echo("Warn: Reset!");
			}
			// some math to get us on the right foot
			this.firePrecision = fireMult * precision;

			// begin allocating all our blocks and groups, and checking them for problems
			yawRotorInv = GridTerminalSystem.GetBlockWithName(blockPrefix + rotorYawName + rotorInvertedString) as IMyMotorStator;
			pitchRotorInv = GridTerminalSystem.GetBlockWithName(blockPrefix + rotorPitchName + rotorInvertedString) as IMyMotorStator;

			yawRotor = GridTerminalSystem.GetBlockWithName(blockPrefix + rotorYawName) as IMyMotorStator;
			pitchRotor = GridTerminalSystem.GetBlockWithName(blockPrefix + rotorPitchName) as IMyMotorStator;

			//fireSoundBlock = GridTerminalSystem.GetBlockWithName(blockPrefix + fireSoundName) as IMySoundBlock; // until we find a way to determine wether sound is playing on a block, this won't work at all.
			targetBeam = GridTerminalSystem.GetBlockWithName(blockPrefix + targetBeamName) as IMyLargeTurretBase;

			// aimgroup
			var blockGroup = this.GridTerminalSystem.GetBlockGroupWithName(blockPrefix + aimingGroupName);
			if (blockGroup != null)
			{
				blockGroup.GetBlocks(aimingGroupBlocks);
			}
			else
			{
				Echo("Warn: Group " + blockPrefix + aimingGroupName + " not found.");
			}

			// firegroup
			blockGroup = null;
			blockGroup = this.GridTerminalSystem.GetBlockGroupWithName(blockPrefix + fireGroupName);
			if (blockGroup != null)
			{
				blockGroup.GetBlocks(fireGroupBlocks);
			}
			else
			{
				throw new Exception("ERROR: Group " + blockPrefix + fireGroupName+" not found.");
			}
			blockGroup = null;
			//if (fireSoundBlock == null) { Echo("Warn: FireSoundblock missing: " + blockPrefix + fireSoundName); } // until we find a way to determine wether sound is playing on a block, this won't work at all.
			if (targetBeam == null) { 		throw new Exception("ERROR: Turret missing: " + blockPrefix + targetBeamName); }

			if (yawRotor == null) { Echo("Warn: yawRotor missing: " + blockPrefix + rotorYawName); }
			if (pitchRotor == null) { Echo("Warn: pitchRotor missing: " + blockPrefix + rotorPitchName); }
			if (yawRotorInv == null) { 		Echo("Warn: yawRotorInv missing: " + blockPrefix + rotorYawName + rotorInvertedString); }
			if (pitchRotorInv == null) { 	Echo("Warn: pitchRotorInv missing: " + blockPrefix + rotorPitchName + rotorInvertedString); }

			if (yawRotor == null && yawRotorInv == null) { throw new Exception("ERROR: No Yaw rotors found! Recompile when corrected!"); }
			if (pitchRotor == null && pitchRotorInv == null) { throw new Exception("ERROR: No Pitch rotors found! Recompile when corrected!"); }
			Echo("Notice: Systems ready!");
		}

		// put our clock and lastangles into storage so no bad shit happens if we reload the program, i.e. shooting ourself
		public void Save()
		{
			Storage = turretClock + "," + lastAzimuth + "," + lastElevation;
		}

		// from gFleka's rotor-turret script
		// TODO: streamline so we're not doing two fucking rounds of this
		void setRotor(IMyMotorStator b, IMyMotorStator binv, int velocity)
		{
			if (b == null && binv == null)
			{
				throw new Exception("Attempted to adjust null rotor set! Please check your rotors' names!");
			}
			else
			{
				if (b != null)
				{
					ITerminalAction a = null;

					// determine our direction
					if (velocity == 1)
					{
						a = b.GetActionWithName("IncreaseVelocity");
					}
					else if (velocity == -1)
					{
						a = b.GetActionWithName("DecreaseVelocity");
					}
					else if (velocity == 0)
					{
						a = b.GetActionWithName("ResetVelocity");
					}

					// reset rotor velocity before changing the velocity
					ITerminalAction neutral = null;
					neutral = b.GetActionWithName("ResetVelocity");
					neutral.Apply(b);

					// apply velocity adjustments
					for (int i = 0; i < rotorMult; ++i)
					{
						a.Apply(b);
					}

					Echo("R/" + b.CustomName + ":" + velocity);
				}
				else
				{
					Echo("Warn: Non-inverted rotor not found.");
				}

				//repeat on inverse rotor
				if (binv != null)
				{
					ITerminalAction ainv = null;

					// determine our direction
					if (velocity == -1)
					{
						ainv = binv.GetActionWithName("IncreaseVelocity");
					}
					else if (velocity == 1)
					{
						ainv = binv.GetActionWithName("DecreaseVelocity");
					}
					else if (velocity == 0)
					{
						ainv = binv.GetActionWithName("ResetVelocity");
					}

					// reset rotor velocity before changing the velocity
					ITerminalAction neutralinv = null;
					neutralinv = binv.GetActionWithName("ResetVelocity");
					neutralinv.Apply(binv);

					// apply velocity adjustments
					for (int i = 0; i < rotorMult; ++i)
					{
						ainv.Apply(binv);
					}

					Echo("R/" + binv.CustomName + ":" + velocity * -1);
				}
				else
				{
					Echo("Warn: Inverted rotor not found.");
				}
			}
		}

		void toggleGroup(List<IMyTerminalBlock> groupBlocks, bool state)
		{
			if (groupBlocks.Count > 0)
			{
				for (int i = 0; i < groupBlocks.Count; i++)
				{
					IMyTerminalBlock block = groupBlocks[i];
					if (state == true)
					{
						block.GetActionWithName("OnOff_On").Apply(block);
					}
					else if (state == false)
					{
						block.GetActionWithName("OnOff_Off").Apply(block);
					}
				}
			} else {
				Echo("Blockgroup is empty!");

			}
		}

		void AimAtTarget(string argument)
		{
			bool doAiming = false;
			if (lastAzimuth != targetBeam.Azimuth && lastElevation != targetBeam.Elevation)
			{
				// beam has moved since last tick, do aiming
				turretClock = 0;
				doAiming = true;
			}
			else
			{
				// beam hasn't moved...
				turretClock = turretClock + 1;
				if (turretClock >= idletickmax)
				{
					// .. and it's been awhile since it has, so don't aim, just shutdown and zero out the angles on the rotor
					doAiming = false;
					turretClock = idletickmax;
				}
				else
				{
					// ... but it hasn't been very long, so keep aiming.
					doAiming = true;
				}
			}
			Echo("Aim: " + doAiming);
			toggleGroup(aimingGroupBlocks, doAiming); // group of spotlights or lights or beacons or something idk

			if (doAiming)
			{ // we're supposed to be aiming, so actuate our rotors and do firing checks.
			  //azimuth/yaw
				if (targetBeam.Azimuth > precision)
				{
					setRotor(yawRotor, yawRotorInv, -1);
				}
				else if (targetBeam.Azimuth < (float)(-1 * precision))
				{
					setRotor(yawRotor, yawRotorInv, 1);
				}
				else
				{
					setRotor(yawRotor, yawRotorInv, 0);
				}

				//elevation/pitch
				if (targetBeam.Elevation > precision)
				{
					setRotor(pitchRotor, pitchRotorInv, 1);
				}
				else if (targetBeam.Elevation < (float)(-1 * precision))
				{
					setRotor(pitchRotor, pitchRotorInv, -1);
				}
				else
				{
					setRotor(pitchRotor, pitchRotorInv, 0);
				}

				// if we're inside our firing cone, fire the guns
				if (
					targetBeam.Elevation > (float)(-1 * firePrecision) &&   // within elevation
					targetBeam.Elevation < firePrecision &&
					targetBeam.Azimuth > (float)(-1 * firePrecision) && // within pitch
					targetBeam.Azimuth < firePrecision &&
					targetBeam.IsWorking == true // the target beam also needs to be ON!
				)
				{
					// all conditions met! fire!
					Echo("Firing!");
					toggleGroup(fireGroupBlocks, true);
					//if (fireSoundBlock.IsPlaying != true) { // until we find a way to determine wether sound is playing on a block, this won't work at all.
					//	fireSoundBlock.Play();
					//}
				}
				else
				{
					// we don't meet all the requirements, stopping the guns and sound.
					Echo("Not firing.");
					toggleGroup(fireGroupBlocks, false);
					//fireSoundBlock.Stop(); // until we find a way to determine wether sound is playing on a block, this won't work at all.
				}
			}
			else
			{
				//the rotor has held still long enough, zero out the rotors.
				if (yawRotor.Angle > zeroPrecision)
				{
					setRotor(yawRotor, yawRotorInv, -1);
				}
				else if (yawRotor.Angle < (-1 * zeroPrecision))
				{
					setRotor(yawRotor, yawRotorInv, 1);
				}
				else
				{
					setRotor(yawRotor, yawRotorInv, 0);
				}

				if (pitchRotor.Angle > zeroPrecision)
				{
					setRotor(pitchRotor, pitchRotorInv, -1);
				}
				else if (pitchRotor.Angle < (-1 * zeroPrecision))
				{
					setRotor(pitchRotor, pitchRotorInv, 1);
				}
				else
				{
					setRotor(pitchRotor, pitchRotorInv, 0);
				}

				// ...and turn off the guns if they're on, and stop the sound
				toggleGroup(fireGroupBlocks, false);
				//fireSoundBlock.Stop(); // until we find a way to determine wether sound is playing on a block, this won't work at all.
			}
			//save our data
			lastAzimuth = targetBeam.Azimuth;
			lastElevation = targetBeam.Elevation;
			this.Storage = turretClock + "," + targetBeam.Azimuth + "," + targetBeam.Elevation;
		}

		//main loop
		void Main(string argument)
		{
			if (argument == "reset")
			{ // hard resetting the block
				this.Storage = "0,0,0";
				lastAzimuth = 0;
				lastElevation = 0;
				turretClock = 0;
			}
			if (argumentOverride != null)
			{
				argument = argumentOverride;
			}
			if (clock % interval == 0)
			{
				AimAtTarget(argument);
				clock = 0;
			}
			clock = clock + 1;
		}



#if DEBUG
	}
}
#endif