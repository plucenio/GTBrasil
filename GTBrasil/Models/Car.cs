using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GTBrasil.Models;

public sealed class Car : INotifyPropertyChanged
{
	private bool isOwned;
	private bool isSelected;

	public Car(
		string id,
		string name,
		string category,
		string image,
		int price,
		int power,
		int braking,
		int handling,
		int tires)
	{
		Id = id;
		Name = name;
		Category = category;
		Image = image;
		Price = price;
		Power = power;
		Braking = braking;
		Handling = handling;
		Tires = tires;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Id { get; }

	public string Name { get; }

	public string Category { get; }

	public string Image { get; }

	public string ImageDescription => $"Carro em pixel art: {Name}";

	public string AutomationId => $"car-{Id}";

	public int Price { get; }

	public int Power { get; }

	public int Braking { get; }

	public int Handling { get; }

	public int Tires { get; }

	public double PowerProgress => Power / 10d;

	public double BrakingProgress => Braking / 10d;

	public double HandlingProgress => Handling / 10d;

	public double TiresProgress => Tires / 10d;

	public bool IsOwned
	{
		get => isOwned;
		set
		{
			if (isOwned == value)
			{
				return;
			}

			isOwned = value;
			OnPropertyChanged();
		}
	}

	public bool IsSelected
	{
		get => isSelected;
		set
		{
			if (isSelected == value)
			{
				return;
			}

			isSelected = value;
			OnPropertyChanged();
		}
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
