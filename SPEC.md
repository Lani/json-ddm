## Specification: Deterministic Deep Merge (DDM) for JSON

This specification defines a schema-agnostic protocol for merging multiple JSON configuration layers into a single document. It is designed for a .NET backend to process frontend-driven configurations where the override layer mimics the original structure. It enables deep merging, item reordering, and partial updates without requiring path-based syntax (like RFC 6902).

### 1. Core Principles

- **Determinism:** The merge result must be identical regardless of the environment, provided the input layers and their order remain the same.
- **Structural Parity:** Overrides follow the nested shape of the base document.
- **Schema-Agnostic:** The engine does not require a C# class or JSON Schema; it operates on the intrinsic types of the JSON DOM.
- **Unified API:** A single set of "Control Keys" handles reordering for both Objects and Arrays.

### 2. Identity and Matching

To merge items instead of simply replacing them, the engine must identify "the same" entity across layers.

- **Objects (as Map keys):** The JSON **Key Name** is the unique identifier.
- **Arrays (as Collections):** The reserved property **`$id`** is the unique identifier. If an object in an array lacks an `$id`, it is treated as a unique, anonymous entry.

### 3. The Control Keys (Metadata)

The engine operations are controlled by specific metadata keys. These keys are configurable, but standard defaults are defined below.

#### 3.1 Naming Strategy

The default strategy uses a `$` prefix to distinguish control keys from data. However, the engine can be configured to use:

- **Different Prefixes:** e.g., `@id`, `_id`, `__id`.
- **No Prefix:** e.g., `id` (Risk: high collision with data).
- **Custom Names:** Valid JSON keys mapped to specific control functions.

#### 3.2 Default Control Keys

| Key             | Purpose                                                                                             |
| :-------------- | :-------------------------------------------------------------------------------------------------- |
| **`$id`**       | (Array only) The unique identifier for an item.                                                     |
| **`$position`** | Defines the target location: `"start"`, `"end"`, `"before"`, or `"after"`.                          |
| **`$anchor`**   | The reference point for `$position`. For Arrays, this is an `$id`. For Objects, this is a Key Name. |
| **`$patch`**    | If set to `"delete"`, the item/key is removed from the resulting document.                          |
| **`$value`**    | (Optional) Used when updating a primitive value AND reordering it simultaneously.                   |

### 4. Merging Logic by Type

#### 4.1 Primitives (String, Number, Boolean, Null)

The value in the higher-priority layer completely overwrites the lower-priority layer.

#### 4.2 Objects (Recursive Merge)

1. **Match:** For every key in the override, find the corresponding key in the base.
2. **Patch:** If the override value is an object containing `$patch: "delete"`, remove the key.
3. **Update:** Recursively merge child objects.
4. **Reorder:** If `$position` is present:

- If the key is a primitive in the base layer:
  - If `$value` is present, update the value.
  - If `$value` is missing, preserve the original base value.
- Detach the key from its current sequence and re-insert it relative to the `$anchor`.

#### 4.3 Arrays (Identify & Reorder)

1. **Alignment:** Iterate through the override array.
2. **Merge:** For items with a matching `$id`, perform a deep merge of their properties.
3. **Insertion:** Items with a new `$id` (or no `$id`) are treated as new entries.
4. **Reorder:** Apply the `$position` and `$anchor` logic.

- _Example:_ If an item has `$position: "before", $anchor: "auth"`, it is moved immediately before the item with `$id: "auth"`.

### 5. Deterministic Conflict Resolution

In cases of ambiguity (e.g., Layer B moves an item to the start, Layer C moves it to the end):

- **Last-In-Wins:** The layer with the highest priority (processed last) dictates the final state of both data and position.
- **Missing Anchors:** If a `$position` refers to an `$anchor` that does not exist in the merged set, the item defaults to the `"end"` of the collection.

### 6. Comprehensive Example

**Base Layer:**

```json
{
  "theme": {
    "primary": "#000",
    "secondary": "#fff"
  },
  "widgets": [
    { "$id": "weather", "unit": "C" },
    { "$id": "clock", "format": "24h" }
  ]
}
```

**Override Layer:**

```json
{
  "theme": {
    "secondary": {
      "$value": "#ccc",
      "$position": "before",
      "$anchor": "primary"
    }
  },
  "widgets": [
    { "$id": "clock", "$position": "start" },
    {
      "$id": "news",
      "source": "rss",
      "$position": "after",
      "$anchor": "weather"
    }
  ]
}
```

**Merged Result:**

```json
{
  "theme": {
    "secondary": "#ccc",
    "primary": "#000"
  },
  "widgets": [
    { "$id": "clock", "format": "24h" },
    { "$id": "weather", "unit": "C" },
    { "$id": "news", "source": "rss" }
  ]
}
```

### 7. .NET Implementation Guidance

- **Configuration:** Use a simple POCO (Plain Old CLR Object) to define the control keys. This ensures the library remains dependency-free and easy to use in any .NET context (Console, Desktop, or Web). Consumers using dependency injection (like in ASP.NET Core) can easily register this POCO with `IOptions<T>`.

  ```csharp
  public class MergeOptions
  {
      public string IdKey { get; set; } = "$id";
      public string PositionKey { get; set; } = "$position";
      public string AnchorKey { get; set; } = "$anchor";
      public string PatchKey { get; set; } = "$patch";
      public string ValueKey { get; set; } = "$value";
  }
  ```

- **Data Structure:** Use `System.Text.Json.Nodes.JsonObject` and `JsonArray`.
- **Sorting:** For Arrays, use a stable sort or a two-pass approach (Merge properties first, then apply positional moves).
- **Object Reordering:** Use `JsonObject`'s ability to manipulate property order by removing and re-inserting nodes at specific indices.
