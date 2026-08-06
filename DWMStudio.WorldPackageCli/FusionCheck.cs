// FusionCheck.cs
// The `fusion` command: the only thing that drives Fusion 360 for real.
//
// WHY THIS EXISTS, WHICH IS THE SAME REASON `turbine --matlab-dir` DOES
//
// Everything in DWMStudio.Tests runs against a fake. FusionRunnerSession is tested with a
// FakeRunner, FusionStageService with a FakeFusionSession, and the Python emitters are checked
// by looking at the STRING they produce. All of that is worth having and NONE of it proves the
// script runs. The Fusion API cannot be unit tested: it needs Windows, an installed Fusion, a
// signed-in Autodesk account and an add-in loaded into a running process.
//
// So the parts most likely to be wrong are precisely the parts with no automated coverage:
//
//   * whether the generated Python is VALID Python inside Fusion's interpreter;
//   * whether `root` and `design` exist when a sequence runs against an already-open document
//     (they did not, until 2026-08-06 -- neither wrapper defined them);
//   * whether a failing script deadlocks the add-in (it would have, until the same day --
//     the failure path opened a modal dialog on the thread the reply depends on);
//   * whether the units coming back mean what this code says they mean.
//
// THE LAST ONE IS WHY `revolve` BUILDS A TUBE RATHER THAN A BOX.
//
// A hollow cylinder has closed-form mass properties, so the reply can be CHECKED rather than
// merely received. Volume, centre of mass and the axial moment of inertia are all computable by
// hand, and the third one settles a question this project answered indirectly: the inertia
// conversion (x 1e-4, kg*cm^2 to kg*m^2) was justified by radius of gyration on the rotor --
// convincing, but inference. Izz / m for a tube is (ri^2 + ro^2) / 2 exactly. That is the
// hand-computed solid the original UNVERIFIED label said was missing.

using CAD.Scripting;
using DWM.Shared.Tooling;
using DWM.Shared.Tooling.Cad;
using Fusion.Application;

namespace DWMStudio.WorldPackageCli;

internal static class FusionCheck
{
    // The tube, in Fusion's internal centimetres. Sketched on XZ so the profile's second
    // coordinate runs along the global Z it is revolved about.
    private const double InnerRadiusCm = 10.0;
    private const double OuterRadiusCm = 12.0;
    private const double HeightCm = 50.0;

    public static async Task<int> RunAsync(string[] a)
    {
        var sub = a.Length > 0 ? a[0].ToLowerInvariant() : "";
        var rest = a.Skip(1).ToArray();

        return sub switch
        {
            "ping" => await PingAsync(rest),
            "massprops" => await MassPropsAsync(rest),
            "revolve" => await RevolveAsync(rest),
            "export" => await ExportAsync(rest),
            "params" => await ParamsAsync(rest),
            "setparam" => await SetParamAsync(rest),
            _ => Usage()
        };
    }

    // ==================================================================
    // ping
    // ==================================================================
    private static async Task<int> PingAsync(string[] a)
    {
        using var session = OpenSession(a);

        Console.WriteLine("[fusion] Pinging the add-in...");
        if (!await session.PingAsync())
        {
            // NOT A SOCKET TEST. This add-in routes /ping through Fusion's main thread and
            // reads app.version, so a reply proves Fusion is responsive, not merely listening.
            Console.Error.WriteLine(
                "[fusion] No answer.\n\n" +
                "This CANNOT distinguish Fusion not running from Fusion running without the\n" +
                "add-in loaded -- both refuse the connection identically. Check Fusion is open,\n" +
                "then Utilities > Scripts and Add-Ins > Add-Ins.\n\n" +
                "If it HANGS rather than refusing, that is the opposite fault: the port is bound\n" +
                "and the main thread is blocked. Close any open Fusion dialog -- including the\n" +
                "Scripts and Add-Ins window itself, which is modal and blocks the add-in it is\n" +
                "used to start.");
            return 1;
        }

        Console.WriteLine("[fusion] OK.");
        return 0;
    }

    // ==================================================================
    // massprops
    // ==================================================================
    private static async Task<int> MassPropsAsync(string[] a)
    {
        // The service opens and disposes its own session -- that is what the factory argument
        // is for, and handing it one this method also owns would dispose it twice.
        var result = await new FusionStageService(() => OpenSession(a)).ReadMassPropertiesAsync();

        return Report(result);
    }

    // ==================================================================
    // revolve
    // ==================================================================
    private static async Task<int> RevolveAsync(string[] a)
    {
        var axis = GetOption(a, "--axis") ?? "Z";
        var angle = ParseDouble(GetOption(a, "--angle"), 360.0);

        var ops = new CadOperationSequence()
            .Add(new CreateSketchOp
            {
                SketchId = "dwm_check",
                Plane = "XZ",
                Comment = "DWM verification tube"
            })
            .Add(new SketchPolylineOp
            {
                SketchId = "dwm_check",
                Closed = true,
                PointsCm = new List<double[]>
                {
                    new[] { InnerRadiusCm, 0.0 },
                    new[] { OuterRadiusCm, 0.0 },
                    new[] { OuterRadiusCm, HeightCm },
                    new[] { InnerRadiusCm, HeightCm }
                }
            })
            .Add(new RevolveOp
            {
                SketchId = "dwm_check",
                Axis = axis,
                AngleDeg = angle
            });

        // --dry-run PRINTS THE PYTHON AND SENDS NOTHING. The only part of this command that
        // works without Fusion, and the fastest way to see what the emitter actually produced
        // rather than what the emitter's tests say it produced.
        if (HasFlag(a, "--dry-run"))
        {
            Console.WriteLine(new FusionPythonScriptFactory()
                .CreateScript(ops, new ScriptMetadata { Name = "DWM_RevolveCheck" })
                .EntrySource);
            return 0;
        }

        using var session = OpenSession(a);

        Console.WriteLine("[fusion] Pinging first, so a build failure is not confused with a dead add-in...");
        if (!await session.PingAsync())
        {
            Console.Error.WriteLine("[fusion] No answer. Run 'fusion ping' for the full diagnosis.");
            return 1;
        }

        Console.WriteLine($"[fusion] Building a tube: ri={InnerRadiusCm} cm, ro={OuterRadiusCm} cm, " +
                          $"h={HeightCm} cm, revolved {angle} deg about {axis.ToUpperInvariant()}.");

        var built = await session.InvokeAsync("operations", ops);
        if (!built.Ok)
        {
            Console.Error.WriteLine("[fusion] BUILD FAILED");
            Console.Error.WriteLine(built.Error);
            return 1;
        }

        Console.WriteLine("[fusion] Built. Reading mass properties back...");
        var result = await new FusionStageService(() => OpenSession(a)).ReadMassPropertiesAsync();

        var exit = Report(result);
        if (exit != 0) return exit;

        CheckAgainstClosedForm(result, angle);
        return 0;
    }

    /// <summary>
    /// Compares what Fusion returned against what a hollow cylinder must weigh and spin like.
    ///
    /// THIS IS THE PART THAT IS NOT A SMOKE TEST. A build that "succeeded" and a build that
    /// produced the right solid look identical from outside; three numbers separate them, and
    /// each one fails differently:
    ///
    ///   DENSITY   mass / volume. Wrong by 1e6 means a centimetre/metre mix-up somewhere;
    ///             wrong by a plausible factor just means a different default material.
    ///   CENTRE    must sit at half the height. Catches a profile revolved about the wrong
    ///             axis, which still builds and still has mass.
    ///   INERTIA   Izz / m = (ri^2 + ro^2) / 2, exactly. THE UNIT CHECK. If the conversion is
    ///             right this comes out in metres squared; if the x 1e-4 should not be there,
    ///             it is out by exactly that factor.
    /// </summary>
    private static void CheckAgainstClosedForm(FusionStageResult result, double angleDeg)
    {
        if (Math.Abs(angleDeg - 360.0) > 1e-9)
        {
            Console.WriteLine();
            Console.WriteLine("[check] Skipped: the closed forms below are for a FULL revolve.");
            return;
        }

        var body = result.Components
            .OrderByDescending(c => c.MassKg)
            .FirstOrDefault(c => c.BodyCount is null or > 0);

        if (body is null)
        {
            Console.WriteLine();
            Console.WriteLine("[check] Skipped: no component with bodies came back.");
            return;
        }

        // Metres throughout, because that is what this code claims to hand downstream.
        var ri = InnerRadiusCm / 100.0;
        var ro = OuterRadiusCm / 100.0;
        var h = HeightCm / 100.0;

        var volume = Math.PI * (ro * ro - ri * ri) * h;
        var density = body.MassKg / volume;

        Console.WriteLine();
        Console.WriteLine($"[check] Against the closed form, using '{body.ComponentName}':");
        Console.WriteLine($"[check]   volume        = {volume:G6} m^3  (hand-computed)");
        Console.WriteLine($"[check]   mass          = {body.MassKg:G6} kg  (Fusion)");
        Console.WriteLine($"[check]   implied rho   = {density:G6} kg/m^3");
        Console.WriteLine("[check]                   steel ~7850, aluminium ~2700, ABS ~1050.");
        Console.WriteLine("[check]                   A value near one of those is right. A value off");
        Console.WriteLine("[check]                   by 1e6 is a centimetre/metre error, not a material.");

        var offset = 0.0;
        if (body.CentreOfMass.Length == 3)
        {
            // Which coordinate carries the height depends on the revolve axis; the height is
            // whichever one is furthest from zero, and its MAGNITUDE must be h/2.
            //
            // THE SIGN IS NOT A FAULT. A sketch on the XZ plane has its local Y along global
            // -Z, so the tube is built below the origin and the centre reads -0.25 m. Fusion's
            // convention, not an error, and asserting on the signed value would fail a
            // perfectly good build.
            offset = body.CentreOfMass.OrderByDescending(Math.Abs).First();

            Console.WriteLine($"[check]   centre        = [{string.Join(", ", body.CentreOfMass.Select(v => v.ToString("G6")))}] m");
            Console.WriteLine($"[check]                   one axis must read +/-{h / 2.0:G6} m and the others ~0.");
            Console.WriteLine("[check]                   The sign follows the sketch plane's own axes.");
            Console.WriteLine("[check]                   A centre at the ORIGIN would mean the profile was");
            Console.WriteLine("[check]                   revolved about the wrong axis -- which builds fine.");
        }

        if (body.Inertia.Length >= 3 && body.MassKg > 0 && Math.Abs(offset) > 1e-9)
        {
            // THE REFERENCE POINT, WHICH IS THE EASIEST THING HERE TO GET WRONG QUIETLY.
            // Fusion reports inertia about the DOCUMENT ORIGIN, not the centre of mass, and
            // both are plausible numbers in the right units. The transverse moment is what
            // separates them: about the centre of mass it is (3(ri^2 + ro^2) + h^2)/12, and
            // about the origin that plus d^2 where d is the offset above.
            //
            // Simscape wants the centre-of-mass value. Whoever feeds it has to subtract the
            // parallel-axis term, and can only know that if this says so.
            var aboutCom = (3.0 * (ri * ri + ro * ro) + h * h) / 12.0;
            var aboutOrigin = aboutCom + offset * offset;
            var measured = body.Inertia.Take(3).Select(i => i / body.MassKg).Max();

            Console.WriteLine();
            Console.WriteLine($"[check]   I/m (transverse) = {measured:G6} m^2");
            Console.WriteLine($"[check]                   about the centre of mass: {aboutCom:G6}");
            Console.WriteLine($"[check]                   about the origin:         {aboutOrigin:G6}");
            Console.WriteLine($"[check]                   ratio to origin form:     {measured / aboutOrigin:G6}");
            Console.WriteLine("[check]                   ~1.0 confirms Fusion measures about the ORIGIN.");
            Console.WriteLine("[check]                   Simscape wants the centre-of-mass value, so the");
            Console.WriteLine("[check]                   parallel-axis term must be subtracted downstream.");
        }

        if (body.Inertia.Length >= 3 && body.MassKg > 0)
        {
            // Izz for a hollow cylinder about its own axis. Exact, no thin-wall assumption.
            var expectedAxial = (ri * ri + ro * ro) / 2.0;

            // THE SMALLEST OF THE THREE IS THE AXIAL ONE, and that is a property of this tube
            // rather than of tubes. Transverse works out at (3(ri^2 + ro^2) + h^2) / 12, which
            // for 50 cm of height over 12 cm of radius is 0.0269 against the axial 0.0122 --
            // the height term dominates. Picking the largest would compare the wrong moment
            // against the right expectation and report a ratio near 2.2.
            var axial = body.Inertia
                .Take(3)
                .Select(i => i / body.MassKg)
                .Min();

            Console.WriteLine($"[check]   I/m (axial)   = {axial:G6} m^2   expected {expectedAxial:G6} m^2");
            Console.WriteLine($"[check]                   ratio {axial / expectedAxial:G6}");
            Console.WriteLine("[check]                   THIS IS THE UNIT CHECK. Near 1.0 confirms the");
            Console.WriteLine("[check]                   kg*cm^2 -> kg*m^2 conversion. Near 1e4 means the");
            Console.WriteLine("[check]                   x 1e-4 in FusionScripts should not be there.");
        }
    }

    // ==================================================================
    // export
    // ==================================================================
    private static async Task<int> ExportAsync(string[] a)
    {
        var outPath = GetOption(a, "--out");
        var formatText = GetOption(a, "--format") ?? "step";

        if (outPath is null)
        {
            Console.Error.WriteLine("Usage: fusion export --out <path on the Fusion machine> [--format step|f3d|stl|obj]");
            return 1;
        }

        if (!Enum.TryParse<ExportFormat>(formatText, ignoreCase: true, out var format))
        {
            Console.Error.WriteLine($"Unknown --format '{formatText}'. Use step, f3d, stl or obj.");
            return 1;
        }

        using var session = OpenSession(a);

        // THE PATH IS RESOLVED BY FUSION, NOT HERE. Fusion writes it, so a relative path is
        // relative to Fusion's working directory rather than this one, and on a remote setup
        // the file lands on that machine. Said plainly because "the export succeeded and there
        // is no file" is otherwise a long afternoon.
        Console.WriteLine($"[fusion] Exporting to {format}: {outPath}");
        Console.WriteLine("[fusion] (the path is interpreted by FUSION, on the machine running it)");

        var reply = await session.InvokeAsync("export",
            new FusionExportRequest { Format = format, OutputPath = outPath });

        if (!reply.Ok)
        {
            Console.Error.WriteLine("[fusion] EXPORT FAILED");
            Console.Error.WriteLine(reply.Error);
            return 1;
        }

        Console.WriteLine("[fusion] Fusion reported success.");
        Console.WriteLine("[fusion] Confirm the file exists on that machine -- this cannot.");
        return 0;
    }

    // ==================================================================
    // params / setparam
    // ==================================================================
    // THE LEAST PROVEN THING IN THIS COMMAND, and it is here so that stops being true.
    // /scripts/execute has been driven against Fusion many times; GET and PATCH on
    // /documents/active/parameters have been driven zero times. They were read from
    // FusionLibrary's client, which is a claim about the add-in rather than a measurement of
    // it. Running `fusion params` once settles it either way.
    private static async Task<int> ParamsAsync(string[] a)
    {
        var result = await new FusionParameterService(() => OpenSession(a)).ReadAsync();

        if (!result.Succeeded)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("[fusion] PARAMETER READ FAILED");
            Console.Error.WriteLine(result.Run.FailureMessage);
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"[fusion] {result.Parameters.Count} user parameter(s):");
        foreach (var p in result.Parameters)
        {
            Console.WriteLine($"[fusion]   {p.Name} = {p.Expression}");
            Console.WriteLine($"[fusion]       internal {p.ValueInternal:G8}  unit '{p.Unit}'" +
                              (string.IsNullOrWhiteSpace(p.Comment) ? "" : $"  -- {p.Comment}"));
        }

        if (result.Parameters.Count > 0)
        {
            // THE UNIT TRAP, STATED WHERE THE NUMBERS ARE. Value is centimetres and radians,
            // Expression is what a human typed. A parameter shown as 120 mm reads back as 12.
            // Nothing converts it, because nothing has measured the conversion the way the
            // tube measured inertia -- and a factor applied on belief is what this project
            // spent an evening undoing.
            Console.WriteLine();
            Console.WriteLine("[fusion] 'internal' is FUSION'S OWN UNIT: centimetres for length,");
            Console.WriteLine("[fusion] radians for angle. A parameter displayed as 120 mm reads 12.");
            Console.WriteLine("[fusion] Nothing here converts it. Compare a known parameter against");
            Console.WriteLine("[fusion] what Fusion shows before feeding any of this downstream.");
        }

        foreach (var w in result.Run.Warnings)
        {
            Console.WriteLine();
            Console.WriteLine($"[fusion] WARNING: {w}");
        }

        return 0;
    }

    private static async Task<int> SetParamAsync(string[] a)
    {
        var name = GetOption(a, "--name");
        var expression = GetOption(a, "--expression");

        if (name is null || expression is null)
        {
            Console.Error.WriteLine("Usage: fusion setparam --name <parameter> --expression \"120 mm\"");
            Console.Error.WriteLine();
            Console.Error.WriteLine("  Names are case-sensitive. Run 'fusion params' first to see them.");
            Console.Error.WriteLine("  A name that does not exist is REFUSED rather than created: the");
            Console.Error.WriteLine("  add-in's set route creates when missing, so a typo would add a");
            Console.Error.WriteLine("  parameter that drives nothing and leave the intended one alone.");
            return 1;
        }

        var result = await new FusionParameterService(() => OpenSession(a))
            .SetAsync(name, expression);

        if (!result.Succeeded)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("[fusion] SET FAILED");
            Console.Error.WriteLine(result.Run.FailureMessage);
            return 1;
        }

        var updated = result.Parameters[0];
        Console.WriteLine($"[fusion] {updated.Name} = {updated.Expression}");
        Console.WriteLine($"[fusion]     internal {updated.ValueInternal:G8}  unit '{updated.Unit}'");

        foreach (var w in result.Run.Warnings)
        {
            Console.WriteLine();
            Console.WriteLine($"[fusion] WARNING: {w}");
        }

        Console.WriteLine();
        Console.WriteLine("[fusion] Fusion regenerates on a parameter change. `fusion massprops`");
        Console.WriteLine("[fusion] afterwards shows whether the geometry actually moved -- a set");
        Console.WriteLine("[fusion] that took and a set that was accepted and ignored read the same.");
        return 0;
    }

    // ==================================================================
    // Shared
    // ==================================================================
    private static int Report(FusionStageResult result)
    {
        if (!result.Succeeded)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("[fusion] READ REFUSED");
            Console.Error.WriteLine(result.Run.FailureMessage);
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"[fusion] {result.Components.Count} component(s):");
        foreach (var c in result.Components)
        {
            var com = c.CentreOfMass.Length == 3
                ? $"  com=[{string.Join(", ", c.CentreOfMass.Select(v => v.ToString("G6")))}] m"
                : string.Empty;
            var bodies = c.BodyCount is null ? "" : $"  bodies={c.BodyCount}";
            Console.WriteLine($"[fusion]   {c.ComponentName}: {c.MassKg:G8} kg{bodies}{com}");
            if (c.Inertia.Length > 0)
                Console.WriteLine($"[fusion]     inertia (kg*m^2) = [{string.Join(", ", c.Inertia.Select(v => v.ToString("G6")))}]");
        }

        foreach (var w in result.Run.Warnings)
        {
            Console.WriteLine();
            Console.WriteLine($"[fusion] WARNING: {w}");
        }

        return 0;
    }

    private static IFusionSession OpenSession(string[] a)
    {
        var transportText = (GetOption(a, "--transport") ?? "bridge").ToLowerInvariant();
        var transport = transportText switch
        {
            "bridge" => FusionTransport.Bridge,
            "mcp" => FusionTransport.Mcp,
            _ => throw new ArgumentException($"Unknown --transport '{transportText}'. Use bridge or mcp.")
        };

        return FusionSessionFactory.For(transport)();
    }

    private static string? GetOption(string[] a, string name)
    {
        var idx = Array.IndexOf(a, name);
        return idx >= 0 && idx + 1 < a.Length ? a[idx + 1] : null;
    }

    private static bool HasFlag(string[] a, string name) => Array.IndexOf(a, name) >= 0;

    private static double ParseDouble(string? text, double fallback) =>
        double.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : fallback;

    private static int Usage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  fusion ping      [--transport bridge|mcp]");
        Console.Error.WriteLine("  fusion massprops [--transport bridge|mcp]");
        Console.Error.WriteLine("  fusion revolve   [--axis Z] [--angle 360] [--dry-run]");
        Console.Error.WriteLine("  fusion export    --out <path> [--format step|f3d|stl|obj]");
        Console.Error.WriteLine("  fusion params    [--transport bridge|mcp]");
        Console.Error.WriteLine("  fusion setparam  --name <parameter> --expression \"120 mm\"");
        Console.Error.WriteLine();
        Console.Error.WriteLine("  'revolve' builds a hollow tube and checks the returned mass, centre");
        Console.Error.WriteLine("  and inertia against the closed form. '--dry-run' prints the generated");
        Console.Error.WriteLine("  Python and sends nothing -- the only mode that works without Fusion.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("  Fusion must be OPEN with the intended document ACTIVE, and no dialog");
        Console.Error.WriteLine("  showing. Nothing out here can make Fusion open a file.");
        return 1;
    }
}
