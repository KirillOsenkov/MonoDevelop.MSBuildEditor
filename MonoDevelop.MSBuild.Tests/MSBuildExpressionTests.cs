// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.MSBuild.Language.Expressions;
using NUnit.Framework;

namespace MonoDevelop.MSBuild.Tests
{
	[TestFixture]
	class MSBuildExpressionTests
	{
		[TestCase ("$(Foo)", "Foo")]
		[TestCase ("$(   Foo   )", "Foo")]
		[TestCase ("$(_Foo)", "_Foo")]
		[TestCase ("$(_Foo12_3)", "_Foo12_3")]
		public void TestSimpleProperty (string expression, string propName)
		{
			var expr = ExpressionParser.Parse (expression);
			var prop = AssertCast<ExpressionProperty> (expr);
			Assert.IsTrue (prop.IsSimpleProperty);
			Assert.AreEqual (propName, prop.Name);
		}

		// properties
		[TestCase ("$(", ExpressionErrorKind.ExpectingPropertyName)]
		[TestCase ("$(    ", ExpressionErrorKind.ExpectingPropertyName)]
		[TestCase ("$(.", ExpressionErrorKind.ExpectingPropertyName)]
		[TestCase ("$(   .", ExpressionErrorKind.ExpectingPropertyName)]
		[TestCase ("$(a", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		[TestCase ("$(a-", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		// metadata
		[TestCase ("%(", ExpressionErrorKind.ExpectingMetadataOrItemName)]
		[TestCase ("%(.", ExpressionErrorKind.ExpectingMetadataOrItemName)]
		[TestCase ("%(a", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		[TestCase ("%(a.b", ExpressionErrorKind.ExpectingRightParen)]
		[TestCase ("%(a.b.", ExpressionErrorKind.ExpectingRightParen)]
		[TestCase ("%(a.", ExpressionErrorKind.ExpectingMetadataName)]
		[TestCase ("%(a.)", ExpressionErrorKind.ExpectingMetadataName)]
		// nonstandard qualified metadata syntax used by F# targets
		[TestCase ("%(a-", ExpressionErrorKind.ExpectingRightAngleBracket)]
		[TestCase ("%(a->", ExpressionErrorKind.ExpectingMetadataName)]
		[TestCase ("%(a->b", ExpressionErrorKind.ExpectingRightParen)]
		// items
		[TestCase ("@(.", ExpressionErrorKind.ExpectingItemName)]
		[TestCase ("@(", ExpressionErrorKind.ExpectingItemName)]
		[TestCase ("@(a", ExpressionErrorKind.ExpectingRightParenOrDash)]
		[TestCase ("@(a.", ExpressionErrorKind.ExpectingRightParenOrDash)]
		[TestCase ("@(a-", ExpressionErrorKind.ExpectingRightAngleBracket)]
		[TestCase ("@(a   -", ExpressionErrorKind.ExpectingRightAngleBracket)]
		[TestCase ("@(a-.", ExpressionErrorKind.ExpectingRightAngleBracket)]
		[TestCase ("@(a->", ExpressionErrorKind.ExpectingMethodOrTransform)]
		[TestCase ("@(a->  ", ExpressionErrorKind.ExpectingMethodOrTransform)]
		[TestCase ("@(a->.", ExpressionErrorKind.ExpectingMethodOrTransform)]
		[TestCase ("@(a->'f", ExpressionErrorKind.IncompleteString)]
		[TestCase ("@(a->''", ExpressionErrorKind.ExpectingRightParen)]
		[TestCase ("@(a->''d", ExpressionErrorKind.ExpectingRightParen)]
		[TestCase ("@(a->'' ", ExpressionErrorKind.ExpectingRightParen)]
		[TestCase ("@(a->a", ExpressionErrorKind.ExpectingLeftParen)]
		[TestCase ("@(a->  a", ExpressionErrorKind.ExpectingLeftParen)]
		// instance property functions
		[TestCase ("$(a.", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$(a..", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$(a.b", ExpressionErrorKind.IncompleteProperty)]
		[TestCase ("$(a.b.", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$(a.b(", ExpressionErrorKind.ExpectingRightParenOrValue)]
		[TestCase ("$(a.b(.", ExpressionErrorKind.IncompleteValue)]
		[TestCase ("$(a.b(/", ExpressionErrorKind.ExpectingRightParenOrValue)]
		[TestCase ("$(a.b()", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		[TestCase ("$(a.b().", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$(a.b()  .  ", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$(a.b(1,", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$(a.b(true,", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$(a.b(true,   ", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$(a.b(true,.", ExpressionErrorKind.IncompleteValue)]
		[TestCase ("$(a.b(true,/", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$(a.b(true,   .", ExpressionErrorKind.IncompleteValue)]
		[TestCase ("$(a.b(true,   /", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$(a.b(true,true", ExpressionErrorKind.ExpectingRightParenOrComma)]
		[TestCase ("$(a.b(true,true)", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		[TestCase ("$(a.b(true,true)   ", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		[TestCase ("$(a.b(true,true)   _", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		// static property functions
		[TestCase ("$([a", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$([ a", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$([a ", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$( [a ", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$([a]", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$([a)", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$([a]:", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$([a]: ", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$([a]::", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$([a]:: ", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$([a]:: (", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$([a]::b", ExpressionErrorKind.IncompleteProperty)]
		[TestCase ("$([a]:: b", ExpressionErrorKind.IncompleteProperty)]
		[TestCase ("$([a]::b ", ExpressionErrorKind.IncompleteProperty)]
		[TestCase ("$([a]::b(", ExpressionErrorKind.ExpectingRightParenOrValue)]
		[TestCase ("$([a]::b($", ExpressionErrorKind.ExpectingRightParenOrValue)]
		[TestCase ("$([a]::b(  $", ExpressionErrorKind.ExpectingRightParenOrValue)]
		[TestCase ("$([a]::b(  %", ExpressionErrorKind.ExpectingRightParenOrValue)]
		[TestCase ("$([a]::b (", ExpressionErrorKind.ExpectingRightParenOrValue)]
		[TestCase ("$([a]::b( ", ExpressionErrorKind.ExpectingRightParenOrValue)]
		[TestCase ("$([a]::b()", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		[TestCase ("$([a]::b().", ExpressionErrorKind.ExpectingMethodName)]
		[TestCase ("$([a]::b(1,", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$([a]::b(1,$", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$([a]::b(1,%", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$([a]::b(1,   %", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$([a]::b(true,", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("$([a]::b(1,1", ExpressionErrorKind.ExpectingRightParenOrComma)]
		[TestCase ("$([a]::b(1,1)", ExpressionErrorKind.ExpectingRightParenOrPeriod)]
		[TestCase ("$([a]::b(1,1x", ExpressionErrorKind.CouldNotParseNumber)]
		[TestCase ("$([a]::b(1,tr", ExpressionErrorKind.ExpectingRightParenOrComma)]
		[TestCase ("$([a]::b(1,foo.bar", ExpressionErrorKind.ExpectingRightParenOrComma)]
		[TestCase ("$([a]::b(1,foo.", ExpressionErrorKind.ExpectingClassNameComponent)]
		[TestCase ("$([a.b", ExpressionErrorKind.ExpectingBracketColonColon)]
		[TestCase ("$([a.b.", ExpressionErrorKind.ExpectingClassNameComponent)]
		[TestCase ("$([1", ExpressionErrorKind.ExpectingClassName)]
		[TestCase ("$([a.1", ExpressionErrorKind.ExpectingClassNameComponent)]
		[TestCase ("$([a.  ", ExpressionErrorKind.ExpectingClassNameComponent)]
		[TestCase ("$([ a .  ", ExpressionErrorKind.ExpectingClassNameComponent)]
		[TestCase ("$([ a . b  . ", ExpressionErrorKind.ExpectingClassNameComponent)]
		[TestCase ("$([ a . )", ExpressionErrorKind.ExpectingClassNameComponent)]
		[TestCase ("$([ a . b . )", ExpressionErrorKind.ExpectingClassNameComponent)]
		[TestCase ("@(foo->'x', ", ExpressionErrorKind.ExpectingValue)]
		[TestCase ("@(foo->'x', '", ExpressionErrorKind.IncompleteString)]
		[TestCase ("@(foo->'x', ''", ExpressionErrorKind.ExpectingRightParen)]
		// incomplete entities
		[TestCase ("&", ExpressionErrorKind.IncompleteOrUnsupportedEntity)]
		[TestCase ("&f", ExpressionErrorKind.IncompleteOrUnsupportedEntity)]
		[TestCase ("& ", ExpressionErrorKind.IncompleteOrUnsupportedEntity)]
		[TestCase ("&amp", ExpressionErrorKind.IncompleteOrUnsupportedEntity)]
		public void TestSimpleError (string expression, ExpressionErrorKind kind)
		{
			//the huge baseOffset can expose parser bugs
			var expr = ExpressionParser.Parse (expression, ExpressionOptions.Metadata, 1000);
			AssertSingleErrorKind (expr, kind);
		}

		[TestCase ("$(prop)", "prop")]
		[TestCase ("%(meta)", "meta")]
		[TestCase ("@(item)", "item")]
		[TestCase ("$(prop123)", "prop123")]
		[TestCase ("%(meta123)", "meta123")]
		[TestCase ("@(item123)", "item123")]
		[TestCase ("$(p)", "p")]
		[TestCase ("%(m)", "m")]
		[TestCase ("@(i)", "i")]
		[TestCase ("$(  prop  )", "prop")]
		[TestCase ("%(  meta  )", "meta")]
		[TestCase ("@(  item  )", "item")]
		[TestCase ("$(_prop)", "_prop")]
		[TestCase ("%(_meta)", "_meta")]
		[TestCase ("@(_item)", "_item")]
		[TestCase ("$(prop-name)", "prop-name")]
		[TestCase ("%(meta-name)", "meta-name")]
		[TestCase ("@(item-name)", "item-name")]
		[TestCase ("$(prop_Name)", "prop_Name")]
		[TestCase ("%(meta_Name)", "meta_Name")]
		[TestCase ("@(item_Name)", "item_Name")]
		public void TestSimpleName (string expression, string name)
		{
			//the huge baseOffset can expose parser bugs
			var expr = ExpressionParser.Parse (expression, ExpressionOptions.Metadata, 1000);
			var actualName = expr switch {
				ExpressionItem item => item.Name,
				ExpressionProperty prop => prop.Name,
				ExpressionMetadata meta => meta.MetadataName,
				_ => null
			};
			Assert.AreEqual (name, actualName);
		}

		[TestCase ("$")]
		[TestCase ("@")]
		[TestCase ("%")]
		[TestCase ("$a")]
		[TestCase ("@b")]
		[TestCase ("%c")]
		[TestCase ("$ ")]
		[TestCase ("@ ")]
		[TestCase ("% ")]
		public void TestLiteral (string expression, string unescaped = null)
		{
			var expr = ExpressionParser.Parse (expression, ExpressionOptions.Metadata);
			var lit = AssertCast<ExpressionText> (expr);
			Assert.AreEqual (unescaped ?? expression, lit.GetUnescapedValue(false, out _, out _));
		}

		[TestCase ("abc", "abc")]
		[TestCase ("abc&apos; ", "abc'")]
		[TestCase ("  abc&apos; ", "abc'")]
		public void TestLiteralTrimmedAndUnescaped (string expression, string expected)
		{
			var expr = ExpressionParser.Parse (expression, ExpressionOptions.Metadata);
			var lit = AssertCast<ExpressionText> (expr);
			Assert.AreEqual (expected, lit.GetUnescapedValue (true, out _, out _));
		}

		[Test]
		public void TestMetadataDisallowed ()
		{
			var expr = ExpressionParser.Parse ("%(Foo)", ExpressionOptions.None);
			AssertSingleErrorKind (expr, ExpressionErrorKind.MetadataDisallowed);
		}

		[Test]
		public void TestItemsDisallowed ()
		{
			var expr = ExpressionParser.Parse ("@(Foo)", ExpressionOptions.None);
			AssertSingleErrorKind (expr, ExpressionErrorKind.ItemsDisallowed);
		}

		[TestCase ("@(Foo)", "Foo")]
		[TestCase ("@(_Foo)", "_Foo")]
		[TestCase ("@(_Foo12_3)", "_Foo12_3")]
		public void TestItem (string expression, string itemName)
		{
			var expr = ExpressionParser.Parse (expression, ExpressionOptions.Items);
			var item = AssertCast<ExpressionItem> (expr);
			Assert.AreEqual (itemName, item.Name);
		}

		[TestCase ("%(Foo)", "Foo")]
		[TestCase ("%(_Foo)", "_Foo")]
		[TestCase ("%(_Foo12_3)", "_Foo12_3")]
		public void TestUnqualifiedMetadata (string expression, string metaName)
		{
			var expr = ExpressionParser.Parse (expression, ExpressionOptions.Metadata);
			var meta = AssertCast<ExpressionMetadata> (expr);
			Assert.AreEqual (metaName, meta.MetadataName);
			Assert.IsNull (meta.ItemName);
		}

		[TestCase ("%(Foo.Bar)", "Foo", "Bar")]
		[TestCase ("%(_Foo._Bar)", "_Foo", "_Bar")]
		[TestCase ("%(_Foo12_3._Bar3_4)", "_Foo12_3", "_Bar3_4")]
		[TestCase ("%(_Foo._Bar)", "_Foo", "_Bar")]
		//surprisingly, this whitespace is permitted, unlike properties and items
		[TestCase ("%( Foo.Bar)", "Foo", "Bar")]
		[TestCase ("%(Foo .Bar)", "Foo", "Bar")]
		[TestCase ("%(Foo. Bar)", "Foo", "Bar")]
		[TestCase ("%(Foo.Bar )", "Foo", "Bar")]
		[TestCase ("%( Foo . Bar )", "Foo", "Bar")]
		public void TestQualifiedMetadata (string expression, string itemName, string metaName)
		{
			var expr = ExpressionParser.Parse (expression, ExpressionOptions.Metadata);
			var meta = AssertCast<ExpressionMetadata> (expr);
			Assert.AreEqual (metaName, meta.MetadataName);
			Assert.AreEqual (itemName, meta.ItemName);
		}

		// %(itename->metadataname) is nonstandard but the F# targets use it and it seems to work
		[TestCase ("%(Foo->Bar)", "Foo", "Bar")]
		public void TestNonstandardQualifiedMetadata (string expression, string itemName, string metaName)
			=> TestQualifiedMetadata (expression, itemName, metaName);


		[Test]
		public void TestPropertyLiteral ()
		{
			TestParse (
				"abc$(Foo)cde@(baritem)510",
				 new ConcatExpression (
					0, 25,
					new ExpressionText (0, "abc", false),
					new ExpressionProperty (3, "Foo"),
					new ExpressionText (9, "cde", false),
					new ExpressionItem (12, "baritem"),
					new ExpressionText (22, "510", false)
				),
				ExpressionOptions.Items
			);
		}

		[Test]
		public void TestList ()
		{
			TestParse (
				"abc;$(Foo)cde;@(baritem);stuff",
				 new ListExpression (
					0, 30, ';',
					new ExpressionText (0, "abc", true),
					new ConcatExpression (
						4, 9,
						new ExpressionProperty (4, "Foo"),
						new ExpressionText (10, "cde", false)
					),
					new ExpressionItem (14, "baritem"),
					new ExpressionText (25, "stuff", true)
				),
				ExpressionOptions.ItemsAndLists
			);
		}

		[Test]
		public void TestNoLists ()
		{
			TestParse (
				"abc;$(Foo)",
				 new ConcatExpression (
					0, 10,
					new ExpressionText (0, "abc;", false),
					new ExpressionProperty (4, "Foo")
				)
			);
		}

		[Test]
		public void TestListWithEntities ()
		{
			TestParse (
				"a&apos;bc;$(Foo)c&#xA;de",
				 new ListExpression (
					0, 24, ';',
					new ExpressionText (0, "a&apos;bc", true),
					new ConcatExpression (
						10, 14,
						new ExpressionProperty (10, "Foo"),
						new ExpressionText (16, "c&#xA;de", false)
					)
				),
				ExpressionOptions.ItemsAndLists
			);
		}

		[Test]
		public void TestListWithBadEntity ()
		{
			var expr = ExpressionParser.Parse ("a&!pos;bc;$(Foo)c&#xA;de", ExpressionOptions.ItemsAndLists, 500);
			AssertSingleErrorKind (expr, ExpressionErrorKind.IncompleteOrUnsupportedEntity);
		}

		[Test]
		public void TestItemTransform ()
		{
			TestParse (
				"@(Foo->'%(Bar.Baz)')",
				new ExpressionItem (
					0, 20,
					new ExpressionItemTransform (
						2, 17,
						new ExpressionItemName (2, "Foo"),
						new QuotedExpression (7, 12, '\'', new ExpressionMetadata (8, 10, "Bar", "Baz")),
						null
					)
				),
				ExpressionOptions.Items
			);
		}

		[Test]
		public void TestItemTransformWithCustomSeparator ()
		{
			TestParse (
				"@(Foo->'%(Bar.Baz)', '$(x)')",
				new ExpressionItem (
					0, 28,
					new ExpressionItemTransform (
						2, 25,
						new ExpressionItemName (2, "Foo"),
						new QuotedExpression (7, 12, '\'', new ExpressionMetadata (8, 10, "Bar", "Baz")),
						new QuotedExpression (21, 6, '\'', new ExpressionProperty (22, "x"))
					)
				),
				ExpressionOptions.Items
			);
		}

		[Test]
		public void TestXmlEntities ()
		{
			TestParse (
				"&quot;;d&foo;bar",
				new ListExpression (
					0, 16, ';',
					new ExpressionText (0, "&quot;", true),
					new ExpressionText (7, "d&foo;bar", true)
				),
				ExpressionOptions.Lists
			);
		}

		static void CheckArgs (List<object> expectedArgs, ExpressionArgumentList actualArgs)
		{
			Assert.AreEqual (expectedArgs.Count, actualArgs.Arguments.Count);
			for (int i = 0; i < expectedArgs.Count; i++) {
				var expected = expectedArgs[i];
				var actual = actualArgs.Arguments[i];

				if (actual is QuotedExpression quotedExpr) {
					actual = quotedExpr.Expression;
				}

				void AssertLiteral<T> (T expectedLit)
					=> Assert.AreEqual (expectedLit, AssertCast<ExpressionArgumentLiteral<T>> (actual).Value);

				switch (expected) {
				case int intVal:
					AssertLiteral<long> (intVal);
					break;
				case bool boolVal:
					AssertLiteral<bool> (boolVal);
					break;
				case float floatVal:
					AssertLiteral<double> (floatVal);
					break;
				case string stringVal:
					if (stringVal.Length > 0) {
						if (stringVal[0] == '$') {
							var prop = AssertCast<ExpressionProperty> (actual);
							Assert.IsTrue (prop.IsSimpleProperty, "Expected property to be simple");
							Assert.AreEqual (stringVal.Substring (1), prop.Name);
							break;
						}
						if (stringVal[0] == '%') {
							var meta = AssertCast<ExpressionMetadata> (actual);
							var metaName = meta.IsQualified ? meta.ItemName + meta.MetadataName : meta.MetadataName;
							Assert.AreEqual (stringVal.Substring (1), metaName);
							break;
						}
					}
					if (actual is ExpressionText txt) {
						Assert.AreEqual (expected, txt.Value);
						break;
					}
					AssertLiteral<string> (stringVal);
					break;
				}
			}
		}

		[TestCase ("$(Foo.Bar())", "Foo", "Bar")]
		[TestCase ("$(   Foo  .  Bar  (  )  )", "Foo", "Bar")]
		[TestCase ("$(Foo.Baz('Hello'))", "Foo", "Baz", "Hello")]
		[TestCase ("$(Foo.A(5))", "Foo", "A", 5)]
		[TestCase ("$(Foo.A(true))", "Foo", "A", true)]
		[TestCase ("$(Foo.A(true,   20 ))", "Foo", "A", true, 20)]
		[TestCase ("$(Foo.A(20.5))", "Foo", "A", 20.5d)]
		[TestCase ("$(Foo.A(.61))", "Foo", "A", .61d)]
		[TestCase ("$(Foo.A('bees'))", "Foo", "A", "bees")]
		[TestCase ("$(Foo.A('bees', 2, 'more bees'))", "Foo", "A", "bees", 2, "more bees")]
		[TestCase ("$(Foo.A(`bees`, `more bees`))", "Foo", "A", "bees", "more bees")]
		[TestCase ("$(a[0])", "a", null, 0)]
		[TestCase ("$(a['hello',true])", "a", null, "hello", true)]
		public void TestSimplePropertyFunctions(object[] args)
		{
			//the huge baseOffset can also expose parser bugs
			var expr = ExpressionParser.Parse ((string)args[0], ExpressionOptions.None, 1000);
			var targetName = (string)args [1];
			var funcName = (string)args [2];
			var funcArgs = args.Skip (3).ToList ();

			var prop = AssertCast<ExpressionProperty> (expr);
			Assert.IsFalse (prop.IsSimpleProperty);

			var invocation = AssertCast<ExpressionPropertyFunctionInvocation> (prop.Expression);
			var target = AssertCast<ExpressionPropertyName> (invocation.Target);

			Assert.AreEqual (targetName, target.Name);
			Assert.AreEqual (funcName, invocation.Function?.Name);

			var argList = AssertCast<ExpressionArgumentList> (invocation.Arguments);
			CheckArgs (funcArgs, argList);
		}

		[TestCase ("$([Foo]::Bar())", "Foo", "Bar")]
		[TestCase ("$(   [Foo]::    Bar  (  )  )", "Foo", "Bar")]
		[TestCase ("$([Foo]::A(5))", "Foo", "A", 5)]
		[TestCase ("$([Foo]::A(true))", "Foo", "A", true)]
		[TestCase ("$([Foo]::A(true,   20 ))", "Foo", "A", true, 20)]
		[TestCase ("$([Foo]::A(20.5))", "Foo", "A", 20.5d)]
		[TestCase ("$([Foo]::A(.61))", "Foo", "A", .61d)]
		[TestCase ("$([Foo.Bar]::A())", "Foo.Bar", "A")]
		[TestCase ("$([Foo  .  Bar]::A())", "Foo.Bar", "A")]
		[TestCase ("$([  Foo  .  Bar  ]::A())", "Foo.Bar", "A")]
		[TestCase ("$([Foo.Bar]::A ( 'bees' , 2 , 'more bees' ) )", "Foo.Bar", "A", "bees", 2, "more bees")]
		[TestCase ("$([Foo.Bar]::A ( 'bees' ,  `more bees` ) )", "Foo.Bar", "A", "bees", "more bees")]
		[TestCase ("$([Foo]::Bar($(Baz)))", "Foo", "Bar", "$Baz")]
		[TestCase ("$([Foo]::Bar(%(Baz)))", "Foo", "Bar", "%Baz")]
		[TestCase ("$([Foo]::Bar('baz', $(abc), %(xyz), 1))", "Foo", "Bar", "baz", "$abc", "%xyz", 1)]
		public void TestStaticPropertyFunctions (object [] args)
		{
			//the huge baseOffset can expose parser bugs
			var expr = ExpressionParser.Parse ((string)args [0], ExpressionOptions.None, 1000);
			var targetName = (string)args [1];
			var funcName = (string)args [2];
			var funcArgs = args.Skip (3).ToList ();

			var prop = AssertCast<ExpressionProperty> (expr);
			Assert.IsFalse (prop.IsSimpleProperty);

			var invocation = AssertCast<ExpressionPropertyFunctionInvocation> (prop.Expression);
			var target = AssertCast<ExpressionClassReference> (invocation.Target);

			Assert.AreEqual (targetName, target.Name);
			Assert.AreEqual (funcName, invocation.Function?.Name);

			var argList = AssertCast<ExpressionArgumentList> (invocation.Arguments);
			CheckArgs (funcArgs, argList);
		}

		[TestCase ("@(Foo->Bar())", "Foo", "Bar")]
		[TestCase ("@(   Foo  ->  Bar  (  )  )", "Foo", "Bar")]
		[TestCase ("@(Foo->Baz('Hello'))", "Foo", "Baz", "Hello")]
		[TestCase ("@(Foo->A(5))", "Foo", "A", 5)]
		[TestCase ("@(Foo->A(true))", "Foo", "A", true)]
		[TestCase ("@(Foo->A(true,   20 ))", "Foo", "A", true, 20)]
		[TestCase ("@(Foo->A(20.5))", "Foo", "A", 20.5d)]
		[TestCase ("@(Foo->A(.61))", "Foo", "A", .61d)]
		[TestCase ("@(Foo->A('bees' , 2 , 'more bees'))", "Foo", "A", "bees", 2, "more bees")]
		[TestCase ("@(Foo->A(`bees`, 'more bees'))", "Foo", "A", "bees", "more bees")]
		public void TestSimpleItemFunctions (object [] args)
		{
			//the huge baseOffset can expose parser bugs
			var expr = ExpressionParser.Parse ((string)args [0], ExpressionOptions.ItemsMetadataAndLists, 1000);
			var targetName = (string)args [1];
			var funcName = (string)args [2];
			var funcArgs = args.Skip (3).ToList ();

			var prop = AssertCast<ExpressionItem> (expr);
			Assert.IsFalse (prop.IsSimpleItem);

			var invocation = AssertCast<ExpressionItemFunctionInvocation> (prop.Expression);
			var target = AssertCast<ExpressionItemName> (invocation.Target);

			Assert.AreEqual (targetName, target.Name);
			Assert.AreEqual (funcName, invocation.Function?.Name);

			var argList = AssertCast<ExpressionArgumentList> (invocation.Arguments);
			CheckArgs (funcArgs, argList);
		}

		[Test]
		public void TestFunctionChaining ()
		{
			TestParse (
				"$(Foo.Bar()[0].Baz(1,'hi'))",
				new ExpressionProperty (
					0, 27,
					new ExpressionPropertyFunctionInvocation (
						2, 24,
						new ExpressionPropertyFunctionInvocation (
							2, 12,
							new ExpressionPropertyFunctionInvocation (
								2, 9,
								new ExpressionPropertyName (2, "Foo"),
								new ExpressionFunctionName (6, "Bar"),
								new ExpressionArgumentList (9, 2, new List<ExpressionNode> ())
							),
							null,
							new ExpressionArgumentList (11, 3, new List<ExpressionNode> {
								new ExpressionArgumentInt (12, 1, 0),
							})
						),
						new ExpressionFunctionName (15, "Baz"),
						new ExpressionArgumentList (18, 8, new List<ExpressionNode> {
							new ExpressionArgumentInt (19, 1, 1),
							new QuotedExpression (21, 4, '\'', new ExpressionText (22, "hi", true))
						})
					)
				),
				ExpressionOptions.ItemsMetadataAndLists
			);
		}

		[Test]
		public void TestComplexArgs ()
		{
			TestParse (
				"$(Foo.Bar($(Baz), 'thing'))",
				new ExpressionProperty (
					0, 27,
					new ExpressionPropertyFunctionInvocation (
						2, 24,
						new ExpressionPropertyName (2, "Foo"),
						new ExpressionFunctionName (6, "Bar"),
						new ExpressionArgumentList (9, 17, new List<ExpressionNode> {
							new ExpressionProperty (
								10, 6,
								new ExpressionPropertyName (12, "Baz")
							),
							new QuotedExpression (18, 7, '\'', new ExpressionText (19, "thing", true))
						})
					)
				),
				ExpressionOptions.ItemsMetadataAndLists
			);
		}

		[Test]
		public void TestRegistryKey ()
		{
			TestParse (
				"$(Registry:HKEY_LOCAL_MACHINE\\Software\\Microsoft\\.NETFramework@InstallRoot)",
				new ExpressionProperty (
					0, 75,
					new ExpressionPropertyRegistryValue (
						2, 72, "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\.NETFramework@InstallRoot"
					)
				)
			);
		}

		static void TestParse (string expression, ExpressionNode expected, ExpressionOptions options = ExpressionOptions.None)
		{
			var expr = ExpressionParser.Parse (expression, options, 0);
			AssertEqual (expected, expr, 0);

			const int baseOffset = 123;
			expr = ExpressionParser.Parse (expression, options, baseOffset);
			AssertEqual (expected, expr, baseOffset);
		}

		internal static void AssertEqual (ExpressionNode expected, ExpressionNode actual, int expectedOffset)
		{
			if (expected == null) {
				Assert.IsNull (actual);
				return;
			}
			if (actual is ExpressionError actualErr && !(expected is ExpressionError)) {
				Assert.Fail ($"Unexpected ExpressionError: {actualErr.Kind} @ {actualErr.Offset}");
			}
			Assert.That (actual, Is.TypeOf (expected.GetType ()));
			switch (actual)
			{
			case IContainerExpression listExpr:
				var expectedListExpr = (IContainerExpression)expected;
				Assert.AreEqual (expectedListExpr.Nodes.Count, listExpr.Nodes.Count);
				for (int i = 0; i < listExpr.Nodes.Count; i++) {
					AssertEqual (expectedListExpr.Nodes[i], listExpr.Nodes[i], expectedOffset);
				}
				break;
			case ExpressionText literal:
				var expectedLit = (ExpressionText)expected;
				Assert.AreEqual (expectedLit.Value, literal.Value);
				Assert.AreEqual (expectedLit.IsPure, literal.IsPure);
				break;
			case ExpressionProperty prop:
				var expectedProp = (ExpressionProperty)expected;
				AssertEqual (expectedProp.Expression, prop.Expression, expectedOffset);
				break;
			case ExpressionItem item:
				var expectedItem = (ExpressionItem)expected;
				AssertEqual (expectedItem.Expression, item.Expression, expectedOffset);
				break;
			case ExpressionMetadata meta:
				var expectedMeta = (ExpressionMetadata)expected;
				Assert.AreEqual (expectedMeta.MetadataName, meta.MetadataName);
				Assert.AreEqual (expectedMeta.ItemName, meta.ItemName);
				break;
			case ExpressionItemName itemName:
				var expectedItemName = (ExpressionItemName)expected;
				Assert.AreEqual (expectedItemName.Name, itemName.Name);
				break;
			case ExpressionPropertyName propName:
				var expectedPropName = (ExpressionPropertyName)expected;
				Assert.AreEqual (expectedPropName.Name, propName.Name);
				break;
			case ExpressionFunctionName funcName:
				var expectedFuncName = (ExpressionFunctionName)expected;
				Assert.AreEqual (expectedFuncName.Name, funcName.Name);
				break;
			case ExpressionPropertyFunctionInvocation propInv:
				var expectedPropInv = (ExpressionPropertyFunctionInvocation)expected;
				AssertEqual (expectedPropInv.Function, propInv.Function, expectedOffset);
				AssertEqual (expectedPropInv.Target, propInv.Target, expectedOffset);
				AssertEqual (expectedPropInv.Arguments, propInv.Arguments, expectedOffset);
				break;
			case ExpressionItemFunctionInvocation itemInv:
				var expectedItemInv = (ExpressionItemFunctionInvocation)expected;
				AssertEqual (expectedItemInv.Function, itemInv.Function, expectedOffset);
				AssertEqual (expectedItemInv.Target, itemInv.Target, expectedOffset);
				AssertEqual (expectedItemInv.Arguments, itemInv.Arguments, expectedOffset);
				break;
			case ExpressionItemTransform itemTransform:
				var expectedItemTransform = (ExpressionItemTransform)expected;
				AssertEqual (expectedItemTransform.Transform, itemTransform.Transform, expectedOffset);
				AssertEqual (expectedItemTransform.Target, itemTransform.Target, expectedOffset);
				AssertEqual (expectedItemTransform.Separator, itemTransform.Separator, expectedOffset);
				break;
			case ExpressionArgumentList argList:
				var expectedArgList = (ExpressionArgumentList)expected;
				Assert.AreEqual (expectedArgList.Arguments.Count, argList.Arguments.Count);
				for (int i = 0; i < argList.Arguments.Count; i++) {
					AssertEqual (expectedArgList.Arguments[i], argList.Arguments[i], expectedOffset);
				}
				break;
			case ExpressionArgumentLiteral argLiteral:
				var expectedArgLiteral = (ExpressionArgumentLiteral)expected;
				Assert.AreEqual (expectedArgLiteral.Kind, argLiteral.Kind);
				Assert.AreEqual (expectedArgLiteral.Value, argLiteral.Value);
				break;
			case ExpressionPropertyRegistryValue regVal:
				var expectedRegVal = (ExpressionPropertyRegistryValue)expected;
				Assert.AreEqual (expectedRegVal.RegistryReference, regVal.RegistryReference);
				break;
			case ExpressionConditionOperator actualOperator:
				var expectedOperator = (ExpressionConditionOperator)expected;
				Assert.AreEqual (expectedOperator.OperatorKind, actualOperator.OperatorKind);
				AssertEqual (expectedOperator.Left, actualOperator.Left, expectedOffset);
				AssertEqual (expectedOperator.Right, actualOperator.Right, expectedOffset);
				break;
			case ExpressionConditionFunction actualConditionFunction:
				var expectedConditionFunction = (ExpressionConditionFunction)expected;
				AssertEqual (expectedConditionFunction.Name, actualConditionFunction.Name, expectedOffset);
				AssertEqual (expectedConditionFunction.Arguments, actualConditionFunction.Arguments, expectedOffset);
				break;
			case QuotedExpression actualQuotedExpr:
				var expectedQuotedExpr = (QuotedExpression)expected;
				Assert.AreEqual (expectedQuotedExpr.QuoteChar, actualQuotedExpr.QuoteChar);
				AssertEqual (expectedQuotedExpr.Expression, actualQuotedExpr.Expression, expectedOffset);
				break;
			case ExpressionParenGroup parentGroup:
				var expectedParentGroup = (ExpressionParenGroup)expected;
				AssertEqual (expectedParentGroup.Expression, parentGroup.Expression, expectedOffset);
				break;
			case IncompleteExpressionError iee:
				var expectedIee = (IncompleteExpressionError)expected;
				AssertEqual (expectedIee.IncompleteNode, iee.IncompleteNode, expectedOffset);
				Assert.AreEqual (expectedIee.Kind, iee.Kind);
				Assert.AreEqual (expectedIee.WasEOF, iee.WasEOF);
				break;
			case ExpressionError err:
				var expectedErr = (ExpressionError)expected;
				Assert.AreEqual (expectedErr.Kind, err.Kind);
				Assert.AreEqual (expectedErr.WasEOF, err.WasEOF);
				break;
			default:
				throw new Exception ($"Unsupported node kind {actual.GetType()}");
			}
			Assert.AreEqual (expected.Length, actual.Length);
			Assert.AreEqual (expected.Offset + expectedOffset, actual.Offset);
		}

		static T AssertCast<T> (object o)
		{
			Assert.IsNotNull (o);

			Type tType = typeof (T);
			Type oType = o.GetType ();

			if (o is ExpressionError err && !tType.IsAssignableFrom (typeof (ExpressionError))) {
				Assert.Fail ($"Unexpected ExpressionError: {err.Kind} @ {err.Offset}");
			}

			Assert.IsTrue (tType.IsAssignableFrom (oType), "Cannot assign {0} from {1}", tType, oType);

			return (T)o;
		}

		static void AssertSingleErrorKind (ExpressionNode n, ExpressionErrorKind kind)
			=> Assert.AreEqual (kind, AssertSingleError<ExpressionError> (n).Kind);

		static T AssertSingleError<T> (ExpressionNode n) where T : ExpressionError
		{
			var list = n.WithAllDescendants ().OfType<T> ().ToList ();
			Assert.AreEqual (1, list.Count);
			return list[0];
		}
	}
}