using Godot;
using System;
using GodotOnReady.Attributes;

public partial class Player : KinematicBody
{
	[OnReadyGet("Head")]
	public Spatial Head;
	[OnReadyGet]
	Spatial LidarRay;
	[OnReadyGet]
	MultiMeshInstance Points;
	[OnReadyGet]
	Camera Camera;

	float MoveSpeed = 7f;
	float Acceleration;
	float Gravity = 17.6f;
	float JumpHeight = 6;
	float AirAcceleration = 1;
	float FloorAcceleration = 5;

	float MouseSensitivity = 0.2f;
	float JoystickSensitivity = 3f;
	bool StopMomentum = true;
	float MaxSlopeAngle = 20;
	bool GroundContact = false;
	float ViewmodelSway = 1;
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
		OS.WindowFullscreen = true;

		Points.SetAsToplevel(true);
		Points.GlobalTransform = new Transform(Basis.Identity, Vector3.Zero);
		Points.Multimesh.InstanceCount = 1_000_000;

		FullScan();
	}

	public override void _Process(float delta)
	{
		if (Input.IsActionJustPressed("toggle_fly"))
			Fly = !Fly;

		var h = Input.GetActionStrength("look_r") - Input.GetActionStrength("look_l");
		var v = Input.GetActionStrength("look_up") - Input.GetActionStrength("look_dw");
		RotateCamera(h * JoystickSensitivity, v * -JoystickSensitivity);

		if (Input.IsActionPressed("attack1"))
			CircleScan(delta);

		if (Input.IsActionJustPressed("attack2"))
			FullScan();

		float angleStep = 2f;
		if (Input.IsActionJustReleased("scan_size_up"))
			MaxRandomRotation -= angleStep;
		else if (Input.IsActionJustReleased("scan_size_down")) MaxRandomRotation += angleStep;

		MaxRandomRotation = Mathf.Clamp(MaxRandomRotation, 2, 90);

		if (Input.IsActionJustPressed("restart"))
		{
			curentMesh = 0;
			Points.Multimesh.VisibleInstanceCount = 0;
		}

		if (Input.IsActionJustPressed("toggle_camera"))
		{
			Camera.CullMask ^= uint.MaxValue;
		}
	}

	[Export]
	float MaxRandomRotation = 10;
	void CircleScan(float delta)
	{
		if(scanning) return;

		int pps = 150;
		int ps = (int)(pps * delta);

		Random rand = new Random();

		for (int i = 0; i < ps; i++)
		{
			Vector2 rv = new Vector2(
				(float)rand.NextDouble().Remap(0, 1, -1, 1),
				(float)rand.NextDouble().Remap(0, 1, -1, 1)
				).Normalized() * (float)rand.NextDouble();
			rv *= MaxRandomRotation;

			LidarRay.RotationDegrees = new Vector3(rv.x, rv.y, 0);

			var start = LidarRay.GlobalTransform.origin;
			var end = start + (-LidarRay.GlobalTransform.basis.z * 200);

			PutPoint(start, end);
		}
	}

	[Export]
	int fullScanSize = 150;
	bool scanning = false;
	async void FullScan()
	{
		if(scanning) return;

		scanning = true;
		for (int y = 0; y < fullScanSize; y++)
		{
			for (int x = 0; x < fullScanSize; x++)
			{
				Vector2 screenpos = new Vector2(
					x / (float)fullScanSize,
					y / (float)fullScanSize
				);
				screenpos *= GetTree().Root.Size;

				var start = Camera.ProjectRayOrigin(screenpos);
				var end = start + (Camera.ProjectRayNormal(screenpos) * 2000);

				PutPoint(start, end);
			}
			await ToSignal(GetTree(), "idle_frame");
		}
		scanning = false;
	}

	int curentMesh = 0;
	void PutPoint(Vector3 start, Vector3 end)
	{
		var spaceState = GetWorld().DirectSpaceState;

		var col = spaceState.IntersectRay(start, end, null, 1, true, false);

		if (col != null && col.Count > 0)
		{
			var hit = (Vector3)col["position"];
			var body = (Spatial)col["collider"];

			Transform trans = new Transform(Basis.Identity, hit);

			Points.Multimesh.SetInstanceTransform(curentMesh, trans);
			bool isEnemy = body.IsInGroup("Enemy");
			
			Color clr = isEnemy ? new Color(1, 0, 0) : new Color(0.2f,0.2f,0.2f);
			// Random color offset
			Random rand = new Random();
			const double maxOffset = 0.025;
			clr += new Color(
				(float)rand.NextDouble().Remap(0,1,-maxOffset, maxOffset),
				(float)rand.NextDouble().Remap(0,1,-maxOffset, maxOffset),
				(float)rand.NextDouble().Remap(0,1,-maxOffset, maxOffset)
				);

			Points.Multimesh.SetInstanceColor(curentMesh, clr);

			Points.Multimesh.VisibleInstanceCount = Mathf.Max(curentMesh + 1, Points.Multimesh.VisibleInstanceCount);

			if (curentMesh >= Points.Multimesh.InstanceCount - 1) curentMesh = 0;
			else curentMesh++;
		}
	}

	public override void _PhysicsProcess(float delta)
	{
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

		Velocity = Velocity.LinearInterpolate(Direction * MoveSpeed, Acceleration * delta);

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

		Head.Rotation = new Vector3(Mathf.Clamp(Head.Rotation.x - Mathf.Deg2Rad(v), Mathf.Deg2Rad(-89), Mathf.Deg2Rad(89)),
			Head.Rotation.y, Head.Rotation.z);
	}
}
