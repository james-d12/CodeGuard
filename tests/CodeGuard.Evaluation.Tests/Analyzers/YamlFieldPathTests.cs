using CodeGuard.Evaluation.Analyzers;
using YamlDotNet.RepresentationModel;

namespace CodeGuard.Evaluation.Tests.Analyzers;

public class YamlFieldPathTests
{
    private static YamlNode Root(string yaml)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));
        return stream.Documents[0].RootNode;
    }

    [Fact]
    public void Resolve_ReturnsValue_ForSingleSegmentPath()
    {
        var root = Root("typeName: Order\n");

        Assert.Equal("Order", YamlFieldPath.Resolve(root, "typeName"));
    }

    [Fact]
    public void Resolve_ReturnsValue_ForMultiSegmentNestedPath()
    {
        var root = Root("client:\n  typeName: Order\n");

        Assert.Equal("Order", YamlFieldPath.Resolve(root, "client.typeName"));
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenIntermediateSegmentMissing()
    {
        var root = Root("client:\n  other: value\n");

        Assert.Null(YamlFieldPath.Resolve(root, "client.typeName"));
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenLeafSegmentMissing()
    {
        var root = Root("client:\n  typeName: Order\n");

        Assert.Null(YamlFieldPath.Resolve(root, "client.other"));
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenIntermediateNodeIsNotAMapping()
    {
        var root = Root("client: Order\n");

        Assert.Null(YamlFieldPath.Resolve(root, "client.typeName"));
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenLeafIsNotAScalar()
    {
        var root = Root("client:\n  typeName:\n    nested: Order\n");

        Assert.Null(YamlFieldPath.Resolve(root, "client.typeName"));
    }
}
