using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GTBrasil.Models;

namespace GTBrasil;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
	private const int StartingCash = 8_000;
	private const string CashPreferenceKey = "garage.cash";

	private Car? selectedCar;
	private int cashBalance;
	private bool isGarageVisible;
	private string notice = string.Empty;

	public MainPage()
	{
		InitializeComponent();
		Cars = CreateCars();
		CashBalance = Preferences.Get(CashPreferenceKey, StartingCash);

		foreach (var car in Cars)
		{
			car.IsOwned = Preferences.Get($"garage.owned.{car.Id}", false);
		}

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
			OnPropertyChanged(nameof(GarageCar));
		}
	}

	public Car? GarageCar => SelectedCar;

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

	public bool IsGarageVisible
	{
		get => isGarageVisible;
		private set
		{
			if (isGarageVisible == value)
			{
				return;
			}

			isGarageVisible = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsCarSelectionVisible));
		}
	}

	public bool IsCarSelectionVisible => !IsGarageVisible;

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
		? "SELECIONE UM MODELO PARA CONTINUAR"
		: SelectedCar.IsOwned
			? $"{SelectedCar.Name} JÁ ESTÁ NA SUA GARAGEM"
			: $"{SelectedCar.Name}  •  R$ {SelectedCar.Price:N0}";

	public string PurchaseButtonText => SelectedCar is null
		? "ESCOLHA UM CARRO"
		: SelectedCar.IsOwned
			? "ENTRAR NA GARAGEM"
			: $"COMPRAR POR R$ {SelectedCar.Price:N0}";

	private static ObservableCollection<Car> CreateCars()
	{
		return new ObservableCollection<Car>
		{
			new(
				"chevette",
				"Chevrolet Chevette",
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
		if (SelectedCar is null)
		{
			Notice = "ESCOLHA UM CARRO PARA CONTINUAR.";
			return;
		}

		if (SelectedCar.IsOwned)
		{
			IsGarageVisible = true;
			Notice = string.Empty;
			return;
		}

		if (CashBalance < SelectedCar.Price)
		{
			Notice = "SALDO INSUFICIENTE PARA COMPRAR ESTE CARRO.";
			return;
		}

		CashBalance -= SelectedCar.Price;
		SelectedCar.IsOwned = true;
		Preferences.Set(CashPreferenceKey, CashBalance);
		Preferences.Set($"garage.owned.{SelectedCar.Id}", true);
		Notice = string.Empty;
		IsGarageVisible = true;
	}
}
