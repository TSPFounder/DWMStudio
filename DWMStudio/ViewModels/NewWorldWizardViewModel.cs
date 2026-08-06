// NewWorldWizardViewModel.cs
// Backs the New World Wizard overlay.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DWMStudio.Models;

namespace DWMStudio.ViewModels
{
    public sealed partial class NewWorldWizardViewModel : ObservableObject
    {
        [ObservableProperty] private int _currentStep = 1;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateWorldCommand))]
        [NotifyPropertyChangedFor(nameof(IsNameMissing))]
        private string _worldName = string.Empty;

        [ObservableProperty] private string _worldDescription = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateWorldCommand))]
        private bool _isCreating;

        public int TotalSteps => 3;

        /// <summary>
        /// Drives the step-3 warning. Step 3 used to say "Ready to create your world"
        /// unconditionally, including when there was no name -- so the one case where the
        /// button does nothing was also the case where the UI insisted everything was fine.
        /// </summary>
        public bool IsNameMissing => string.IsNullOrWhiteSpace(WorldName);

        public bool CanGoBack => CurrentStep > 1;
        public bool CanGoNext => CurrentStep < TotalSteps;

        partial void OnCurrentStepChanged(int value)
        {
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
        }

        [RelayCommand]
        private void Next() { if (CanGoNext) CurrentStep++; }

        [RelayCommand]
        private void Back() { if (CanGoBack) CurrentStep--; }

        /// <summary>
        /// Gates the Create World button. This used to be an early `return` inside the command
        /// body, which meant a blank name produced NO EFFECT WHATSOEVER -- no message, no close,
        /// no error -- and the button looked perfectly clickable. "Nothing happens when I click
        /// it" was the accurate bug report. A disabled button says the same thing honestly.
        /// </summary>
        private bool CanCreateWorld() => !IsNameMissing && !IsCreating;

        [RelayCommand(CanExecute = nameof(CanCreateWorld))]
        private async Task CreateWorldAsync()
        {
            // Belt and braces: CanExecute already blocks this, but a command can still be
            // invoked directly in code, and a silent return here would be as opaque as before.
            if (IsNameMissing) return;

            IsCreating = true;

            var world = new WorldProject
            {
                Name        = WorldName,
                Description = WorldDescription,
            };

            await Task.Delay(200); // placeholder for persistence
            WeakReferenceMessenger.Default.Send(new WorldCreatedMessage(world));
            IsCreating = false;
        }

        public void Reset()
        {
            CurrentStep      = 1;
            WorldName        = string.Empty;
            WorldDescription = string.Empty;
            IsCreating       = false;
        }
    }
}
