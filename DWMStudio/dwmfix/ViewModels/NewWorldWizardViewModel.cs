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
        [ObservableProperty] private int    _currentStep      = 1;
        [ObservableProperty] private string _worldName        = string.Empty;
        [ObservableProperty] private string _worldDescription = string.Empty;
        [ObservableProperty] private bool   _isCreating;

        public int TotalSteps => 3;

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

        [RelayCommand]
        private async Task CreateWorldAsync()
        {
            if (string.IsNullOrWhiteSpace(WorldName)) return;
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
