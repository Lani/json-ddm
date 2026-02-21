using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace JsonDdm.Benchmarks;

/// <summary>
/// Benchmarks for basic merge operations.
/// This establishes baseline performance for common scenarios.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
[RPlotExporter] // Generate plots if R is available
public class MergeBenchmarks
{
  private JsonDdm _merger = null!;
  private JsonNode _smallBase = null!;
  private JsonNode _smallOverride = null!;
  private JsonNode _mediumBase = null!;
  private JsonNode _mediumOverride = null!;
  private JsonNode _largeBase = null!;
  private JsonNode _largeOverride = null!;

  [GlobalSetup]
  public void Setup()
  {
    _merger = new JsonDdm();

    // Small: Simple object with a few properties
    _smallBase = JsonNode.Parse("""
        {
            "name": "test",
            "value": 42,
            "enabled": true
        }
        """)!;

    _smallOverride = JsonNode.Parse("""
        {
            "value": 100,
            "description": "updated"
        }
        """)!;

    // Medium: Nested objects with arrays
    _mediumBase = JsonNode.Parse("""
        {
            "config": {
                "settings": {
                    "timeout": 30,
                    "retries": 3
                },
                "features": [
                    { "$id": "feature1", "name": "Feature 1", "enabled": true },
                    { "$id": "feature2", "name": "Feature 2", "enabled": false },
                    { "$id": "feature3", "name": "Feature 3", "enabled": true }
                ]
            },
            "metadata": {
                "version": "1.0",
                "author": "system"
            }
        }
        """)!;

    _mediumOverride = JsonNode.Parse("""
        {
            "config": {
                "settings": {
                    "timeout": 60
                },
                "features": [
                    { "$id": "feature2", "enabled": true },
                    { "$id": "feature4", "name": "Feature 4", "enabled": true }
                ]
            }
        }
        """)!;

    // Large: Deep nesting with multiple arrays
    _largeBase = GenerateLargeDocument(depth: 5, itemsPerArray: 20);
    _largeOverride = GenerateLargeOverride(depth: 5, itemsPerArray: 5);
  }

  [Benchmark(Baseline = true)]
  public JsonNode? MergeSmallDocuments()
  {
    return _merger.Merge(_smallBase, _smallOverride);
  }

  [Benchmark]
  public JsonNode? MergeMediumDocuments()
  {
    return _merger.Merge(_mediumBase, _mediumOverride);
  }

  [Benchmark]
  public JsonNode? MergeLargeDocuments()
  {
    return _merger.Merge(_largeBase, _largeOverride);
  }

  [Benchmark]
  public JsonNode? MergePrimitiveOverride()
  {
    var baseNode = JsonNode.Parse("""{"value": 42}""")!;
    var overrideNode = JsonNode.Parse("""{"value": 100}""")!;
    return _merger.Merge(baseNode, overrideNode);
  }

  [Benchmark]
  public JsonNode? MergeNullOverride()
  {
    return _merger.Merge(_smallBase, null);
  }

  private static JsonNode GenerateLargeDocument(int depth, int itemsPerArray)
  {
    var root = new JsonObject
    {
      ["level"] = 0,
      ["data"] = new JsonObject
      {
        ["value"] = "root"
      }
    };

    var current = root;
    for (int d = 1; d < depth; d++)
    {
      var items = new JsonArray();
      for (int i = 0; i < itemsPerArray; i++)
      {
        items.Add(new JsonObject
        {
          ["$id"] = $"item-{d}-{i}",
          ["name"] = $"Item {d}-{i}",
          ["value"] = i,
          ["enabled"] = i % 2 == 0
        });
      }

      current["items"] = items;
      current["nested"] = new JsonObject
      {
        ["level"] = d
      };
      current = (JsonObject)current["nested"]!;
    }

    return root;
  }

  private static JsonNode GenerateLargeOverride(int depth, int itemsPerArray)
  {
    var root = new JsonObject
    {
      ["data"] = new JsonObject
      {
        ["value"] = "updated"
      }
    };

    var current = root;
    for (int d = 1; d < depth; d++)
    {
      var items = new JsonArray();
      // Update some existing items
      for (int i = 0; i < itemsPerArray; i++)
      {
        items.Add(new JsonObject
        {
          ["$id"] = $"item-{d}-{i * 2}", // Update every other item
          ["value"] = i * 10
        });
      }

      current["items"] = items;
      current["nested"] = new JsonObject();
      current = (JsonObject)current["nested"]!;
    }

    return root;
  }
}
