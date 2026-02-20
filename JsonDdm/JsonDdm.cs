using System.Text.Json.Nodes;

namespace JsonDdm;

/// <summary>
/// JSON Deterministic Deep Merge.
/// </summary>
public class JsonDdm
{
  private readonly JsonDdmOptions _options;

  /// <summary>
  /// Initializes a new instance of the <see cref="JsonDdm"/> class.
  /// </summary>
  /// <param name="options">The merge options.</param>
  public JsonDdm(JsonDdmOptions? options = null)
  {
    _options = options ?? new JsonDdmOptions();
  }

  /// <summary>
  /// Merges the override node into the base node according to the DDM protocol.
  /// </summary>
  /// <param name="baseNode">The base JSON layer.</param>
  /// <param name="overrideNode">The override JSON layer.</param>
  /// <returns>The merged JSON node.</returns>
  public JsonNode? Merge(JsonNode? baseNode, JsonNode? overrideNode)
  {
    if (overrideNode is null)
    {
      return null;
    }

    if (overrideNode is JsonValue)
    {
      return overrideNode.DeepClone();
    }

    if (baseNode is null || baseNode is JsonValue)
    {
      return overrideNode.DeepClone();
    }

    if (baseNode.GetType() != overrideNode.GetType())
    {
      return overrideNode.DeepClone();
    }

    if (overrideNode is JsonObject overrideObj && baseNode is JsonObject baseObj)
    {
      return MergeObjects(baseObj, overrideObj);
    }

    if (baseNode is JsonArray)
    {
      return overrideNode.DeepClone();
    }

    return overrideNode.DeepClone();
  }

  private JsonNode? MergeObjects(JsonObject baseObj, JsonObject overrideObj)
  {
    var result = (JsonObject)baseObj.DeepClone();

    foreach (var kvp in overrideObj)
    {
      var key = kvp.Key;
      var overrideValue = kvp.Value;

      if (result.ContainsKey(key))
      {
        var baseValue = result[key];
        result[key] = Merge(baseValue, overrideValue);
      }
      else
      {
        if (overrideValue is null)
        {
          result[key] = null;
        }
        else
        {
          result[key] = Merge(null, overrideValue);
        }
      }
    }

    return result;
  }
}
