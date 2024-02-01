// Copyright (c) 2016 Xamarin Inc.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.MSBuild.Language.Typesystem;

public sealed class CustomTypeValue (string name, DisplayText description, string? deprecationMessage = null, string? helpUrl = null)
	: ISymbol, ITypedSymbol, IDeprecatable, IHasHelpUrl
{
	public CustomTypeInfo CustomType { get; private set; }

	public MSBuildValueKind ValueKind => MSBuildValueKind.CustomType;

	public string Name { get; } = name;

	public DisplayText Description { get; } = description;

	public string? DeprecationMessage { get; } = deprecationMessage;
	public string? HelpUrl { get; } = helpUrl;

	internal void SetParent (CustomTypeInfo parent) => CustomType = parent;
}