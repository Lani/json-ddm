using System.Text.Json.Nodes;
using Xunit;

namespace JsonDdm.Tests;

public class ReorderingTests
{
  private readonly JsonDdm _merger;

  public ReorderingTests()
  {
    _merger = new JsonDdm();
  }

  [Fact]
  public void Merge_Array_ReordersItemToStart()
  {
    var baseJson = """
        [
            { "$id": "a", "val": 1 },
            { "$id": "b", "val": 2 },
            { "$id": "c", "val": 3 }
        ]
        """;

    var overrideJson = """
        [
            { "$id": "c", "$position": "start" }
        ]
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsArray();

    Assert.Equal(3, result.Count);
    Assert.Equal("c", result[0]?["$id"]?.GetValue<string>());
    Assert.Equal("a", result[1]?["$id"]?.GetValue<string>());
    Assert.Equal("b", result[2]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void Merge_Array_ReordersItemToEnd()
  {
    var baseJson = """
        [
            { "$id": "a", "val": 1 },
            { "$id": "b", "val": 2 },
            { "$id": "c", "val": 3 }
        ]
        """;

    var overrideJson = """
        [
            { "$id": "a", "$position": "end" }
        ]
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsArray();

    Assert.Equal(3, result.Count);
    Assert.Equal("b", result[0]?["$id"]?.GetValue<string>());
    Assert.Equal("c", result[1]?["$id"]?.GetValue<string>());
    Assert.Equal("a", result[2]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void Merge_Array_ReordersItemBeforeOther()
  {
    var baseJson = """
        [
            { "$id": "a", "val": 1 },
            { "$id": "b", "val": 2 },
            { "$id": "c", "val": 3 }
        ]
        """;

    var overrideJson = """
        [
            { "$id": "c", "$position": "before", "$anchor": "b" }
        ]
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsArray();

    Assert.Equal(3, result.Count);
    // Original: a, b, c
    // Move c before b -> a, c, b
    Assert.Equal("a", result[0]?["$id"]?.GetValue<string>());
    Assert.Equal("c", result[1]?["$id"]?.GetValue<string>());
    Assert.Equal("b", result[2]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void Merge_Array_ReordersItemAfterOther()
  {
    var baseJson = """
        [
            { "$id": "a", "val": 1 },
            { "$id": "b", "val": 2 },
            { "$id": "c", "val": 3 }
        ]
        """;

    var overrideJson = """
        [
            { "$id": "a", "$position": "after", "$anchor": "b" }
        ]
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsArray();

    Assert.Equal(3, result.Count);
    // Original: a, b, c
    // Move a after b -> b, a, c
    Assert.Equal("b", result[0]?["$id"]?.GetValue<string>());
    Assert.Equal("a", result[1]?["$id"]?.GetValue<string>());
    Assert.Equal("c", result[2]?["$id"]?.GetValue<string>());
  }
}
