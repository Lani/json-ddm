## Specification: Deterministic Deep Merge (DDM) for JSON

This specification defines a schema-agnostic protocol for merging multiple JSON configuration layers into a single document. It is designed for a .NET backend to process frontend-driven configurations where the override layer mimics the original structure. It enables deep merging, item reordering, and partial updates without requiring path-based syntax (like RFC 6902).

For a detailed comparison with other libraries (like standard .NET config, JSON Patch, etc.), see the [Why JsonDdm? section in the README](README.md#why-jsonddm-comparison).

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

#### 3.1 Naming Strategy & Escaping

The default strategy uses a `$` prefix to distinguish control keys from data.

- **Escaping:** If a data property literally matches a control key (e.g., you need a field named `$id`), escape it by doubling the prefix (e.g., `$$id`). The engine will unescape it to `$id` in the final output.

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
   - **Deletion:** If an item contains `$patch: "delete"`, it is removed from the array.
3. **Insertion:** Items with a new `$id` (or no `$id`) are treated as new entries.
4. **Reorder:** Apply the `$position` and `$anchor` logic.

- _Example:_ If an item has `$position: "before", $anchor: "auth"`, it is moved immediately before the item with `$id: "auth"`.

**Note on Primitive Arrays:** Arrays containining primitive values (strings, numbers, booleans) do not support `$id` matching. They are treated as "Append Only" by default. To modify a specific primitive, the entire array must be replaced in the override layer, or the Primitives must be wrapped in objects with IDs in the base layer.

### 5. Deterministic Conflict Resolution

In cases of ambiguity (e.g., Layer B moves an item to the start, Layer C moves it to the end):

- **Last-In-Wins:** The layer with the highest priority (processed last) dictates the final state of both data and position.
- **Missing Anchors:** If a `$position` refers to an `$anchor` that does not exist in the merged set:
  - **Strict Mode (Recommended):** The merge operation throws an exception, preventing silent misconfiguration.
  - **Relaxed Mode:** The item defaults to the `"end"` of the collection.

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
      /// <summary>
      /// If true, throws an exception when an anchor is missing. Default is true for safety.
      /// </summary>
      public bool ThrowOnMissingAnchor { get; set; } = true;
  }
  ```

- **Data Structure:** Use `System.Text.Json.Nodes.JsonObject` and `JsonArray`.
- **Sorting:** For Arrays, use a stable sort or a two-pass approach (Merge properties first, then apply positional moves).
- **Object Reordering:** Use `JsonObject`'s ability to manipulate property order by removing and re-inserting nodes at specific indices.
- **Trade-offs:** Be aware that using `$value` changes the schema type of a property in the override layer (e.g., a number becomes an object). This prevents standard JSON Schema validation on the raw override files.

### 8. JSON Schema & Tooling

To validate DDM override files, the original JSON Schema must be patched to allow properties to be _either_ their original type _or_ a DDM Control Object.

#### 8.1 Schema Pattern

Use `$defs` to define the control structure once, then reference it via `oneOf`.

```json
{
  "$defs": {
    "ddmControls": {
      "type": "object",
      "properties": {
        "$position": { "enum": ["start", "end", "before", "after"] },
        "$anchor": { "type": "string" },
        "$patch": { "const": "delete" }
      }
    },
    "ddmInteger": {
      "oneOf": [
        { "type": "integer" },
        {
          "allOf": [
            { "$ref": "#/$defs/ddmControls" },
            {
              "properties": { "$value": { "type": "integer" } },
              "required": ["$value"]
            }
          ]
        }
      ]
    }
  }
}
```

#### 8.2 CLI Tooling Requirement

The implementation should include a command-line tool (e.g., `json-ddm-schema-patch`) that:

1.  Accepts a standard JSON Schema file.
2.  Recursively traverses the schema.
3.  Wraps every property definition in a `oneOf` block allowing DDM controls.
4.  Injects the standard `$defs` for DDM keys.
5.  Outputs a "DDM-Compatible" schema file for use in validation pipelines.

### 9. Diff Generation (Reverse Merge)

To support editors and UI tools, the library must support generating a DDM override document by comparing a **Base** document and a **Target** document.

`Diff(Base, Target) -> Override`

The generated Override must satisfy the condition: `Merge(Base, Override) is structurally equal to Target`.

#### 9.1 Diff Logic

- **Equality:** If a node in Target is identical to Base, it is omitted from the Override.
- **Modifications:** If a primitive value differs, the Target value is written to the Override.
- **Additions:** If a key/item exists in Target but not Base, it is added to the Override.
- **Deletions:** If a key/item exists in Base but not Target, it is added to the Override with `$patch: "delete"`.

#### 9.2 Reordering Detection

The Diff engine must detect changes in order for both Object keys and Array items (matched by `$id`).

- If the order in Target differs from Base, the engine must generate minimal `$position` and `$anchor` markers.
- **Heuristic:** The engine should prefer stable anchors. For example, if item A is moved to be before item B, generate `{ "$id": "A", "$position": "before", "$anchor": "B" }`.
