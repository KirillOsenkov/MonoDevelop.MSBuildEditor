// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using MonoDevelop.MSBuild.Language;
using MonoDevelop.MSBuild.Language.Typesystem;
using MonoDevelop.MSBuild.Schema;

using Roslyn.Utilities;

namespace MonoDevelop.MSBuild.Editor.CodeActions;

static class MSBuildSpellChecker
{
	const int MAX_RESULTS = 5;

	public static IEnumerable<ItemInfo> FindSimilarItems (MSBuildDocument document, string itemName, bool includePrivateSymbols)
	{
		var items = document.GetSchemas ().GetItems (includePrivateSymbols);
		return FindSimilar (items, itemName);
	}

	public static IEnumerable<PropertyInfo> FindSimilarProperties (MSBuildDocument document, string propertyName, bool includeReadonly, bool includePrivateSymbols)
	{
		var items = document.GetSchemas ().GetProperties (includeReadonly, includePrivateSymbols);
		return FindSimilar (items, propertyName);
	}

	public static IEnumerable<MetadataInfo> FindSimilarMetadata (MSBuildDocument document, string itemName, string metadataName, bool includeReserved)
	{
		var items = document.GetSchemas ().GetMetadata (itemName, includeReserved);
		return FindSimilar (items, metadataName);
	}

	public static IEnumerable<ISymbol> FindSimilarValues (MSBuildDocument document, ITypedSymbol expectedType, string valueName)
	{
		var kind = expectedType.ValueKind.WithoutModifiers ();

		IReadOnlyList<ISymbol>? knownValues = null;
		bool isCaseSensitive = false;

		if (kind != MSBuildValueKind.CustomType) {
			knownValues = kind.GetSimpleValues ();
		} else if (expectedType.CustomType is CustomTypeInfo customType) {
			knownValues = customType.Values;
			isCaseSensitive = customType.CaseSensitive;
		}

		if (knownValues is null || knownValues.Count == 0) {
			return [];
		}

		return FindSimilar (knownValues, valueName, isCaseSensitive: isCaseSensitive);
	}

	static IEnumerable<TSymbol> FindSimilar<TSymbol> (IEnumerable<TSymbol> candidates, string actualName, bool substringsAreSimilar = true, bool isCaseSensitive = false, int resultCountLimit = MAX_RESULTS)
		where TSymbol : ISymbol
	{
		using var checker = new WordSimilarityChecker (actualName, substringsAreSimilar);

		var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

		var results = new Dictionary<string, (TSymbol symbol, double weight)> (comparer);

		foreach (var candidate in candidates) {
			// ignore exact matches, they will have come from the inferred schema adding the incorrect value we are fixing
			if (comparer.Compare (candidate.Name, actualName) == 0) {
				continue;
			}
			if (checker.AreSimilar (candidate.Name, out double similarityWeight)) {
				results.Add (candidate.Name, (candidate, similarityWeight));
			}
		}

		return results.Values.OrderBy (v => v.weight).Take (resultCountLimit).Select (v => v.symbol);
	}
}
