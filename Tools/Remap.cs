public static class RemapExtension
{
	public static double Remap(this double input, double inputMin, double inputMax, double min, double max)
	{
		return min + (input - inputMin) * (max - min) / (inputMax - inputMin);
	}
}