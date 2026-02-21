using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace JsonDdm.Benchmarks;

/// <summary>
/// Benchmarks for deeply nested structures.
/// Tests the security concern about stack overflow with deep nesting.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class DeepNestingBenchmarks
{
  private JsonDdm _merger = null!;

  [Params(5, 10, 20, 50)]
  public int NestingDepth { get; set; }

  private JsonNode _deepBase = null!;
  private JsonNode _deepOverride = null!;

  [GlobalSetup]
  public void Setup()
  {
    _merger = new JsonDdm();
    _deepBase = GenerateDeeplyNestedDocument(NestingDepth, withData: true);
    _deepOverride = GenerateDeeplyNestedDocument(NestingDepth, withData: false);
  }

  [Benchmark]
  public JsonNode? MergeDeepNesting()
  {
    return _merger.Merge(_deepBase, _deepOverride);
  }

  [Benchmark]
  public JsonNode? MergeDeepNestingWithArrays()
  {
    var baseDoc = GenerateDeepNestingWithArrays(NestingDepth, itemsPerLevel: 5);
    var overrideDoc = GenerateDeepNestingWithArrays(NestingDepth / 2, itemsPerLevel: 2);
    return _merger.Merge(baseDoc, overrideDoc);
  }

  private static JsonNode GenerateDeeplyNestedDocument(int depth, bool withData)
  {
    var root = new JsonObject
    {
      ["level"] = 0
    };

    if (withData)
    {
      root["data"] = new JsonObject
      {
        ["value"] = "Level 0",
        ["count"] = 0,
        ["enabled"] = true
      };
    }
    else
    {
      root["data"] = new JsonObject
      {
        ["value"] = "Updated Level 0"
      };
    }

    var current = root;
    for (int i = 1; i < depth; i++)
    {
      var nested = new JsonObject
      {
        ["level"] = i
      };

      if (withData)
      {
        nested["data"] = new JsonObject
        {
          ["value"] = $"Level {i}",
          ["count"] = i,
          ["enabled"] = i % 2 == 0
        };
      }
      else
      {
        nested["data"] = new JsonObject
        {
          ["value"] = $"Updated Level {i}"
        };
      }

      current["child"] = nested;
      current = nested;
    }

    return root;
  }

  private static JsonNode GenerateDeepNestingWithArrays(int depth, int itemsPerLevel)
  {
    var root = new JsonObject
    {
      ["level"] = 0
    };

    var items = new JsonArray();
    for (int i = 0; i < itemsPerLevel; i++)
    {
      items.Add(new JsonObject
      {
        ["$id"] = $"item-0-{i}",
        ["value"] = i
      });
    }
    root["items"] = items;

    var current = root;
    for (int level = 1; level < depth; level++)
    {
      var nested = new JsonObject
      {
        ["level"] = level
      };

      var levelItems = new JsonArray();
      for (int i = 0; i < itemsPerLevel; i++)
      {
        levelItems.Add(new JsonObject
        {
          ["$id"] = $"item-{level}-{i}",
          ["value"] = i
        });
      }
      nested["items"] = levelItems;

      current["nested"] = nested;
      current = nested;
    }

    return root;
  }
}
