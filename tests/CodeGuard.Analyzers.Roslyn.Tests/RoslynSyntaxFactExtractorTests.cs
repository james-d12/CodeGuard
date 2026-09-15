using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.Analyzers.Roslyn.Tests;

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
    public void Extract_DetectsObjectCreationCallSite_WithConstructorArgument()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Client
            {
                public Client(string name) { }
            }
            public class Factory
            {
                public void Create() => new Client("acme");
            }
            """);

        var callSite = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.ObjectCreation);
        var argument = Assert.Single(callSite.Arguments);
        Assert.True(argument.IsLiteral);
        Assert.Equal("acme", argument.LiteralValue);
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
        Assert.Contains("_", switchModel.ArmLabels);
    }

    [Fact]
    public void Extract_DetectsSwitchStatement_ArmLabelsAndDefault()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public enum Status { Active, Inactive }
            public class Mapper
            {
                public string Format(string value) => value;
                public string Map(Status status)
                {
                    switch (status)
                    {
                        case Status.Active:
                            return Format("A");
                        default:
                            return "?";
                    }
                }
            }
            """);

        var switchModel = Assert.Single(facts.Switches);
        Assert.Contains("Status.Active", switchModel.ArmLabels);
        Assert.Contains("default", switchModel.ArmLabels);
        Assert.True(switchModel.HasDefaultOrDiscardArm);
        Assert.Contains(facts.CallSites, cs => cs.InvokedMember.Contains("Format"));
    }

    [Fact]
    public void Extract_DetectsSwitchStatement_WithoutDefault()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public enum Status { Active, Inactive }
            public class Mapper
            {
                public string Map(Status status)
                {
                    switch (status)
                    {
                        case Status.Active:
                            return "A";
                        case Status.Inactive:
                            return "I";
                    }
                    return "?";
                }
            }
            """);

        var switchModel = Assert.Single(facts.Switches);
        Assert.False(switchModel.HasDefaultOrDiscardArm);
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
    public void Extract_DetectsThrowStatement_WhenNotFirstStatementInBlock()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Guard
            {
                public void Validate(bool ok)
                {
                    System.Console.WriteLine("checking");
                    throw new System.InvalidOperationException("bad");
                }
            }
            """);

        var throwSite = Assert.Single(facts.ThrowSites);
        Assert.False(throwSite.IsFirstStatementInMethod);
    }

    [Fact]
    public void Extract_DetectsRethrow_WithoutExceptionType()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Guard
            {
                public void Handle()
                {
                    try
                    {
                        Risky();
                    }
                    catch (System.Exception)
                    {
                        throw;
                    }
                }
                public void Risky() { }
            }
            """);

        var throwSite = Assert.Single(facts.ThrowSites);
        Assert.Null(throwSite.ExceptionTypeName);
    }

    [Fact]
    public void Extract_DetectsMutationSite_ForPropertyAssignment()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Order
            {
                public int Total { get; set; }
                public int Compute() => 42;
                public void Recalculate() => Total = Compute();
            }
            """);

        var mutation = Assert.Single(facts.MutationSites);
        Assert.Equal("Total", mutation.TargetMemberName);
        Assert.Equal("Recalculate", mutation.ContainingMethod);
        Assert.Contains(facts.CallSites, cs => cs.InvokedMember.Contains("Compute"));
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
                    try { Compute(); }
                    catch (System.InvalidOperationException) { }
                    catch (System.ArgumentException) { }
                    catch { }
                }
                public int Compute() => 1;
            }
            """);

        var tryBlock = Assert.Single(facts.TryBlocks);
        Assert.Equal(3, tryBlock.CatchClauseCount);
        Assert.Contains(tryBlock.CatchTypeNames, t => t.Contains("InvalidOperationException"));
        Assert.Contains(tryBlock.CatchTypeNames, t => t.Contains("ArgumentException"));
        Assert.Contains(tryBlock.CatchTypeNames, t => t == "System.Exception");
        Assert.Contains(facts.CallSites, cs => cs.InvokedMember.Contains("Compute"));
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

    [Fact]
    public void Extract_DetectsMethodBodyShape_NoBody()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public abstract class Base
            {
                public abstract void DoWork();
            }
            """);

        var shape = Assert.Single(facts.MethodBodyShapes, s => s.ContainingType == "Contoso.Domain.Base");
        Assert.Equal(0, shape.StatementCount);
        Assert.False(shape.IsSingleBaseCallDelegation);
        Assert.Equal(4, shape.Line);
        Assert.Equal("Test.cs", shape.FilePath);
    }

    [Fact]
    public void Extract_DetectsMethodBodyShape_SingleStatement_NotBaseDelegation()
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
                    if (true) { }
                }
            }
            """);

        var shape = Assert.Single(facts.MethodBodyShapes, s => s.ContainingType == "Contoso.Domain.OrderRepository");
        Assert.Equal(1, shape.StatementCount);
        Assert.False(shape.IsSingleBaseCallDelegation);
    }

    [Fact]
    public void Extract_RecursesIntoNestedExpressions_AcrossVisitedConstructs()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Widget
            {
                public string Name { get; set; } = "";
            }
            public class Client
            {
                public Client(string name) { }
            }
            public class Service
            {
                public string Describe(Widget w) => w.Name.ToString();
                public string GetName() => "n";
                public Widget GetWidget() => new Widget();
                public string DescribeReceiver() => GetWidget().Name;
                public void CreateClient() => new Client(GetName());
                public void Throwing() => throw new System.InvalidOperationException(GetName());
                public string Switching(int x) => x switch { 1 => GetName(), _ => "?" };
                public string Format(string value) => value;
                public void FormatCall() => Format(GetName());
            }
            """);

        // reached only via VisitMemberAccessExpression's early-return-branch recursion (base.Visit at the "is
        // invocation receiver" skip) into the nested "w.Name" member access. Scoped to "Describe" specifically -
        // "GetWidget().Name" below would also match a looser "InvokedMember.Contains(Name)" check.
        Assert.Contains(facts.CallSites, cs => cs.Kind == CallSiteKind.MemberAccess && cs.InvokedMember.Contains("Name") && cs.ContainingMethod == "Describe");

        // reached only via VisitMemberAccessExpression's fallthrough recursion (base.Visit at the end of the
        // method) into the nested "GetWidget()" invocation behind a non-invocation member access.
        Assert.Contains(facts.CallSites, cs => cs.Kind == CallSiteKind.Invocation && cs.InvokedMember.Contains("GetWidget"));

        // reached only via VisitObjectCreationExpression's base.Visit into the constructor argument.
        Assert.Contains(facts.CallSites, cs => cs.InvokedMember.Contains("GetName") && cs.ContainingMethod == "CreateClient");

        // reached only via VisitSwitchExpression's base.Visit into the arm expression.
        Assert.Contains(facts.CallSites, cs => cs.InvokedMember.Contains("GetName") && cs.ContainingMethod == "Switching");

        // the throw *expression* site itself is only recorded if RecordThrowSite runs (separate from recursion).
        Assert.Contains(facts.ThrowSites, t => t.ContainingMethod == "Throwing" && t.ExceptionTypeName != null && t.ExceptionTypeName.Contains("InvalidOperationException"));

        // the nested GetName() call is only reached via VisitThrowExpression's own base.Visit into the
        // exception-constructor argument (distinct from RecordThrowSite running, above).
        Assert.Contains(facts.CallSites, cs => cs.InvokedMember.Contains("GetName") && cs.ContainingMethod == "Throwing");

        // reached only via VisitInvocationExpression's own base.Visit into its argument list (invocation nested
        // inside another invocation's argument, as opposed to inside an object creation's argument above).
        Assert.Contains(facts.CallSites, cs => cs.InvokedMember.Contains("GetName") && cs.ContainingMethod == "FormatCall");
    }

    [Fact]
    public void Extract_DetectsThrowStatement_RecursesIntoExceptionConstructorArguments()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Guard
            {
                public string GetMessage() => "bad";
                public void Validate()
                {
                    throw new System.InvalidOperationException(GetMessage());
                }
            }
            """);

        // reached only via VisitThrowStatement's base.Visit into the exception constructor's argument.
        Assert.Contains(facts.CallSites, cs => cs.InvokedMember.Contains("GetMessage"));
    }

    [Fact]
    public void Extract_DoesNotDoubleCount_DelegatePropertyInvokedDirectly()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Worker
            {
                public System.Action DoIt { get; set; } = () => { };
                public void Run() => this.DoIt();
            }
            """);

        Assert.DoesNotContain(facts.CallSites, cs => cs.Kind == CallSiteKind.MemberAccess && cs.InvokedMember.Contains("DoIt"));
    }

    [Fact]
    public void Extract_DoesNotCaptureEnclosingComparison_ForNonInvocationCallSites()
    {
        var facts = Extract("""
            namespace Contoso.Domain;
            public class Widget
            {
                public int Count { get; set; }
            }
            public class Checker
            {
                public bool HasItems(Widget w) => w.Count > 0;
            }
            """);

        var callSite = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.MemberAccess);
        Assert.Null(callSite.EnclosingComparisonOperator);
        Assert.Null(callSite.EnclosingComparisonValue);
    }

    [Fact]
    public void Extract_PopulatesFilePathLineAndColumn_ForCallSite()
    {
        var facts = RoslynSyntaxFactExtractor.Extract(
            CompilationFactory.Create("""
                namespace Contoso.Domain;
                public static class Logger
                {
                    public static void Write(string message) { }
                }
                public class Caller
                {
                    public void Log() => Logger.Write("hello");
                }
                """, path: "Contoso/Caller.cs"),
            "Contoso.Domain");

        var callSite = Assert.Single(facts.CallSites, cs => cs.Kind == CallSiteKind.Invocation);
        Assert.Equal("Contoso/Caller.cs", callSite.FilePath);
        Assert.Equal(8, callSite.Line);
        Assert.Equal(26, callSite.Column);
    }
}
