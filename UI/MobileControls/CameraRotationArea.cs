using Godot;
using System;

public class CameraRotationArea : Control
{
	[Export]
	public string ActionX;
	[Export]
	public string ActionY;
	[Export]
	public string ActionXNeg;
	[Export]
	public string ActionYNeg;
	[Export]
	public Vector2 ActionScale { get; set; } = new Vector2(1, 1);


	public int touchid = -1;
	Vector2 lastPosition = new Vector2();

	public override void _Process(float delta)
	{
		base._Process(delta);
		Input.ActionRelease(ActionX);
		Input.ActionRelease(ActionXNeg);
		Input.ActionRelease(ActionY);
		Input.ActionRelease(ActionYNeg);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventScreenTouch tap)
		{
			if (touchid == -1)
			{
				if (tap.Pressed && IsVisibleInTree())
				{
					if (GetGlobalRect().HasPoint(tap.Position))
					{
						DragStart(tap.Index, tap.Position);
						GetTree().SetInputAsHandled();
					}
				}
			}
			else
			{
				if (touchid == tap.Index)
				{
					DragStop();
					GetTree().SetInputAsHandled();
				}
			}
		}
		else if (@event is InputEventScreenDrag drag)
		{
			if (drag.Index == touchid)
			{
				Drag(drag.Position);
				GetTree().SetInputAsHandled();
			}
		}
	}

	public void DragStart(int id, Vector2 pos)
	{
		touchid = id;
		lastPosition = pos;
	}

	public void DragStop()
	{
		touchid = -1;
	}

	public void Drag(Vector2 pos)
	{
		var diff = lastPosition - pos;
		if (diff.x > 0)
		{
			Input.ActionPress(ActionX, diff.x * ActionScale.x);
		}
		else
		{
			Input.ActionPress(ActionXNeg, -diff.x * ActionScale.x);
		}

		if (diff.y > 0)
		{
			Input.ActionPress(ActionY, diff.y * ActionScale.y);
		}
		else
		{
			Input.ActionPress(ActionYNeg, -diff.y * ActionScale.y);
		}

		lastPosition = pos;
	}
}
