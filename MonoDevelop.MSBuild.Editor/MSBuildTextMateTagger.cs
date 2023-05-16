// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.MSBuild.Editor
{
	[Export (typeof (ITaggerProvider))]
	[TagType (typeof (IClassificationTag))]
	[TagType (typeof (IStructureTag))]
	[ContentType (MSBuildContentType.Name)]
	sealed class MSBuildTextMateTagger : ITaggerProvider
	{
		[ImportingConstructor]
		public MSBuildTextMateTagger (ICommonEditorAssetServiceFactory assetServiceFactory)
		{
			AssetServiceFactory = assetServiceFactory;
		}

		public ICommonEditorAssetServiceFactory AssetServiceFactory { get; }

		public ITagger<T> CreateTagger<T> (ITextBuffer buffer) where T : ITag =>
			AssetServiceFactory.GetOrCreate (buffer)
			.FindAsset<ITaggerProvider> (
				(metadata) => metadata.TagTypes.Any (tagType => typeof (T).IsAssignableFrom (tagType))
			)
			?.CreateTagger<T> (buffer);
	}
}
