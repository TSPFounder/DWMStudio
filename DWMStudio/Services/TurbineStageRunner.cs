// TurbineStageRunner.cs
// DWMStudio's entry point into the MATLAB pipeline stage.
//
// The stage itself lives in DWM.Shared (DWM.Shared.Matlab.MatlabStageService) rather than
// here, for a reason worth stating: DWMStudio targets net10.0-windows and cannot be built or
// tested on the Linux build agent, while DWM.Shared targets net10.0 and can. Putting the
// orchestration -- and every check that protects against exporting stale or absent data --
// in the shared library is what makes it testable at all. See MatlabStageServiceTests.
//
// What is left here is genuinely DWMStudio's: choosing the COM transport, deciding where the
// output goes, and marking the pipeline stage complete on the WorldProject.
//
// PIPELINE STAGE 3 IS CURRENTLY A LABEL. WorldProject models the pipeline as a five-value
// enum cast to a list index (SysML, Cad, Matlab, CoSim, Runtime) with five hardcoded
// ViewModel properties behind it. This class makes PipelineStage.Matlab the first stage that
// actually does something. It does NOT fix the enum -- see SCOPE.md's fragility audit,
// item 3, for why that shape will not survive contact with a system that needs an FEA or a
// wind-tunnel stage.

using System;
using System.Threading;
using System.Threading.Tasks;
using DWM.Shared.Matlab;
using DWMStudio.Models;

namespace DWMStudio.Services
{
    public sealed class TurbineStageRunner
    {
        private readonly Func<IMatlabSession> _sessionFactory;

        /// <summary>
        /// Defaults to COM, attaching to a MATLAB the user already has open when there is one.
        /// </summary>
        /// <param name="sessionFactory">
        /// Override to supply a different transport -- TSPFounder/MatlabLibrary is the likely
        /// long-term one, and implementing IMatlabSession is all it needs to do.
        /// </param>
        public TurbineStageRunner(Func<IMatlabSession>? sessionFactory = null)
        {
            _sessionFactory = sessionFactory ?? (() => new MatlabComSession(allowLaunch: true));
        }

        /// <summary>
        /// Run the turbine model and write a world package, then mark the project's MATLAB
        /// stage complete.
        ///
        /// LONG-RUNNING. A 600 s ramp is tens of seconds of wall clock and MATLAB's COM
        /// Execute is synchronous with no progress callback, so this must not be awaited on
        /// the UI thread's synchronization context without a busy indicator. The cancellation
        /// token guards the queue, not the run: once MATLAB is simulating, nothing here can
        /// interrupt it.
        /// </summary>
        public async Task<MatlabStageResult> RunAsync(
            WorldProject project,
            MatlabStageRequest request,
            CancellationToken cancellationToken = default)
        {
            if (project is null) throw new ArgumentNullException(nameof(project));
            if (request is null) throw new ArgumentNullException(nameof(request));

            var service = new MatlabStageService(_sessionFactory);
            var result = await service.RunAndExportAsync(request, cancellationToken)
                                      .ConfigureAwait(false);

            // The note carries the warnings, not just a tick. A run that produced a package
            // with a missing channel, or one exported from the gust scenario, is "complete"
            // in the sense the pipeline means -- and is exactly the kind of thing that should
            // not be represented on screen by an unqualified green mark.
            project.MarkStageComplete(PipelineStage.Matlab, DescribeOutcome(result));

            return result;
        }

        internal static string DescribeOutcome(MatlabStageResult result)
        {
            var scenario = result.Scenario.ToMatlabToken();
            var channels = result.ChannelsExported.Count;

            if (!result.HasWarnings)
                return $"Complete - {scenario}, {channels}/5 channels";

            return $"Complete with {result.Warnings.Count} warning(s) - {scenario}, " +
                   $"{channels}/5 channels";
        }
    }
}
