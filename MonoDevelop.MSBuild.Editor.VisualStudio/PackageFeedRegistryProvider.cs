using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.MSBuild.Editor.Completion;

using NuGet.VisualStudio;

using ProjectFileTools.NuGetSearch.Contracts;

namespace MonoDevelop.MSBuild.Editor.VisualStudio
{
	[Export (typeof (IPackageFeedRegistryProvider))]
	[Name ("Visual Studio Package Feed Registry Provider")]
	partial class PackageFeedRegistryProvider : IPackageFeedRegistryProvider
	{
		readonly IVsPackageSourceProvider provider;
		readonly ILogger logger;

		[ImportingConstructor]
		public PackageFeedRegistryProvider (IVsPackageSourceProvider provider, MSBuildEnvironmentLogger logger)
		{
			this.provider = provider;
			this.logger = logger.Logger;
		}

		public IReadOnlyList<string> ConfiguredFeeds {
			get {
				var sources = new List<string> ();

				// IVsPackageSourceProvider seems to be broken in 17.3 previews so add a fallback
				try {
					IEnumerable<KeyValuePair<string, string>> enabledSources = provider.GetSources (true, false);

					foreach (KeyValuePair<string, string> curEnabledSource in enabledSources) {
						string source = curEnabledSource.Value;
						sources.Add (source);
					}
				} catch (Exception ex) {
					LogFailedToGetConfiguredSources (logger, ex);
					sources.Add ("https://api.nuget.org/v3/index.json");
				}

				if (!sources.Any (x => x.IndexOf ("\\.nuget", StringComparison.OrdinalIgnoreCase) > -1)) {
					sources.Add (Environment.ExpandEnvironmentVariables ("%USERPROFILE%\\.nuget\\packages"));
				}

				return sources;
			}
		}

		[LoggerMessage (EventId = 0, Level = LogLevel.Error, Message = "Failed to get configured NuGet sources")]
		static partial void LogFailedToGetConfiguredSources (ILogger logger, Exception ex);
	}
}
