// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.MSBuild;

/// <summary>
/// Represents text that can be displayed to the user and that may have richer formatting available
/// </summary>
public readonly struct DisplayText
{
	public DisplayText (string text, object? displayElement = null)
	{
		Text = text;
		DisplayElement = displayElement;
	}

	/// <summary>
	/// An optional formatted display element for tooltip in the editor. If not provided, one will be created from the text.
	/// </summary>
	public object? DisplayElement { get; }

	public string? Text { get; }
	public bool IsEmpty => string.IsNullOrEmpty (Text);

	public static implicit operator DisplayText (string? s) => new (s);
}
