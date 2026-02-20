# JsonDdm

A JSON Deterministic Deep Merge .NET library.

## Features

- Deep merging of JSON documents with structural parity.
- Declarative array item reordering based on unique identifiers.
- Schema-agnostic: No need for C# classes or JSON Schema.
- Control keys for patching, reordering, and value updates.
- Broad compatibility (.NET Standard 2.0+)

## Why JsonDdm? (Comparison)

`JsonDdm` solves the specific problem of **Declarative Array Reordering & Identity Merging** in a schema-agnostic way, which existing libraries do not fully address.

| Feature                 | **JsonDdm**                          | **Newtonsoft.Json (Merge)** | **Microsoft.Extensions.Configuration** | **JSON Patch (RFC 6902)**     |
| :---------------------- | :----------------------------------- | :-------------------------- | :------------------------------------- | :---------------------------- |
| **Deep Merge Objects**  | ✅ Yes                               | ✅ Yes                      | ✅ Yes                                 | ❌ No (Requires manual paths) |
| **Array Item Identity** | ✅ **By `$id`** (Robust)             | ❌ Index only               | ❌ Index only (`array:0`)              | ❌ Index only (`/array/0`)    |
| **Array Reordering**    | ✅ **Declarative** (`before` `auth`) | ❌ No support               | ❌ No support                          | ✅ Imperative (`move` op)     |
| **Readability**         | ✅ High (Mirror shape)               | ✅ High                     | ✅ High                                | ❌ Low (List of Ops)          |
| **Conflict Resolution** | ✅ Deterministic rules               | ❌ Last-write wins          | ❌ Lacks context                       | ❌ Depends on Op order        |

### Key Differentiators

1.  **The Array Identity Gap:** In standard tools, updating an item in a list usually requires knowing its index. If the base layer changes, your index-based override breaks. `JsonDdm` solves this with robust `$id` matching.
2.  **Declarative Reordering:** Instead of calculating target indices (which break easily), `JsonDdm` allows you to position items relative to others (e.g., "Put this _before_ the 'Save' button").

## An Opinionated Library

`JsonDdm` is not a general-purpose JSON utility; it is a specialized protocol for configuration management.

### ✅ When to use it

- **Frontend-Driven Configuration:** You are building a CMS, Dashboard, or Page Builder where the UI sends a "delta" of changes.
- **Complex Default Management:** You ship a large default configuration (e.g., a standard set of reports or widgets) and want users to hide, reorder, or tweak specific items without copying the entire file.
- **Resilience:** You need overrides to survive changes in the base layer (e.g., "Add my plugin after the Core plugin", regardless of where the Core plugin is located in the list).

### ❌ When NOT to use it

- **High-Frequency Data Merging:** The reordering logic involves object manipulations that are more expensive than a simple dictionary merge. Do not use this for real-time telemetry pipelines.
- **Simple Key-Value Stores:** If you just need to override connection strings or feature flags, standard `.NET Configuration` is faster and standard.
- **Strict "Pure" Data Requirements:** If your consuming application cannot tolerate the possibility of a property becoming an object (e.g., `{ "timeout": 30 }` becoming `{ "timeout": { "$value": 60... } }`), widespread adoption of DDM might require too many changes to your deserialization logic.
  **No conform identity property:** If your configuraiton array items do not have a unique identifier (like `$id`), you will lose the benefits of identity merging and reordering, and it may be simpler to use a different approach.

## Installation

```bash
dotnet add package JsonDdm
```

## Details

See [SPEC.md](SPEC.md) for details on the Deterministic Deep Merge (DDM) protocol.

## About

Made with 🤘 by [Niklas "Lani" Lagergren](https://github.com/Lani) and [Gemini](https://gemini.google.com/).
