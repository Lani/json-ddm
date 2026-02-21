using System.Text.Json.Nodes;
using Xunit;

namespace JsonDdm.Tests;

/// <summary>
/// Tests to verify Last-In-Wins conflict resolution and deterministic behavior
/// as specified in SPEC.md Section 5: Deterministic Conflict Resolution.
/// </summary>
public class ConflictResolutionTests
{
  private readonly JsonDdm _merger = new();

  #region Last-In-Wins: Data Value Conflicts

  [Fact]
  public void MultiMerge_ConflictingPrimitiveValues_LastWins()
  {
    var baseJson = """
        {
          "value": 1
        }
        """;

    var override1 = """
        {
          "value": 2
        }
        """;

    var override2 = """
        {
          "value": 3
        }
        """;

    // Merge sequentially: base -> override1 -> override2
    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override1));
    var result2 = _merger.Merge(result1, JsonNode.Parse(override2));

    Assert.Equal(3, result2!["value"]?.GetValue<int>());
  }

  [Fact]
  public void MultiMerge_ConflictingNestedValues_LastWins()
  {
    var baseJson = """
        {
          "config": {
            "timeout": 30,
            "retries": 3
          }
        }
        """;

    var override1 = """
        {
          "config": {
            "timeout": 60
          }
        }
        """;

    var override2 = """
        {
          "config": {
            "timeout": 90,
            "retries": 5
          }
        }
        """;

    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override1));
    var result2 = _merger.Merge(result1, JsonNode.Parse(override2));

    Assert.Equal(90, result2!["config"]!["timeout"]?.GetValue<int>());
    Assert.Equal(5, result2!["config"]!["retries"]?.GetValue<int>());
  }

  [Fact]
  public void MultiMerge_ConflictingArrayItemValues_LastWins()
  {
    var baseJson = """
        [
          { "$id": "item1", "priority": 1 },
          { "$id": "item2", "priority": 2 }
        ]
        """;

    var override1 = """
        [
          { "$id": "item1", "priority": 10 }
        ]
        """;

    var override2 = """
        [
          { "$id": "item1", "priority": 100 }
        ]
        """;

    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override1));
    var result2 = _merger.Merge(result1, JsonNode.Parse(override2));

    var items = result2!.AsArray();
    var item1 = items.First(x => x?["$id"]?.GetValue<string>() == "item1");
    Assert.Equal(100, item1!["priority"]?.GetValue<int>());
  }

  #endregion

  #region Last-In-Wins: Position Conflicts

  [Fact]
  public void MultiMerge_ConflictingArrayPositions_LastWins()
  {
    // Spec example: Layer B moves item to start, Layer C moves it to end
    var baseJson = """
        [
          { "$id": "a", "val": 1 },
          { "$id": "b", "val": 2 },
          { "$id": "c", "val": 3 }
        ]
        """;

    // Override 1: Move 'c' to start
    var override1 = """
        [
          { "$id": "c", "$position": "start" }
        ]
        """;

    // Override 2: Move 'c' to end
    var override2 = """
        [
          { "$id": "c", "$position": "end" }
        ]
        """;

    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override1));
    var result1Array = result1!.AsArray();
    // After override1: c, a, b
    Assert.Equal("c", result1Array[0]?["$id"]?.GetValue<string>());

    var result2 = _merger.Merge(result1, JsonNode.Parse(override2));
    var result2Array = result2!.AsArray();
    // After override2 (last wins): a, b, c
    Assert.Equal("a", result2Array[0]?["$id"]?.GetValue<string>());
    Assert.Equal("b", result2Array[1]?["$id"]?.GetValue<string>());
    Assert.Equal("c", result2Array[2]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void MultiMerge_ConflictingObjectPropertyPositions_LastWins()
  {
    var baseJson = """
        {
          "first": 1,
          "second": 2,
          "third": 3
        }
        """;

    // Override 1: Move 'third' to start
    var override1 = """
        {
          "third": { "$value": 3, "$position": "start" }
        }
        """;

    // Override 2: Move 'third' to end (explicit)
    var override2 = """
        {
          "third": { "$value": 3, "$position": "end" }
        }
        """;

    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override1));
    var keys1 = result1!.AsObject().Select(kvp => kvp.Key).ToList();
    // After override1: third, first, second
    Assert.Equal("third", keys1[0]);

    var result2 = _merger.Merge(result1, JsonNode.Parse(override2));
    var keys2 = result2!.AsObject().Select(kvp => kvp.Key).ToList();
    // After override2 (last wins): first, second, third
    Assert.Equal("first", keys2[0]);
    Assert.Equal("second", keys2[1]);
    Assert.Equal("third", keys2[2]);
  }

  [Fact]
  public void MultiMerge_ConflictingAnchors_LastWins()
  {
    var baseJson = """
        [
          { "$id": "a" },
          { "$id": "b" },
          { "$id": "c" },
          { "$id": "d" }
        ]
        """;

    // Override 1: Move 'd' before 'b'
    var override1 = """
        [
          { "$id": "d", "$position": "before", "$anchor": "b" }
        ]
        """;

    // Override 2: Move 'd' after 'c'
    var override2 = """
        [
          { "$id": "d", "$position": "after", "$anchor": "c" }
        ]
        """;

    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override1));
    var array1 = result1!.AsArray();
    // After override1: a, d, b, c
    Assert.Equal("a", array1[0]?["$id"]?.GetValue<string>());
    Assert.Equal("d", array1[1]?["$id"]?.GetValue<string>());
    Assert.Equal("b", array1[2]?["$id"]?.GetValue<string>());

    var result2 = _merger.Merge(result1, JsonNode.Parse(override2));
    var array2 = result2!.AsArray();
    // After override2 (last wins): a, b, c, d
    Assert.Equal("a", array2[0]?["$id"]?.GetValue<string>());
    Assert.Equal("b", array2[1]?["$id"]?.GetValue<string>());
    Assert.Equal("c", array2[2]?["$id"]?.GetValue<string>());
    Assert.Equal("d", array2[3]?["$id"]?.GetValue<string>());
  }

  #endregion

  #region Determinism Tests

  [Fact]
  public void Determinism_SameInputs_ProduceSameOutput()
  {
    var baseJson = """
        {
          "a": 1,
          "b": { "x": 2, "y": 3 },
          "c": [
            { "$id": "i1", "val": 10 },
            { "$id": "i2", "val": 20 }
          ]
        }
        """;

    var overrideJson = """
        {
          "a": 100,
          "b": { "y": 30 },
          "c": [
            { "$id": "i2", "$position": "start" },
            { "$id": "i3", "val": 30 }
          ]
        }
        """;

    // Merge multiple times
    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var result2 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var result3 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));

    // All results should be identical
    Assert.Equal(result1!.ToString(), result2!.ToString());
    Assert.Equal(result1!.ToString(), result3!.ToString());
  }

  [Fact]
  public void Determinism_ComplexMultiLayerMerge_ProduceSameOutput()
  {
    var base1 = """{ "val": 1 }""";
    var override1 = """{ "val": 2, "new": "a" }""";
    var override2 = """{ "val": 3 }""";
    var override3 = """{ "new": "b", "another": true }""";

    // Perform the same sequence of merges twice
    var seq1_step1 = _merger.Merge(JsonNode.Parse(base1), JsonNode.Parse(override1));
    var seq1_step2 = _merger.Merge(seq1_step1, JsonNode.Parse(override2));
    var seq1_step3 = _merger.Merge(seq1_step2, JsonNode.Parse(override3));

    var seq2_step1 = _merger.Merge(JsonNode.Parse(base1), JsonNode.Parse(override1));
    var seq2_step2 = _merger.Merge(seq2_step1, JsonNode.Parse(override2));
    var seq2_step3 = _merger.Merge(seq2_step2, JsonNode.Parse(override3));

    Assert.Equal(seq1_step3!.ToString(), seq2_step3!.ToString());
  }

  [Fact]
  public void Determinism_ArrayReordering_ProduceSameOutput()
  {
    var baseJson = """
        [
          { "$id": "a" },
          { "$id": "b" },
          { "$id": "c" },
          { "$id": "d" }
        ]
        """;

    var overrideJson = """
        [
          { "$id": "d", "$position": "start" },
          { "$id": "b", "$position": "after", "$anchor": "c" }
        ]
        """;

    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var result2 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var result3 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));

    // Order should be: d, a, c, b
    var verifyOrder = (JsonNode? result) =>
    {
      var array = result!.AsArray();
      return new[]
      {
        array[0]?["$id"]?.GetValue<string>(),
        array[1]?["$id"]?.GetValue<string>(),
        array[2]?["$id"]?.GetValue<string>(),
        array[3]?["$id"]?.GetValue<string>()
      };
    };

    var order1 = verifyOrder(result1);
    var order2 = verifyOrder(result2);
    var order3 = verifyOrder(result3);

    Assert.Equal(order1, order2);
    Assert.Equal(order1, order3);
    Assert.Equal(new[] { "d", "a", "c", "b" }, order1);
  }

  #endregion

  #region Complex Multi-Layer Scenarios

  [Fact]
  public void ComplexScenario_ThreeLayerMerge_WithDataAndPositionConflicts()
  {
    // Base configuration
    var layer0 = """
        {
          "servers": [
            { "$id": "primary", "host": "server1.com", "priority": 1 },
            { "$id": "backup", "host": "server2.com", "priority": 2 },
            { "$id": "cache", "host": "cache1.com", "priority": 3 }
          ]
        }
        """;

    // Layer 1: Change priority and move cache to start
    var layer1 = """
        {
          "servers": [
            { "$id": "cache", "priority": 10, "$position": "start" }
          ]
        }
        """;

    // Layer 2: Change priority again and move cache to end
    var layer2 = """
        {
          "servers": [
            { "$id": "cache", "priority": 5, "$position": "end" }
          ]
        }
        """;

    // Layer 3: Add new server and change backup priority
    var layer3 = """
        {
          "servers": [
            { "$id": "backup", "priority": 99 },
            { "$id": "mirror", "host": "mirror.com", "priority": 50 }
          ]
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(layer0), JsonNode.Parse(layer1));
    result = _merger.Merge(result, JsonNode.Parse(layer2));
    result = _merger.Merge(result, JsonNode.Parse(layer3));

    var servers = result!["servers"]!.AsArray();

    // Verify final state:
    // - cache priority: 5 (layer2 wins over layer1)
    // - cache position: end (layer2 moves it to end, layer3 doesn't reposition it)
    // - backup priority: 99 (layer3)
    // - mirror added (layer3) - appends to end, so it comes after cache
    Assert.Equal(4, servers.Count);

    var cache = servers.First(s => s?["$id"]?.GetValue<string>() == "cache");
    Assert.Equal(5, cache!["priority"]?.GetValue<int>());

    var backup = servers.First(s => s?["$id"]?.GetValue<string>() == "backup");
    Assert.Equal(99, backup!["priority"]?.GetValue<int>());

    var mirror = servers.FirstOrDefault(s => s?["$id"]?.GetValue<string>() == "mirror");
    Assert.NotNull(mirror);
    Assert.Equal("mirror.com", mirror!["host"]?.GetValue<string>());

    // Expected final order: primary, backup, cache, mirror
    // (cache was moved to end in layer2, mirror was appended in layer3)
    var serverOrder = servers.Select(s => s?["$id"]?.GetValue<string>()).ToArray();
    Assert.Equal(new[] { "primary", "backup", "cache", "mirror" }, serverOrder);
  }

  [Fact]
  public void ComplexScenario_FiveLayerMerge_ObjectReordering()
  {
    var base1 = """{ "a": 1, "b": 2, "c": 3 }""";
    var override1 = """{ "c": { "$value": 3, "$position": "start" } }""";
    var override2 = """{ "b": { "$value": 2, "$position": "start" } }""";
    var override3 = """{ "d": 4 }""";
    var override4 = """{ "a": { "$value": 100, "$position": "end" } }""";

    var result = _merger.Merge(JsonNode.Parse(base1), JsonNode.Parse(override1));
    result = _merger.Merge(result, JsonNode.Parse(override2));
    result = _merger.Merge(result, JsonNode.Parse(override3));
    result = _merger.Merge(result, JsonNode.Parse(override4));

    var keys = result!.AsObject().Select(kvp => kvp.Key).ToList();

    // Expected order after all merges:
    // - override1: c, a, b
    // - override2: b, c, a
    // - override3: b, c, a, d
    // - override4: b, c, d, a (a moved to end)
    Assert.Equal(new[] { "b", "c", "d", "a" }, keys);
    Assert.Equal(100, result["a"]?.GetValue<int>());
  }

  [Fact]
  public void ComplexScenario_MixedDataTypes_NestedConflicts()
  {
    var baseJson = """
        {
          "config": {
            "database": {
              "timeout": 30,
              "pool": 10
            },
            "cache": {
              "ttl": 300
            }
          },
          "features": [
            { "$id": "auth", "enabled": true },
            { "$id": "logging", "enabled": true }
          ]
        }
        """;

    var override1 = """
        {
          "config": {
            "database": {
              "timeout": 60
            }
          },
          "features": [
            { "$id": "auth", "enabled": false }
          ]
        }
        """;

    var override2 = """
        {
          "config": {
            "database": {
              "timeout": 90,
              "pool": 20
            },
            "cache": {
              "ttl": 600
            }
          },
          "features": [
            { "$id": "auth", "enabled": true },
            { "$id": "metrics", "enabled": true }
          ]
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override1));
    result = _merger.Merge(result, JsonNode.Parse(override2));

    // Verify last-in-wins for all conflicts
    Assert.Equal(90, result!["config"]!["database"]!["timeout"]?.GetValue<int>());
    Assert.Equal(20, result!["config"]!["database"]!["pool"]?.GetValue<int>());
    Assert.Equal(600, result!["config"]!["cache"]!["ttl"]?.GetValue<int>());

    var features = result["features"]!.AsArray();
    var auth = features.First(f => f?["$id"]?.GetValue<string>() == "auth");
    Assert.True(auth!["enabled"]?.GetValue<bool>()); // override2 wins

    var metrics = features.FirstOrDefault(f => f?["$id"]?.GetValue<string>() == "metrics");
    Assert.NotNull(metrics);
  }

  #endregion

  #region Order Independence for Commutative Operations

  [Fact]
  public void Determinism_CommutativeOperations_ProduceSameOutput()
  {
    // When merging operations that don't conflict, order shouldn't matter
    var baseJson = """
        {
          "a": 1,
          "b": 2
        }
        """;

    var override1 = """{ "c": 3 }""";
    var override2 = """{ "d": 4 }""";

    // Path 1: override1 then override2
    var result1 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override1));
    result1 = _merger.Merge(result1, JsonNode.Parse(override2));

    // Path 2: override2 then override1
    var result2 = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(override2));
    result2 = _merger.Merge(result2, JsonNode.Parse(override1));

    // Both should have all four keys
    var obj1 = result1!.AsObject();
    var obj2 = result2!.AsObject();

    Assert.Equal(4, obj1.Count);
    Assert.Equal(4, obj2.Count);
    Assert.True(obj1.ContainsKey("a") && obj1.ContainsKey("b") && obj1.ContainsKey("c") && obj1.ContainsKey("d"));
    Assert.True(obj2.ContainsKey("a") && obj2.ContainsKey("b") && obj2.ContainsKey("c") && obj2.ContainsKey("d"));
  }

  #endregion
}
