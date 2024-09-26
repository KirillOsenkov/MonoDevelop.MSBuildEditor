// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP
#nullable enable
#endif

using System;
using System.Linq;

using Microsoft.Extensions.Logging;

using MonoDevelop.MSBuild.Language.Expressions;
using MonoDevelop.MSBuild.Language.Syntax;
using MonoDevelop.MSBuild.Language.Typesystem;
using MonoDevelop.MSBuild.Language.References;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.MSBuild.Language
{
	[Flags]
	public enum ReferenceUsage
	{
		Unknown = 0,
		Declaration = 1,
		Read = 1 << 1,
		Write = 1 << 2,
	}

	record struct FindReferencesResult (int Offset, int Length, ReferenceUsage Usage);

	delegate void FindReferencesReporter (FindReferencesResult result);
	abstract class MSBuildReferenceCollector : MSBuildDocumentVisitor
	{
		readonly FindReferencesReporter reportResult;

		protected MSBuildReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string name, FindReferencesReporter reportResult)
			: base (document, textSource, logger)
		{
			if (string.IsNullOrEmpty (name)) {
				throw new ArgumentException ("Name cannot be null or empty", name);
			}
			Name = name;
			this.reportResult = reportResult;
		}

		public string Name { get; }

		protected virtual bool IsMatch (string? name) => string.Equals (name, Name, StringComparison.OrdinalIgnoreCase);
		protected bool IsMatch (INamedXObject obj) => IsMatch (obj.Name.Name);

		protected bool IsPureMatch (ExpressionText t, out int offset, out int length, bool ignoreWhitespace)
		{
			offset = 0;
			length = 0;
			if (!t.IsPure) {
				return false;
			}

			var possibleMatch = t.GetUnescapedValue (ignoreWhitespace, out int trimmedOffset, out int escapedLength);

			if (IsMatch (possibleMatch)) {
				offset = trimmedOffset;
				length = escapedLength;
				return true;
			}

			return false;
		}

		protected void AddResult (int offset, int length, ReferenceUsage usage) => reportResult (new FindReferencesResult (offset, length, usage));
		protected void AddResult (TextSpan span, ReferenceUsage usage) => reportResult (new FindReferencesResult (span.Start, span.Length, usage));

		public static bool CanCreate (MSBuildResolveResult rr)
		{
			if (rr == null || rr.ElementSyntax == null) {
				return false;
			}

			switch (rr.ReferenceKind) {
			case MSBuildReferenceKind.Property:
			case MSBuildReferenceKind.Item:
			case MSBuildReferenceKind.Task:
			case MSBuildReferenceKind.Metadata:
			case MSBuildReferenceKind.Target:
			case MSBuildReferenceKind.ItemFunction:
			case MSBuildReferenceKind.PropertyFunction:
			case MSBuildReferenceKind.StaticPropertyFunction:
			case MSBuildReferenceKind.ClassName:
			case MSBuildReferenceKind.Enum:
			case MSBuildReferenceKind.KnownValue:
				return true;
			}

			return false;
		}

		public static MSBuildReferenceCollector Create (MSBuildDocument document, ITextSource textSource, ILogger logger, MSBuildResolveResult rr, IFunctionTypeProvider functionTypeProvider, FindReferencesReporter reportResult)
		{
			switch (rr.ReferenceKind) {
			case MSBuildReferenceKind.Property:
				return new MSBuildPropertyReferenceCollector (document, textSource, logger, rr.GetPropertyReference (), reportResult);
			case MSBuildReferenceKind.Item:
				return new MSBuildItemReferenceCollector (document, textSource, logger, rr.GetItemReference (), reportResult);
			case MSBuildReferenceKind.Metadata:
				var m = rr.GetMetadataReference ();
				return new MSBuildMetadataReferenceCollector (document, textSource, logger, m.itemName, m.metaName, reportResult);
			case MSBuildReferenceKind.Task:
				return new MSBuildTaskReferenceCollector (document, textSource, logger, rr.GetTaskReference (), reportResult);
			case MSBuildReferenceKind.Target:
				return new MSBuildTargetReferenceCollector (document, textSource, logger, rr.GetTargetReference (), reportResult);
			case MSBuildReferenceKind.ItemFunction:
				return new MSBuildItemFunctionReferenceCollector (document, textSource, logger, rr.GetItemFunctionReference (), reportResult);
			case MSBuildReferenceKind.StaticPropertyFunction:
				(string className, string name) = rr.GetStaticPropertyFunctionReference ();
				return new MSBuildStaticPropertyFunctionReferenceCollector (document, textSource, logger, className, name, reportResult);
			case MSBuildReferenceKind.PropertyFunction:
				(MSBuildValueKind valueKind, string funcName) = rr.GetPropertyFunctionReference ();
				return new MSBuildPropertyFunctionReferenceCollector (document, textSource, logger, valueKind, funcName, functionTypeProvider, reportResult);
			case MSBuildReferenceKind.ClassName:
				return new MSBuildClassReferenceCollector (document, textSource, logger, rr.GetClassNameReference (), reportResult);
			case MSBuildReferenceKind.Enum:
				return new MSBuildEnumReferenceCollector (document, textSource, logger, rr.GetEnumReference (), reportResult);
			case MSBuildReferenceKind.KnownValue:
				return new MSBuildKnownValueReferenceCollector (document, textSource, logger, rr.GetKnownValueReference (), reportResult);
			}

			throw new ArgumentException ($"Cannot create collector for resolve result", nameof (rr));
		}
	}

	class MSBuildItemReferenceCollector : MSBuildReferenceCollector
	{
		public MSBuildItemReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string itemName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, itemName, reportResult) { }

		protected override void VisitResolvedElement (XElement element, MSBuildElementSyntax elementSyntax, ITypedSymbol elementSymbol)
		{
			if ((elementSyntax.SyntaxKind == MSBuildSyntaxKind.Item || elementSyntax.SyntaxKind == MSBuildSyntaxKind.ItemDefinition) && IsMatch (element.Name.Name)) {
				AddResult (element.NameSpan, ReferenceUsage.Write);
			}

			base.VisitResolvedElement (element, elementSyntax, elementSymbol);
		}

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			bool isItemNameWrite =
				// <TaskName><Output ItemName="
				attributeSyntax?.SyntaxKind == MSBuildSyntaxKind.Output_ItemName ||
				// <ProjectReference><OutputItemType>
				(elementSyntax.SyntaxKind == MSBuildSyntaxKind.Metadata && attributeSyntax is null && element.Name.Equals ("OutputItemType", true) && ((element.Parent as XElement)?.Name.Equals ("ProjectReference", true) ?? false)) ||
				// <ProjectReference OutputItemType="
				(attributeSyntax?.SyntaxKind == MSBuildSyntaxKind.Item_Metadata && element.Name.Equals ("ProjectReference", true) && (attribute?.Name.Equals ("OutputItemType", true) ?? false));

			if (isItemNameWrite && node is ExpressionText text && text.GetUnescapedValue (true, out var trimmedOffset, out var trimmedLength) is string unescapedText && IsMatch (unescapedText)) {
				AddResult (trimmedOffset, trimmedLength, ReferenceUsage.Write);
			}

			foreach (var n in node.WithAllDescendants ()) {
				switch (n) {
				case ExpressionItemName ei:
					if (IsMatch (ei.Name)) {
						AddResult (ei.Span, ReferenceUsage.Read);
					}
					break;
				case ExpressionMetadata em:
					if (em.IsQualified && IsMatch (em.ItemName)) {
						AddResult (em.ItemNameSpan, ReferenceUsage.Read);
					}
					break;
				}
			}
		}
	}

	class MSBuildPropertyReferenceCollector : MSBuildReferenceCollector
	{
		public MSBuildPropertyReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string propertyName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, propertyName, reportResult) { }


		protected override void VisitResolvedElement (XElement element, MSBuildElementSyntax elementSyntax, ITypedSymbol elementSymbol)
		{
			if ((elementSyntax.SyntaxKind == MSBuildSyntaxKind.Property) && IsMatch (element.Name.Name)) {
				AddResult (element.NameSpan, ReferenceUsage.Write);
			}
			base.VisitResolvedElement (element, elementSyntax, elementSymbol);
		}

		protected override void VisitResolvedAttribute (XElement element, XAttribute attribute, MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax attributeSyntax, ITypedSymbol elementSymbol, ITypedSymbol attributeSymbol)
		{
			if (attributeSymbol.ValueKind == MSBuildValueKind.PropertyName.AsLiteral () && attribute.HasNonEmptyValue && IsMatch (attribute.Value)) {
				AddResult (attribute.ValueSpan.Value, ReferenceUsage.Write);
			}

			base.VisitResolvedAttribute (element, attribute, elementSyntax, attributeSyntax, elementSymbol, attributeSymbol);
		}

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			foreach (var n in node.WithAllDescendants ()) {
				switch (n) {
				case ExpressionPropertyName ep:
					if (IsMatch (ep.Name)) {
						AddResult (ep.Span, ReferenceUsage.Read);
					}
					break;
				}
			}
		}
	}

	class MSBuildTaskReferenceCollector : MSBuildReferenceCollector
	{
		public MSBuildTaskReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string taskName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, taskName, reportResult) { }

		protected override void VisitResolvedElement (XElement element, MSBuildElementSyntax elementSyntax, ITypedSymbol elementSymbol)
		{
			switch (elementSyntax.SyntaxKind) {
			case MSBuildSyntaxKind.Task:
				if (IsMatch (element.Name.Name)) {
					AddResult (element.NameSpan, ReferenceUsage.Read);
				}
				break;
			case MSBuildSyntaxKind.UsingTask:
				var nameAtt = element.Attributes.Get (MSBuildAttributeName.TaskName, true);
				if (nameAtt is not null && nameAtt.TryGetNonEmptyValue (out var taskName)) {
					var nameIdx = taskName.LastIndexOf ('.') + 1;
					string name = nameIdx > 0 ? nameAtt.Value.Substring (nameIdx) : taskName;
					if (IsMatch (name)) {
						AddResult (nameAtt.ValueOffset.Value + nameIdx, name.Length, ReferenceUsage.Declaration);
					}
				}
				break;
			}
			base.VisitResolvedElement (element, elementSyntax, elementSymbol);
		}
	}

	class MSBuildMetadataReferenceCollector : MSBuildReferenceCollector
	{
		readonly string itemName;

		public MSBuildMetadataReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string itemName, string metadataName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, metadataName, reportResult)
		{
			this.itemName = itemName;
		}

		protected override void VisitResolvedElement (XElement element, MSBuildElementSyntax elementSyntax, ITypedSymbol elementSymbol)
		{
			if (elementSyntax.SyntaxKind == MSBuildSyntaxKind.Metadata && IsMatch (element.Name.Name) && IsItemNameMatch (element.ParentElement?.Name.Name)) {
				AddResult (element.NameSpan, ReferenceUsage.Write);
			}
			base.VisitResolvedElement (element, elementSyntax, elementSymbol);
		}

		protected override void VisitResolvedAttribute (XElement element, XAttribute attribute, MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax attributeSyntax, ITypedSymbol elementSymbol, ITypedSymbol attributeSymbol)
		{
			if (attributeSyntax.AbstractKind == MSBuildSyntaxKind.Metadata && IsMatch (attribute.Name.Name) && IsItemNameMatch (element.Name.Name)) {
				AddResult (attribute.NameSpan, ReferenceUsage.Write);
			}

			base.VisitResolvedAttribute (element, attribute, elementSyntax, attributeSyntax, elementSymbol, attributeSymbol);
		}

		// null doc means offsets will not be correct
		internal static ExpressionNode? GetIncludeExpression (XElement itemElement)
		{
			var include = itemElement.Attributes
				.FirstOrDefault (e => e.Name.Equals (MSBuildAttributeName.Include, true));

			if (include == null || string.IsNullOrWhiteSpace (include.Value)) {
				return null;
			}
			return ExpressionParser.Parse (
				include.Value,
				ExpressionOptions.ItemsMetadataAndLists,
				include.Span.Start);
		}

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			var valueSymbol = attributeSymbol ?? elementSymbol;

			//these are things like <Foo Include="@(Bar)" RemoveMetadata="SomeBarMetadata" />
			if (valueSymbol.IsKindOrListOfKind (MSBuildValueKind.MetadataName)) {
				var expr = GetIncludeExpression (element);
				if (expr != null && expr
					.WithAllDescendants ()
					.OfType<ExpressionItemName> ()
					.Any (n => IsItemNameMatch (n.ItemName))
				) {
					switch (node) {
					case ListExpression list:
						foreach (var c in list.Nodes) {
							if (c is ExpressionText l) {
								CheckMatch (l);
								break;
							}
						}
						break;
					case ExpressionText lit:
						CheckMatch (lit);
						break;
					}
				}

				void CheckMatch (ExpressionText t)
				{
					if (IsPureMatch (t, out int offset, out int length, ignoreWhitespace: true)) {
						AddResult (offset, length, ReferenceUsage.Read);
					}
				}
				return;
			}

			foreach (var n in node.WithAllDescendants ()) {
				switch (n) {
				case ExpressionMetadata em:
					var iname = em.GetItemName ();
					if (iname != null && IsItemNameMatch (iname) && IsMatch (em.MetadataName)) {
						AddResult (em.MetadataNameSpan, ReferenceUsage.Read);
					}
					break;
				}
			}
		}

		bool IsItemNameMatch (string? name) => string.Equals (name, itemName, StringComparison.OrdinalIgnoreCase);
	}

	class MSBuildTargetReferenceCollector : MSBuildReferenceCollector
	{
		public MSBuildTargetReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string targetName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, targetName, reportResult) { }

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			var valueSymbol = attributeSymbol ?? elementSymbol;

			if (!valueSymbol.IsKindOrListOfKind (MSBuildValueKind.TargetName)) {
				return;
			}
			bool isDeclaration = attributeSyntax?.SyntaxKind == MSBuildSyntaxKind.Target_Name;

			switch (node) {
			case ListExpression list:
				foreach (var c in list.Nodes) {
					if (c is ExpressionText l) {
						CheckMatch (l, isDeclaration);
					}
				}
				break;
			case ExpressionText lit:
				CheckMatch (lit, isDeclaration);
				break;
			}
		}

		void CheckMatch (ExpressionText node, bool isDeclaration)
		{
			if (IsPureMatch (node, out int offset, out int length, ignoreWhitespace: true)) {
				AddResult (offset, length, isDeclaration ? ReferenceUsage.Declaration : ReferenceUsage.Read);
			}
		}
	}

	class MSBuildTargetDefinitionCollector : MSBuildReferenceCollector
	{
		public MSBuildTargetDefinitionCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string targetName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, targetName, reportResult) { }

		protected override void VisitResolvedElement (XElement element, MSBuildElementSyntax elementSyntax, ITypedSymbol elementSymbol)
		{
			if (elementSyntax.SyntaxKind == MSBuildSyntaxKind.Target) {
				var nameAtt = element.Attributes.Get (MSBuildAttributeName.Name, true);
				if (nameAtt != null && nameAtt.HasValue && IsMatch (nameAtt.Value)) {
					AddResult (nameAtt.ValueSpan.Value, ReferenceUsage.Declaration);
				}
			}
			base.VisitResolvedElement (element, elementSyntax, elementSymbol);
		}
	}

	class MSBuildStaticPropertyFunctionReferenceCollector : MSBuildReferenceCollector
	{
		readonly string className;

		public MSBuildStaticPropertyFunctionReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string className, string functionName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, MSBuildPropertyFunctionReferenceCollector.StripGetPrefix (functionName), reportResult)
		{
			this.className = className;
		}

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			foreach (var n in node.WithAllDescendants ()) {
				switch (n) {
				case ExpressionFunctionName func:
					if (func.Parent is ExpressionPropertyFunctionInvocation inv) {
						string baseName = MSBuildPropertyFunctionReferenceCollector.StripGetPrefix (func.Name);
						if (IsMatch (baseName) && inv.Target is ExpressionClassReference cn && cn.Name == className) {
							AddResult (func.Span, ReferenceUsage.Read);
						}
					}
					break;
				}
			}
		}
	}

	class MSBuildPropertyFunctionReferenceCollector : MSBuildReferenceCollector
	{
		//ensure that props and function-style props are equivalent e.g get_Now() and Now
		//this is kinda hacky, we should really check whether the get variant is a
		//method and the non-get variant is a property
		internal static string StripGetPrefix (string name)
		{
			if (name.StartsWith ("get_", StringComparison.Ordinal) && name.Length > 4) {
				return name.Substring (4);
			}
			return name;
		}

		readonly MSBuildValueKind valueKind;
		readonly IFunctionTypeProvider functionTypeProvider;

		public MSBuildPropertyFunctionReferenceCollector (
			MSBuildDocument document, ITextSource textSource, ILogger logger,
			MSBuildValueKind valueKind, string functionName, IFunctionTypeProvider functionTypeProvider,
			FindReferencesReporter reportResult)
			: base (document, textSource, logger, StripGetPrefix(functionName), reportResult)
		{
			if (valueKind == MSBuildValueKind.Unknown) {
				valueKind = MSBuildValueKind.String;
			}
			this.valueKind = valueKind;
			this.functionTypeProvider = functionTypeProvider;
		}

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			foreach (var n in node.WithAllDescendants ()) {
				switch (n) {
				case ExpressionFunctionName func:
					if (func.Parent is ExpressionPropertyFunctionInvocation inv) {
						string baseName = StripGetPrefix (func.Name);
						if (IsMatch (baseName)) {
							//TODO: should we be fuzzy here and accept "unknown"?
							var resolvedKind = functionTypeProvider.ResolveType (inv);
							if (resolvedKind == MSBuildValueKind.Unknown) {
								resolvedKind = MSBuildValueKind.String;
							}
							if (resolvedKind == valueKind) {
								AddResult (func.Span, ReferenceUsage.Read);
							}
						}
					}
					break;
				}
			}
		}
	}

	class MSBuildItemFunctionReferenceCollector : MSBuildReferenceCollector
	{
		public MSBuildItemFunctionReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string functionName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, functionName, reportResult) { }

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			foreach (var n in node.WithAllDescendants ()) {
				switch (n) {
				case ExpressionFunctionName func:
					if (func.Parent is ExpressionItemFunctionInvocation && IsMatch (func.Name)) {
						AddResult (func.Span, ReferenceUsage.Read);
					}
					break;
				}
			}
		}
	}

	class MSBuildClassReferenceCollector : MSBuildReferenceCollector
	{
		public MSBuildClassReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string className, FindReferencesReporter reportResult)
			: base (document, textSource, logger, className, reportResult) { }

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			foreach (var n in node.WithAllDescendants ()) {
				switch (n) {
				case ExpressionClassReference classRef:
					if (IsMatch (classRef.Name) && classRef.Parent is ExpressionPropertyFunctionInvocation) {
						AddResult (classRef.Span, ReferenceUsage.Read);
					}
					break;
				}
			}
		}
	}

	class MSBuildEnumReferenceCollector : MSBuildReferenceCollector
	{
		public MSBuildEnumReferenceCollector (MSBuildDocument document, ITextSource textSource, ILogger logger, string enumName, FindReferencesReporter reportResult)
			: base (document, textSource, logger, enumName, reportResult) { }

		protected override void VisitValue (
			XElement element, XAttribute? attribute,
			MSBuildElementSyntax elementSyntax, MSBuildAttributeSyntax? attributeSyntax,
			ITypedSymbol elementSymbol, ITypedSymbol? attributeSymbol,
			string expressionText, ExpressionNode node)
		{
			foreach (var n in node.WithAllDescendants ()) {
				switch (n) {
				case ExpressionClassReference classRef:
					if (IsMatch (classRef.Name) && classRef.Parent is ExpressionArgumentList) {
						AddResult (classRef.Span, ReferenceUsage.Read);
					}
					break;
				}
			}
		}
	}
}