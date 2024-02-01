// Copyright (c) 2016 Xamarin Inc.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using MonoDevelop.MSBuild.Language.Typesystem;

namespace MonoDevelop.MSBuild.Language.Syntax;

public abstract class MSBuildSyntax : ISymbol, ITypedSymbol, IDeprecatable, IHasHelpUrl
{
	protected MSBuildSyntax (
		string name, DisplayText description, MSBuildValueKind valueKind = MSBuildValueKind.Unknown,
		CustomTypeInfo? customType = null,
		string? deprecationMessage = null,
		string? helpUrl = null)
	{
		Name = name;
		Description = description;
		DeprecationMessage = deprecationMessage;
		HelpUrl = helpUrl;

		ValueKind = valueKind;
		CustomType = customType;
	}

	public string Name { get; }
	public DisplayText Description { get; }

	public virtual MSBuildValueKind ValueKind { get; }
	public CustomTypeInfo? CustomType { get; }
	public string? DeprecationMessage { get; }
	public string? HelpUrl { get; }
}