using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace JsonDdm.Benchmarks;

/// <summary>
/// Benchmarks for cloning operations.
/// Tests the performance concern about excessive DeepClone() calls.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class CloneBenchmarks
{
  private JsonDdm _merger = null!;

  [Params(100, 500, 1000)]
  public int ObjectCount { get; set; }

  private JsonNode _largeObjectBase = null!;
  private JsonNode _largeObjectOverride = null!;
  private JsonNode _largeArrayBase = null!;
  private JsonNode _largeArrayOverride = null!;

  [GlobalSetup]
  public void Setup()
  {
    _merger = new JsonDdm();

    // Create large object with many properties
    var baseObj = new JsonObject();
    var overrideObj = new JsonObject();

    for (int i = 0; i < ObjectCount; i++)
    {
      baseObj[$"prop{i}"] = new JsonObject
      {
        ["id"] = i,
        ["name"] = $"Property {i}",
        ["value"] = i * 2,
        ["enabled"] = true,
        ["metadata"] = new JsonObject
        {
          ["created"] = DateTime.Now.ToString(),
          ["tags"] = new JsonArray { "tag1", "tag2", "tag3" }
        }
      };
    }

    // Override half the properties
    for (int i = 0; i < ObjectCount / 2; i += 2)
    {
      overrideObj[$"prop{i}"] = new JsonObject
      {
        ["value"] = i * 10
      };
    }

    _largeObjectBase = baseObj;
    _largeObjectOverride = overrideObj;

    // Create large array
    var baseArray = new JsonArray();
    var overrideArray = new JsonArray();

    for (int i = 0; i < ObjectCount; i++)
    {
      baseArray.Add(new JsonObject
      {
        ["$id"] = $"item-{i}",
        ["name"] = $"Item {i}",
        ["value"] = i,
        ["data"] = new JsonObject
        {
          ["nested"] = new JsonObject
          {
            ["deep"] = i * 2
          }
        }
      });
    }

    // Update some items
    for (int i = 0; i < ObjectCount / 4; i++)
    {
      overrideArray.Add(new JsonObject
      {
        ["$id"] = $"item-{i * 4}",
        ["value"] = i * 100
      });
    }

    _largeArrayBase = new JsonObject { ["items"] = baseArray };
    _largeArrayOverride = new JsonObject { ["items"] = overrideArray };
  }

  [Benchmark]
  public JsonNode? MergeLargeObject()
  {
    return _merger.Merge(_largeObjectBase, _largeObjectOverride);
  }

  [Benchmark]
  public JsonNode? MergeLargeArray()
  {
    return _merger.Merge(_largeArrayBase, _largeArrayOverride);
  }

  [Benchmark]
  public JsonNode? MergeWithFullReplacement()
  {
    // This will trigger many clones as all properties are replaced
    var baseObj = new JsonObject();
    var overrideObj = new JsonObject();

    for (int i = 0; i < 100; i++)
    {
      baseObj[$"prop{i}"] = $"value{i}";
      overrideObj[$"prop{i}"] = $"new-value{i}";
    }

    return _merger.Merge(baseObj, overrideObj);
  }
}
