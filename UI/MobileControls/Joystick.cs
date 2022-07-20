using Godot;
using System;

public class Joystick : TextureRect
{
	[Signal]
	public delegate void OnDragStart();
	[Signal]
	public delegate void OnDragEnd();

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

	[Export]
	public bool Rotate = false;
	[Export]
	public bool RotateThumb = false;
	[Export]
	public bool OverFlowDrag = false;
	[Export]
	public float OverFlowDragTreshold = 0.1f;

	public Vector2 JoystickPosition;
	public Vector2 JoystickPositionCapped;

	public int touchid = -1;
	TextureRect thumb;

	public override void _Ready()
	{
		thumb = GetNode<TextureRect>("Thumb");
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
		Drag(pos);
		EmitSignal(nameof(OnDragStart));
	}

	public void DragStop()
	{
		touchid = -1;

		RectRotation = 0;
		thumb.RectRotation = 0;

		thumb.RectPosition = (RectSize / 2) - (thumb.RectSize / 2);
		Input.ActionRelease(ActionX);
		Input.ActionRelease(ActionXNeg);
		Input.ActionRelease(ActionY);
		Input.ActionRelease(ActionYNeg);
		
		EmitSignal(nameof(OnDragEnd));
	}

	public void Drag(Vector2 pos)
	{
		thumb.RectPivotOffset = thumb.RectSize / 2;
		RectPivotOffset = RectSize / 2;

		RectRotation = 0;
		var gr = GetGlobalRect();
		pos -= gr.Position;
		pos /= gr.Size;
		pos = new Vector2(remap(pos.x, 0, 1, -1, 1), remap(pos.y, 0, 1, -1, 1));

		JoystickPosition = pos;
		if (pos.LengthSquared() > 1)
			pos = pos.Normalized();
		JoystickPositionCapped = pos;


		if (Rotate)
		{
			RectRotation = Mathf.Rad2Deg(pos.Rotated(Mathf.Deg2Rad(90)).Angle());

			thumb.RectPosition = new Vector2(0,-Mathf.Abs(pos.Length())) * (thumb.RectSize) + (thumb.RectSize/2);
			thumb.RectRotation = RotateThumb ? 0 : -RectRotation;
		}
		else
		{
			RectRotation = 0;
			thumb.RectPosition = (((pos + Vector2.One) / 2) * (gr.Size)) - (thumb.RectSize / 2);
			thumb.RectRotation = RotateThumb ? Mathf.Rad2Deg(pos.Rotated(Mathf.Deg2Rad(90)).Angle()) : 0;
		}

		if(OverFlowDrag)
		{
			var diff = JoystickPosition - JoystickPositionCapped;

			if(Mathf.Abs(diff.Length()) > OverFlowDragTreshold)
			{
				RectPosition += diff * GetGlobalRect().Size;
			}
		}

		if (JoystickPositionCapped.x > 0)
		{
			Input.ActionPress(ActionX, JoystickPositionCapped.x * ActionScale.x);
		}
		else
		{
			Input.ActionPress(ActionXNeg, -JoystickPositionCapped.x * ActionScale.x);
		}

		if (JoystickPositionCapped.y > 0)
		{
			Input.ActionPress(ActionY, JoystickPositionCapped.y * ActionScale.y);
		}
		else
		{
			Input.ActionPress(ActionYNeg, -JoystickPositionCapped.y * ActionScale.y);
		}
	}

	float remap(float value, float InputA, float InputB, float OutputA, float OutputB)
	{
		return (value - InputA) / (InputB - InputA) * (OutputB - OutputA) + OutputA;
	}
}
