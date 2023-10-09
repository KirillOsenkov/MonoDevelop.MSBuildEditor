// Copyright (c) 2016 Xamarin Inc.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using MonoDevelop.MSBuild.SdkResolution;

namespace MonoDevelop.MSBuild.Language
{
	[DebuggerDisplay ("{OriginalImport} (Resolved = {IsResolved})")]
	class Import
	{
		public string Filename { get; private set; }
		public string OriginalImport { get; private set; }
		public string Sdk { get; }
		public SdkInfo ResolvedSdk { get; }
		public DateTime TimeStampUtc { get; }
		public MSBuildDocument Document { get; set; }
		public bool IsResolved { get { return Document != null; } }

		/// <summary>
		///  Whether the import was added implicitly rather than being an actual import in the file.
		/// </summary>
		public bool IsImplicitImport { get; }

		public Import (string importExpr, string sdk, string resolvedFilename, SdkInfo resolvedSdk, DateTime timeStampUtc, bool isImplicitImport)
		{
			OriginalImport = importExpr;
			Filename = resolvedFilename;
			Sdk = sdk;
			TimeStampUtc = timeStampUtc;
			IsImplicitImport = isImplicitImport;
			ResolvedSdk = resolvedSdk;
		}
	}
}