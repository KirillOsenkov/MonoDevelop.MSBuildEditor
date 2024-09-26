// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;

using MonoDevelop.MSBuild.Editor.Completion;
using MonoDevelop.Xml.Editor.Parsing;
using MonoDevelop.Xml.Logging;

namespace MonoDevelop.MSBuild.Editor
{
	class MSBuildValidationTagger : ITagger<MSBuildDiagnosticTag>, IDisposable
	{
		readonly MSBuildBackgroundParser parser;
		readonly JoinableTaskContext joinableTaskContext;
		readonly ILogger logger;
		ParseCompletedEventArgs<MSBuildParseResult> lastArgs;

		public MSBuildValidationTagger (ITextBuffer buffer, JoinableTaskContext joinableTaskContext, MSBuildParserProvider parserProvider, ILogger logger)
		{
			parser = parserProvider.GetParser (buffer);
			parser.ParseCompleted += ParseCompleted;
			this.joinableTaskContext = joinableTaskContext;
			this.logger = logger;
		}

		void ParseCompleted (object sender, ParseCompletedEventArgs<MSBuildParseResult> args)
		{
			lastArgs = args;

			joinableTaskContext.Factory.Run (async delegate {
				await joinableTaskContext.Factory.SwitchToMainThreadAsync ();
				//FIXME: figure out which spans changed, if any, and only invalidate those
				TagsChanged?.Invoke (this, new SnapshotSpanEventArgs (new SnapshotSpan (args.Snapshot, 0, args.Snapshot.Length)));
			});
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public void Dispose ()
		{
			parser.ParseCompleted -= ParseCompleted;
		}

		public IEnumerable<ITagSpan<MSBuildDiagnosticTag>> GetTags (NormalizedSnapshotSpanCollection spans)
			=> logger.InvokeAndLogExceptions (() => GetTagsInternal (spans));

		public IEnumerable<ITagSpan<MSBuildDiagnosticTag>> GetTagsInternal (NormalizedSnapshotSpanCollection spans)
		{
			//this may be assigned from another thread so capture a consistent value
			var args = lastArgs;

			if (args == null || spans.Count == 0)
				yield break;

			var parse = args.ParseResult;
			var snapshot = args.Snapshot;

			//FIXME is this correct handling of errors that span multiple spans?
			foreach (var taggingSpan in spans) {
				foreach (var diag in parse.MSBuildDocument.Diagnostics) {
					var diagSpan = new SnapshotSpan (snapshot, diag.Span.Start, diag.Span.Length);

					//if the parse was from an older snapshot, map the positions into the current snapshot
					if (snapshot != taggingSpan.Snapshot) {
						var trackingSpan = snapshot.CreateTrackingSpan (diagSpan, SpanTrackingMode.EdgeInclusive);
						diagSpan = trackingSpan.GetSpan (taggingSpan.Snapshot);
					}

					if (diagSpan.IntersectsWith (taggingSpan)) {
						yield return new TagSpan<MSBuildDiagnosticTag> (diagSpan, new MSBuildDiagnosticTag (diag));
					}
				}
			}
		}
	}
}
