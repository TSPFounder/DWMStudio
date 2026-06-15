// WorldProject.cs
// Core domain model for a DWM world project. Tracks the five pipeline stages
// and provides metadata needed by the UI and external tool integrations.

using System;
using System.Collections.Generic;
using System.Linq;

namespace DWMStudio.Models
{
    public enum PipelineStage { SysML, Cad, Matlab, CoSim, Runtime }

    public sealed class PipelineStageStatus
    {
        public PipelineStage Stage      { get; init; }
        public string        Label      { get; init; } = string.Empty;
        public string        Status     { get; set; }  = "Pending";
        public bool          IsComplete { get; set; }
    }

    public sealed class WorldProject
    {
        public string WorldId            { get; init; } = Guid.NewGuid().ToString();
        public string Name               { get; set;  } = "Untitled World";
        public string Description        { get; set;  } = string.Empty;
        public string Version            { get; set;  } = "1.0";
        public string FusionDocumentPath { get; set;  } = string.Empty;
        public string SimulinkModelPath  { get; set;  } = string.Empty;

        public int RequirementCount { get; set; }
        public int ActorCount       { get; set; }
        public int UseCaseCount     { get; set; }
        public int UserStoryCount   { get; set; }

        public DateTimeOffset LastModifiedOn { get; set; } = DateTimeOffset.Now;

        // Ordered list (SysML → CAD → MATLAB → CoSim → Runtime) for binding by index
        public List<PipelineStageStatus> Stages { get; } = new()
        {
            new PipelineStageStatus { Stage = PipelineStage.SysML,   Label = "SysML"   },
            new PipelineStageStatus { Stage = PipelineStage.Cad,     Label = "CAD"     },
            new PipelineStageStatus { Stage = PipelineStage.Matlab,  Label = "MATLAB"  },
            new PipelineStageStatus { Stage = PipelineStage.CoSim,   Label = "Co-Sim"  },
            new PipelineStageStatus { Stage = PipelineStage.Runtime, Label = "Runtime" },
        };

        public PipelineStageStatus GetStage(PipelineStage stage) =>
            Stages[(int)stage];

        public void MarkStageComplete(PipelineStage stage, string? note = null)
        {
            var s = Stages[(int)stage];
            s.IsComplete = true;
            s.Status     = note ?? "Complete";
            LastModifiedOn = DateTimeOffset.Now;
        }

        public int CompletedStageCount => Stages.Count(s => s.IsComplete);

        public string LastModifiedDisplay =>
            LastModifiedOn.LocalDateTime.ToString("MMM d, yyyy h:mm tt");
    }
}
