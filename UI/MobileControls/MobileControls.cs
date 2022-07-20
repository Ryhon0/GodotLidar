using Godot;
using System;

public class MobileControls : Control
{
	public override void _Ready()
	{
		if(!OS.HasTouchscreenUiHint()) QueueFree();
	}
}
