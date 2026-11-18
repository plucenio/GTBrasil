using System.Collections.ObjectModel;
using System.ComponentModel;
using GTBrasil.Models;

namespace GTBrasil;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
	private const int StartingCash = 8_000;
	private const string CashPreferenceKey = "garage.v2.cash";
	private const string ActiveCarPreferenceKey = "garage.v2.active-car";
	private const string OwnedCarPreferencePrefix = "garage.v2.owned.";

	private Car? selectedCar;
	private Car? garageCar;
	private int cashBalance;
	private bool isClassifiedsVisible;
	private string notice = string.Empty;

	public MainPage()
	{
		InitializeComponent();
		Cars = CreateCars();
		CashBalance = Preferences.Get(CashPreferenceKey, StartingCash);

		foreach (var car in Cars)
		{
			car.IsOwned = Preferences.Get(GetOwnedCarPreferenceKey(car), false);
		}

		var activeCarId = Preferences.Get(ActiveCarPreferenceKey, string.Empty);
		GarageCar = Cars.FirstOrDefault(car => car.Id == activeCarId && car.IsOwned)
			?? Cars.FirstOrDefault(car => car.IsOwned);

		BindingContext = this;
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

	public bool IsGarageVisible => !IsClassifiedsVisible;

	public bool HasGarageCar => GarageCar is not null;

	public bool IsEmptyGarageVisible => GarageCar is null;

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

	private static ObservableCollection<Car> CreateCars()
	{
		return new ObservableCollection<Car>
		{
			new(
				"chevette",
				"Chevrolet Chevette 1983",
				"CLÁSSICO NACIONAL  •  TRAÇÃO TRASEIRA",
				"chevette.png",
				6_000,
				Random.Shared.Next(1, 11),
				Random.Shared.Next(1, 11),
				Random.Shared.Next(1, 11),
				Random.Shared.Next(1, 11)),
			new(
				"corcel",
				"Ford Corcel",
				"CLÁSSICO NACIONAL  •  ESPÍRITO DE ESTRADA",
				"corcel.png",
				5_000,
				Random.Shared.Next(1, 11),
				Random.Shared.Next(1, 11),
				Random.Shared.Next(1, 11),
				Random.Shared.Next(1, 11))
		};
	}

	private static string GetOwnedCarPreferenceKey(Car car) => $"{OwnedCarPreferencePrefix}{car.Id}";

	private void OnClassifiedsClicked(object? sender, EventArgs e)
	{
		Notice = string.Empty;
		SelectedCar = null;
		IsClassifiedsVisible = true;
	}

	private void OnGarageClicked(object? sender, EventArgs e)
	{
		Notice = string.Empty;
		SelectedCar = null;
		IsClassifiedsVisible = false;
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
			Preferences.Set(GetOwnedCarPreferenceKey(car), true);
		}

		GarageCar = car;
		Preferences.Set(ActiveCarPreferenceKey, car.Id);
		OnGarageClicked(sender, e);
	}
}
