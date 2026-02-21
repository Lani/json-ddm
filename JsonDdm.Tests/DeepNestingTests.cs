using System.Text.Json.Nodes;
using Xunit;

namespace JsonDdm.Tests;

/// <summary>
/// Tests that verify all control keys ($id, $position, $anchor, $patch, $value)
/// work correctly in deeply nested structures (3+ levels).
/// </summary>
public class DeepNestingTests
{
  private readonly JsonDdm _merger = new();

  #region $id - Array Item Identification at Depth

  [Fact]
  public void DeepMerge_ArrayId_ThreeLevelsDeep_MatchesAndMerges()
  {
    var baseJson = """
        {
          "level1": {
            "level2": {
              "items": [
                { "$id": "item-a", "value": 100 },
                { "$id": "item-b", "value": 200 }
              ]
            }
          }
        }
        """;

    var overrideJson = """
        {
          "level1": {
            "level2": {
              "items": [
                { "$id": "item-a", "value": 999 }
              ]
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var items = result!["level1"]!["level2"]!["items"]!.AsArray();

    Assert.Equal(2, items.Count);
    var itemA = items.First(x => x?["$id"]?.GetValue<string>() == "item-a");
    Assert.Equal(999, itemA!["value"]?.GetValue<int>());
  }

  [Fact]
  public void DeepMerge_ArrayId_FourLevelsDeep_AddsNewItem()
  {
    var baseJson = """
        {
          "app": {
            "config": {
              "modules": {
                "plugins": [
                  { "$id": "auth", "enabled": true }
                ]
              }
            }
          }
        }
        """;

    var overrideJson = """
        {
          "app": {
            "config": {
              "modules": {
                "plugins": [
                  { "$id": "logging", "enabled": false }
                ]
              }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var plugins = result!["app"]!["config"]!["modules"]!["plugins"]!.AsArray();

    Assert.Equal(2, plugins.Count);
    Assert.Contains(plugins, p => p?["$id"]?.GetValue<string>() == "auth");
    Assert.Contains(plugins, p => p?["$id"]?.GetValue<string>() == "logging");
  }

  #endregion

  #region $patch (delete) - Deletion at Depth

  [Fact]
  public void DeepMerge_PatchDelete_ThreeLevelsDeep_RemovesProperty()
  {
    var baseJson = """
        {
          "level1": {
            "level2": {
              "level3": {
                "keepMe": "value1",
                "deleteMe": "value2"
              }
            }
          }
        }
        """;

    var overrideJson = """
        {
          "level1": {
            "level2": {
              "level3": {
                "deleteMe": { "$patch": "delete" }
              }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var level3 = result!["level1"]!["level2"]!["level3"]!.AsObject();

    Assert.True(level3.ContainsKey("keepMe"));
    Assert.False(level3.ContainsKey("deleteMe"));
  }

  [Fact]
  public void DeepMerge_PatchDelete_ArrayItem_ThreeLevelsDeep()
  {
    var baseJson = """
        {
          "root": {
            "container": {
              "items": [
                { "$id": "keep", "data": 1 },
                { "$id": "remove", "data": 2 }
              ]
            }
          }
        }
        """;

    var overrideJson = """
        {
          "root": {
            "container": {
              "items": [
                { "$id": "remove", "$patch": "delete" }
              ]
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var items = result!["root"]!["container"]!["items"]!.AsArray();

    Assert.Single(items);
    Assert.Equal("keep", items[0]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void DeepMerge_PatchDelete_FourLevelsDeep_NestedInArray()
  {
    var baseJson = """
        {
          "system": {
            "modules": [
              {
                "$id": "mod1",
                "settings": {
                  "feature-a": true,
                  "feature-b": false
                }
              }
            ]
          }
        }
        """;

    var overrideJson = """
        {
          "system": {
            "modules": [
              {
                "$id": "mod1",
                "settings": {
                  "feature-b": { "$patch": "delete" }
                }
              }
            ]
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var settings = result!["system"]!["modules"]!.AsArray()[0]!["settings"]!.AsObject();

    Assert.True(settings.ContainsKey("feature-a"));
    Assert.False(settings.ContainsKey("feature-b"));
  }

  #endregion

  #region $value - Value Extraction at Depth

  [Fact]
  public void DeepMerge_Value_ThreeLevelsDeep_UpdatesPrimitive()
  {
    var baseJson = """
        {
          "level1": {
            "level2": {
              "timeout": 30
            }
          }
        }
        """;

    var overrideJson = """
        {
          "level1": {
            "level2": {
              "timeout": { "$value": 60 }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var timeout = result!["level1"]!["level2"]!["timeout"];

    Assert.Equal(60, timeout?.GetValue<int>());
  }

  [Fact]
  public void DeepMerge_Value_FourLevelsDeep_InArrayItem()
  {
    var baseJson = """
        {
          "config": {
            "environment": {
              "vars": [
                { "$id": "PORT", "value": 3000 }
              ]
            }
          }
        }
        """;

    var overrideJson = """
        {
          "config": {
            "environment": {
              "vars": [
                { "$id": "PORT", "value": { "$value": 8080 } }
              ]
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var vars = result!["config"]!["environment"]!["vars"]!.AsArray();
    var port = vars.First(v => v?["$id"]?.GetValue<string>() == "PORT");

    Assert.Equal(8080, port!["value"]?.GetValue<int>());
  }

  [Fact]
  public void DeepMerge_Value_ThreeLevelsDeep_ConvertsObjectToPrimitive()
  {
    var baseJson = """
        {
          "a": {
            "b": {
              "c": { "x": 1, "y": 2 }
            }
          }
        }
        """;

    var overrideJson = """
        {
          "a": {
            "b": {
              "c": { "$value": "simplified" }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var cValue = result!["a"]!["b"]!["c"];

    Assert.Equal("simplified", cValue?.GetValue<string>());
  }

  #endregion

  #region $position + $anchor - Reordering at Depth

  [Fact]
  public void DeepMerge_Position_ArrayReordering_ThreeLevelsDeep()
  {
    var baseJson = """
        {
          "root": {
            "section": {
              "items": [
                { "$id": "first" },
                { "$id": "second" },
                { "$id": "third" }
              ]
            }
          }
        }
        """;

    var overrideJson = """
        {
          "root": {
            "section": {
              "items": [
                { "$id": "third", "$position": "start" }
              ]
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var items = result!["root"]!["section"]!["items"]!.AsArray();

    Assert.Equal("third", items[0]?["$id"]?.GetValue<string>());
    Assert.Equal("first", items[1]?["$id"]?.GetValue<string>());
    Assert.Equal("second", items[2]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void DeepMerge_Position_ArrayReordering_BeforeAnchor_FourLevelsDeep()
  {
    var baseJson = """
        {
          "app": {
            "ui": {
              "widgets": {
                "list": [
                  { "$id": "widget-a" },
                  { "$id": "widget-b" },
                  { "$id": "widget-c" }
                ]
              }
            }
          }
        }
        """;

    var overrideJson = """
        {
          "app": {
            "ui": {
              "widgets": {
                "list": [
                  { "$id": "widget-c", "$position": "before", "$anchor": "widget-a" }
                ]
              }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var list = result!["app"]!["ui"]!["widgets"]!["list"]!.AsArray();

    Assert.Equal("widget-c", list[0]?["$id"]?.GetValue<string>());
    Assert.Equal("widget-a", list[1]?["$id"]?.GetValue<string>());
    Assert.Equal("widget-b", list[2]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void DeepMerge_Position_ArrayReordering_AfterAnchor_ThreeLevelsDeep()
  {
    var baseJson = """
        {
          "level1": {
            "level2": {
              "tasks": [
                { "$id": "task1" },
                { "$id": "task2" },
                { "$id": "task3" }
              ]
            }
          }
        }
        """;

    var overrideJson = """
        {
          "level1": {
            "level2": {
              "tasks": [
                { "$id": "task1", "$position": "after", "$anchor": "task3" }
              ]
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var tasks = result!["level1"]!["level2"]!["tasks"]!.AsArray();

    Assert.Equal("task2", tasks[0]?["$id"]?.GetValue<string>());
    Assert.Equal("task3", tasks[1]?["$id"]?.GetValue<string>());
    Assert.Equal("task1", tasks[2]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void DeepMerge_Position_ObjectReordering_ThreeLevelsDeep()
  {
    var baseJson = """
        {
          "outer": {
            "middle": {
              "inner": {
                "first": 1,
                "second": 2,
                "third": 3
              }
            }
          }
        }
        """;

    var overrideJson = """
        {
          "outer": {
            "middle": {
              "inner": {
                "third": { "$value": 3, "$position": "start" }
              }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var inner = result!["outer"]!["middle"]!["inner"]!.AsObject();
    var keys = inner.Select(kvp => kvp.Key).ToList();

    Assert.Equal("third", keys[0]);
    Assert.Equal("first", keys[1]);
    Assert.Equal("second", keys[2]);
  }

  [Fact]
  public void DeepMerge_Position_ObjectReordering_BeforeAnchor_FourLevelsDeep()
  {
    var baseJson = """
        {
          "a": {
            "b": {
              "c": {
                "d": {
                  "prop1": "value1",
                  "prop2": "value2",
                  "prop3": "value3"
                }
              }
            }
          }
        }
        """;

    var overrideJson = """
        {
          "a": {
            "b": {
              "c": {
                "d": {
                  "prop3": { "$value": "value3", "$position": "before", "$anchor": "prop1" }
                }
              }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var d = result!["a"]!["b"]!["c"]!["d"]!.AsObject();
    var keys = d.Select(kvp => kvp.Key).ToList();

    Assert.Equal("prop3", keys[0]);
    Assert.Equal("prop1", keys[1]);
    Assert.Equal("prop2", keys[2]);
  }

  #endregion

  #region Combined Control Keys at Depth

  [Fact]
  public void DeepMerge_CombinedControls_ThreeLevelsDeep_ValueAndPosition()
  {
    var baseJson = """
        {
          "config": {
            "settings": {
              "options": {
                "timeout": 30,
                "retries": 3
              }
            }
          }
        }
        """;

    var overrideJson = """
        {
          "config": {
            "settings": {
              "options": {
                "timeout": { "$value": 60, "$position": "end" }
              }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var options = result!["config"]!["settings"]!["options"]!.AsObject();
    var keys = options.Select(kvp => kvp.Key).ToList();

    // timeout should be moved to end
    Assert.Equal("retries", keys[0]);
    Assert.Equal("timeout", keys[1]);
    Assert.Equal(60, options["timeout"]?.GetValue<int>());
  }

  [Fact]
  public void DeepMerge_CombinedControls_ArrayMergeUpdateAndReorder_ThreeLevelsDeep()
  {
    var baseJson = """
        {
          "app": {
            "modules": {
              "plugins": [
                { "$id": "auth", "priority": 1 },
                { "$id": "logging", "priority": 2 },
                { "$id": "metrics", "priority": 3 }
              ]
            }
          }
        }
        """;

    var overrideJson = """
        {
          "app": {
            "modules": {
              "plugins": [
                { "$id": "metrics", "priority": 10, "$position": "start" }
              ]
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var plugins = result!["app"]!["modules"]!["plugins"]!.AsArray();

    Assert.Equal("metrics", plugins[0]?["$id"]?.GetValue<string>());
    Assert.Equal(10, plugins[0]?["priority"]?.GetValue<int>());
    Assert.Equal("auth", plugins[1]?["$id"]?.GetValue<string>());
    Assert.Equal("logging", plugins[2]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void DeepMerge_CombinedControls_DeleteAndReorder_FourLevelsDeep()
  {
    var baseJson = """
        {
          "system": {
            "config": {
              "database": {
                "connections": [
                  { "$id": "primary", "host": "db1.example.com" },
                  { "$id": "secondary", "host": "db2.example.com" },
                  { "$id": "cache", "host": "redis.example.com" }
                ]
              }
            }
          }
        }
        """;

    var overrideJson = """
        {
          "system": {
            "config": {
              "database": {
                "connections": [
                  { "$id": "secondary", "$patch": "delete" },
                  { "$id": "cache", "$position": "start" }
                ]
              }
            }
          }
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var connections = result!["system"]!["config"]!["database"]!["connections"]!.AsArray();

    Assert.Equal(2, connections.Count);
    Assert.Equal("cache", connections[0]?["$id"]?.GetValue<string>());
    Assert.Equal("primary", connections[1]?["$id"]?.GetValue<string>());
  }

  [Fact]
  public void DeepMerge_MultipleArraysNested_EachWithOwnIds()
  {
    var baseJson = """
        {
          "teams": [
            {
              "$id": "team-a",
              "members": [
                { "$id": "alice", "role": "dev" },
                { "$id": "bob", "role": "qa" }
              ]
            },
            {
              "$id": "team-b",
              "members": [
                { "$id": "charlie", "role": "pm" }
              ]
            }
          ]
        }
        """;

    var overrideJson = """
        {
          "teams": [
            {
              "$id": "team-a",
              "members": [
                { "$id": "alice", "role": "lead" },
                { "$id": "dave", "role": "intern" }
              ]
            }
          ]
        }
        """;

    var result = _merger.Merge(JsonNode.Parse(baseJson), JsonNode.Parse(overrideJson));
    var teams = result!["teams"]!.AsArray();

    Assert.Equal(2, teams.Count);

    var teamA = teams.First(t => t?["$id"]?.GetValue<string>() == "team-a");
    var teamAMembers = teamA!["members"]!.AsArray();

    Assert.Equal(3, teamAMembers.Count);
    var alice = teamAMembers.First(m => m?["$id"]?.GetValue<string>() == "alice");
    Assert.Equal("lead", alice!["role"]?.GetValue<string>());
    Assert.Contains(teamAMembers, m => m?["$id"]?.GetValue<string>() == "dave");
  }

  #endregion
}
