using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RulesEngine.Analyzers.Roslyn.Tests;

internal static class CompilationFactory
{
    private static readonly IReadOnlyList<MetadataReference> References = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
        .ToList();

    public static CSharpCompilation Create(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
