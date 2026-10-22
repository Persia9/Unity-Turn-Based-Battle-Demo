using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class ArchitectureTests
{
    private static string AssetsPath([CallerFilePath] string sourceFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile), "../.."));

    [Test] public void ApplicationDoesNotReferencePresentation()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(AssetsPath(), "Scripts/Application"), "*.cs", SearchOption.AllDirectories))
            Assert.That(Regex.IsMatch(File.ReadAllText(file), @"\bPresentation\s*\."), Is.False,
                $"Application must not reference Presentation: {file}");
    }
    [Test] public void DomainDoesNotReferenceExternalLayersOrFrameworks()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(AssetsPath(), "Scripts/Domain"), "*.cs", SearchOption.AllDirectories))
            Assert.That(Regex.IsMatch(File.ReadAllText(file), @"\b(UnityEngine|Zenject|MessagePipe|Infrastructure|Application|Presentation)\s*\."), Is.False,
                $"Domain dependency violation: {file}");
    }
}
