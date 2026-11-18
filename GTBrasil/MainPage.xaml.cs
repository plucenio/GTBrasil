using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using GTBrasil.Models;

namespace GTBrasil;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
	private const int StartingCash = 8_000;
	private const string CashPreferenceKey = "garage.v2.cash";
	private const string ActiveCarPreferenceKey = "garage.v2.active-car";
	private const string OwnedCarPreferencePrefix = "garage.v2.owned.";
	private static readonly JsonSerializerOptions CatalogJsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};
	private readonly InterlagosTrackDrawable interlagosTrack = new();

	private Car? selectedCar;
	private Car? garageCar;
	private int cashBalance;
	private bool isClassifiedsVisible;
	private bool isRaceVisible;
	private bool carsLoaded;
	private int raceAnimationGeneration;
	private string notice = string.Empty;

	public MainPage()
	{
		InitializeComponent();
		InterlagosTrackView.Drawable = interlagosTrack;
		Cars = new ObservableCollection<Car>();
		CashBalance = Preferences.Get(CashPreferenceKey, StartingCash);
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (!carsLoaded)
		{
			await LoadCarsAsync();
		}

		if (IsRaceVisible)
		{
			StartRaceAnimation();
		}
	}

	protected override void OnDisappearing()
	{
		StopRaceAnimation();
		base.OnDisappearing();
	}

	public ObservableCollection<Car> Cars { get; }

	public Car? SelectedCar
	{
		get => selectedCar;
		private set
		{
			if (selectedCar == value)
			{
				return;
			}

			foreach (var car in Cars)
			{
				car.IsSelected = car == value;
			}

			selectedCar = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(HasSelectedCar));
			OnPropertyChanged(nameof(SelectionSummary));
			OnPropertyChanged(nameof(PurchaseButtonText));
		}
	}

	public Car? GarageCar
	{
		get => garageCar;
		private set
		{
			if (garageCar == value)
			{
				return;
			}

			garageCar = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(HasGarageCar));
			OnPropertyChanged(nameof(IsEmptyGarageVisible));
			OnPropertyChanged(nameof(GarageSubtitle));
			OnPropertyChanged(nameof(RaceButtonText));
			OnPropertyChanged(nameof(RacePaceDisplay));
		}
	}

	public int CashBalance
	{
		get => cashBalance;
		private set
		{
			if (cashBalance == value)
			{
				return;
			}

			cashBalance = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(CashDisplay));
		}
	}

	public bool IsClassifiedsVisible
	{
		get => isClassifiedsVisible;
		private set
		{
			if (isClassifiedsVisible == value)
			{
				return;
			}

			isClassifiedsVisible = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsGarageVisible));
		}
	}

	public bool IsRaceVisible
	{
		get => isRaceVisible;
		private set
		{
			if (isRaceVisible == value)
			{
				return;
			}

			isRaceVisible = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsGarageVisible));
		}
	}

	public bool IsGarageVisible => !IsClassifiedsVisible && !IsRaceVisible;

	public bool HasGarageCar => GarageCar is not null;

	public bool IsEmptyGarageVisible => GarageCar is null;

	public string RaceButtonText => HasGarageCar ? "CORRIDA\nINTERLAGOS" : "CORRIDA\nCOMPRE UM CARRO";

	public string CurrentLapDisplay => $"VOLTA {interlagosTrack.LapCount + 1}";

	public string RacePaceDisplay => GarageCar is null
		? "RITMO --"
		: $"RITMO {(GarageCar.Power * 0.4 + GarageCar.Braking * 0.2 + GarageCar.Handling * 0.25 + GarageCar.Tires * 0.15):0.0}/10";

	public string GarageSubtitle => HasGarageCar
		? "SEU PRÓXIMO CAPÍTULO COMEÇA AQUI"
		: "SUA JORNADA COMEÇA NOS CLASSIFICADOS";

	public bool HasSelectedCar => SelectedCar is not null;

	public bool HasNotice => !string.IsNullOrWhiteSpace(Notice);

	public string Notice
	{
		get => notice;
		private set
		{
			if (notice == value)
			{
				return;
			}

			notice = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(HasNotice));
		}
	}

	public string CashDisplay => $"CAIXA  R$ {CashBalance:N0}";

	public string SelectionSummary => SelectedCar is null
		? "SELECIONE UM CARRO NOS CLASSIFICADOS"
		: SelectedCar.IsOwned
			? $"{SelectedCar.Name} JÁ ESTÁ NA SUA GARAGEM"
			: $"{SelectedCar.Name}  •  R$ {SelectedCar.Price:N0}";

	public string PurchaseButtonText => SelectedCar is null
		? "SELECIONE UM CARRO"
		: SelectedCar.IsOwned
			? "LEVAR À GARAGEM"
			: $"COMPRAR POR R$ {SelectedCar.Price:N0}";

	private async Task LoadCarsAsync()
	{
		await using var stream = await FileSystem.OpenAppPackageFileAsync("cars.json");
		var catalog = await JsonSerializer.DeserializeAsync<CarCatalog>(stream, CatalogJsonOptions)
			?? throw new InvalidDataException("The car catalog is empty or invalid.");
		var definitions = catalog.Cars
			?? throw new InvalidDataException("The car catalog must define a cars list.");

		if (definitions.Count == 0)
		{
			throw new InvalidDataException("The car catalog must contain at least one car.");
		}

		var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var definition in definitions)
		{
			var stats = ValidateDefinition(definition, ids);
			var car = new Car(
				definition.Id,
				definition.Brand,
				definition.Model,
				definition.Year,
				definition.Category,
				definition.Image,
				definition.Price,
				stats.Power,
				stats.Braking,
				stats.Handling,
				stats.Tires)
			{
				IsOwned = Preferences.Get(GetOwnedCarPreferenceKey(definition.Id), false)
			};
			Cars.Add(car);
		}

		var activeCarId = Preferences.Get(ActiveCarPreferenceKey, string.Empty);
		GarageCar = Cars.FirstOrDefault(car => car.Id == activeCarId && car.IsOwned)
			?? Cars.FirstOrDefault(car => car.IsOwned);
		carsLoaded = true;
	}

	private static CarStats ValidateDefinition(CarDefinition? definition, ISet<string> ids)
	{
		if (definition is null
			|| string.IsNullOrWhiteSpace(definition.Id)
			|| string.IsNullOrWhiteSpace(definition.Brand)
			|| string.IsNullOrWhiteSpace(definition.Model)
			|| string.IsNullOrWhiteSpace(definition.Category)
			|| string.IsNullOrWhiteSpace(definition.Image)
			|| definition.Price <= 0
			|| !ids.Add(definition.Id))
		{
			throw new InvalidDataException($"Invalid or duplicate car definition: '{definition?.Id}'.");
		}

		var stats = definition.Stats
			?? throw new InvalidDataException($"Missing stats for car '{definition.Id}'.");
		ValidateStat(definition.Id, nameof(stats.Power), stats.Power);
		ValidateStat(definition.Id, nameof(stats.Braking), stats.Braking);
		ValidateStat(definition.Id, nameof(stats.Handling), stats.Handling);
		ValidateStat(definition.Id, nameof(stats.Tires), stats.Tires);
		return stats;
	}

	private static void ValidateStat(string carId, string statName, int value)
	{
		if (value is < 1 or > 10)
		{
			throw new InvalidDataException($"Invalid {statName} value for car '{carId}'. Expected a value from 1 to 10.");
		}
	}

	private static string GetOwnedCarPreferenceKey(string carId) => $"{OwnedCarPreferencePrefix}{carId}";

	private void OnClassifiedsClicked(object? sender, EventArgs e)
	{
		StopRaceAnimation();
		IsRaceVisible = false;
		Notice = string.Empty;
		SelectedCar = null;
		IsClassifiedsVisible = true;
	}

	private void OnGarageClicked(object? sender, EventArgs e)
	{
		StopRaceAnimation();
		IsRaceVisible = false;
		Notice = string.Empty;
		SelectedCar = null;
		IsClassifiedsVisible = false;
	}

	private void OnRaceClicked(object? sender, EventArgs e)
	{
		if (GarageCar is null)
		{
			Notice = "COMPRE UM CARRO ANTES DE INICIAR UMA CORRIDA.";
			return;
		}

		Notice = string.Empty;
		SelectedCar = null;
		IsClassifiedsVisible = false;
		interlagosTrack.Reset();
		OnPropertyChanged(nameof(CurrentLapDisplay));
		IsRaceVisible = true;
		StartRaceAnimation();
	}

	private void StartRaceAnimation()
	{
		var generation = ++raceAnimationGeneration;
		var previousFrame = DateTime.UtcNow;
		Dispatcher.StartTimer(TimeSpan.FromMilliseconds(33), () =>
		{
			if (generation != raceAnimationGeneration || !IsRaceVisible || GarageCar is null)
			{
				return false;
			}

			var now = DateTime.UtcNow;
			var elapsedSeconds = Math.Min((now - previousFrame).TotalSeconds, 0.1);
			previousFrame = now;
			var previousLap = interlagosTrack.LapCount;
			interlagosTrack.Advance(elapsedSeconds, GarageCar);
			InterlagosTrackView.Invalidate();
			if (interlagosTrack.LapCount != previousLap)
			{
				OnPropertyChanged(nameof(CurrentLapDisplay));
			}

			return true;
		});
	}

	private void StopRaceAnimation()
	{
		raceAnimationGeneration++;
	}

	private void OnCarTapped(object? sender, TappedEventArgs e)
	{
		if (sender is Border { BindingContext: Car car })
		{
			SelectedCar = car;
			Notice = string.Empty;
		}
	}

	private void OnPurchaseClicked(object? sender, EventArgs e)
	{
		var car = SelectedCar;
		if (car is null)
		{
			Notice = "ESCOLHA UM CARRO PARA CONTINUAR.";
			return;
		}

		if (!car.IsOwned && CashBalance < car.Price)
		{
			Notice = "SALDO INSUFICIENTE PARA COMPRAR ESTE CARRO.";
			return;
		}

		if (!car.IsOwned)
		{
			CashBalance -= car.Price;
			car.IsOwned = true;
			Preferences.Set(CashPreferenceKey, CashBalance);
			Preferences.Set(GetOwnedCarPreferenceKey(car.Id), true);
		}

		GarageCar = car;
		Preferences.Set(ActiveCarPreferenceKey, car.Id);
		OnGarageClicked(sender, e);
	}
}
