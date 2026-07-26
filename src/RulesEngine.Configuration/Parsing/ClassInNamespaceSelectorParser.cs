using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Selectors;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Configuration.Parsing;

public sealed class ClassInNamespaceSelectorParser : ISelectorParser
{
    public string Kind => "class";

    public ITargetSelector Parse(JsonObject node) =>
        new ClassInNamespaceSelector(node.GetRequiredString("namespace"));
}
