using Godot;

public static class TimerIsOver
{
	public static bool SuppressOneshotWarnings = false;

	public static bool IsOver(this Timer t)
	{
		if (!t.OneShot)
		{
#if DEBUG
			if (!SuppressOneshotWarnings)
				GD.PrintErr($"Property OneShot in timer {t.Name}({t.GetPath()}) is set to false - " +
							"`Timer.IsOver()` will always return false");
#endif
			return false;
		}

		return t.TimeLeft == 0f;
	}
}
