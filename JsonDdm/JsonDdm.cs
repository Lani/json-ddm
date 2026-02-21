using System.Text.Json.Nodes;

namespace JsonDdm;

/// <summary>
/// JSON Deterministic Deep Merge.
/// </summary>
public class JsonDdm
{
  private readonly JsonDdmOptions _options;
  private readonly char? _prefixChar;

  /// <summary>
  /// Initializes a new instance of the <see cref="JsonDdm"/> class.
  /// </summary>
  /// <param name="options">The merge options.</param>
  public JsonDdm(JsonDdmOptions? options = null)
  {
    _options = options ?? new JsonDdmOptions();
    _prefixChar = GetPrefixChar();
  }

  /// <summary>
  /// Extracts the prefix character used for control keys (e.g., '$' from '$id').
  /// Returns null if the first character is alphanumeric (no special prefix).
  /// </summary>
  private char? GetPrefixChar()
  {
    if (string.IsNullOrEmpty(_options.IdKey))
      return null;

    var firstChar = _options.IdKey[0];
    return char.IsLetterOrDigit(firstChar) ? null : firstChar;
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
      if (overrideNode is JsonObject ovObj)
      {
        return MergeObjects(new JsonObject(), ovObj);
      }
      if (overrideNode is JsonArray ovArr)
      {
        return MergeArrays(new JsonArray(), ovArr);
      }
      return overrideNode.DeepClone();
    }

    if (baseNode.GetType() != overrideNode.GetType())
    {
      if (overrideNode is JsonObject ovObj)
      {
        return MergeObjects(new JsonObject(), ovObj);
      }
      if (overrideNode is JsonArray ovArr)
      {
        return MergeArrays(new JsonArray(), ovArr);
      }
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

  private JsonNode? CloneWithoutControlKeys(JsonNode? node)
  {
    if (node is null) return null;

    if (node is JsonObject obj)
    {
      var newObj = new JsonObject();
      foreach (var kvp in obj)
      {
        if (kvp.Key == _options.PositionKey ||
            kvp.Key == _options.AnchorKey ||
            kvp.Key == _options.PatchKey)
        {
          continue;
        }
        // Do NOT strip ValueKey here because Merge() needs it to unwrap the value.

        newObj[kvp.Key] = kvp.Value?.DeepClone();
      }
      return newObj;
    }
    return node.DeepClone();
  }


  private JsonArray MergeArrays(JsonArray baseArr, JsonArray overrideArr)
  {
    // Use clone-on-write: store references initially, only clone when actually modifying
    var workingList = new List<(JsonNode? Item, bool NeedsClone)>();
    foreach (var item in baseArr)
    {
      workingList.Add((item, true)); // Mark as needing clone when used
    }

    // Build ID map to find index of base items by ID
    var baseIdMap = new Dictionary<string, int>();
    for (int i = 0; i < workingList.Count; i++)
    {
      var id = GetId(workingList[i].Item);
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
          workingList[baseIndex] = (null, false); // Mark for removal (no clone needed)
        }
        else
        {
          // Merge handles cloning internally, so result is already a new node
          var merged = Merge(workingList[baseIndex].Item, overrideItem);
          workingList[baseIndex] = (merged, false); // Already cloned by Merge
        }
      }
      else
      {
        // No match (new ID or no ID): Append
        if (!IsDelete(overrideItem))
        {
          appendList.Add(Merge(null, overrideItem));
        }
      }
    }

    // Construct result list
    var resultList = new List<JsonNode?>();
    foreach (var (item, needsClone) in workingList)
    {
      if (item != null)
      {
        // Only clone if item was never modified (still references original)
        resultList.Add(needsClone ? item.DeepClone() : item);
      }
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
        resultArr.Add(CloneWithoutControlKeys(item)); // Strip control keys here
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

    // Build ID-to-index dictionary once to avoid O(n²) complexity
    var idToIndex = new Dictionary<string, int>();
    for (int i = 0; i < list.Count; i++)
    {
      var id = GetId(list[i]);
      if (id != null)
      {
        idToIndex[id] = i;
      }
    }

    foreach (var (item, pos, anchor) in moves)
    {
      int currentIdx = list.IndexOf(item);
      if (currentIdx == -1) continue;

      list.RemoveAt(currentIdx);

      // Update ID-to-index map after removal
      for (int i = currentIdx; i < list.Count; i++)
      {
        var id = GetId(list[i]);
        if (id != null && idToIndex.ContainsKey(id))
        {
          idToIndex[id] = i;
        }
      }

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
        if (idToIndex.TryGetValue(anchor, out anchorIdx))
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

      // Update ID-to-index map after insertion
      for (int i = targetIdx; i < list.Count; i++)
      {
        var id = GetId(list[i]);
        if (id != null)
        {
          idToIndex[id] = i;
        }
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
    // Clone base to preserve property order
    var result = (JsonObject)baseObj.DeepClone();
    var moves = new List<(string Key, string Position, string? Anchor)>();

    foreach (var kvp in overrideObj)
    {
      var key = kvp.Key;
      var overrideValue = kvp.Value;

      // Handle key escaping (Phase 6.1)
      // If key starts with doubled prefix (e.g., "$$" when prefix is '$'), unescape it.
      // Spec: "If a data property literally matches a control key... escape it"
      // Default strategy is doubling prefix.
      string targetKey = key;
      if (_prefixChar.HasValue && key.Length >= 2 && key[0] == _prefixChar.Value && key[1] == _prefixChar.Value)
      {
        targetKey = key.Substring(1);
      }

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
          moves.Add((targetKey, pos, anchor));
        }

        // Check for $patch: "delete"
        if (overrideSubObj.TryGetPropertyValue(_options.PatchKey, out var patchVal) &&
            patchVal is JsonValue &&
            patchVal.GetValueKind() == System.Text.Json.JsonValueKind.String &&
            patchVal.GetValue<string>() == "delete")
        {
          result.Remove(targetKey);
          continue;
        }
      }

      if (result.ContainsKey(targetKey))
      {
        var baseValue = result[targetKey];
        // Special case: Primitive base + Metadata-only override (no $value) -> preserve base value
        if ((baseValue is JsonValue || baseValue is null) &&
            overrideValue is JsonObject ovObj &&
            !ovObj.ContainsKey(_options.ValueKey) &&
            (ovObj.ContainsKey(_options.PositionKey) || ovObj.ContainsKey(_options.PatchKey)))
        {
          bool hasControl = ovObj.ContainsKey(_options.PositionKey) || ovObj.ContainsKey(_options.AnchorKey);
          bool hasValue = ovObj.ContainsKey(_options.ValueKey);

          if (hasControl && !hasValue)
          {
            // Preserve base value, apply reordering (already captured in moves)
            continue;
          }
        }

        var cleanOverride = CloneWithoutControlKeys(overrideValue);
        result[targetKey] = Merge(baseValue, cleanOverride);
      }
      else
      {
        if (overrideValue is null)
        {
          result[targetKey] = null;
        }
        else
        {
          var cleanOverride = CloneWithoutControlKeys(overrideValue);
          result[targetKey] = Merge(null, cleanOverride);
        }
      }
    }

    if (moves.Count > 0)
    {
      // Apply Reordering
      var props = new List<KeyValuePair<string, JsonNode?>>();
      foreach (var kvp in result) props.Add(new KeyValuePair<string, JsonNode?>(kvp.Key, kvp.Value));

      // Build key-to-index dictionary once to avoid O(n²) complexity
      var keyToIndex = new Dictionary<string, int>();
      for (int i = 0; i < props.Count; i++)
      {
        keyToIndex[props[i].Key] = i;
      }

      foreach (var (key, pos, anchor) in moves)
      {
        int currentIdx = -1;
        if (!keyToIndex.TryGetValue(key, out currentIdx)) continue;

        var kvp = props[currentIdx];
        props.RemoveAt(currentIdx);

        // Update key-to-index map after removal
        for (int i = currentIdx; i < props.Count; i++)
        {
          keyToIndex[props[i].Key] = i;
        }

        int targetIdx = props.Count;

        if (pos == "start") targetIdx = 0;
        else if (pos == "end") targetIdx = props.Count;
        else if ((pos == "before" || pos == "after") && anchor != null)
        {
          int anchorIdx = -1;
          if (keyToIndex.TryGetValue(anchor, out anchorIdx))
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

        // Update key-to-index map after insertion
        for (int i = targetIdx; i < props.Count; i++)
        {
          keyToIndex[props[i].Key] = i;
        }
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
