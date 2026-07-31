using Microsoft.CodeAnalysis.CSharp;

namespace CodeGuard.Analyzers.Roslyn;

public static class RoslynSyntaxFactExtractor
{
    public static ExtractedSyntaxFacts Extract(CSharpCompilation compilation, string projectName)
    {
        var sink = new SyntaxFactSink();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            new SyntaxFactWalker(semanticModel, projectName, sink).Visit(tree.GetRoot());
        }

        return sink.Build();
    }
}
