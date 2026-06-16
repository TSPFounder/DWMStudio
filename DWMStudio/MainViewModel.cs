// MainViewModel.cs
// Shell view model. Owns navigation between Dashboard / WorldDetail / Library,
// tool status indicators, and wizard overlay state.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DWMStudio.Models;
using DWMStudio.Services;

namespace DWMStudio.ViewModels
{
    public sealed partial class MainViewModel : ObservableObject,
        IRecipient<OpenWizardMessage>,
        IRecipient<OpenWorldMessage>
    {
        private readonly ToolStatusService _toolStatus;

        // ------------------------------------------------------------------
        // Child view models
        // ------------------------------------------------------------------

        public DashboardViewModel Dashboard { get; } = new();
        public NewWorldWizardViewModel Wizard { get; } = new();
        public LibraryViewModel Library { get; } = new();

        [ObservableProperty] private ObservableObject? _currentView;
        [ObservableProperty] private bool _isWizardOpen;
        [ObservableProperty] private string _activeNavItem = "Dashboard";

        // ------------------------------------------------------------------
        // Tool status (bound to status bar)
        // ------------------------------------------------------------------

        public ToolStatusService ToolStatus => _toolStatus;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public MainViewModel()
        {
            _toolStatus = new ToolStatusService();
            CurrentView = Dashboard;

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        // ------------------------------------------------------------------
        // Navigation commands
        // ------------------------------------------------------------------

        [RelayCommand]
        private void NavigateDashboard()
        {
            CurrentView = Dashboard;
            ActiveNavItem = "Dashboard";
            IsWizardOpen = false;
        }

        [RelayCommand]
        private void NavigateLibrary()
        {
            CurrentView = Library;
            ActiveNavItem = "Library";
            IsWizardOpen = false;
        }

        [RelayCommand]
        private void CloseWizard()
        {
            IsWizardOpen = false;
            Wizard.Reset();
        }

        // ------------------------------------------------------------------
        // Message handlers
        // ------------------------------------------------------------------

        public void Receive(OpenWizardMessage message)
        {
            Wizard.Reset();
            IsWizardOpen = true;
        }

        public void Receive(OpenWorldMessage message)
        {
            CurrentView = new WorldDetailViewModel(message.World);
            ActiveNavItem = "World";
            IsWizardOpen = false;
        }
    }
}