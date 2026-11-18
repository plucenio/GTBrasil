using GTBrasil.Models;
using Microsoft.Maui.Graphics;

namespace GTBrasil;

public sealed class InterlagosTrackDrawable : IDrawable
{
	private const float BaseSpeed = 105f;
	private const float TrackMinX = 45f;
	private const float TrackMaxX = 980f;
	private const float TrackMinY = 10f;
	private const float TrackMaxY = 610f;

	private static readonly CircuitPoint[] Circuit =
	[
		new(930, 570, true),
		new(947, 545, true),
		new(956, 516, true),
		new(947, 488, true),
		new(930, 466, true),
		new(908, 450, true),
		new(922, 435, true),
		new(945, 410, true),
		new(950, 380, true),
		new(938, 348, true),
		new(918, 323, true),
		new(898, 300, true),
		new(887, 270, true),
		new(884, 235, true),
		new(870, 205, true),
		new(850, 175, false),
		new(780, 158, false),
		new(700, 140, false),
		new(610, 120, false),
		new(520, 97, false),
		new(430, 75, false),
		new(350, 52, false),
		new(288, 28, true),
		new(255, 30, true),
		new(225, 45, true),
		new(203, 70, true),
		new(195, 102, true),
		new(205, 130, true),
		new(230, 150, true),
		new(260, 165, true),
		new(300, 205, false),
		new(340, 250, false),
		new(380, 295, false),
		new(420, 340, false),
		new(465, 385, true),
		new(500, 414, true),
		new(515, 432, true),
		new(532, 451, true),
		new(535, 476, true),
		new(515, 494, true),
		new(485, 497, true),
		new(455, 520, true),
		new(425, 542, true),
		new(390, 552, true),
		new(365, 569, true),
		new(337, 581, true),
		new(325, 575, true),
		new(298, 560, true),
		new(269, 546, true),
		new(241, 538, true),
		new(223, 518, true),
		new(225, 490, true),
		new(246, 463, true),
		new(275, 444, true),
		new(255, 421, true),
		new(225, 391, true),
		new(191, 361, true),
		new(151, 332, true),
		new(115, 310, true),
		new(95, 311, true),
		new(78, 326, true),
		new(70, 348, true),
		new(76, 376, true),
		new(87, 411, true),
		new(105, 450, true),
		new(119, 486, true),
		new(120, 507, false),
		new(220, 520, false),
		new(340, 532, false),
		new(480, 542, false),
		new(620, 550, false),
		new(760, 558, false),
		new(850, 565, false)
	];

	private static readonly TrackMarker[] CornerMarkers =
	[
		new(930, 536, 1),
		new(953, 435, 2),
		new(835, 160, 3),
		new(288, 18, 4),
		new(236, 160, 5),
		new(540, 418, 6),
		new(500, 508, 7),
		new(390, 570, 8),
		new(320, 590, 9),
		new(215, 525, 10),
		new(270, 425, 11),
		new(105, 294, 12),
		new(53, 348, 13),
		new(112, 520, 14)
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
		DrawCornerMarkers(canvas, offsetX, offsetY, scale);
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

	private static void DrawCornerMarkers(ICanvas canvas, float offsetX, float offsetY, float scale)
	{
		canvas.FontSize = 9;
		canvas.FontColor = Color.FromArgb("#08101B");
		foreach (var marker in CornerMarkers)
		{
			var point = MapPoint(new CircuitPoint(marker.X, marker.Y, false), offsetX, offsetY, scale);
			canvas.FillColor = Color.FromArgb("#F5C451");
			canvas.FillCircle(point.X, point.Y, 7);
			canvas.DrawString(
				marker.Number.ToString(),
				point.X - 7,
				point.Y - 7,
				14,
				14,
				HorizontalAlignment.Center,
				VerticalAlignment.Center);
		}
	}

	private void DrawStartLine(ICanvas canvas, float offsetX, float offsetY, float scale)
	{
		var start = MapPoint(Circuit[0], offsetX, offsetY, scale);
		var previous = MapPoint(Circuit[^1], offsetX, offsetY, scale);
		var dx = start.X - previous.X;
		var dy = start.Y - previous.Y;
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

	private readonly record struct TrackMarker(float X, float Y, int Number);
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
