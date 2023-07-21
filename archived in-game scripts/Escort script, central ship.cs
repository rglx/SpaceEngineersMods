//@h@ Mothership Script

// SHIP REQUIREMENTS
// an Antenna, with its range at least double the distance set in the config section just below.
// Run the script no more than once every 2 or three seconds. Use a timer block.

// CONFIG
int distance = 200; 		// radius of sphere escort ships will generate and follow
int fuzziness = 15; 		// how many meters from their waypoints can the escort ships consider "in position"
string escortgroup = "Sentinel A"; 	// name of escort group you want to escort you (set on drone ships' receiver scripts as well)

IMyRadioAntenna transmitter = null;

public Program()
{
    List<IMyRadioAntenna> list = new List<IMyRadioAntenna>();
    GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list);
    if (list.Count > 0)
    {
        transmitter = list[0] as IMyRadioAntenna;
        Echo("Initialized.");
        Echo("An:" + transmitter.CustomName);
    }
    else
    {
        throw new Exception("No antennas on ship!");
    }
}
void Main(string argument)
{
    string message = "ESCORT," + escortgroup + "," + Math.Floor(transmitter.GetPosition().X) + "," + Math.Floor(transmitter.GetPosition().Y) + "," + Math.Floor(transmitter.GetPosition().Z) + "," + distance + "," + fuzziness;
    transmitter.TransmitMessage(message, MyTransmitTarget.Default); //only broadcast to allied/owned antennas by default.
    Echo("Di:" + distance);
    Echo("Fu:" + fuzziness);
    Echo("Me:" + message);
    Echo("An:" + transmitter.CustomName);

}