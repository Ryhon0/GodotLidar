using System;
using Godot;
using GodotOnReady.Attributes;

public partial class Player
{
	[OnReadyGet]
	Spatial LIDARContainer;

	[Export]
	PackedScene LIDARMeshScene;

	// The amount of points in a single multimesh
	// Smaller numbers means it takes less time to update the VBO
	const int LIDARSize = 10_000;
	const int MaxPoints = LIDARSize * 100;
	MultiMeshInstance CreateLIDARMesh()
	{
		var mesh = LIDARMeshScene.Instance() as MultiMeshInstance;

		// Duplicate the resource
		mesh.Multimesh = mesh.Multimesh.Duplicate() as MultiMesh;
		// Clear the multimesh
		mesh.Multimesh.InstanceCount = LIDARSize;
		mesh.Multimesh.VisibleInstanceCount = 0;

		LIDARContainer.AddChild(mesh);
		mesh.SetAsToplevel(true);
		mesh.GlobalTransform = Transform.Identity;

		return mesh;
	}

	void RemoveLIDARMeshes()
	{
		currentPoint = 0;
		foreach (Spatial m in LIDARContainer.GetChildren())
			m.QueueFree();
	}

	void setPoint(int idx, Transform trans, Color col)
	{
		int meshid = idx / LIDARSize;

		var childcount = LIDARContainer.GetChildCount();
		if (meshid >= childcount)
			CreateLIDARMesh();

		var mesh = (LIDARContainer.GetChild(meshid) as MultiMeshInstance).Multimesh;

		var pointid = idx % LIDARSize;
		mesh.VisibleInstanceCount = Mathf.Max(mesh.VisibleInstanceCount, pointid + 1);
		mesh.SetInstanceTransform(pointid, trans);
		mesh.SetInstanceColor(pointid, col);
	}

	int currentPoint = 0;

	[Export]
	float MaxRandomRotation = 10;
	[Export]
	int PointPerSecond = 250;
	void CircleScan(float delta)
	{
		if (scanning) return;

		int ps = (int)(PointPerSecond * delta);

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
	int fullScanSize = 75;
	bool scanning = false;
	async void FullScan()
	{
		if (scanning) return;
		scanning = true;
	}

	void PutPoint(Vector3 start, Vector3 end)
	{
		var spaceState = GetWorld().DirectSpaceState;

		var col = spaceState.IntersectRay(start, end, null, 1, true, false);

		if (col != null && col.Count > 0)
		{
			var hit = (Vector3)col["position"];
			var body = (Spatial)col["collider"];

			if (body is IScannable sc) sc.OnScan(start, hit);

			if (body.IsInGroup("Skip")) return;

			Transform trans = new Transform(Basis.Identity, hit);

			bool isEnemy = body.IsInGroup("Enemy");

			Color clr = isEnemy ? new Color(1, 0, 0) : new Color(0.2f, 0.2f, 0.2f);
			// Random color offset
			Random rand = new Random();
			const double maxOffset = 0.025;
			clr += new Color(
				(float)rand.NextDouble().Remap(0, 1, -maxOffset, maxOffset),
				(float)rand.NextDouble().Remap(0, 1, -maxOffset, maxOffset),
				(float)rand.NextDouble().Remap(0, 1, -maxOffset, maxOffset)
				);

			setPoint(currentPoint, trans, clr);

			currentPoint++;
			// Loop back to improve performance
			if (currentPoint == MaxPoints) currentPoint = 0;
		}
	}

	int fullScanProgress = 0;
	void PhysicsLidar(float delta)
	{
		if (!scanning)
		{
			if (Input.IsActionPressed("attack1"))
				CircleScan(delta);
			else if (Input.IsActionJustPressed("attack2"))
				FullScan();
		}
		else
		{
			int pointcount = Mathf.CeilToInt(PointPerSecond * delta);
			pointcount *= 15;

			for(int i=0; i<pointcount; i++)
			{
				int y = (fullScanProgress + i) / fullScanSize;
				int x = (fullScanProgress + i) % fullScanSize;

				Vector2 screenpos = new Vector2(
					x / (float)fullScanSize,
					y / (float)fullScanSize
				);
				screenpos *= GetTree().Root.Size;

				var start = Camera.ProjectRayOrigin(screenpos);
				var end = start + (Camera.ProjectRayNormal(screenpos) * 2000);

				PutPoint(start, end);
			}

			fullScanProgress += pointcount;
			if(fullScanProgress >= fullScanSize * fullScanSize)
			{
				fullScanProgress = 0;
				scanning = false;
			}
		}
	}
}