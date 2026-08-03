// TurbineMatlabRunner.cs
// The `turbine --matlab-dir` path: everything that actually touches MATLAB.
//
// WHY THIS IS A CLASS AND NOT A LOCAL FUNCTION IN Program.cs
//
// It started as one, and CA1416 was right to complain. Two things defeat the platform
// analyzer in a top-level program:
//
//   1. [SupportedOSPlatform] on a LOCAL FUNCTION is not honoured, so annotating the factory
//      there did nothing.
//   2. A LAMBDA body is analysed independently of the guard in its enclosing method, so
//      `() => new MatlabComSession(...)` reads as reachable on every platform even when the
//      surrounding code has already returned on non-Windows.
//
// Putting the Windows-only work in a Windows-only TYPE states the constraint where the
// analyzer can see it, and Program.cs then calls it under a plain OperatingSystem.IsWindows()
// check. That is not a workaround for the warning -- the warning was pointing at a real
// mismatch between where the constraint was written and where it applied.

using System.Runtime.Versioning;
using DWM.Shared.Matlab;

namespace DWMStudio.WorldPackageCli
{
    [SupportedOSPlatform("windows")]
    internal static class TurbineMatlabRunner
    {
        /// <param name="dumpDatabase">
        /// Program.cs's verify dump, passed in rather than duplicated -- re-opening the output
        /// with a fresh ReadOnly connection is the same proof `export` performs, and it should
        /// stay one implementation.
        /// </param>
        internal static int Run(
            string matlabDir,
            string outPath,
            string worldId,
            TurbineScenario scenario,
            int sampleRateHz,
            string csvBaseName,
            string? csvOutputDirectory,
            bool requireAllChannels,
            bool allowLaunch,
            string? progId,
            Action<string> dumpDatabase)
        {
            var request = new MatlabStageRequest
            {
                TurbineCodeDirectory = matlabDir,
                Scenario = scenario,
                CsvBaseName = csvBaseName,
                CsvOutputDirectory = csvOutputDirectory,
                SampleRateHz = sampleRateHz,
                OutputPackagePath = outPath,
                WorldId = worldId,
                RequireAllChannels = requireAllChannels
            };

            Console.WriteLine($"[turbine] Scenario    : {scenario.ToMatlabToken()}");
            Console.WriteLine($"[turbine] MATLAB code : {matlabDir}");
            Console.WriteLine($"[turbine] Sample rate : {sampleRateHz} Hz");
            Console.WriteLine($"[turbine] ProgID      : {progId ?? MatlabComSession.DefaultProgId}");
            Console.WriteLine(allowLaunch
                ? "[turbine] MATLAB      : attach to a running instance; launch one only if none is found."
                : "[turbine] MATLAB      : attach only (--no-launch); will fail if none is running.");

            if (string.IsNullOrWhiteSpace(progId))
            {
                Console.WriteLine("[turbine] NOTE        : the generic ProgID resolves to ONE release -- whichever");
                Console.WriteLine("[turbine]               registered last, usually the newest installed. If that is");
                Console.WriteLine("[turbine]               not R2011a, the attach will MISS an open R2011a and launch");
                Console.WriteLine("[turbine]               the newer one instead. Pass --progid matlab.application.7.12");
            }
            Console.WriteLine("[turbine] This BLOCKS for the whole simulation. A 600 s run is tens of seconds");
            Console.WriteLine("[turbine] of wall clock, and COM Execute gives no progress. Nothing has hung.");
            Console.WriteLine();

            // Report attach-vs-launch AS SOON AS THE SESSION OPENS, not from the result at the
            // end. The first real run of this command failed on the very next command, and the
            // one fact that would have explained it fastest -- whether this was the MATLAB the
            // user had open and set up, or a bare new one -- was only ever printed on success.
            var service = new MatlabStageService(() =>
            {
                var session = new MatlabComSession(allowLaunch, progId);
                Console.WriteLine(session.IsAttachedToExistingInstance
                    ? $"[turbine] MATLAB      : ATTACHED to an already-open '{session.ProgId}'."
                    : $"[turbine] MATLAB      : LAUNCHED a new '{session.ProgId}' (none was running).\n" +
                      "[turbine]               Its path and current folder are whatever a fresh MATLAB\n" +
                      "[turbine]               starts with -- CHECK THE VERSION IN ITS TITLE BAR.");
                return session;
            });

            var result = service.RunAndExport(request);

            Console.WriteLine();
            Console.WriteLine($"[turbine] Finished in {result.Duration.TotalSeconds:F1} s.");
            Console.WriteLine($"[turbine] MATLAB      : {(result.AttachedToExistingMatlab ? "attached to a session already open" : "launched by this command")}");
            Console.WriteLine($"[turbine] Channels    : {string.Join(", ", result.ChannelsExported)}");
            if (result.ChannelsMissing.Count > 0)
                Console.WriteLine($"[turbine] NOT PRESENT : {string.Join(", ", result.ChannelsMissing)}");

            foreach (var warning in result.Warnings)
            {
                Console.WriteLine();
                Console.WriteLine("[turbine] WARNING: " + warning);
            }

            Console.WriteLine();
            Console.WriteLine($"[verify] Re-opening '{outPath}' fresh (ReadOnly) immediately after export...");
            dumpDatabase(outPath);
            return 0;
        }
    }
}
