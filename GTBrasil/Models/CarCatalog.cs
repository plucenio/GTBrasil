namespace GTBrasil.Models;

public sealed class CarCatalog
{
	public List<CarDefinition>? Cars { get; set; }
}

public sealed class CarDefinition
{
	public string Id { get; set; } = string.Empty;

	public string Brand { get; set; } = string.Empty;

	public string Model { get; set; } = string.Empty;

	public int? Year { get; set; }

	public string Category { get; set; } = string.Empty;

	public string Image { get; set; } = string.Empty;

	public int Price { get; set; }

	public CarStatRanges? Stats { get; set; }
}

public sealed class CarStatRanges
{
	public StatRange? Power { get; set; }

	public StatRange? Braking { get; set; }

	public StatRange? Handling { get; set; }

	public StatRange? Tires { get; set; }
}

public sealed class StatRange
{
	public int Min { get; set; }

	public int Max { get; set; }
}
