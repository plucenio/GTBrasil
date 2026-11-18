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

	public CarStats? Stats { get; set; }
}

public sealed class CarStats
{
	public int Power { get; set; }

	public int Braking { get; set; }

	public int Handling { get; set; }

	public int Tires { get; set; }
}
