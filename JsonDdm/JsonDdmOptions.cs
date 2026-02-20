namespace JsonDdm;

/// <summary>
/// Configuration options for the JSON Deterministic Deep Merge operation.
/// </summary>
public class JsonDdmOptions
{
  /// <summary>
  /// Gets or sets the key used to identify unique items in an array.
  /// Default is "$id".
  /// </summary>
  public string IdKey { get; set; } = "$id";

  /// <summary>
  /// Gets or sets the key used to specify the position of an item relative to an anchor.
  /// Default is "$position".
  /// </summary>
  public string PositionKey { get; set; } = "$position";

  /// <summary>
  /// Gets or sets the key used to specify the anchor item for positioning.
  /// Default is "$anchor".
  /// </summary>
  public string AnchorKey { get; set; } = "$anchor";

  /// <summary>
  /// Gets or sets the key used to specify a patch operation (e.g., "delete").
  /// Default is "$patch".
  /// </summary>
  public string PatchKey { get; set; } = "$patch";

  /// <summary>
  /// Gets or sets the key used to specify the value of a primitive when it is being upgraded to an object for metadata purposes.
  /// Default is "$value".
  /// </summary>
  public string ValueKey { get; set; } = "$value";

  /// <summary>
  /// Gets or sets a value indicating whether to throw an exception when a specified anchor is missing.
  /// If false, the item will be appended to the end of the collection (relaxed mode).
  /// Default is true.
  /// </summary>
  public bool ThrowOnMissingAnchor { get; set; } = true;
}
