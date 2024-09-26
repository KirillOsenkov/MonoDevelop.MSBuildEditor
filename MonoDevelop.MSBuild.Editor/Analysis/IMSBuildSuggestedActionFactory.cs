// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.MSBuild.Editor.CodeActions;

namespace MonoDevelop.MSBuild.Editor.Analysis
{
	//works around the WPF dependency in ISuggestedAction
	interface IMSBuildSuggestedActionFactory
	{
		ISuggestedAction CreateSuggestedAction (PreviewChangesService previewService, ITextView textView, ITextBuffer buffer, MSBuildCodeAction action);
	}
}
