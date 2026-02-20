using System.Text.Json.Nodes;
using Xunit;
using JsonDdm;

namespace JsonDdm.Tests;

public class PrimitiveMergeTests
{
  private readonly JsonDdm _merger = new();

  [Fact]
  public void Merge_NullBase_ReturnsOverride()
  {
    var overrideNode = JsonNode.Parse("42");
    var result = _merger.Merge(null, overrideNode);
    Assert.Equal(42, result?.GetValue<int>());
  }

  [Fact]
  public void Merge_NullOverride_ReturnsNull()
  {
    var baseNode = JsonNode.Parse("42");
    var result = _merger.Merge(baseNode, null);
    Assert.Null(result);
  }

  [Fact]
  public void Merge_PrimitiveCalculatedValue_ReplacesBase()
  {
    var baseNode = JsonNode.Parse("1");
    var overrideNode = JsonNode.Parse("2");
    var result = _merger.Merge(baseNode, overrideNode);
    Assert.Equal(2, result?.GetValue<int>());
  }

  [Fact]
  public void Merge_StringOverNumber_ReplacesBase()
  {
    var baseNode = JsonNode.Parse("1");
    var overrideNode = JsonNode.Parse("\"hello\"");
    var result = _merger.Merge(baseNode, overrideNode);
    Assert.Equal("hello", result?.GetValue<string>());
  }
}
