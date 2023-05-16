// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.MSBuild.Language;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Editor.Logging;
using ProjectFileTools.NuGetSearch.Contracts;

namespace MonoDevelop.MSBuild.Editor.Completion
{
	[Export (typeof (IAsyncCompletionSourceProvider))]
	[Name ("MSBuild Completion Source Provider")]
	[ContentType (MSBuildContentType.Name)]
	class MSBuildCompletionSourceProvider : IAsyncCompletionSourceProvider
	{
		[ImportingConstructor]
		public MSBuildCompletionSourceProvider (
			IFunctionTypeProvider functionTypeProvider,
			IPackageSearchManager packageSearchManager,
			DisplayElementFactory displayElementFactory,
			JoinableTaskContext joinableTaskContext,
			MSBuildParserProvider parserProvider,
			XmlParserProvider xmlParserProvider,
			IEditorLoggerFactory loggerService)
		{
			FunctionTypeProvider = functionTypeProvider;
			PackageSearchManager = packageSearchManager;
			DisplayElementFactory = displayElementFactory;
			JoinableTaskContext = joinableTaskContext;
			ParserProvider = parserProvider;
			XmlParserProvider = xmlParserProvider;
			LoggerService = loggerService;
		}

		public IFunctionTypeProvider FunctionTypeProvider { get; }
		public IPackageSearchManager PackageSearchManager { get; }
		public DisplayElementFactory DisplayElementFactory { get; }
		public JoinableTaskContext JoinableTaskContext { get; }
		public MSBuildParserProvider ParserProvider { get; }
		public IEditorLoggerFactory LoggerService { get; }
		public XmlParserProvider XmlParserProvider { get; }

		[Import]
		public IEditorLoggerService EditorLoggerService { get; set; }

		[Import]
		public XmlParserProvider XmlParserProvider { get; set; }

		public IAsyncCompletionSource GetOrCreate (ITextView textView) =>
			textView.Properties.GetOrCreateSingletonProperty (() => {
				var logger = LoggerService.CreateLogger<MSBuildCompletionSource> (textView);
				return new MSBuildCompletionSource (textView, this, logger);
			});
	}
}