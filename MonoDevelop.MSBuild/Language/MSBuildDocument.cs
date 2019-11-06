// Copyright (c) 2014 Xamarin Inc.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using MonoDevelop.MSBuild.Analysis;
using MonoDevelop.MSBuild.Dom;
using MonoDevelop.MSBuild.Language.Expressions;
using MonoDevelop.MSBuild.Schema;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.MSBuild.Language
{
	class MSBuildDocument
	{
		static readonly XName xnProject = "Project";

		//NOTE: this is keyed on the filepath of resolved imports and original expression of unresolved imports
		//the reason for this is that a single expression can resolve to multiple imports
		public List<Import> Imports { get; } = new List<Import> ();

		/// <summary>
		/// Things that derive data from the imports can use this to avoid unnecessarily rebuilding it
		/// </summary>
		public int ImportsHash { get; private set; }

		public AnnotationTable<XObject> Annotations { get; } = new AnnotationTable<XObject> ();
		public List<MSBuildDiagnostic> Diagnostics { get; }
		public bool IsToplevel { get; }

		public MSBuildProjectElement ProjectElement { get; private set; }

		public MSBuildDocument (string filename, bool isToplevel)
		{
			Filename = filename;
			IsToplevel = isToplevel;

			if (isToplevel) {
				Diagnostics = new List<MSBuildDiagnostic> ();
			}

			InferredSchema = new MSBuildInferredSchema (isToplevel);
		}

		public string Filename { get; }
		public MSBuildSchema Schema { get; internal set; }
		public MSBuildInferredSchema InferredSchema { get; private set; }

		public void Build (
			XDocument doc, ITextSource textSource,
			MSBuildParserContext context)
		{
			var project = doc.Nodes.OfType<XElement> ().FirstOrDefault (x => x.Name == xnProject);
			if (project == null) {
				//TODO: error
				return;
			}

			var sdks = ResolveSdks (context, project).ToList ();

			GetPropertiesToTrack (context.PropertyCollector, project);

			var importResolver = context.CreateImportResolver (Filename);

			AddSdkProps (sdks, context.PropertyCollector, importResolver);

			var projectElement = new MSBuildProjectElement (project);
			if (IsToplevel) {
				ProjectElement = projectElement;
			}

			var resolver = new MSBuildSchemaBuilder (IsToplevel, context, importResolver);
			resolver.Run (doc, textSource, this);

			AddSdkTargets (sdks, context.PropertyCollector, importResolver);
		}

		static void GetPropertiesToTrack (PropertyValueCollector propertyVals, XElement project)
		{
			foreach (var el in project.Elements) {
				if (el.NameEquals ("Import", true)) {
					var impAtt = el.Attributes.Get (xnProject, true)?.Value;
					if (impAtt != null) {
						MarkProperties (impAtt);
					}
				} else if (el.NameEquals ("UsingTask", true)) {
					var afAtt = el.Attributes.Get ("AssemblyFile", true)?.Value;
					if (afAtt != null) {
						MarkProperties (afAtt);
					}
				}
			}

			void MarkProperties (string val)
			{
				var expr = ExpressionParser.Parse (val, ExpressionOptions.None);
				foreach (var prop in expr.WithAllDescendants ().OfType<ExpressionProperty> ()) {
					propertyVals.Mark (prop.Name);
				}
			}
		}

		IEnumerable<(string id, TextSpan span)> SplitSdkValue (int offset, string value)
		{
			int start = 0, end;
			while ((end = value.IndexOf (';', start)) > -1) {
				yield return MakeResult ();
				start = end + 1;
			}
			end = value.Length;
			yield return MakeResult ();

			TextSpan CreateSpan (int s, int e) => new TextSpan (offset + s, offset + e);

			(string id, TextSpan loc) MakeResult ()
			{
				int trimStart = start, trimEnd = end;
				while (trimStart < trimEnd) {
					if (!char.IsWhiteSpace (value[trimStart]))
						break;
					trimStart++;
				}
				while (trimEnd > trimStart) {
					if (!char.IsWhiteSpace (value[trimEnd - 1]))
						break;
					trimEnd--;
				}
				if (trimEnd > trimStart) {
					return (value.Substring (trimStart, trimEnd - trimStart), CreateSpan (trimStart, trimEnd));
				}
				return (null, CreateSpan (start, end));
			}
		}

		IEnumerable<(string id, string path, TextSpan span)> ResolveSdks (MSBuildParserContext context, XElement project)
		{
			var sdksAtt = project.Attributes.Get ("Sdk", true);
			if (sdksAtt == null) {
				yield break;
			}

			string sdks = sdksAtt?.Value;
			if (string.IsNullOrEmpty (sdks)) {
				yield break;
			}

			int offset = IsToplevel ? sdksAtt.ValueOffset : sdksAtt.Span.Start;

			foreach (var sdk in SplitSdkValue (offset, sdksAtt.Value)) {
				if (sdk.id == null) {
					if (IsToplevel) {
						Diagnostics.Add (CoreDiagnostics.EmptySdkAttribute, sdk.span);
					}
				}
				else {
					var sdkPath = context.GetSdkPath (this, sdk.id, sdk.span);
					if (sdkPath != null) {
						yield return (sdk.id, sdkPath, sdk.span);
					}
					if (IsToplevel) {
						Annotations.Add (sdksAtt, new NavigationAnnotation (sdkPath, sdk.span) { IsSdk = true });
					}
				}
			}
		}

		public virtual void AddImport (Import import)
		{
			ImportsHash ^= import.Document?.GetHashCode () ?? 0;
			Imports.Add (import);
		}

		void AddSdkProps (IEnumerable<(string id, string path, TextSpan loc)> sdkPaths, PropertyValueCollector propVals, MSBuildImportResolver importResolver)
		{
			foreach (var sdk in sdkPaths) {
				var propsPath = $"{sdk.path}\\Sdk.props";
				var sdkProps = importResolver.Resolve (propsPath, sdk.id).FirstOrDefault ();
				if (sdkProps != null) {
					AddImport (sdkProps);
				}
			}
		}

		void AddSdkTargets (IEnumerable<(string id, string path, TextSpan loc)> sdkPaths, PropertyValueCollector propVals, MSBuildImportResolver importResolver)
		{
			foreach (var sdk in sdkPaths) {
				var targetsPath = $"{sdk.path}\\Sdk.targets";
				var sdkTargets = importResolver.Resolve (targetsPath, sdk.id).FirstOrDefault ();
				if (sdkTargets != null) {
					AddImport (sdkTargets);
				}
			}
		}

		public IEnumerable<Import> GetDescendentImports ()
		{
			foreach (var i in Imports) {
				yield return i;
				if (i.Document != null) {
					foreach (var d in i.Document.GetDescendentImports ()) {
						yield return d;
					}
				}
			}
		}

		public IEnumerable<MSBuildDocument> GetDescendentDocuments ()
		{
			foreach (var i in GetDescendentImports ()) {
				if (i.Document != null) {
					yield return i.Document;
				}
			}
		}

		public IEnumerable<MSBuildDocument> GetSelfAndDescendents ()
		{
			yield return this;
			foreach (var i in GetDescendentImports ()) {
				if (i.Document != null) {
					yield return i.Document;
				}
			}
		}

		//actual schemas, if they exist, take precedence over inferred schemas
		public IEnumerable<IMSBuildSchema> GetSchemas (bool skipThisDocumentInferredSchema = false)
		{
			if (Schema != null) {
				yield return Schema;
			}
			foreach (var d in GetDescendentDocuments ()) {
				if (d?.Schema is IMSBuildSchema s) {
					yield return s;
				}
			}
			if (!skipThisDocumentInferredSchema && InferredSchema is MSBuildInferredSchema inferred) {
				yield return inferred;
			}
			foreach (var d in GetDescendentDocuments ()) {
				if (d.InferredSchema is IMSBuildSchema descendent) {
					yield return descendent;
				}
			}
		}

		/// <summary>
		/// Gets the files in which the given info has been seen, excluding the current one.
		/// </summary>
		public IEnumerable<string> GetFilesSeenIn (BaseInfo info)
		{
			var files = new HashSet<string> ();
			foreach (var doc in GetDescendentDocuments ()) {
				if (doc.Filename != null && doc.InferredSchema.ContainsInfo (info)) {
					files.Add (doc.Filename);
				}
			}
			return files;
		}
	}
}