// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.MSBuild.Editor.Navigation;

namespace MonoDevelop.MSBuild.Editor.Commands
{
	[Export (typeof (ICommandHandler))]
	[ContentType (MSBuildContentType.Name)]
	[Name ("MSBuild Find References")]
	class MSBuildFindReferencesCommandHandler : ICommandHandler<FindReferencesCommandArgs>
	{
		[ImportingConstructor]
		public MSBuildFindReferencesCommandHandler (MSBuildNavigationService navigationService)
		{
			NavigationService = navigationService;
		}

		public MSBuildNavigationService NavigationService { get; }

		public string DisplayName { get; } = "Find References";

		public bool ExecuteCommand (FindReferencesCommandArgs args, CommandExecutionContext executionContext)
			=> NavigationService.FindReferences (args.SubjectBuffer, args.TextView.Caret.Position.BufferPosition);

		public CommandState GetCommandState (FindReferencesCommandArgs args)
		{
			var pos = args.TextView.Caret.Position.BufferPosition;
			if (NavigationService.CanFindReferences (args.SubjectBuffer, pos)) {
				return CommandState.Available;
			}

			// visible but disabled
			return new CommandState (true, false, false, true);
		}
	}
}
