using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Selectors;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public sealed class ClassInNamespaceSelectorParser : ISelectorParser
{
    public string Kind => "class";

    public ITargetSelector Parse(JsonObject node) =>
        new ClassInNamespaceSelector(node.GetRequiredString("namespace"));
}
