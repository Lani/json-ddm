using System.Text.Json.Nodes;

namespace JsonDdm.Tests;

public class PatchDeleteTests
{
    private readonly JsonDdm _merger = new();

    [Fact]
    public void Merge_PatchDelete_RemovesKey()
    {
        var baseNode = JsonNode.Parse("{\"a\": 1, \"b\": 2}");
        var overrideNode = JsonNode.Parse("{\"a\": {\"$patch\": \"delete\"}}");
        var result = _merger.Merge(baseNode, overrideNode);

        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.False(obj.ContainsKey("a"));
        Assert.True(obj.ContainsKey("b"));
        Assert.Equal(2, obj["b"]?.GetValue<int>());
    }

    [Fact]
    public void Merge_PatchDelete_IgnoresOtherPropertiesInOverride()
    {
        // If $patch: delete is present, the node is removed. Other properties in the override (siblings of $patch) are irrelevant for that node because the node itself is gone.
        var baseNode = JsonNode.Parse("{\"a\": 1}");
        var overrideNode = JsonNode.Parse("{\"a\": {\"$patch\": \"delete\", \"c\": 3}}");
        var result = _merger.Merge(baseNode, overrideNode);

        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.False(obj.ContainsKey("a"));
    }

    [Fact]
    public void Merge_PatchDelete_OnNonExistentKey_NoOp()
    {
        var baseNode = JsonNode.Parse("{\"b\": 2}");
        var overrideNode = JsonNode.Parse("{\"a\": {\"$patch\": \"delete\"}}");
        var result = _merger.Merge(baseNode, overrideNode);

        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.False(obj.ContainsKey("a"));
        Assert.True(obj.ContainsKey("b"));
    }

    [Fact]
    public void Merge_PatchDelete_Nested()
    {
        var baseNode = JsonNode.Parse("{\"a\": {\"x\": 1, \"y\": 2}}");
        var overrideNode = JsonNode.Parse("{\"a\": {\"x\": {\"$patch\": \"delete\"}}}");
        var result = _merger.Merge(baseNode, overrideNode);

        Assert.NotNull(result);
        var subObj = result["a"]?.AsObject();
        Assert.NotNull(subObj);
        Assert.False(subObj.ContainsKey("x"));
        Assert.True(subObj.ContainsKey("y"));
    }
}
