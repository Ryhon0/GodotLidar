public static class RemapExtension
{
	public static double Remap(this double input, double inputMin, double inputMax, double min, double max)
	{
		return min + (input - inputMin) * (max - min) / (inputMax - inputMin);
	}

	public static float Remap(this float input, float inputMin, float inputMax, float min, float max)
	{
		return min + (input - inputMin) * (max - min) / (inputMax - inputMin);
	}
}