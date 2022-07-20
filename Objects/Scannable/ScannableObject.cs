using Godot;
using System;

public class ScannableObject : Spatial, IScannable
{
	[Signal]
	public delegate void Scanned();

	public void OnScan(Vector3 from, Vector3 to)
	{
		EmitSignal(nameof(Scanned));
	}
}