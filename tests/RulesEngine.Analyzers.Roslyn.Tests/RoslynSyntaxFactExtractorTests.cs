using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.Analyzers.Roslyn.Tests;

public class RoslynSyntaxFactExtractorTests
{
    private static ExtractedSyntaxFacts Extract(string source) =>
        RoslynSyntaxFactExtractor.Extract(CompilationFactory.Create(source), "Contoso.Domain");

    [Fact]
    public void Extract_DetectsInvocationCallSite_WithLiteralArgument()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public static class Logger
            {
                public static void Write(string message) { }
            }
            public class Caller
            {
                public void Log() => Logger.Write("hello");
            }
            """);

        var callSite = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.Invocation);
        Assert.Contains("Write", callSite.InvokedMember);
        Assert.Equal("Log", callSite.ContainingMethod);
        Assert.Equal("Contoso.Domain.Caller", callSite.ContainingType);
        var argument = Assert.Single(callSite.Arguments);
        Assert.True(argument.IsLiteral);
        Assert.Equal("hello", argument.LiteralValue);
    }

    [Fact]
    public void Extract_DetectsObjectCreationCallSite()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Client { }
            public class Factory
            {
                public void Create() => new Client();
            }
            """);

        var callSite = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.ObjectCreation);
        Assert.Contains("Client", callSite.InvokedMember);
        Assert.Equal("Contoso.Domain.Client", callSite.TargetTypeName);
    }

    [Fact]
    public void Extract_DetectsMemberAccessCallSite_NotDoubleCountedWithInvocation()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public static class Clock
            {
                public static int Now => 0;
            }
            public class Reader
            {
                public int Read() => Clock.Now;
            }
            """);

        var memberAccess = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.MemberAccess);
        Assert.Contains("Now", memberAccess.InvokedMember);
    }

    [Fact]
    public void Extract_MarksNonLiteralArguments_AsNotLiteral()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public static class Logger
            {
                public static void Write(string message) { }
            }
            public class Caller
            {
                public void Log(string message) => Logger.Write(message);
            }
            """);

        var callSite = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.Invocation);
        var argument = Assert.Single(callSite.Arguments);
        Assert.False(argument.IsLiteral);
        Assert.Null(argument.LiteralValue);
    }

    [Fact]
    public void Extract_ResolvesContainingMethod_ForTopLevelStatements()
    {
        var facts = Extract("""
            public static class Logger
            {
                public static void Write(string message) { }
            }
            Logger.Write("booting");
            """);

        var callSite = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.Invocation);
        Assert.NotEqual("<unknown>", callSite.ContainingMethod);
        Assert.NotEqual("<unknown>", callSite.ContainingType);
    }

    [Fact]
    public void Extract_CapturesEnclosingComparison_ForCountGreaterThanZero()
    {
        var facts = Extract("""
            using System.Collections.Generic;
            using System.Linq;
            namespace Contoso.Domain;
            public class Checker
            {
                public bool HasItems(List<int> items) => items.Count() > 0;
            }
            """);

        var callSite = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.Invocation && cs.InvokedMember.Contains("Count"));
        Assert.Equal(">", callSite.EnclosingComparisonOperator);
        Assert.Equal("0", callSite.EnclosingComparisonValue);
    }

    [Fact]
    public void Extract_DetectsSwitchExpression_ArmLabelsAndDiscard()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public enum Status { Active, Inactive }
            public class Mapper
            {
                public string Map(Status status) => status switch
                {
                    Status.Active => "A",
                    Status.Inactive => "I",
                    _ => "?"
                };
            }
            """);

        var switchModel = Assert.Single(facts.Switches);
        Assert.Equal(3, switchModel.ArmLabels.Count);
        Assert.True(switchModel.HasDefaultOrDiscardArm);
        Assert.Equal("Map", switchModel.ContainingMethod);
    }

    [Fact]
    public void Extract_DetectsSwitchExpression_WithoutDiscard()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public enum Status { Active, Inactive }
            public class Mapper
            {
                public string Map(Status status) => status switch
                {
                    Status.Active => "A",
                    Status.Inactive => "I"
                };
            }
            """);

        var switchModel = Assert.Single(facts.Switches);
        Assert.False(switchModel.HasDefaultOrDiscardArm);
    }

    [Fact]
    public void Extract_DetectsThrowStatement_AsFirstStatement()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Guard
            {
                public void Validate(bool ok)
                {
                    if (!ok) throw new System.InvalidOperationException("bad");
                }
            }
            """);

        var throwSite = Assert.Single(facts.ThrowSites);
        Assert.Contains("InvalidOperationException", throwSite.ExceptionTypeName);
        Assert.Equal("Validate", throwSite.ContainingMethod);
    }

    [Fact]
    public void Extract_DetectsMutationSite_ForPropertyAssignment()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Order
            {
                public int Total { get; set; }
                public void Recalculate() => Total = 42;
            }
            """);

        var mutation = Assert.Single(facts.MutationSites);
        Assert.Equal("Total", mutation.TargetMemberName);
        Assert.Equal("Recalculate", mutation.ContainingMethod);
    }

    [Fact]
    public void Extract_DetectsTryBlock_WithCatchClauseCountAndTypes()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Runner
            {
                public void Run()
                {
                    try { }
                    catch (System.InvalidOperationException) { }
                    catch (System.ArgumentException) { }
                }
            }
            """);

        var tryBlock = Assert.Single(facts.TryBlocks);
        Assert.Equal(2, tryBlock.CatchClauseCount);
        Assert.Contains(tryBlock.CatchTypeNames, t => t.Contains("InvalidOperationException"));
        Assert.Contains(tryBlock.CatchTypeNames, t => t.Contains("ArgumentException"));
    }

    [Fact]
    public void Extract_DetectsMethodBodyShape_SingleBaseCallDelegation()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class BaseRepository
            {
                public virtual void Save() { }
            }
            public class OrderRepository : BaseRepository
            {
                public override void Save() => base.Save();
            }
            """);

        var shape = Assert.Single(facts.MethodBodyShapes, s => s.ContainingType == "Contoso.Domain.OrderRepository");
        Assert.Equal(1, shape.StatementCount);
        Assert.True(shape.IsSingleBaseCallDelegation);
    }

    [Fact]
    public void Extract_DetectsMethodBodyShape_NotDelegation_WhenMultipleStatements()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class BaseRepository
            {
                public virtual void Save() { }
            }
            public class OrderRepository : BaseRepository
            {
                public override void Save()
                {
                    base.Save();
                    System.Console.WriteLine("saved");
                }
            }
            """);

        var shape = Assert.Single(facts.MethodBodyShapes, s => s.ContainingType == "Contoso.Domain.OrderRepository");
        Assert.Equal(2, shape.StatementCount);
        Assert.False(shape.IsSingleBaseCallDelegation);
    }
}
