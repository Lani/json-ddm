using System.Text.Json.Nodes;
using Xunit;

namespace JsonDdm.Tests;

public class ObjectReorderingTests
{
  private readonly JsonDdm _merger;

  public ObjectReorderingTests()
  {
    _merger = new JsonDdm();
  }

  [Fact]
  public void Merge_Object_ReordersItemToStart()
  {
    var baseJson = """
        {
            "a": 1,
            "b": 2,
            "c": 3
        }
        """;

    var overrideJson = """
        {
            "c": { "$position": "start" }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsObject();

    var keys = result.Select(x => x.Key).ToList();
    Assert.Equal("c", keys[0]);
    Assert.Equal("a", keys[1]);
    Assert.Equal("b", keys[2]);

    // Correctly preserved the value of "c" (primitive 3)
    Assert.Equal(3, result["c"]?.GetValue<int>());
  }

  [Fact]
  public void Merge_Object_ReordersItemBeforeOther()
  {
    var baseJson = """
        {
            "a": 1,
            "b": 2,
            "c": 3
        }
        """;

    var overrideJson = """
        {
            "c": { "$position": "before", "$anchor": "b" }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsObject();

    var keys = result.Select(x => x.Key).ToList();
    // Original: a, b, c. C moves before B -> a, c, b
    Assert.Equal("a", keys[0]);
    Assert.Equal("c", keys[1]);
    Assert.Equal("b", keys[2]);
  }

  [Fact]
  public void Merge_Object_UpdatesValueAndReorders()
  {
    var baseJson = """
        {
            "a": 1,
            "b": 2
        }
        """;

    var overrideJson = """
        {
            "b": { "$value": 20, "$position": "start" }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsObject();

    var keys = result.Select(x => x.Key).ToList();
    Assert.Equal("b", keys[0]);
    Assert.Equal("a", keys[1]);

    Assert.Equal(20, result["b"]?.GetValue<int>());
  }

  [Fact]
  public void Merge_Object_NewItemWithPosition()
  {
    var baseJson = """
        {
            "a": 1,
            "b": 2
        }
        """;

    // Note: previously tested "between" which is invalid, now testing "after"
    var overrideJson2 = """
        {
            "c": { "$value": 3, "$position": "after", "$anchor": "a" } 
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson2))!.AsObject();

    var keys = result.Select(x => x.Key).ToList();
    // a, c, b
    Assert.Equal("a", keys[0]);
    Assert.Equal("c", keys[1]);
    Assert.Equal("b", keys[2]);
    Assert.Equal(3, result["c"]?.GetValue<int>());
  }
}
