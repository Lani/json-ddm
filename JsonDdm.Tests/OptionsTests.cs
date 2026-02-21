using System.Text.Json.Nodes;
using Xunit;

namespace JsonDdm.Tests;

public class OptionsTests
{
  [Fact]
  public void Merge_CustomIdKey_MatchesArrayItemsCorrectly()
  {
    var options = new JsonDdmOptions { IdKey = "@id" };
    var merger = new JsonDdm(options);

    var baseJson = """
        [
            { "@id": "first", "value": 1 },
            { "@id": "second", "value": 2 }
        ]
        """;

    var overrideJson = """
        [
            { "@id": "second", "value": 20 }
        ]
        """;

    var expectedJson = """
        [
            { "@id": "first", "value": 1 },
            { "@id": "second", "value": 20 }
        ]
        """;

    var result = merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    Assert.Equal(JsonNode.Parse(expectedJson)!.ToString(), result!.ToString());
  }

  [Fact]
  public void Merge_CustomControlKeys_ReordersItemsCorrectly()
  {
    var options = new JsonDdmOptions
    {
      IdKey = "_id",
      PositionKey = "_pos",
      AnchorKey = "_ref"
    };
    var merger = new JsonDdm(options);

    var baseJson = """
        [
            { "_id": "a" },
            { "_id": "b" },
            { "_id": "c" }
        ]
        """;

    var overrideJson = """
        [
            { "_id": "c", "_pos": "start" },
            { "_id": "b", "_pos": "after", "_ref": "a" }
        ]
        """;
    // result should be c, a, b

    var expectedJson = """
        [
            { "_id": "c" },
            { "_id": "a" },
            { "_id": "b" }
        ]
        """;

    var result = merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    Assert.Equal(JsonNode.Parse(expectedJson)!.ToString(), result!.ToString());
  }

  [Fact]
  public void Merge_CustomPatchKey_DeletesItems()
  {
    var options = new JsonDdmOptions
    {
      IdKey = "key",
      PatchKey = "op"
    };
    var merger = new JsonDdm(options);

    var baseJson = """
        [
            { "key": "abc", "data": 123 },
            { "key": "def", "data": 456 }
        ]
        """;

    var overrideJson = """
        [
            { "key": "abc", "op": "delete" }
        ]
        """;

    var expectedJson = """
        [
            { "key": "def", "data": 456 }
        ]
        """;

    var result = merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    Assert.Equal(JsonNode.Parse(expectedJson)!.ToString(), result!.ToString());
  }

  [Fact]
  public void Merge_CustomValueKey_UpdatesPrimitiveViaObject()
  {
    var options = new JsonDdmOptions { ValueKey = "val" };
    var merger = new JsonDdm(options);

    var baseJson = """
        {
            "timeout": 30
        }
        """;

    var overrideJson = """
        {
            "timeout": { "val": 60 }
        }
        """;

    var expectedJson = """
        {
            "timeout": 60
        }
        """;

    var result = merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    Assert.Equal(JsonNode.Parse(expectedJson)!.ToString(), result!.ToString());
  }

  [Fact]
  public void Merge_ThrowOnMissingAnchor_False_DoesNotThrow()
  {
    var options = new JsonDdmOptions
    {
      ThrowOnMissingAnchor = false
    };
    var merger = new JsonDdm(options);

    var baseJson = """
        [
            { "$id": "a" }
        ]
        """;

    var overrideJson = """
        [
            { "$id": "b", "$position": "after", "$anchor": "nonexistent" }
        ]
        """;

    // Should default to end
    var expectedJson = """
        [
            { "$id": "a" },
            { "$id": "b" }
        ]
        """;

    var result = merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    Assert.Equal(JsonNode.Parse(expectedJson)!.ToString(), result!.ToString());
  }

  [Fact]
  public void Merge_ThrowOnMissingAnchor_True_ThrowsException()
  {
    var options = new JsonDdmOptions
    {
      ThrowOnMissingAnchor = true
    };
    var merger = new JsonDdm(options);

    var baseJson = """
        [
            { "$id": "a" }
        ]
        """;

    var overrideJson = """
        [
            { "$id": "b", "$position": "after", "$anchor": "nonexistent" }
        ]
        """;

    Assert.Throws<InvalidOperationException>(() =>
        merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson)));
  }

  [Fact]
  public void Merge_ThrowOnMissingAnchor_ObjectReordering_False_DoesNotThrow()
  {
    var options = new JsonDdmOptions
    {
      ThrowOnMissingAnchor = false
    };
    var merger = new JsonDdm(options);

    var baseJson = """
        {
            "a": 1
        }
        """;

    var overrideJson = """
        {
            "b": { "$value": 2, "$position": "after", "$anchor": "nonexistent" }
        }
        """;

    // Should default to end (after 'a')
    var expectedJson = """
        {
            "a": 1,
            "b": 2
        }
        """;

    var result = merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    Assert.Equal(JsonNode.Parse(expectedJson)!.ToString(), result!.ToString());
  }

  [Fact]
  public void Merge_ThrowOnMissingAnchor_ObjectReordering_True_ThrowsException()
  {
    var options = new JsonDdmOptions
    {
      ThrowOnMissingAnchor = true
    };
    var merger = new JsonDdm(options);

    var baseJson = """
        {
            "a": 1
        }
        """;

    var overrideJson = """
        {
            "b": { "$value": 2, "$position": "after", "$anchor": "nonexistent" }
        }
        """;

    Assert.Throws<InvalidOperationException>(() =>
        merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson)));
  }
}
