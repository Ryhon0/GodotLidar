using Godot;
using System;

[Tool]
public class JoystickArea : Control
{
	Joystick joy;
	public override void _Ready()
    {
        joy = GetNode<Joystick>("Joystick");
	
		if(!Engine.EditorHint) joy.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if(!IsVisibleInTree()) return;

		if(joy.touchid != -1) return;
		if(@event is InputEventScreenTouch touch)
		{
			if(touch.Pressed)
			{
				if (GetGlobalRect().HasPoint(touch.Position))
				{
					joy.RectGlobalPosition = touch.Position - joy.GetGlobalRect().Size / 2;
					joy.DragStart(touch.Index, touch.Position);
					joy.Visible = true;
				}
			}
		}
	}

	void OnDragStop()
	{
		joy.Visible = false;
	}
}
