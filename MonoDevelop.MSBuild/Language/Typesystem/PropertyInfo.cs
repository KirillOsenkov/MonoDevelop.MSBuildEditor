// Copyright (c) 2016 Xamarin Inc.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.MSBuild.Language.Typesystem
{
	public class PropertyInfo : VariableInfo, IHasHelpUrl
	{
		public bool IsReserved { get; }
		public bool IsReadOnly { get; }
		public string? HelpUrl { get; }


		public PropertyInfo (
			string name, DisplayText description,
			MSBuildValueKind valueKind = MSBuildValueKind.Unknown,
			CustomTypeInfo? customType = null, string? defaultValue = null,
			string? deprecationMessage = null,
			string? helpUrl = null)
			: base (name, description, valueKind, customType, defaultValue, deprecationMessage)
		{
			HelpUrl = helpUrl;
		}

		public PropertyInfo (
			string name, DisplayText description,
			bool isReserved, bool isReadOnly,
			MSBuildValueKind valueKind = MSBuildValueKind.Unknown,
			CustomTypeInfo? customType = null, string? defaultValue = null,
			string? deprecationMessage = null,
			string? helpUrl = null)
			: base (name, description, valueKind, customType, defaultValue, deprecationMessage)
		{
			IsReserved = isReserved;
			IsReadOnly = isReadOnly;
			HelpUrl = helpUrl;
		}
	}
}