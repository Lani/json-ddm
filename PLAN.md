# Implementation Plan: JsonDdm

This plan outlines the steps to implement the Deterministic Deep Merge (DDM) library for .NET, as defined in `SPEC.md` and `README.md`.

## Phase 1: Project Setup & Core Infrastructure

- [x] **1.1. Create Project Structure**
  - Verify the existing solution structure (`JsonDdm` library and `JsonDdm.Tests`).
  - Ensure target frameworks are set correctly (net8.0, net9.0, net10.0, netstandard2.0) in `JsonDdm.csproj`.
  - Check that `System.Text.Json` dependency is included if needed for older frameworks (netstandard2.0).

- [x] **1.2. Define Configuration Options**
  - Create the `JsonDdmOptions` class as per the spec.
  - specific properties: `IdKey` (default "$id"), `PositionKey` (default "$position"), `AnchorKey` (default "$anchor"), `PatchKey` (default "$patch"), `ValueKey` (default "$value"), `ThrowOnMissingAnchor` (default true).

- [ ] **1.3. Create the Merger Class Skeleton**
  - Verify that the `JsonDdm` class exists.
  - Define the main public entry point: `JsonNode? Merge(JsonNode? baseNode, JsonNode? overrideNode)`.

## Phase 2: Basic Merging Logic (No Reordering)

- [ ] **2.1. Implement Primitive Merging**
  - Handle nulls, strings, numbers, booleans.
  - Implement "override replaces base" logic for primitives.
  - Create unit tests for basic primitive overrides.

- [ ] **2.2. Implement Object Merging (Recursive)**
  - Iterate through keys in the override object.
  - If key doesn't exist in base, add it.
  - If key exists, recursively call `Merge`.
  - Create unit tests for nested object merging.

## Phase 3: Control Keys & Patching

- [ ] **3.1. Implement `$patch: "delete"`**
  - Modify Object merging logic to check for `$patch` property in the override object.
  - If found, remove the key from the result instead of merging.
  - Handle array item deletion via `$patch` inside array processing (to be implemented in Phase 4, but structure it now).
  - Create unit tests for deleting keys.

- [ ] **3.2. Implement `$value` Handling (Type Switching)**
  - Handle cases where a primitive in base is overridden by an object with `$value`.
  - Extract the value from `$value` and use it as the merger result for that node.
  - Create unit tests for upgrading a primitive to an object-wrapper and back.

## Phase 4: Array Merging & Identity

- [ ] **4.1. Implement Array Matching Strategy**
  - Iterate override array.
  - Identify items by `$id` (using the configured `IdKey`).
  - Match against base array items with the same `$id`.
  - Handle items without `$id` (treat as new/append).

- [ ] **4.2. Implement Array Item Merging**
  - If match found, recursively `Merge` the array items.
  - If no match, add the new item.
  - Respect `$patch: "delete"` within array items to remove them from the list.
  - Create unit tests for array merging (updating existing items vs appending new ones).

## Phase 5: Reordering Logic (The Core Complexity)

- [ ] **5.1. Implement Reordering Logic Structure**
  - Create a helper method/class to handle list manipulation based on `$position` and `$anchor`.
  - Supported positions: `start`, `end`, `before`, `after`.

- [ ] **5.2. Apply Reordering to Arrays**
  - After merging array items, apply single-pass or multi-pass reordering.
  - Handle `ThrowOnMissingAnchor` logic (throw vs default to end).
  - Create unit tests for array item reordering.

- [ ] **5.3. Apply Reordering to Object Properties**
  - Use `JsonObject` manipulation to change property order.
  - Apply the same `$position`/`$anchor` logic to object keys.
  - Create unit tests for object property reordering.

## Phase 6: Refinement & Edge Cases

- [ ] **6.1. Escaping Support**
  - Implement logic to handle double prefixes (e.g., `$$id` -> `$id`).
  - Ensure control keys can be used as literal data keys if escaped.
  - Create unit tests for escaping.

- [ ] **6.2. Conflict Resolution Verification**
  - Verify "Last-In-Wins" behavior (ensure strict order of operations).
  - Create complex scenarios with multiple merges to verify determinism.

## Phase 7: CLI Tool (Schema Patching)

- [ ] **7.1. Create CLI Project**
  - Create a new console application project `JsonDdm.Tools` (or similar).
  - Add argument parsing.

- [ ] **7.2. Implement Schema Walker**
  - Load a JSON Schema file.
  - Recursively traverse the schema.

- [ ] **7.3. Implement Schema Injection**
  - Inject the `$defs` for DDM controls.
  - Wrap properties in `oneOf` to allow DDM control objects.
  - Output the patched schema.
  - Test with a sample schema.

## Phase 8: Final Polish

- [ ] **8.1. Documentation**
  - Update README with simple usage examples based on the actual API.
  - Ensure XML documentation comments are present on public APIs.

- [ ] **8.2. CI/CD Preparation**
  - Ensure all tests pass.
  - Verify NuGet packaging metadata.
