// WorldProject.cs
// Core domain model for a DWM world project. Tracks the five pipeline stages
// and provides metadata needed by the UI and external tool integrations.

using System.Collections.Generic;

namespace DWMStudio.Models
{
    public enum PipelineStage { SysML, Cad, Matlab, CoSim, Runtime }

    public sealed class PipelineStageStatus
    {
        public PipelineStage Stage      { get; init; }
        public string        Status     { get; set; } = "Pending";
        public bool          IsComplete { get; set; }
    }

    public sealed class WorldProject
    {
        public string WorldId            { get; init; } = string.Empty;
        public string Name               { get; set;  } = string.Empty;
        public string FusionDocumentPath { get; set;  } = string.Empty;

        private readonly Dictionary<PipelineStage, PipelineStageStatus> _stages = new()
        {
            { PipelineStage.SysML,   new PipelineStageStatus { Stage = PipelineStage.SysML   } },
            { PipelineStage.Cad,     new PipelineStageStatus { Stage = PipelineStage.Cad     } },
            { PipelineStage.Matlab,  new PipelineStageStatus { Stage = PipelineStage.Matlab  } },
            { PipelineStage.CoSim,   new PipelineStageStatus { Stage = PipelineStage.CoSim   } },
            { PipelineStage.Runtime, new PipelineStageStatus { Stage = PipelineStage.Runtime } },
        };

        public PipelineStageStatus GetStage(PipelineStage stage) => _stages[stage];
    }
}
