using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Text.Classification;
using MonoDevelop.MSBuild.Editor.Host;

namespace MonoDevelop.MSBuild.Editor.VisualStudio.FindReferences
{
	static class MSBuildOptions
	{
		public static int DefinitionGroupingPriority { get; set; }
	}

	static class FindReferencesExtensions
	{
		public static HashSet<T> ToSet<T> (this IEnumerable<T> items) => new HashSet<T> (items);
	}
}