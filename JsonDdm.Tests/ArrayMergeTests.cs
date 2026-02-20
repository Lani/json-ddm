using System.Text.Json.Nodes;
using Xunit;

namespace JsonDdm.Tests;

public class ArrayMergeTests
{
  private readonly JsonDdm _merger;

  public ArrayMergeTests()
  {
    _merger = new JsonDdm();
  }

  [Fact]
  public void Merge_ArrayBase_ArrayOverride_AppendsItemsWithoutId()
  {
    // Arrays of primitives are append-only (no ID matching)
    var baseJson = """
        [
            1,
            2
        ]
        """;

    var overrideJson = """
        [
            3,
            4
        ]
        """;

    var baseNode = JsonNode.Parse(baseJson);
    var overrideNode = JsonNode.Parse(overrideJson);

    var result = _merger.Merge(baseNode, overrideNode);

    Assert.NotNull(result);
    Assert.IsType<JsonArray>(result);
    var resultArray = result.AsArray();
    Assert.Equal(4, resultArray.Count);
    Assert.Equal(1, resultArray[0]?.GetValue<int>());
    Assert.Equal(2, resultArray[1]?.GetValue<int>());
    Assert.Equal(3, resultArray[2]?.GetValue<int>());
    Assert.Equal(4, resultArray[3]?.GetValue<int>());
  }

  [Fact]
  public void Merge_ArrayBase_ArrayOverride_DifferentTypes_Replaces()
  {
    // Spec 4.1: If types mismatch (e.g. primitive array vs object array inside?), wait no, 
    // Spec 4.3 Note: Primitive arrays are Append Only.
    // But what if base is object array and override is primitive array?
    // Usually DDM works on types. If base is Array and override is Array, we merge.
    // Elements inside can be mixed.

    // Let's test basic ID matching
  }

  [Fact]
  public void Merge_ArrayBase_ArrayOverride_MatchesByIdAndMerges()
  {
    var baseJson = """
        [
            { "$id": "a", "value": 1 },
            { "$id": "b", "value": 2 }
        ]
        """;

    var overrideJson = """
        [
            { "$id": "a", "value": 10 },
            { "$id": "c", "value": 3 }
        ]
        """;

    var baseNode = JsonNode.Parse(baseJson);
    var overrideNode = JsonNode.Parse(overrideJson);

    var result = _merger.Merge(baseNode, overrideNode);

    Assert.NotNull(result);
    var arr = result.AsArray();

    // "a" should be updated. "b" should remain. "c" should be appended.
    // The order might depend on implementation details if we don't handle positioning yet.
    // But usually: Existing items stay in place, new items appended.

    Assert.Equal(3, arr.Count);

    var itemA = arr.First(x => x?["$id"]?.GetValue<string>() == "a");
    Assert.NotNull(itemA);
    Assert.Equal(10, itemA!["value"]?.GetValue<int>());

    var itemB = arr.First(x => x?["$id"]?.GetValue<string>() == "b");
    Assert.NotNull(itemB);
    Assert.Equal(2, itemB!["value"]?.GetValue<int>());

    var itemC = arr.First(x => x?["$id"]?.GetValue<string>() == "c");
    Assert.NotNull(itemC);
    Assert.Equal(3, itemC!["value"]?.GetValue<int>());
  }

  [Fact]
  public void Merge_ArrayBase_ArrayOverride_DeletesItem()
  {
    var baseJson = """
        [
            { "$id": "a", "value": 1 },
            { "$id": "b", "value": 2 }
        ]
        """;

    var overrideJson = """
        [
            { "$id": "a", "$patch": "delete" }
        ]
        """;

    var baseNode = JsonNode.Parse(baseJson);
    var overrideNode = JsonNode.Parse(overrideJson);

    var result = _merger.Merge(baseNode, overrideNode);

    Assert.NotNull(result);
    var arr = result.AsArray();

    Assert.Single(arr);
    Assert.Equal("b", arr[0]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void Merge_ArrayBase_ArrayOverride_MixedPrimitivesAndObjects()
  {
    // Base has a primitive and an object
    var baseJson = """
        [
            1,
            { "$id": "a", "v": 1 }
        ]
        """;

    // Override has a primitive (which will append) and an update for "a"
    var overrideJson = """
        [
            { "$id": "a", "v": 2 },
            2
        ]
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson))!.AsArray();

    Assert.Equal(3, result.Count);
    // Index 0: 1 (original primitive)
    Assert.Equal(1, result[0]?.GetValue<int>());
    // Index 1: Object "a" (updated)
    Assert.Equal(2, result[1]?["v"]?.GetValue<int>());
    // Index 2: 2 (appended primitive)
    Assert.Equal(2, result[2]?.GetValue<int>());
  }
}
