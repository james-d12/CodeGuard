using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeGuard.Analyzers.Roslyn.Tests;

public class RoslynDiagnosticExtractorTests
{
    // CS1591 is only reported when the compiler is asked to diagnose missing XML doc comments -
    // plain CompilationFactory.Create (DocumentationMode.Parse, the default) never produces it.
    private static CSharpCompilation CreateWithDocumentationDiagnostics(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose),
            path: "Documented.cs");
        return CSharpCompilation.Create(
            "TestAssembly",
            [tree],
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
                .ToList(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void Extract_ReturnsCS1591_ForUndocumentedPublicMember()
    {
        var compilation = CreateWithDocumentationDiagnostics("""
            namespace Contoso.Domain;

            /// <summary>Documented.</summary>
            public class Foo
            {
                public void Bar() { }
            }
            """);

        var diagnostics = RoslynDiagnosticExtractor.Extract(compilation, "Contoso.Domain");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CS1591", diagnostic.Id);
        Assert.Equal("Contoso.Domain", diagnostic.ProjectName);
        Assert.False(string.IsNullOrEmpty(diagnostic.Message));
        Assert.Equal(6, diagnostic.Line);
        Assert.Equal(17, diagnostic.Column);
    }

    [Fact]
    public void Extract_FiltersOutUnsupportedDiagnosticIds()
    {
        var compilation = CreateWithDocumentationDiagnostics("""
            namespace Contoso.Domain;

            /// <summary>Documented.</summary>
            public class Foo
            {
                public void Bar()
                {
                    int unused;
                }
            }
            """);

        var diagnostics = RoslynDiagnosticExtractor.Extract(compilation, "Contoso.Domain");

        Assert.DoesNotContain(diagnostics, d => d.Id != "CS1591");
    }
}
