// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// stubs to help imported files work w/o bringing in too many dependencies

using System.Composition;
using System.Runtime.CompilerServices;

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
	enum WellKnownLspServerKinds
	{
		MSBuild,
		Any,

		// some imported classes use this, alias it to our MSBuild value
		CSharpVisualBasicLspServer = MSBuild,

		// used by AbstractLanguageServerProtocolTests
		AlwaysActiveVSLspServer
	}

	static class WellKnownLspServerKindExtensions
	{
		public static string ToTelemetryString(this WellKnownLspServerKinds serverKind)
			=> serverKind switch
			{
				WellKnownLspServerKinds.MSBuild => LanguageName.MSBuild,
				_ => throw ExceptionUtilities.UnexpectedValue(serverKind),
			};
	}

    class LanguageName
    {
        public const string MSBuild = nameof(MSBuild);
    }
}

// Logger.cs has a Using for this namespace but doesn't actually use classes from it
namespace Microsoft.CodeAnalysis.Options {
}

namespace Microsoft.CodeAnalysis.LanguageServer
{
    interface ExperimentalCapabilitiesProvider : ICapabilitiesProvider { }
}

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    class RoslynDocumentSymbol : Roslyn.LanguageServer.Protocol.DocumentSymbol { }
}

namespace Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions
{
	class StarredCompletionAssemblyHelper
	{
		// not called unless serverConfiguration.StarredCompletionsPath is non-null
		public static string GetStarredCompletionAssemblyPath(string starredCompletionsPath)
			=> throw new NotSupportedException ();
	}

}

namespace Microsoft.CodeAnalysis.Serialization
{
    struct AssetPath
    {
        public bool IncludeDocumentAttributes { get; }
        public bool IncludeDocumentText { get; }
    }
}

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    [Shared]
    [Export(typeof(IAsynchronousOperationListenerProvider))]
    [Export(typeof(AsynchronousOperationListenerProvider))]
    internal sealed partial class AsynchronousOperationListenerProvider : IAsynchronousOperationListenerProvider
    {
        public static readonly IAsynchronousOperationListenerProvider NullProvider = new NullListenerProvider();
        public static readonly IAsynchronousOperationListener NullListener = new NullOperationListener();

        internal static void Enable(bool enable, bool diagnostics)
        {
        }

        public IAsynchronousOperationListener GetListener(string featureName) => NullListener;

        internal IEnumerable<string> GetTokens() => [];

        internal Task WaitAllDispatcherOperationAndTasksAsync(Workspace? workspace) => Task.CompletedTask;
    }
}