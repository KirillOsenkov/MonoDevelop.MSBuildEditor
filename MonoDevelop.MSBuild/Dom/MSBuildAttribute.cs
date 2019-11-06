// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MonoDevelop.MSBuild.Language.Expressions;
using MonoDevelop.MSBuild.Schema;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.MSBuild.Dom
{
	class MSBuildAttribute : MSBuildObject
	{
		internal MSBuildAttribute nextSibling;

		internal MSBuildAttribute (MSBuildElement parent, XAttribute xattribute, MSBuildAttributeSyntax syntax, ExpressionNode value)
			: base (parent, value)
		{
			XAttribute = xattribute;
			Syntax = syntax;
		}

		public MSBuildAttributeSyntax Syntax { get; }
		public XAttribute XAttribute { get; }

		public override MSBuildSyntaxKind SyntaxKind => Syntax.SyntaxKind;
	}
}
