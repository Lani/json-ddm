using System.Text.Json.Nodes;
using Xunit;

namespace JsonDdm.Tests;

public class EscapingTests
{
  private readonly JsonDdm _merger;

  public EscapingTests()
  {
    _merger = new JsonDdm();
  }

  [Fact]
  public void Merge_EscapedControlKey_UnescapesInResult()
  {
    var baseJson = """
        {
            "data": 1
        }
        """;

    // $$patch should become $patch in the output, and NOT be treated as a patch operation (delete)
    var overrideJson = """
        {
            "$$patch": "not a patch"
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsObject();

    Assert.True(result.ContainsKey("$patch"));
    Assert.Equal("not a patch", result["$patch"]?.GetValue<string>());
    Assert.False(result.ContainsKey("$$patch"));

    // Ensure "data" is still there (it wasn't deleted or anything)
    Assert.Equal(1, result["data"]?.GetValue<int>());
  }

  [Fact]
  public void Merge_EscapedId_UnescapesInResult()
  {
    var baseJson = "{}";
    var overrideJson = """
        {
            "$$id": "123"
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsObject();

    Assert.True(result.ContainsKey("$id"));
    Assert.Equal("123", result["$id"]?.GetValue<string>());
  }

  [Fact]
  public void Merge_DoubleEscaped_UnescapesOnce()
  {
    var baseJson = "{}";
    var overrideJson = """
        {
            "$$$id": "value"
        }
        """;
    // $$$id -> $$id

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsObject();

    Assert.True(result.ContainsKey("$$id"));
    Assert.Equal("value", result["$$id"]?.GetValue<string>());
  }

  [Fact]
  public void Merge_Escaping_InsideArrayObjects()
  {
    var baseJson = "[]";
    var overrideJson = """
        [
            { "$id": "1", "$$value": "escaped" }
        ]
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsArray();
    var item = result[0]!.AsObject();

    Assert.True(item.ContainsKey("$value"));
    Assert.Equal("escaped", item["$value"]?.GetValue<string>());
  }
}
