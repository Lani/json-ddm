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

    // Check for $value in overrideNode
    if (overrideNode is JsonObject overrideObjectValue &&
        overrideObjectValue.TryGetPropertyValue(_options.ValueKey, out var valueNode))
    {
      return valueNode?.DeepClone();
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

    if (baseNode is JsonArray baseArr && overrideNode is JsonArray overrideArr)
    {
      return MergeArrays(baseArr, overrideArr);
    }

    return overrideNode.DeepClone();
  }

  private JsonArray MergeArrays(JsonArray baseArr, JsonArray overrideArr)
  {
    // Clone base array to a working list of (Item, IsDeleted) tuples
    // Actually, use a simple list and null out deleted items.
    var workingList = new List<JsonNode?>();
    foreach (var item in baseArr)
    {
      workingList.Add(item?.DeepClone());
    }

    // Build ID map to find index of base items by ID
    var baseIdMap = new Dictionary<string, int>();
    for (int i = 0; i < workingList.Count; i++)
    {
      var id = GetId(workingList[i]);
      if (id != null)
      {
        baseIdMap[id] = i;
      }
    }

    // List for completely new items to be appended
    var appendList = new List<JsonNode?>();

    foreach (var overrideItem in overrideArr)
    {
      var id = GetId(overrideItem);

      if (id != null && baseIdMap.TryGetValue(id, out var baseIndex))
      {
        // Match found in base: Merge or Delete
        if (IsDelete(overrideItem))
        {
          workingList[baseIndex] = null; // Mark for removal
        }
        else
        {
          workingList[baseIndex] = Merge(workingList[baseIndex], overrideItem);
        }
      }
      else
      {
        // No match (new ID or no ID): Append
        if (!IsDelete(overrideItem))
        {
          appendList.Add(overrideItem?.DeepClone());
        }
      }
    }

    // Construct result list
    var resultList = new List<JsonNode?>();
    foreach (var item in workingList)
    {
      if (item != null) resultList.Add(item);
    }
    foreach (var item in appendList)
    {
      if (item != null) resultList.Add(item);
    }

    // Apply Reordering (Phase 5)
    ReorderList(resultList);

    var resultArr = new JsonArray();
    foreach (var item in resultList)
    {
      if (item != null)
      {
        resultArr.Add(item.DeepClone()); // Use DeepClone to detach from previous parent if necessary, but actually we created new nodes via Merge/Clone earlier.
      }
    }

    return resultArr;
  }

  private void ReorderList(List<JsonNode?> list)
  {
    var moves = new List<(JsonNode Item, string Position, string? Anchor)>();

    foreach (var item in list)
    {
      if (item is JsonObject obj &&
          obj.TryGetPropertyValue(_options.PositionKey, out var posNode) &&
          posNode is JsonValue &&
          posNode.GetValueKind() == System.Text.Json.JsonValueKind.String)
      {
        var pos = posNode.GetValue<string>();
        string? anchor = null;
        if (obj.TryGetPropertyValue(_options.AnchorKey, out var anchorNode) &&
            anchorNode is JsonValue &&
            anchorNode.GetValueKind() == System.Text.Json.JsonValueKind.String)
        {
          anchor = anchorNode.GetValue<string>();
        }

        moves.Add((item, pos, anchor));
      }
    }

    foreach (var (item, pos, anchor) in moves)
    {
      int currentIdx = list.IndexOf(item);
      if (currentIdx == -1) continue;

      list.RemoveAt(currentIdx);

      int targetIdx = list.Count; // Default to end

      if (pos == "start")
      {
        targetIdx = 0;
      }
      else if (pos == "end")
      {
        targetIdx = list.Count;
      }
      else if ((pos == "before" || pos == "after") && anchor != null)
      {
        int anchorIdx = -1;
        for (int i = 0; i < list.Count; i++)
        {
          var id = GetId(list[i]);
          if (id == anchor)
          {
            anchorIdx = i;
            break;
          }
        }

        if (anchorIdx != -1)
        {
          if (pos == "before") targetIdx = anchorIdx;
          else if (pos == "after") targetIdx = anchorIdx + 1;
        }
        else if (_options.ThrowOnMissingAnchor)
        {
          throw new InvalidOperationException($"Anchor '{anchor}' not found for item.");
        }
      }

      if (targetIdx >= list.Count)
      {
        list.Add(item);
      }
      else
      {
        list.Insert(targetIdx, item);
      }
    }
  }

  private string? GetId(JsonNode? node)
  {
    if (node is JsonObject obj &&
        obj.TryGetPropertyValue(_options.IdKey, out var idNode) &&
        idNode is JsonValue &&
        idNode.GetValueKind() == System.Text.Json.JsonValueKind.String)
    {
      return idNode.GetValue<string>();
    }
    return null;
  }

  private bool IsDelete(JsonNode? node)
  {
    if (node is JsonObject obj &&
        obj.TryGetPropertyValue(_options.PatchKey, out var val) &&
        val is JsonValue &&
        val.GetValueKind() == System.Text.Json.JsonValueKind.String &&
        val.GetValue<string>() == "delete")
    {
      return true;
    }
    return false;
  }

  private JsonNode? MergeObjects(JsonObject baseObj, JsonObject overrideObj)
  {
    var result = (JsonObject)baseObj.DeepClone();
    var moves = new List<(string Key, string Position, string? Anchor)>();

    foreach (var kvp in overrideObj)
    {
      var key = kvp.Key;
      var overrideValue = kvp.Value;

      // Extract positioning info from overrideValue BEFORE merging
      if (overrideValue is JsonObject overrideSubObj)
      {
        bool hasPosition = overrideSubObj.TryGetPropertyValue(_options.PositionKey, out var posNode) &&
            posNode is JsonValue &&
            posNode.GetValueKind() == System.Text.Json.JsonValueKind.String;

        if (hasPosition)
        {
          var pos = posNode!.GetValue<string>();
          string? anchor = null;
          if (overrideSubObj.TryGetPropertyValue(_options.AnchorKey, out var anchorNode) &&
               anchorNode is JsonValue &&
               anchorNode.GetValueKind() == System.Text.Json.JsonValueKind.String)
          {
            anchor = anchorNode.GetValue<string>();
          }
          moves.Add((key, pos, anchor));
        }

        // Check for $patch: "delete"
        if (overrideSubObj.TryGetPropertyValue(_options.PatchKey, out var patchVal) &&
            patchVal is JsonValue &&
            patchVal.GetValueKind() == System.Text.Json.JsonValueKind.String &&
            patchVal.GetValue<string>() == "delete")
        {
          result.Remove(key);
          continue;
        }
      }

      if (result.ContainsKey(key))
      {
        var baseValue = result[key];
        // Special case: Primitive base + Metadata-only override (no $value) -> preserve base value
        if ((baseValue is JsonValue || baseValue is null) &&
            overrideValue is JsonObject ovObj &&
            !ovObj.ContainsKey(_options.ValueKey) &&
            (ovObj.ContainsKey(_options.PositionKey) || ovObj.ContainsKey(_options.PatchKey)))
        {
          // If it's just metadata (position/patch), we keep baseValue.
          // But we already handled patch:delete above.
          // If it's just position, we keep baseValue.
          // But if it has other properties, it replaces baseValue (primitive).
          // Spec: "If the key is a primitive... If $value missing, preserve original base value."
          // This implies we ignore the override object structure and keep the primitive?
          // Yes, effectively strict preservation of primitive if no new value provided.
          // But what if override has other data? E.g. {"a": 1} overrides {"a": {"$position": "start", "b": 2}}?
          // No, the case is: Base={"a": 1}, Override={"a": {"$position": "start"}}.
          // Result should be {"a": 1} (but moved).
          // Currently Merge(1, {"$position": "start"}) returns {"$position": "start"}.
          // Correct approach: Return baseValue.

          // However, we need to be careful not to discard other updates.
          // If override has ANY data keys, it replaces primitive.
          // How to distinguish data keys from control keys?
          // Only strictly if override has NO $value and ONLY control keys?
          // Spec says "If the key is a primitive... If $value missing, preserve..."
          // It doesn't say "unless other keys present".
          // So if base is primitive, and override is object without $value, we ALWAYS preserve base primitive?
          // This seems to imply override object CANNOT replace a primitive with an object unless it uses implicit replacement (by NOT using control keys?).
          // Wait, if I want to replace 1 with {"x": 2}, I just do {"a": {"x": 2}}.
          // Does {"x": 2} contain control keys? No.
          // So logic: If override is Object AND contains control keys ($position), treat as metadata wrapper?

          bool hasControl = ovObj.ContainsKey(_options.PositionKey) || ovObj.ContainsKey(_options.AnchorKey);
          bool hasValue = ovObj.ContainsKey(_options.ValueKey);

          if (hasControl && !hasValue)
          {
            // Preserve base value, apply reordering (already captured in moves)
            // Do NOT call Merge. Keep result[key] as is.
            continue;
          }
        }

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
          // New key.
          // If override is object with $value, extract it. (Handled in Merge)
          // If override is object with $position, Merge returns object.
          // If purely metadata? e.g. {"a": {"$position": "start"}} where "a" didn't exist.
          // Result: {"a": {"$position": "start"}}. 
          // This seems fine for new keys? Or should it be ignored?
          // Spec doesn't say.
          result[key] = Merge(null, overrideValue);
        }
      }
    }

    if (moves.Count > 0)
    {
      // Apply Reordering
      var props = new List<KeyValuePair<string, JsonNode?>>();
      foreach (var kvp in result) props.Add(new KeyValuePair<string, JsonNode?>(kvp.Key, kvp.Value));

      foreach (var (key, pos, anchor) in moves)
      {
        int currentIdx = props.FindIndex(p => p.Key == key);
        if (currentIdx == -1) continue;

        var kvp = props[currentIdx];
        props.RemoveAt(currentIdx);

        int targetIdx = props.Count;

        if (pos == "start") targetIdx = 0;
        else if (pos == "end") targetIdx = props.Count;
        else if ((pos == "before" || pos == "after") && anchor != null)
        {
          int anchorIdx = props.FindIndex(p => p.Key == anchor);
          if (anchorIdx != -1)
          {
            if (pos == "before") targetIdx = anchorIdx;
            else if (pos == "after") targetIdx = anchorIdx + 1;
          }
          else if (_options.ThrowOnMissingAnchor)
          {
            throw new InvalidOperationException($"Anchor '{anchor}' not found for property '{key}'.");
          }
        }

        if (targetIdx >= props.Count) props.Add(kvp);
        else props.Insert(targetIdx, kvp);
      }

      result.Clear();
      foreach (var kvp in props)
      {
        result[kvp.Key] = kvp.Value;
      }
    }

    return result;
  }
}
