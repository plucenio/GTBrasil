using GTBrasil.Models;
using Microsoft.Maui.Graphics;

namespace GTBrasil;

public sealed class InterlagosTrackDrawable : IDrawable
{
	private const float BaseSpeed = 105f;
	private const float TrackMinX = 145f;
	private const float TrackMaxX = 855f;
	private const float TrackMinY = 90f;
	private const float TrackMaxY = 515f;

	private static readonly CircuitPoint[] Circuit =
	[
		new(790, 468, false),
		new(700, 454, false),
		new(610, 438, false),
		new(520, 421, false),
		new(455, 407, true),
		new(407, 382, true),
		new(388, 348, true),
		new(410, 319, true),
		new(463, 300, false),
		new(535, 294, false),
		new(610, 284, false),
		new(674, 262, true),
		new(723, 232, true),
		new(744, 195, true),
		new(721, 163, true),
		new(674, 148, false),
		new(597, 147, false),
		new(520, 155, false),
		new(445, 174, false),
		new(376, 201, true),
		new(326, 239, true),
		new(300, 282, true),
		new(318, 321, true),
		new(365, 344, false),
		new(431, 353, false),
		new(494, 367, true),
		new(522, 395, true),
		new(505, 427, true),
		new(527, 455, true),
		new(581, 474, false),
		new(653, 486, false),
		new(724, 491, true),
		new(776, 483, true)
	];

	private readonly float[] segmentLengths;
	private readonly float trackLength;
	private float distance;

	public TrackPaceWeights PaceWeights { get; } = new(5, 3, 2, 2);

	public InterlagosTrackDrawable()
	{
		segmentLengths = new float[Circuit.Length];
		for (var i = 0; i < Circuit.Length; i++)
		{
			segmentLengths[i] = Distance(Circuit[i], Circuit[(i + 1) % Circuit.Length]);
			trackLength += segmentLengths[i];
		}
	}

	public int LapCount { get; private set; }

	public void Reset()
	{
		distance = 0;
		LapCount = 0;
	}

	public void Advance(double elapsedSeconds, Car car)
	{
		if (elapsedSeconds <= 0)
		{
			return;
		}

		var segmentIndex = FindSegmentIndex(distance);
		var speedFactor = PaceWeights.GetSpeedFactor(car, Circuit[segmentIndex].IsCorner);

		distance += (float)elapsedSeconds * BaseSpeed * speedFactor;
		while (distance >= trackLength)
		{
			distance -= trackLength;
			LapCount++;
		}
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		canvas.SaveState();
		var scale = Math.Min(
			(dirtyRect.Width - 36) / (TrackMaxX - TrackMinX),
			(dirtyRect.Height - 36) / (TrackMaxY - TrackMinY));
		var offsetX = dirtyRect.X + (dirtyRect.Width - (TrackMaxX - TrackMinX) * scale) / 2 - TrackMinX * scale;
		var offsetY = dirtyRect.Y + (dirtyRect.Height - (TrackMaxY - TrackMinY) * scale) / 2 - TrackMinY * scale;

		canvas.StrokeLineCap = LineCap.Round;
		DrawCircuitLine(canvas, offsetX, offsetY, scale, 20, Color.FromArgb("#08101B"));
		DrawCircuitLine(canvas, offsetX, offsetY, scale, 14, Color.FromArgb("#8090A4"));
		DrawCircuitLine(canvas, offsetX, offsetY, scale, 10, Color.FromArgb("#26374B"));
		DrawStartLine(canvas, offsetX, offsetY, scale);
		DrawCarMarker(canvas, offsetX, offsetY, scale);
		canvas.RestoreState();
	}

	private static void DrawCircuitLine(ICanvas canvas, float offsetX, float offsetY, float scale, float width, Color color)
	{
		canvas.StrokeSize = width * scale;
		canvas.StrokeColor = color;
		for (var i = 0; i < Circuit.Length; i++)
		{
			var start = MapPoint(Circuit[i], offsetX, offsetY, scale);
			var end = MapPoint(Circuit[(i + 1) % Circuit.Length], offsetX, offsetY, scale);
			canvas.DrawLine(start.X, start.Y, end.X, end.Y);
		}
	}

	private void DrawStartLine(ICanvas canvas, float offsetX, float offsetY, float scale)
	{
		var start = MapPoint(Circuit[0], offsetX, offsetY, scale);
		var next = MapPoint(Circuit[1], offsetX, offsetY, scale);
		var dx = next.X - start.X;
		var dy = next.Y - start.Y;
		var length = MathF.Sqrt(dx * dx + dy * dy);
		var halfWidth = 9 * scale;
		var perpendicularX = -dy / length * halfWidth;
		var perpendicularY = dx / length * halfWidth;

		canvas.StrokeSize = 3;
		canvas.StrokeColor = Color.FromArgb("#F5C451");
		canvas.DrawLine(
			start.X - perpendicularX,
			start.Y - perpendicularY,
			start.X + perpendicularX,
			start.Y + perpendicularY);
	}

	private void DrawCarMarker(ICanvas canvas, float offsetX, float offsetY, float scale)
	{
		var segmentIndex = FindSegmentIndex(distance);
		var positionOnSegment = (distance - GetSegmentStartDistance(segmentIndex)) / segmentLengths[segmentIndex];
		var start = Circuit[segmentIndex];
		var end = Circuit[(segmentIndex + 1) % Circuit.Length];
		var point = MapPoint(
			new CircuitPoint(
				start.X + (end.X - start.X) * positionOnSegment,
				start.Y + (end.Y - start.Y) * positionOnSegment,
				false),
			offsetX,
			offsetY,
			scale);

		canvas.FillColor = Color.FromArgb("#39D8E8");
		canvas.FillCircle(point.X, point.Y, 11);
		canvas.FillColor = Color.FromArgb("#F5C451");
		canvas.FillCircle(point.X, point.Y, 6);
		canvas.StrokeSize = 2;
		canvas.StrokeColor = Color.FromArgb("#FFFFFF");
		canvas.DrawCircle(point.X, point.Y, 6);
	}

	private int FindSegmentIndex(float currentDistance)
	{
		var remaining = currentDistance;
		for (var i = 0; i < segmentLengths.Length; i++)
		{
			if (remaining < segmentLengths[i])
			{
				return i;
			}

			remaining -= segmentLengths[i];
		}

		return segmentLengths.Length - 1;
	}

	private float GetSegmentStartDistance(int segmentIndex)
	{
		var result = 0f;
		for (var i = 0; i < segmentIndex; i++)
		{
			result += segmentLengths[i];
		}

		return result;
	}

	private static PointF MapPoint(CircuitPoint point, float offsetX, float offsetY, float scale)
		=> new(offsetX + point.X * scale, offsetY + point.Y * scale);

	private static float Distance(CircuitPoint start, CircuitPoint end)
	{
		var x = end.X - start.X;
		var y = end.Y - start.Y;
		return MathF.Sqrt(x * x + y * y);
	}

	private readonly record struct CircuitPoint(float X, float Y, bool IsCorner);
}

public readonly record struct TrackPaceWeights(int Power, int Handling, int Tires, int Braking)
{
	public double CalculateScore(Car car)
	{
		var totalWeight = Power + Handling + Tires + Braking;
		return (car.Power * Power
			+ car.Handling * Handling
			+ car.Tires * Tires
			+ car.Braking * Braking) / (double)totalWeight;
	}

	public float GetSpeedFactor(Car car, bool isCorner)
	{
		var normalizedScore = (CalculateScore(car) - 1) / 9;
		return (float)(isCorner
			? 0.3 + normalizedScore * 0.7
			: 0.45 + normalizedScore * 0.55);
	}
}
