using Godot;
using System;
using GodotOnReady.Attributes;

public partial class Player : KinematicBody
{
	[OnReadyGet]
	public Spatial Head;
	[OnReadyGet]
	Spatial Hand;
	[OnReadyGet]
	Camera Camera;

	float MoveSpeed = 4f;
	float Acceleration;
	float Gravity = 17.6f;
	float JumpHeight = 5.75f;
	float AirAcceleration = 1;
	float FloorAcceleration = 5;

	float MouseSensitivity = 0.2f;
	float JoystickSensitivity = 3f;
	bool StopMomentum = true;
	float MaxSlopeAngle = 20;
	bool GroundContact = false;
	float ViewmodelSway = 2f;
	bool InAir = false;

	public Vector3 Direction;
	public Vector3 Velocity;
	public Vector3 GravityVector;
	public Vector3 Movement;

	public bool Fly = false;

	[OnReady]
	void Ready()
	{
		Input.SetMouseMode(Input.MouseMode.Captured);
#if (!DEBUG)
			OS.WindowFullscreen = true;
#endif

		FullScan();
	}

	public override void _Process(float delta)
	{
		if (Input.IsActionJustPressed("toggle_fly"))
			Fly = !Fly;

		var h = Input.GetActionStrength("look_r") - Input.GetActionStrength("look_l");
		var v = Input.GetActionStrength("look_up") - Input.GetActionStrength("look_dw");
		RotateCamera(h * JoystickSensitivity, v * -JoystickSensitivity);

		float angleStep = 2f;
		if (Input.IsActionJustReleased("scan_size_up"))
			MaxRandomRotation -= angleStep;
		else if (Input.IsActionJustReleased("scan_size_down")) MaxRandomRotation += angleStep;

		MaxRandomRotation = Mathf.Clamp(MaxRandomRotation, 2, 90);

		Hand.Rotation = new Vector3(
			Mathf.LerpAngle(Hand.Rotation.x, 0, ViewmodelSway * delta * 10),
			Mathf.LerpAngle(Hand.Rotation.y, 0, ViewmodelSway * delta * 10),
			0);

		Lines.Rotation = -Hand.Rotation;

		if (Input.IsActionJustPressed("restart"))
			RemoveLIDARMeshes();

		if (Input.IsActionJustPressed("toggle_camera"))
		{
			Camera.CullMask ^= uint.MaxValue;
		}
	}

	public override void _PhysicsProcess(float delta)
	{
		PhysicsLidar(delta);

		Direction = Vector3.Zero;

		InAir = !IsOnFloor();

		if (!Fly)
		{
			Acceleration = IsOnFloor() ? FloorAcceleration : AirAcceleration;

			if (Input.IsActionJustPressed("jump") && IsOnFloor())
				Jump();
		}
		else
		{
			InAir = true;
			Acceleration = FloorAcceleration;
			GravityVector = Velocity;
		}

		var DirectionNotRotated = new Vector3(Input.GetActionStrength("move_r") - Input.GetActionStrength("move_l"),
		0, Input.GetActionStrength("move_fw") - Input.GetActionStrength("move_bw"));

		var DirNode = Fly ? Head : this;
		Direction -= Input.GetActionStrength("move_fw") * DirNode.GlobalTransform.basis.z;
		Direction += Input.GetActionStrength("move_bw") * DirNode.GlobalTransform.basis.z;
		Direction -= Input.GetActionStrength("move_l") * DirNode.GlobalTransform.basis.x;
		Direction += Input.GetActionStrength("move_r") * DirNode.GlobalTransform.basis.x;
		if (Direction.LengthSquared() > 1)
			Direction = Direction.Normalized();

		if (StopMomentum)
		{
			var v = Velocity;
			v.y = 0;
			v = v.Normalized();

			if (Direction.Dot(v) <= -0.8f)
				Velocity = Vector3.Zero;
		}

		var acutalSepeed = Input.IsActionPressed("attack1") || scanning ?
			MoveSpeed / 3 :
			MoveSpeed;

		Velocity = Velocity.LinearInterpolate(Direction * acutalSepeed, Acceleration * delta);

		if (IsOnFloor() && !InAir || IsOnCeiling() && InAir) GravityVector = Vector3.Zero;
		else GravityVector.y -= Gravity * delta;

		Movement = Velocity + GravityVector;
		Movement = InAir ? MoveAndSlide(Movement, Vector3.Up, true) : MoveAndSlideWithSnap(Movement, -GetFloorNormal(), Vector3.Up, true);
	}

	void Jump()
	{
		InAir = true;
		GravityVector = Vector3.Up * JumpHeight;
		GroundContact = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion m)
		{
			RotateCamera(m.Relative.x * MouseSensitivity, m.Relative.y * MouseSensitivity);
		}

		if (OS.GetName() == "HTML5" && @event is InputEventMouseButton mb)
		{
			OS.WindowFullscreen = true;
			Input.SetMouseMode(Input.MouseMode.Captured);
		}
	}

	void RotateCamera(float h, float v)
	{
		RotateY(Mathf.Deg2Rad(-h));

		Hand.Rotation = new Vector3(
			Mathf.Clamp(Hand.Rotation.x + Mathf.Deg2Rad(v / 5) * ViewmodelSway, Mathf.Deg2Rad(-15), Mathf.Deg2Rad(15)),
			Mathf.Clamp(Hand.Rotation.y + Mathf.Deg2Rad(h / 5) * ViewmodelSway, Mathf.Deg2Rad(-15), Mathf.Deg2Rad(15)), 0);

		Lines.Rotation = -Hand.Rotation;

		Head.Rotation = new Vector3(Mathf.Clamp(Head.Rotation.x - Mathf.Deg2Rad(v), Mathf.Deg2Rad(-89), Mathf.Deg2Rad(89)),
			Head.Rotation.y, Head.Rotation.z);
	}
}
