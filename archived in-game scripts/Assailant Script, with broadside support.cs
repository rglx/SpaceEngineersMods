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

// vanilla assailant script, modified by DJ Arghlex intended for use on larger, more sturdier and meaner vessels than the Incisor or the Assailant.

// INSTALLATION
// - Install a remote control block, a timer block, and a programmable block with this script loaded on your ship.
// - Remote control needs to be facing forward, and its Patrol Direction set to One Way, and (while reccommended, not neccessary) turn on Collision Avoidance.
// - Timer block should have a delay set to one second, and set to run this programmable block (with its default argument) and "Start" itself.
// - Transfer all three blocks (and optionally the rest of your ship) to the Space Pirate faction.

// FEATURES (and changes from vanilla Assailant script)
// - Lurks 500m away from the player (rather than plowing into the player at full power)
// - Ignores the fact that it may have no working/ammo-filled turrets (and won't retreat when such a case presents itself)
// - When within 1km, ship can face a side to the player (for broadside-mounted weaponry etc)
// - Extra error-checking code to help players create new spacecraft using this script (and the vanilla one)
// - Hunt down players inside 50km instead of 20km

// TODO
// - broadside flight-control *DONE!*
// - fixed weaponry support *On hold until rumored LiDAR is implemented for Ingame API*
// - intermittently triggering timer blocks (for torpedo launchers etc)

// CONFIG
// - Implies that your remote control faces forward by default.
// - Set to "Forward" to disable. Valid values shown in Remote Control's Control Panel.
string broadsideDirection = "Left";

List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
void Main(string argument) {
	Vector3D origin = new Vector3D(0, 0, 0);
	if (this.Storage == null || this.Storage == "") {
		origin = Me.GetPosition();
		this.Storage = origin.ToString();
	} else {
		Vector3D.TryParse(this.Storage, out origin);
	}
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);
	if (list.Count > 0) {
		var remote = list[0] as IMyRemoteControl;
		remote.ClearWaypoints();
		Vector3D player = new Vector3D(0, 0, 0);
		bool success = remote.GetNearestPlayer(out player);
		if (success) {
			bool gotoOrigin = false;
			if (Vector3D.DistanceSquared(player, origin) > 50000 * 50000) {
				gotoOrigin = true;
			}
			if (gotoOrigin) {
				remote.AddWaypoint(origin, "Origin");
			} else {
				remote.AddWaypoint(player, "Player");
			}
			if (Vector3D.DistanceSquared(remote.GetPosition(), player) < 1000 * 1000) {
				remote.ApplyAction(broadsideDirection);
			} else {
				remote.ApplyAction("Forward");
			}
			if (Vector3D.DistanceSquared(remote.GetPosition(), player) < 500 * 500) { // keep 500m away from the player
				remote.SetAutoPilotEnabled(false);
			} else {
				remote.SetAutoPilotEnabled(true);
			}
		} else {
			Echo("Ship (at least the remote control, timer block, and this programmable block) must be owned by Space Pirates faction.");
		}
	}
}

#if DEBUG
    }
}
#endif