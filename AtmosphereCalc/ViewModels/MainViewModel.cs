using CommunityToolkit.Mvvm.ComponentModel;

namespace AtmosphereCalc.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}
