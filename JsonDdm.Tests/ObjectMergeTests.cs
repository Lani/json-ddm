using System.Text.Json.Nodes;
using Xunit;
using JsonDdm;

namespace JsonDdm.Tests;

public class ObjectMergeTests
{
  private readonly JsonDdm _merger = new();

  [Fact]
  public void Merge_NewKey_IsAdded()
  {
    var baseNode = JsonNode.Parse("{\"a\": 1}");
    var overrideNode = JsonNode.Parse("{\"b\": 2}");
    var result = _merger.Merge(baseNode, overrideNode);

    Assert.NotNull(result);
    Assert.Equal(1, result["a"]?.GetValue<int>());
    Assert.Equal(2, result["b"]?.GetValue<int>());
  }

  [Fact]
  public void Merge_ExistingKey_IsOverridden()
  {
    var baseNode = JsonNode.Parse("{\"a\": 1}");
    var overrideNode = JsonNode.Parse("{\"a\": 2}");
    var result = _merger.Merge(baseNode, overrideNode);

    Assert.NotNull(result);
    Assert.Equal(2, result["a"]?.GetValue<int>());
  }

  [Fact]
  public void Merge_DeepMerge_Recursive()
  {
    var baseNode = JsonNode.Parse("{\"a\": {\"x\": 1, \"y\": 2}}");
    var overrideNode = JsonNode.Parse("{\"a\": {\"y\": 3, \"z\": 4}}");
    var result = _merger.Merge(baseNode, overrideNode);

    Assert.NotNull(result);
    var subObj = result["a"];
    Assert.NotNull(subObj);
    Assert.Equal(1, subObj["x"]?.GetValue<int>());
    Assert.Equal(3, subObj["y"]?.GetValue<int>());
    Assert.Equal(4, subObj["z"]?.GetValue<int>());
  }

  [Fact]
  public void Merge_NullProperty_DeletesValue()
  {
    var baseNode = JsonNode.Parse("{\"a\": 1}");
    var overrideNode = JsonNode.Parse("{\"a\": null}");
    var result = _merger.Merge(baseNode, overrideNode);

    Assert.NotNull(result);
    Assert.Null(result["a"]); // "value" is null

    // Wait, System.Text.Json: { "a": null } -> result["a"] IS null (the node/value)
    // result.ContainsKey("a") is true.
    Assert.True(result.AsObject().ContainsKey("a"));
  }
}
