void Main(string argument) {
	IMyTextPanel torplcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Torpedo LCD");
	torplcd.WritePublicText(" ~ Torpedo Launcher ~ \n"+argument);
}