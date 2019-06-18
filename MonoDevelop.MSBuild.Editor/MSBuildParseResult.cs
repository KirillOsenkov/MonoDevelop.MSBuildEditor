﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.IntelliSense;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.MSBuild.Editor.Completion
{
	public class MSBuildParseResult : XmlParseResult
	{
		public MSBuildParseResult (XDocument xDocument, List<XmlDiagnosticInfo> diagnostics) : base (xDocument, diagnostics)
		{
		}
	}
}