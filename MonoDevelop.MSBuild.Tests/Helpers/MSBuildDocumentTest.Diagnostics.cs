// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;

using MonoDevelop.MSBuild.Analysis;
using MonoDevelop.MSBuild.Language;
using MonoDevelop.MSBuild.Schema;

using MonoDevelop.Xml.Tests;

using NUnit.Framework;

namespace MonoDevelop.MSBuild.Tests;

partial class MSBuildDocumentTest
{
	public static IList<MSBuildDiagnostic> GetDiagnostics (
		string source,
		out MSBuildRootDocument parsedDocument,
		ICollection<MSBuildAnalyzer>? analyzers = null,
		bool includeCoreDiagnostics = false,
		ILogger? logger = null,
		MSBuildSchema? schema = null,
		MSBuildRootDocument? previousDocument = null,
		CancellationToken cancellationToken = default
		)
	{
		// internal errors should cause test failure
		logger ??= TestLoggerFactory.CreateTestMethodLogger ().RethrowExceptions ();

		parsedDocument = GetParsedDocument (source, logger, schema, previousDocument, cancellationToken);

		var analyzerDriver = new MSBuildAnalyzerDriver (logger);

		if (analyzers != null && analyzers.Count > 0) {
			analyzerDriver.AddAnalyzers (analyzers);
		} else if (!includeCoreDiagnostics) {
			throw new ArgumentException ("Analyzers can only be null or empty if core diagnostics are included", nameof (analyzers));
		}

		var actualDiagnostics = analyzerDriver.Analyze (parsedDocument, includeCoreDiagnostics, cancellationToken);

		return actualDiagnostics ?? [];
	}

	public static void VerifyDiagnostics (string source, MSBuildAnalyzer analyzer, params MSBuildDiagnostic[] expectedDiagnostics)
		=> VerifyDiagnostics (source, out _, [analyzer], expectedDiagnostics: expectedDiagnostics);

	public static void VerifyDiagnostics (string source, ICollection<MSBuildAnalyzer> analyzers, bool includeCoreDiagnostics, params MSBuildDiagnostic[] expectedDiagnostics)
		=> VerifyDiagnostics (source, out _, analyzers, includeCoreDiagnostics, expectedDiagnostics: expectedDiagnostics);

	public static void VerifyDiagnostics (
		string source,
		ICollection<MSBuildAnalyzer>? analyzers = null,
		bool includeCoreDiagnostics = false,
		bool ignoreUnexpectedDiagnostics = false,
		MSBuildSchema? schema = null,
		MSBuildDiagnostic[]? expectedDiagnostics = null,
		ILogger? logger = null,
		MSBuildRootDocument? previousDocument = null,
		bool includeNoTargetsWarning = false
		)
		=> VerifyDiagnostics (source, out _, analyzers, includeCoreDiagnostics, ignoreUnexpectedDiagnostics, schema, expectedDiagnostics, logger, previousDocument, includeNoTargetsWarning);

	public static void VerifyDiagnostics (
		string source,
		out MSBuildRootDocument parsedDocument,
		ICollection<MSBuildAnalyzer>? analyzers = null,
		bool includeCoreDiagnostics = false,
		bool ignoreUnexpectedDiagnostics = false,
		MSBuildSchema? schema = null,
		MSBuildDiagnostic[]? expectedDiagnostics = null,
		ILogger? logger = null,
		MSBuildRootDocument? previousDocument = null,
		bool includeNoTargetsWarning = false
		)
	{
		var actualDiagnostics = GetDiagnostics (source, out parsedDocument, analyzers, includeCoreDiagnostics, logger, schema, previousDocument);

		var missingDiags = new List<MSBuildDiagnostic> ();

		foreach (var expectedDiag in expectedDiagnostics ?? []) {
			bool found = false;
			for (int i = 0; i < actualDiagnostics.Count; i++) {
				var actualDiag = actualDiagnostics[i];
				if (actualDiag.Descriptor == expectedDiag.Descriptor && actualDiag.Span.Equals (expectedDiag.Span)) {
					Assert.That (actualDiag.Properties ?? Enumerable.Empty<KeyValuePair<string, object>> (),
						Is.EquivalentTo (expectedDiag.Properties ?? Enumerable.Empty<KeyValuePair<string, object>> ())
						.UsingDictionaryComparer<string, object> ());
					// checks messageArgs
					Assert.That (actualDiag.GetFormattedMessage (), Is.EquivalentTo (expectedDiag.GetFormattedMessage ()));
					found = true;
					actualDiagnostics.RemoveAt (i);
					break;
				}
			}
			if (!found) {
				missingDiags.Add (expectedDiag);
			}
		}

		if (!includeNoTargetsWarning) {
			for (int i = 0; i < actualDiagnostics.Count; i++) {
				if (actualDiagnostics[i].Descriptor.Id == CoreDiagnostics.NoTargets_Id) {
					actualDiagnostics.RemoveAt (i);
					i--;
				}
			}
		}

		if (!ignoreUnexpectedDiagnostics && actualDiagnostics.Count > 0) {
			Assert.Fail ($"Found unexpected diagnostics: {string.Join (", ", actualDiagnostics.Select (diag => $"\n\t{diag.Descriptor.Id}@{diag.Span.Start}-{diag.Span.End}"))}");
		}

		if (missingDiags.Count > 0) {
			Assert.Fail ($"Did not find expected diagnostics: {string.Join (", ", missingDiags.Select (diag => $"{diag.Descriptor.Id}@{diag.Span.Start}-{diag.Span.End}"))}");
		}
	}
}
