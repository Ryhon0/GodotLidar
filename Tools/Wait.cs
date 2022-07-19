using System.Threading.Tasks;
using Godot;

public static class WaitClass
{
	public static SignalAwaiter Wait(this Node n, float time)
	{
		return n.ToSignal(n.GetTree().CreateTimer(time), "timeout");
	}
}