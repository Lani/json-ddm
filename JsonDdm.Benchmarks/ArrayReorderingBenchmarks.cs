using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace JsonDdm.Benchmarks;

/// <summary>
/// Benchmarks for array reordering operations.
/// This tests the performance concern identified in the code review about O(n²) complexity.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class ArrayReorderingBenchmarks
{
  private JsonDdm _merger = null!;

  [Params(10, 50, 100, 500)]
  public int ArraySize { get; set; }

  [Params(1, 5, 10)]
  public int ReorderOperations { get; set; }

  private JsonNode _baseArray = null!;
  private JsonNode _overrideArray = null!;

  [GlobalSetup]
  public void Setup()
  {
    _merger = new JsonDdm();
    _baseArray = GenerateBaseArray(ArraySize);
    _overrideArray = GenerateReorderingOverride(ArraySize, ReorderOperations);
  }

  [Benchmark]
  public JsonNode? ReorderArray()
  {
    var baseDoc = new JsonObject { ["items"] = _baseArray.DeepClone() };
    var overrideDoc = new JsonObject { ["items"] = _overrideArray.DeepClone() };
    return _merger.Merge(baseDoc, overrideDoc);
  }

  [Benchmark]
  public JsonNode? ReorderArrayWithPositionBefore()
  {
    var baseDoc = new JsonObject
    {
      ["items"] = GenerateBaseArray(ArraySize)
    };

    var items = new JsonArray();
    for (int i = 0; i < ReorderOperations && i < ArraySize - 1; i++)
    {
      items.Add(new JsonObject
      {
        ["$id"] = $"item-{ArraySize - 1 - i}",
        ["$position"] = "before",
        ["$anchor"] = $"item-{i}"
      });
    }

    var overrideDoc = new JsonObject { ["items"] = items };
    return _merger.Merge(baseDoc, overrideDoc);
  }

  [Benchmark]
  public JsonNode? ReorderArrayWithPositionAfter()
  {
    var baseDoc = new JsonObject
    {
      ["items"] = GenerateBaseArray(ArraySize)
    };

    var items = new JsonArray();
    for (int i = 0; i < ReorderOperations && i < ArraySize - 1; i++)
    {
      items.Add(new JsonObject
      {
        ["$id"] = $"item-{i}",
        ["$position"] = "after",
        ["$anchor"] = $"item-{ArraySize - 1}"
      });
    }

    var overrideDoc = new JsonObject { ["items"] = items };
    return _merger.Merge(baseDoc, overrideDoc);
  }

  [Benchmark]
  public JsonNode? ReorderToStartAndEnd()
  {
    var baseDoc = new JsonObject
    {
      ["items"] = GenerateBaseArray(ArraySize)
    };

    var items = new JsonArray
        {
            new JsonObject
            {
                ["$id"] = $"item-{ArraySize / 2}",
                ["$position"] = "start"
            },
            new JsonObject
            {
                ["$id"] = $"item-0",
                ["$position"] = "end"
            }
        };

    var overrideDoc = new JsonObject { ["items"] = items };
    return _merger.Merge(baseDoc, overrideDoc);
  }

  private static JsonNode GenerateBaseArray(int size)
  {
    var array = new JsonArray();
    for (int i = 0; i < size; i++)
    {
      array.Add(new JsonObject
      {
        ["$id"] = $"item-{i}",
        ["name"] = $"Item {i}",
        ["value"] = i,
        ["data"] = new JsonObject
        {
          ["nested"] = i * 2,
          ["description"] = $"Nested data for item {i}"
        }
      });
    }
    return array;
  }

  private static JsonNode GenerateReorderingOverride(int size, int reorderCount)
  {
    var array = new JsonArray();

    // Create reorder operations that move items around
    for (int i = 0; i < reorderCount && i < size - 1; i++)
    {
      // Move items from end to positions near the beginning
      array.Add(new JsonObject
      {
        ["$id"] = $"item-{size - 1 - i}",
        ["$position"] = "before",
        ["$anchor"] = $"item-{i}"
      });
    }

    return array;
  }
}
