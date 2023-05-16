// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Completion;

namespace MonoDevelop.MSBuild.Editor.TextStructure
{
	[Export (typeof (ITextStructureNavigatorProvider))]
	[ContentType (MSBuildContentType.Name)]
	class MSBuildTextStructureNavigatorProvider : ITextStructureNavigatorProvider
	{
		[ImportingConstructor]
		public MSBuildTextStructureNavigatorProvider (
			ITextStructureNavigatorSelectorService navigatorService,
			IContentTypeRegistryService contentTypeRegistry,
			XmlParserProvider xmlParserProvider)
		{
			NavigatorService = navigatorService;
			ContentTypeRegistry = contentTypeRegistry;
			XmlParserProvider = xmlParserProvider;
		}

		public ITextStructureNavigatorSelectorService NavigatorService { get; }
		public IContentTypeRegistryService ContentTypeRegistry { get; }
		public XmlParserProvider XmlParserProvider { get; }

		public ITextStructureNavigator CreateTextStructureNavigator (ITextBuffer textBuffer)
		{
			return new MSBuildTextStructureNavigator (textBuffer, this);
		}
	}
}
