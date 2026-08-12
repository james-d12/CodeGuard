using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Configuration.Parsing;

namespace CodeGuard.Configuration.Testing;

/// <summary>
/// Builds a <see cref="RepositoryModel"/> directly from a rule test's <c>setup:</c> block - no disk,
/// no Roslyn/MSBuild. See docs/RULES_TEST_DESIGN.md for the setup shape, which now also covers the
/// syntax-fact records (switches, throw sites, mutation sites, try blocks, method-body shapes, and
/// raw diagnostics) consumed by the `switch`/`throw_site`/`mutation_site`/`try_block`/
/// `method_body_shape`/`diagnostic` selectors - these are supplied directly as flat records in the
/// setup block, not derived from real source, since virtual rule test setup never runs Roslyn.
/// </summary>
public static class TestSetupBuilder
{
    private const string DefaultProjectName = "TestProject";
    private const string DefaultSolutionPath = "TestSolution.sln";
    private const string VirtualRootPath = "<virtual>";

    public static RepositoryModel Build(JsonObject setup)
    {
        var projects = new List<ProjectModel>();

        if (setup["projects"]?.AsArray() is { } projectsNode)
        {
            projects.AddRange(projectsNode.Select(node => ParseProject(node!.AsObject())));
        }

        if (setup["types"]?.AsArray() is { } typesNode)
        {
            var types = typesNode.Select(node => ParseType(node!.AsObject(), DefaultProjectName)).ToList();
            projects.Add(new ProjectModel(
                DefaultProjectName, DefaultProjectName, "net10.0", "Microsoft.NET.Sdk", [], [],
                new Dictionary<string, string>(), types));
        }

        var solutions = projects.Count == 0
            ? []
            : new List<SolutionModel> { new(DefaultSolutionPath, projects) };

        var files = setup["files"]?.AsArray()?.Select(node => ParseFile(node!.AsObject())).ToList() ?? [];
        var callSites = setup["callSites"]?.AsArray()?.Select(node => ParseCallSite(node!.AsObject())).ToList() ?? [];
        var switches = setup["switches"]?.AsArray()?.Select(node => ParseSwitch(node!.AsObject())).ToList() ?? [];
        var throwSites = setup["throwSites"]?.AsArray()?.Select(node => ParseThrowSite(node!.AsObject())).ToList() ?? [];
        var mutationSites = setup["mutationSites"]?.AsArray()?.Select(node => ParseMutationSite(node!.AsObject())).ToList() ?? [];
        var tryBlocks = setup["tryBlocks"]?.AsArray()?.Select(node => ParseTryBlock(node!.AsObject())).ToList() ?? [];
        var methodBodyShapes = setup["methodBodyShapes"]?.AsArray()?.Select(node => ParseMethodBodyShape(node!.AsObject())).ToList() ?? [];
        var diagnostics = setup["diagnostics"]?.AsArray()?.Select(node => ParseDiagnostic(node!.AsObject())).ToList() ?? [];
        var directories = setup.GetStringArray("directories");

        return new RepositoryModel(
            VirtualRootPath, solutions, files, callSites, switches, throwSites, mutationSites, tryBlocks,
            methodBodyShapes, diagnostics)
        {
            Directories = directories
        };
    }

    private static SwitchModel ParseSwitch(JsonObject node) => new(
        node.GetOptionalString("containingMethod") ?? "",
        node.GetOptionalString("containingType") ?? "",
        node.GetOptionalString("project") ?? DefaultProjectName,
        node.GetStringArray("armLabels"),
        node.GetOptionalBool("hasDefaultOrDiscardArm", false),
        node.GetOptionalString("filePath") ?? "",
        node.GetOptionalInt("line") ?? 0);

    private static ThrowSiteModel ParseThrowSite(JsonObject node) => new(
        node.GetOptionalString("containingMethod") ?? "",
        node.GetOptionalString("containingType") ?? "",
        node.GetOptionalString("project") ?? DefaultProjectName,
        node.GetOptionalString("exceptionTypeName"),
        node.GetOptionalBool("isFirstStatementInMethod", false),
        node.GetOptionalString("filePath") ?? "",
        node.GetOptionalInt("line") ?? 0);

    private static MutationSiteModel ParseMutationSite(JsonObject node) => new(
        node.GetOptionalString("containingMethod") ?? "",
        node.GetOptionalString("containingType") ?? "",
        node.GetRequiredString("targetMemberName"),
        node.GetOptionalString("project") ?? DefaultProjectName,
        node.GetOptionalString("filePath") ?? "",
        node.GetOptionalInt("line") ?? 0);

    private static TryBlockModel ParseTryBlock(JsonObject node) => new(
        node.GetOptionalString("containingMethod") ?? "",
        node.GetOptionalString("containingType") ?? "",
        node.GetOptionalString("project") ?? DefaultProjectName,
        node.GetOptionalInt("catchClauseCount") ?? 0,
        node.GetStringArray("catchTypeNames"),
        node.GetOptionalString("filePath") ?? "",
        node.GetOptionalInt("line") ?? 0);

    private static MethodBodyShapeModel ParseMethodBodyShape(JsonObject node) => new(
        node.GetOptionalString("containingMethod") ?? "",
        node.GetOptionalString("containingType") ?? "",
        node.GetOptionalString("project") ?? DefaultProjectName,
        node.GetOptionalInt("statementCount") ?? 0,
        node.GetOptionalBool("isSingleBaseCallDelegation", false),
        node.GetOptionalString("filePath") ?? "",
        node.GetOptionalInt("line") ?? 0);

    private static DiagnosticModel ParseDiagnostic(JsonObject node) => new(
        node.GetRequiredString("id"),
        node.GetOptionalString("message") ?? "",
        node.GetOptionalString("project") ?? DefaultProjectName,
        node.GetOptionalString("filePath") ?? "",
        node.GetOptionalInt("line") ?? 0,
        node.GetOptionalInt("column") ?? 0);

    private static FileModel ParseFile(JsonObject node)
    {
        var path = node.GetRequiredString("path");
        var relativePath = node.GetOptionalString("relativePath") ?? path;
        var extension = node.GetOptionalString("extension") ?? Path.GetExtension(path);
        var content = node.GetOptionalString("content");
        return new FileModel(path, relativePath, extension, content);
    }

    private static ProjectModel ParseProject(JsonObject node)
    {
        var name = node.GetRequiredString("name");
        var path = node.GetOptionalString("path") ?? name;
        var targetFramework = node.GetOptionalString("targetFramework") ?? "net10.0";
        var sdk = node.GetOptionalString("sdk") ?? "Microsoft.NET.Sdk";
        var projectReferences = node.GetStringArray("projectReferences");
        var packageReferences = node["packageReferences"]?.AsArray()
            .Select(n => ParsePackageReference(n!.AsObject())).ToList() ?? [];
        var properties = ParseStringMap(node["properties"]?.AsObject());
        var types = node["types"]?.AsArray().Select(n => ParseType(n!.AsObject(), name)).ToList() ?? [];

        return new ProjectModel(name, path, targetFramework, sdk, projectReferences, packageReferences, properties, types);
    }

    private static PackageReferenceModel ParsePackageReference(JsonObject node) =>
        new(node.GetRequiredString("id"), node.GetOptionalString("version") ?? "");

    private static TypeModel ParseType(JsonObject node, string defaultProjectName)
    {
        var name = node.GetRequiredString("name");
        var @namespace = node.GetOptionalString("namespace") ?? "";
        var fullName = node.GetOptionalString("fullName") ?? (@namespace.Length == 0 ? name : $"{@namespace}.{name}");
        var kind = node.GetOptionalString("kind") is { } kindValue
            ? EnumParsing.ParseSnakeCase<TypeKind>(kindValue)
            : TypeKind.Class;
        var baseType = node.GetOptionalString("baseType");
        var interfaces = node.GetStringArray("interfaces");
        var accessibility = ParseAccessibility(node, "accessibility");
        var modifiers = ParseTypeModifiers(node);
        var attributes = ParseAttributes(node);
        var projectName = node.GetOptionalString("project") ?? defaultProjectName;
        var filePath = node.GetOptionalString("filePath") ?? "";
        var line = node.GetOptionalInt("line") ?? 0;
        var column = node.GetOptionalInt("column") ?? 0;

        var methods = node["methods"]?.AsArray().Select(n => ParseMethod(n!.AsObject(), fullName, projectName)).ToList() ?? [];
        var properties = node["properties"]?.AsArray().Select(n => ParseProperty(n!.AsObject(), fullName, projectName)).ToList() ?? [];
        var constructors = node["constructors"]?.AsArray().Select(n => ParseConstructor(n!.AsObject(), fullName, projectName)).ToList() ?? [];
        var fields = node["fields"]?.AsArray().Select(n => ParseField(n!.AsObject(), fullName, projectName)).ToList() ?? [];

        return new TypeModel(
            name, fullName, @namespace, kind, baseType, interfaces, accessibility, modifiers, attributes,
            methods, properties, constructors, fields, projectName, filePath, line, column);
    }

    private static MethodModel ParseMethod(JsonObject node, string declaringType, string defaultProjectName)
    {
        var name = node.GetRequiredString("name");
        var returnType = node.GetOptionalString("returnType") ?? "void";
        var parameters = node["parameters"]?.AsArray().Select(n => ParseParameter(n!.AsObject())).ToList() ?? [];
        var accessibility = ParseAccessibility(node, "accessibility");
        var modifiers = ParseMethodModifiers(node);
        var attributes = ParseAttributes(node);
        var projectName = node.GetOptionalString("project") ?? defaultProjectName;
        var filePath = node.GetOptionalString("filePath") ?? "";
        var line = node.GetOptionalInt("line") ?? 0;
        var column = node.GetOptionalInt("column") ?? 0;

        return new MethodModel(name, returnType, parameters, accessibility, modifiers, attributes, declaringType, projectName, filePath, line, column);
    }

    private static PropertyModel ParseProperty(JsonObject node, string declaringType, string defaultProjectName)
    {
        var name = node.GetRequiredString("name");
        var type = node.GetOptionalString("type") ?? "object";
        var accessibility = ParseAccessibility(node, "accessibility");
        var hasGetter = node.GetOptionalBool("hasGetter", true);
        var hasSetter = node.GetOptionalBool("hasSetter", false);
        var setterAccessibility = node.GetOptionalString("setterAccessibility") is { } setterValue
            ? EnumParsing.ParseSnakeCase<Accessibility>(setterValue)
            : (Accessibility?)null;
        var isRequired = node.GetOptionalBool("isRequired", false);
        var isInit = node.GetOptionalBool("isInit", false);
        var isStatic = node.GetOptionalBool("isStatic", false);
        var attributes = ParseAttributes(node);
        var projectName = node.GetOptionalString("project") ?? defaultProjectName;
        var filePath = node.GetOptionalString("filePath") ?? "";
        var line = node.GetOptionalInt("line") ?? 0;
        var column = node.GetOptionalInt("column") ?? 0;

        return new PropertyModel(
            name, type, accessibility, hasGetter, hasSetter, setterAccessibility, isRequired, isInit, isStatic,
            attributes, declaringType, projectName, filePath, line, column);
    }

    private static ConstructorModel ParseConstructor(JsonObject node, string declaringType, string defaultProjectName)
    {
        var accessibility = ParseAccessibility(node, "accessibility");
        var parameters = node["parameters"]?.AsArray().Select(n => ParseParameter(n!.AsObject())).ToList() ?? [];
        var attributes = ParseAttributes(node);
        var projectName = node.GetOptionalString("project") ?? defaultProjectName;
        var filePath = node.GetOptionalString("filePath") ?? "";
        var line = node.GetOptionalInt("line") ?? 0;
        var column = node.GetOptionalInt("column") ?? 0;

        return new ConstructorModel(accessibility, parameters, attributes, declaringType, projectName, filePath, line, column);
    }

    private static FieldModel ParseField(JsonObject node, string declaringType, string defaultProjectName)
    {
        var name = node.GetRequiredString("name");
        var type = node.GetOptionalString("type") ?? "object";
        var accessibility = ParseAccessibility(node, "accessibility");
        var modifiers = ParseFieldModifiers(node);
        var attributes = ParseAttributes(node);
        var projectName = node.GetOptionalString("project") ?? defaultProjectName;
        var filePath = node.GetOptionalString("filePath") ?? "";
        var line = node.GetOptionalInt("line") ?? 0;
        var column = node.GetOptionalInt("column") ?? 0;
        var constantValue = node.GetOptionalString("constantValue");

        return new FieldModel(name, type, accessibility, modifiers, attributes, declaringType, projectName, filePath, line, column, constantValue);
    }

    private static ParameterModel ParseParameter(JsonObject node)
    {
        var name = node.GetRequiredString("name");
        var type = node.GetOptionalString("type") ?? "object";
        var attributes = ParseAttributes(node);
        var hasDefaultValue = node.GetOptionalBool("hasDefaultValue", false);
        return new ParameterModel(name, type, attributes, hasDefaultValue);
    }

    private static IReadOnlyList<AttributeModel> ParseAttributes(JsonObject node) =>
        node["attributes"]?.AsArray().Select(n => ParseAttribute(n!.AsObject())).ToList() ?? [];

    private static AttributeModel ParseAttribute(JsonObject node)
    {
        var typeName = node.GetRequiredString("typeName");
        var constructorArgumentLiterals = node.GetStringArray("constructorArgumentLiterals");
        var namedArguments = ParseStringMap(node["namedArguments"]?.AsObject());
        return new AttributeModel(typeName, constructorArgumentLiterals, namedArguments);
    }

    private static CallSiteModel ParseCallSite(JsonObject node)
    {
        var kind = node.GetOptionalString("kind") is { } kindValue
            ? EnumParsing.ParseSnakeCase<CallSiteKind>(kindValue)
            : CallSiteKind.Invocation;
        var invokedMember = node.GetRequiredString("invokedMember");
        var targetTypeName = node.GetOptionalString("targetTypeName");
        var containingMethod = node.GetOptionalString("containingMethod") ?? "";
        var containingType = node.GetOptionalString("containingType") ?? "";
        var projectName = node.GetOptionalString("project") ?? DefaultProjectName;
        var arguments = node["arguments"]?.AsArray().Select(n => ParseCallSiteArgument(n!.AsObject())).ToList() ?? [];
        var filePath = node.GetOptionalString("filePath") ?? "";
        var line = node.GetOptionalInt("line") ?? 0;
        var column = node.GetOptionalInt("column") ?? 0;
        var enclosingComparisonOperator = node.GetOptionalString("enclosingComparisonOperator");
        var enclosingComparisonValue = node.GetOptionalString("enclosingComparisonValue");

        return new CallSiteModel(
            kind, invokedMember, targetTypeName, containingMethod, containingType, projectName, arguments,
            filePath, line, column, enclosingComparisonOperator, enclosingComparisonValue);
    }

    private static CallSiteArgument ParseCallSiteArgument(JsonObject node)
    {
        var index = node.GetOptionalInt("index") ?? 0;
        var literalValue = node.GetOptionalString("literalValue");
        var isLiteral = node.GetOptionalBool("isLiteral", literalValue is not null);
        return new CallSiteArgument(index, literalValue, isLiteral);
    }

    private static Accessibility ParseAccessibility(JsonObject node, string property) =>
        node.GetOptionalString(property) is { } value
            ? EnumParsing.ParseSnakeCase<Accessibility>(value)
            : Accessibility.Public;

    private static TypeModifiers ParseTypeModifiers(JsonObject node)
    {
        var result = TypeModifiers.None;
        foreach (var token in node.GetStringArray("modifiers"))
        {
            result |= EnumParsing.ParseSnakeCase<TypeModifiers>(token);
        }

        return result;
    }

    private static MethodModifiers ParseMethodModifiers(JsonObject node)
    {
        var result = MethodModifiers.None;
        foreach (var token in node.GetStringArray("modifiers"))
        {
            result |= EnumParsing.ParseSnakeCase<MethodModifiers>(token);
        }

        return result;
    }

    private static FieldModifiers ParseFieldModifiers(JsonObject node)
    {
        var result = FieldModifiers.None;
        foreach (var token in node.GetStringArray("modifiers"))
        {
            result |= EnumParsing.ParseSnakeCase<FieldModifiers>(token);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> ParseStringMap(JsonObject? node)
    {
        if (node is null)
        {
            return new Dictionary<string, string>();
        }

        return node.ToDictionary(pair => pair.Key, pair => pair.Value?.GetValue<string>() ?? "");
    }
}
