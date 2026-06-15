// Messages.cs
// CommunityToolkit.Mvvm messenger messages used for cross-ViewModel communication.

namespace DWMStudio.Models
{
    public sealed class OpenWizardMessage { }

    public sealed class OpenWorldMessage
    {
        public WorldProject World { get; }
        public OpenWorldMessage(WorldProject world) => World = world;
    }
}
