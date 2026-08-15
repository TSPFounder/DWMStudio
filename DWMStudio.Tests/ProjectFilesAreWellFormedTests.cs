// ProjectFilesAreWellFormedTests.cs
// Parses every project and markup file in the solution as XML.
//
// WHY A TEST FOR SOMETHING THE COMPILER CATCHES
//
// It does catch it, and that is the problem: MSB4025 is a PROJECT LOAD failure, so the build
// stops before compiling anything and the test suite never runs. The feedback arrives from
// whoever typed `dotnet build`, which on this project is not whoever wrote the file. This test
// moves the check to where the rest of the checks are.
//
// THE BUG IT EXISTS FOR HAS HAPPENED TWICE, both times in a comment nobody would reread:
//
//   2026-08-05  ToolWorkspaceWindow.xaml     "-- see the tree" broke the XAML build.
//   2026-08-06  DWMStudio.WorldPackageCli.csproj, DWMStudio.Tests.csproj, DWM.Shared.csproj
//               the same double hyphen, in comments explaining a project reference.
//
// XML forbids "--" inside a comment. An em dash is legal and a double hyphen is not, which is
// a distinction no C# habit prepares anyone for -- and in a .csproj it takes the whole project
// file down rather than the one comment.
//
// (The line above is in a C# comment, where a double hyphen is fine. That contrast is the
// entire trap.)

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class ProjectFilesAreWellFormedTests
    {
        [Fact]
        public void EveryProjectAndMarkupFile_ParsesAsXml()
        {
            var roots = SolutionRoots();
            Assert.NotEmpty(roots);

            var files = roots
                .SelectMany(r => new[] { "*.csproj", "*.xaml", "*.slnx", "*.props", "*.targets" }
                    .SelectMany(pattern => Directory.EnumerateFiles(r, pattern, SearchOption.AllDirectories)))
                .Where(NotBuildOutput)
                .Distinct()
                .ToList();

            // A pass on an empty list would be the same shape as the bugs this project keeps
            // recording: a check that reports success without having looked at anything.
            Assert.True(files.Count >= 5,
                $"Only found {files.Count} project/markup file(s) under [{string.Join(", ", roots)}]. " +
                "The search is anchored on DWMStudio.slnx; if the layout moved, this test is " +
                "no longer checking what it claims to.");

            var broken = files
                .Select(f => (File: f, Error: ParseError(f)))
                .Where(x => x.Error is not null)
                .ToList();

            Assert.True(broken.Count == 0,
                "These files are not well-formed XML, so MSBuild will refuse to load them:\n" +
                string.Join("\n", broken.Select(b => $"  {b.File}\n    {b.Error}")) +
                "\n\nThe usual cause on this project is a DOUBLE HYPHEN inside an XML comment, " +
                "which is illegal and takes the whole file down with MSB4025. Use a comma, or " +
                "an em dash.");
        }

        private static string? ParseError(string path)
        {
            try
            {
                XDocument.Load(path);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// The solution directory, plus DWM.Shared beside it.
        ///
        /// DWM.Shared IS IN A DIFFERENT REPOSITORY and its .csproj is referenced by every
        /// project here, so a malformed comment in it breaks this build too. That is not
        /// hypothetical: the same double hyphen went into DWM.Shared.csproj on 2026-08-06.
        /// </summary>
        private static string[] SolutionRoots()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DWMStudio.slnx")))
                dir = dir.Parent;

            if (dir is null) return Array.Empty<string>();

            var sibling = Path.GetFullPath(Path.Combine(dir.FullName, "..", "DWM.Shared"));

            return Directory.Exists(sibling)
                ? new[] { dir.FullName, sibling }
                : new[] { dir.FullName };
        }

        private static bool NotBuildOutput(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !parts.Any(p =>
                p.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("obj", StringComparison.OrdinalIgnoreCase));
        }
    }
}
