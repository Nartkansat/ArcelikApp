using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcelikApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        public MainViewModel()
        {
            // Initial view can be Dashboard
            // CurrentView = new DashboardViewModel();
        }

        [RelayCommand]
        private void Navigate(string viewName)
        {
            // switch (viewName) { ... }
        }
    }
}
