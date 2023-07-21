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

		//@h@ Escort Vessel Script
		//SHIP REQUIREMENTS
		// a Remote Control (it'll be configured automatically.)
		// an Antenna (It'll be configured automatically, but make sure it's set to "Only Allied Broadcasts")
		// this script, loaded into a programmable block, and the block should have a space then a number at the end.
		// By default, the formation has ten points, so assign your ships to use 1-10 (not zero!)
		// so, name THIS BLOCK something like "Programmable Block - EscortRx Ship 4" or similar.
		
		// ship's name and antenna ranges will be modified! don't freak out about it.
		
		//known issues:
		// - formation does not rotate in relation to the mothership
		//   that's because everyone's games would die if I implemented it.
		// 

		//CONFIG
		string escortgroup = "Sentinel A";	// make sure this matches your mothership's transmitter script value


		List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
		List<Vector3D> formationPoints = new List<Vector3D>();
		IMyTerminalBlock antenna = null;

		public Program() // setup the sphere, find the antenna, some other first-run things...
		{
			Echo("FormationTemplateGen...");
			/*
			// 22-point geodesic sphere
			//Currently the ships in the formation don't all function when setup due to the per-user script execution limit.
			//also it's laggy as FUUUUUCK
			formationPoints.Add(new Vector3D(0, 0, -1));
			formationPoints.Add(new Vector3D(0.581108570098877, 0, -0.8138260245323181));
			formationPoints.Add(new Vector3D(0.1707366406917572, 0.5554603338241577, -0.8138260245323181));
			formationPoints.Add(new Vector3D(-0.4729212820529938, 0.3376871943473816, -0.8138260245323181));
			formationPoints.Add(new Vector3D(-0.4729212820529938, -0.3376871943473816, -0.8138260245323181));
			formationPoints.Add(new Vector3D(0.1707366406917572, -0.5554603338241577, -0.8138260245323181));
			formationPoints.Add(new Vector3D(0.9558632373809815, 0, -0.2938119471073151));
			formationPoints.Add(new Vector3D(0.2808440327644348, 0.913674533367157, -0.2938119471073151));
			formationPoints.Add(new Vector3D(-0.7779064178466797, 0.5554603338241577, -0.2938119471073151));
			formationPoints.Add(new Vector3D(-0.7779064178466797, -0.5554603338241577, -0.2938119471073151));
			formationPoints.Add(new Vector3D(0.2808440327644348, -0.913674533367157, -0.2938119471073151));
			formationPoints.Add(new Vector3D(0.9558632373809815, 0, 0.2938119471073151));
			formationPoints.Add(new Vector3D(0.2808440327644348, 0.913674533367157, 0.2938119471073151));
			formationPoints.Add(new Vector3D(-0.7779064178466797, 0.5554603338241577, 0.2938119471073151));
			formationPoints.Add(new Vector3D(-0.7779064178466797, -0.5554603338241577, 0.2938119471073151));
			formationPoints.Add(new Vector3D(0.2808440327644348, -0.913674533367157, 0.2938119471073151));
			formationPoints.Add(new Vector3D(0.581108570098877, 0, 0.8138260245323181));
			formationPoints.Add(new Vector3D(0.1707366406917572, 0.5554603338241577, 0.8138260245323181));
			formationPoints.Add(new Vector3D(-0.4729212820529938, 0.3376871943473816, 0.8138260245323181));
			formationPoints.Add(new Vector3D(-0.4729212820529938, -0.3376871943473816, 0.8138260245323181));
			formationPoints.Add(new Vector3D(0.1707366406917572, -0.5554603338241577, 0.8138260245323181));
			formationPoints.Add(new Vector3D(0, 0, 1));
			*/
			// 10-point pentagonal prism
			// might work a lot better than the sphere performance-wise
			formationPoints.Add(new Vector3D( -0.5877852439880371,-0.8090170025825501,-1 ));
			formationPoints.Add(new Vector3D( -0.5877852439880371,-0.8090170025825501,1  ));
			formationPoints.Add(new Vector3D( -0.9510565400123596,0.3090170025825501,-1  ));
			formationPoints.Add(new Vector3D( -0.9510565400123596,0.3090170025825501,1   ));
			formationPoints.Add(new Vector3D( 0,1,-1									 ));
			formationPoints.Add(new Vector3D( 0,1,1									  ));
			formationPoints.Add(new Vector3D( 0.5877852439880371,-0.8090170025825501,-1  ));
			formationPoints.Add(new Vector3D( 0.5877852439880371,-0.8090170025825501,1   ));
			formationPoints.Add(new Vector3D( 0.9510565400123596,0.3090170025825501,-1   ));
			formationPoints.Add(new Vector3D( 0.9510565400123596,0.3090170025825501,1	));
			
			// you can generate your own points for ships to use, just make sure the max radius for your
			// formation's template is no more than 1 in each direction, or you'll have ships going a lot
			// further than you like.
			
			Echo("Antenna...");
			GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);
			if (list.Count > 0)
			{
				antenna = list[0] as IMyTerminalBlock;
				antenna.SetValue("PBList", Me.EntityId);
			}
			else
			{
				Echo("ERROR: Antenna missing!");
			}
			Echo("Ready!");
		}

		// recieve coordinates, modify slightly, and then tell the remote control block to move there.
		void Main(string argument)
		{
			string[] argumentsplit = new string[] { "scriptpurpose", "0", "0", "0", "0", "0", "0" };
			argumentsplit = argument.Split(',');
			if (argumentsplit[0] == "ESCORT")
			{
				if (argumentsplit[1] == escortgroup)
				{
					string[] namesplit = Me.CustomName.Split(' ');
					int pointnumber = Convert.ToInt32(namesplit[namesplit.Length - 1]) - 1;
					if (pointnumber > formationPoints.Count)
					{
						pointnumber = 0;
						Echo("Warning: point number setting incorrect, using point #1 on sphere.\n	Append a space and then a number between 1 and " + (formationPoints.Count + 1) + "to the end of this programmable block's name.");
					} else {
						Echo("Point setting OK.");
					}
					Echo("Pn: " + pointnumber);
					Me.CubeGrid.CustomName = "Escort " + escortgroup + "-" + (pointnumber + 1);
					Vector3D mothership = new Vector3D(0, 0, 0);
					mothership.X = Convert.ToDouble(argumentsplit[2]);
					mothership.Y = Convert.ToDouble(argumentsplit[3]);
					mothership.Z = Convert.ToDouble(argumentsplit[4]);
					double distance = Convert.ToDouble(argumentsplit[5]);
					double fuzziness = Convert.ToDouble(argumentsplit[6]);
					Echo("Di: " + distance);
					Echo("Fu: " + fuzziness);
					Vector3D offset = Vector3D.Multiply(formationPoints[pointnumber], distance);
					Vector3D destination = Vector3D.Add(mothership, offset);
					GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list);
					if (list.Count > 0)
					{
						var remote = list[0] as IMyRemoteControl;
						if (Vector3D.DistanceSquared(remote.Position, destination) < fuzziness * fuzziness)
						{
							remote.SetAutoPilotEnabled(false);
							Echo("C: Hold");
						}
						else
						{
							remote.SetValueBool("CollisionAvoidance", false); // DISABLED until keen fixes their fucking shit
							remote.SetValue<Int64>("FlightMode", 2);
							remote.ClearWaypoints();
							remote.AddWaypoint(destination, "Escort Position");
							remote.SetAutoPilotEnabled(true);
							Echo("C: Move, " + destination.ToString());
						}
					}
					else
					{
						Echo("ERROR: Remote control not found on ship. Script will not function.");
					}
					if (antenna != null)
					{
						antenna.SetValue("Radius", Convert.ToSingle( distance + 1200 )); // adjust this to something of your liking.
					}

				}
				else
				{
					Echo("escortgroup doesn't match, ignoring");
				}
			}
			else
			{
				Echo("Message not intended for this script. Ignoring it and continuing.");
			}
		}

#if DEBUG
	}
}
#endif