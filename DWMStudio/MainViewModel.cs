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
        IRecipient<OpenWorldMessage>,
        IRecipient<WorldCreatedMessage>
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

        private readonly WorldLibraryService _library = new();

        public MainViewModel()
        {
            _toolStatus = new ToolStatusService();
            CurrentView = Dashboard;

            LoadLibrary();

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        private void LoadLibrary()
        {
            var worlds = _library.Load();

            foreach (var world in worlds)
                Dashboard.Worlds.Add(world);

            // Seed the samples ONLY on a genuinely empty first run. Adding them whenever the
            // list is empty would resurrect them every launch, so deleting the last world
            // would appear to work and then undo itself on restart.
            if (worlds.Count == 0 && _library.LoadMessage is null)
                Dashboard.SeedSampleWorlds();

            if (_library.LoadMessage is not null)
                Dashboard.StatusMessage = _library.LoadMessage;
        }

        private void SaveLibrary()
        {
            try
            {
                _library.Save(Dashboard.Worlds);
            }
            catch (Exception ex)
            {
                // A failed save must SAY SO. Swallowing it would leave the world visible on
                // screen and absent from disk -- looking saved is worse than failing to save,
                // because nobody retries something that appeared to work.
                Dashboard.StatusMessage =
                    $"Could not save the world library to {_library.Path}: {ex.Message}";
            }
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

        /// <summary>
        /// THE WIZARD'S COMPLETION HANDLER, WHICH DID NOT EXIST.
        ///
        /// NewWorldWizardViewModel has always ended by sending WorldCreatedMessage, and until
        /// now NOTHING ANYWHERE IMPLEMENTED IRecipient&lt;WorldCreatedMessage&gt;. RegisterAll
        /// only registers the recipient interfaces a type actually implements, so the message
        /// was sent into an empty room: no world added, no navigation, and the overlay left
        /// open exactly as it was. From the outside that is indistinguishable from a hang, and
        /// it happened even when the name was filled in correctly.
        ///
        /// A messenger send with no recipient is silent BY DESIGN -- that is what makes
        /// WeakReferenceMessenger decoupled, and it is also why this class of bug does not
        /// announce itself. Adding a message type and forgetting the handler compiles, runs,
        /// and does nothing.
        /// </summary>
        public void Receive(WorldCreatedMessage message)
        {
            Dashboard.Worlds.Add(message.World);
            SaveLibrary();

            IsWizardOpen = false;
            Wizard.Reset();

            // Open the thing that was just made, mirroring OpenWorldMessage. The new card is
            // also on the dashboard behind this, so navigating away loses nothing.
            CurrentView = new WorldDetailViewModel(message.World);
            ActiveNavItem = "World";
        }
    }
}