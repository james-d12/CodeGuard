using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.Analyzers.Roslyn;

internal sealed class SyntaxFactWalker(SemanticModel semanticModel, string projectName, SyntaxFactSink sink)
    : CSharpSyntaxWalker
{
    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        RecordCallSite(node, CallSiteKind.Invocation, node.ArgumentList.Arguments);
        base.VisitInvocationExpression(node);
    }

    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        RecordCallSite(node, CallSiteKind.ObjectCreation, node.ArgumentList?.Arguments ?? default);
        base.VisitObjectCreationExpression(node);
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        // The receiver of an invocation (e.g. "Console" in "Console.WriteLine(...)") is itself a
        // MemberAccessExpressionSyntax; skip it here so the call isn't recorded twice.
        if (node.Parent is InvocationExpressionSyntax invocation && invocation.Expression == node)
        {
            base.VisitMemberAccessExpression(node);
            return;
        }

        // Only record accesses to an actual member (field/property) - a MemberAccessExpressionSyntax
        // is also how nested namespace/type qualifiers parse (e.g. "System" in "System.Console"),
        // which aren't call sites.
        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol is IPropertySymbol or IFieldSymbol)
        {
            RecordCallSite(node, CallSiteKind.MemberAccess, default);
        }

        base.VisitMemberAccessExpression(node);
    }

    public override void VisitSwitchStatement(SwitchStatementSyntax node)
    {
        var labels = node.Sections.SelectMany(s => s.Labels).ToList();
        var armLabels = labels.Select(DescribeLabel).ToList();
        var hasDefault = labels.Any(l => l is DefaultSwitchLabelSyntax);
        RecordSwitch(node, armLabels, hasDefault);
        base.VisitSwitchStatement(node);
    }

    public override void VisitSwitchExpression(SwitchExpressionSyntax node)
    {
        var armLabels = node.Arms.Select(a => DescribePattern(a.Pattern)).ToList();
        var hasDefault = node.Arms.Any(a => a.Pattern is DiscardPatternSyntax);
        RecordSwitch(node, armLabels, hasDefault);
        base.VisitSwitchExpression(node);
    }

    public override void VisitThrowStatement(ThrowStatementSyntax node)
    {
        RecordThrowSite(node, node.Expression);
        base.VisitThrowStatement(node);
    }

    public override void VisitThrowExpression(ThrowExpressionSyntax node)
    {
        RecordThrowSite(node, node.Expression);
        base.VisitThrowExpression(node);
    }

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node.Left).Symbol;
        if (symbol is IPropertySymbol or IFieldSymbol)
        {
            RecordMutationSite(node, symbol.Name);
        }

        base.VisitAssignmentExpression(node);
    }

    public override void VisitTryStatement(TryStatementSyntax node)
    {
        var catchTypeNames = node.Catches
            .Select(c => c.Declaration?.Type is { } type
                ? semanticModel.GetTypeInfo(type).Type?.ToDisplayString() ?? type.ToString()
                : "System.Exception")
            .ToList();
        RecordTryBlock(node, node.Catches.Count, catchTypeNames);
        base.VisitTryStatement(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        RecordMethodBodyShape(node);
        base.VisitMethodDeclaration(node);
    }

    private void RecordCallSite(SyntaxNode node, CallSiteKind kind, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol is null)
        {
            return;
        }

        var invokedMember = symbol.ContainingType is not null
            ? $"{symbol.ContainingType.ToDisplayString()}.{symbol.Name}"
            : symbol.Name;

        var callArguments = arguments
            .Select((argument, index) =>
            {
                var constant = semanticModel.GetConstantValue(argument.Expression);
                return new CallSiteArgument(index, constant.HasValue ? constant.Value?.ToString() : null, constant.HasValue);
            })
            .ToList();

        var (containingMethod, containingType, filePath, line, column) = DescribeLocation(node);
        var (comparisonOperator, comparisonValue) = kind == CallSiteKind.Invocation
            ? TryGetEnclosingComparison(node)
            : (null, null);

        sink.AddCallSite(new CallSiteModel(
            Kind: kind,
            InvokedMember: invokedMember,
            TargetTypeName: symbol.ContainingType?.ToDisplayString(),
            ContainingMethod: containingMethod,
            ContainingType: containingType,
            ProjectName: projectName,
            Arguments: callArguments,
            FilePath: filePath,
            Line: line,
            Column: column,
            EnclosingComparisonOperator: comparisonOperator,
            EnclosingComparisonValue: comparisonValue));
    }

    private (string? Operator, string? Value) TryGetEnclosingComparison(SyntaxNode node)
    {
        if (node.Parent is BinaryExpressionSyntax { Left: var left, Right: var right } binary && left == node)
        {
            var constant = semanticModel.GetConstantValue(right);
            if (constant.HasValue)
            {
                return (binary.OperatorToken.Text, constant.Value?.ToString());
            }
        }

        return (null, null);
    }

    private void RecordSwitch(SyntaxNode node, IReadOnlyList<string> armLabels, bool hasDefaultOrDiscard)
    {
        var (containingMethod, containingType, filePath, line, _) = DescribeLocation(node);
        sink.AddSwitch(new SwitchModel(containingMethod, containingType, projectName, armLabels, hasDefaultOrDiscard, filePath, line));
    }

    private void RecordThrowSite(SyntaxNode node, ExpressionSyntax? expression)
    {
        var exceptionTypeName = expression is not null ? semanticModel.GetTypeInfo(expression).Type?.ToDisplayString() : null;
        var isFirst = IsFirstStatementInEnclosingBlock(node);
        var (containingMethod, containingType, filePath, line, _) = DescribeLocation(node);
        sink.AddThrowSite(new ThrowSiteModel(containingMethod, containingType, projectName, exceptionTypeName, isFirst, filePath, line));
    }

    private void RecordMutationSite(SyntaxNode node, string targetMemberName)
    {
        var (containingMethod, containingType, filePath, line, _) = DescribeLocation(node);
        sink.AddMutationSite(new MutationSiteModel(containingMethod, containingType, targetMemberName, projectName, filePath, line));
    }

    private void RecordTryBlock(SyntaxNode node, int catchClauseCount, IReadOnlyList<string> catchTypeNames)
    {
        var (containingMethod, containingType, filePath, line, _) = DescribeLocation(node);
        sink.AddTryBlock(new TryBlockModel(containingMethod, containingType, projectName, catchClauseCount, catchTypeNames, filePath, line));
    }

    private void RecordMethodBodyShape(MethodDeclarationSyntax node)
    {
        int statementCount;
        bool isSingleBaseCallDelegation;

        if (node.ExpressionBody is not null)
        {
            statementCount = 1;
            isSingleBaseCallDelegation = IsBaseDelegationCall(node.ExpressionBody.Expression, node.Identifier.Text);
        }
        else if (node.Body is not null)
        {
            statementCount = node.Body.Statements.Count;
            isSingleBaseCallDelegation = statementCount == 1 && IsBaseDelegationStatement(node.Body.Statements[0], node.Identifier.Text);
        }
        else
        {
            statementCount = 0;
            isSingleBaseCallDelegation = false;
        }

        var methodSymbol = semanticModel.GetDeclaredSymbol(node);
        var containingType = methodSymbol?.ContainingType?.ToDisplayString() ?? "<unknown>";
        var location = node.GetLocation();
        var lineSpan = location.GetLineSpan();

        sink.AddMethodBodyShape(new MethodBodyShapeModel(
            node.Identifier.Text, containingType, projectName, statementCount, isSingleBaseCallDelegation,
            location.SourceTree?.FilePath ?? string.Empty, lineSpan.StartLinePosition.Line + 1));
    }

    private static bool IsBaseDelegationStatement(StatementSyntax statement, string methodName) => statement switch
    {
        ExpressionStatementSyntax expressionStatement => IsBaseDelegationCall(expressionStatement.Expression, methodName),
        ReturnStatementSyntax { Expression: { } expression } => IsBaseDelegationCall(expression, methodName),
        _ => false
    };

    private static bool IsBaseDelegationCall(ExpressionSyntax expression, string methodName) =>
        expression is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Expression: BaseExpressionSyntax,
                Name.Identifier.Text: var calledName
            }
        } && calledName == methodName;

    private static bool IsFirstStatementInEnclosingBlock(SyntaxNode node)
    {
        var statement = node.FirstAncestorOrSelf<StatementSyntax>();
        return statement?.Parent is not BlockSyntax block || block.Statements.FirstOrDefault() == statement;
    }

    private (string ContainingMethod, string ContainingType, string FilePath, int Line, int Column) DescribeLocation(SyntaxNode node)
    {
        var enclosingSymbol = semanticModel.GetEnclosingSymbol(node.SpanStart);
        var containingMethod = (enclosingSymbol as IMethodSymbol)?.Name ?? enclosingSymbol?.Name ?? "<unknown>";
        var containingType = enclosingSymbol?.ContainingType?.ToDisplayString() ?? "<unknown>";

        var location = node.GetLocation();
        var lineSpan = location.GetLineSpan();

        return (
            containingMethod,
            containingType,
            location.SourceTree?.FilePath ?? string.Empty,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1);
    }

    private static string DescribeLabel(SwitchLabelSyntax label) => label switch
    {
        CaseSwitchLabelSyntax c => c.Value.ToString(),
        CasePatternSwitchLabelSyntax p => DescribePattern(p.Pattern),
        DefaultSwitchLabelSyntax => "default",
        _ => label.ToString()
    };

    private static string DescribePattern(PatternSyntax pattern) => pattern switch
    {
        ConstantPatternSyntax c => c.Expression.ToString(),
        DeclarationPatternSyntax d => d.Type.ToString(),
        DiscardPatternSyntax => "_",
        _ => pattern.ToString()
    };
}
