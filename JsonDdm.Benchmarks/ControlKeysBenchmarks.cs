using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace JsonDdm.Benchmarks;

/// <summary>
/// Benchmarks for control key operations (patch, position, anchor, value).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class ControlKeysBenchmarks
{
  private JsonDdm _merger = null!;

  [Params(50, 100, 200)]
  public int ItemCount { get; set; }

  private JsonNode _baseWithManyItems = null!;
  private JsonNode _deleteOverride = null!;
  private JsonNode _patchWithValue = null!;

  [GlobalSetup]
  public void Setup()
  {
    _merger = new JsonDdm();

    // Base with many items
    var items = new JsonArray();
    for (int i = 0; i < ItemCount; i++)
    {
      items.Add(new JsonObject
      {
        ["$id"] = $"item-{i}",
        ["name"] = $"Item {i}",
        ["value"] = i
      });
    }
    _baseWithManyItems = new JsonObject { ["items"] = items };

    // Override that deletes half the items
    var deleteItems = new JsonArray();
    for (int i = 0; i < ItemCount / 2; i++)
    {
      deleteItems.Add(new JsonObject
      {
        ["$id"] = $"item-{i * 2}",
        ["$patch"] = "delete"
      });
    }
    _deleteOverride = new JsonObject { ["items"] = deleteItems };

    // Override using $value
    var baseObj = new JsonObject();
    var overrideObj = new JsonObject();
    for (int i = 0; i < ItemCount; i++)
    {
      baseObj[$"prop{i}"] = i;
      overrideObj[$"prop{i}"] = new JsonObject
      {
        ["$value"] = i * 10,
        ["$position"] = i % 10 == 0 ? "start" : null
      };
    }
    _patchWithValue = overrideObj;
  }

  [Benchmark]
  public JsonNode? DeleteManyItems()
  {
    return _merger.Merge(_baseWithManyItems, _deleteOverride);
  }

  [Benchmark]
  public JsonNode? UseValueKey()
  {
    var baseDoc = new JsonObject { ["simple"] = 42 };
    var overrideDoc = new JsonObject
    {
      ["simple"] = new JsonObject
      {
        ["$value"] = 100
      }
    };
    return _merger.Merge(baseDoc, overrideDoc);
  }

  [Benchmark]
  public JsonNode? CheckIsDelete()
  {
    // Benchmark the IsDelete check performance
    var items = new JsonArray();
    for (int i = 0; i < ItemCount; i++)
    {
      items.Add(new JsonObject
      {
        ["$id"] = $"item-{i}",
        ["$patch"] = i % 2 == 0 ? "delete" : "keep"
      });
    }

    var baseDoc = new JsonObject { ["items"] = GenerateBaseArray(ItemCount) };
    var overrideDoc = new JsonObject { ["items"] = items };

    return _merger.Merge(baseDoc, overrideDoc);
  }

  [Benchmark]
  public JsonNode? EscapedKeys()
  {
    var baseObj = new JsonObject();
    var overrideObj = new JsonObject();

    for (int i = 0; i < ItemCount; i++)
    {
      baseObj[$"prop{i}"] = i;
      // Use escaped keys ($$id should become $id)
      overrideObj[$"$$prop{i}"] = i * 2;
    }

    return _merger.Merge(baseObj, overrideObj);
  }

  private static JsonArray GenerateBaseArray(int count)
  {
    var array = new JsonArray();
    for (int i = 0; i < count; i++)
    {
      array.Add(new JsonObject
      {
        ["$id"] = $"item-{i}",
        ["value"] = i
      });
    }
    return array;
  }
}
