﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Globalization;
using System.IO;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Collation;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Collation
{
	[TestFixture]
	[Obsolete("remove when CollationKeyFilter is removed.")]
	public class TestCollationKeyFilter : CollationTestBase
	{
		private readonly bool InstanceFieldsInitialized = false;

		public TestCollationKeyFilter()
		{
			if (!this.InstanceFieldsInitialized)
			{
				this.InitializeInstanceFields();
				this.InstanceFieldsInitialized = true;
			}
		}

		private void InitializeInstanceFields()
		{
			this.analyzer = new TestAnalyzer(this, this.collator);
			this.firstRangeBeginning = new BytesRef(this.EncodeCollationKey(this.collator.GetCollationKey(this.FirstRangeBeginningOriginal).KeyData.ToSByteArray()));
			this.firstRangeEnd = new BytesRef(this.EncodeCollationKey(this.collator.GetCollationKey(this.FirstRangeEndOriginal).KeyData.ToSByteArray()));
			this.secondRangeBeginning = new BytesRef(this.EncodeCollationKey(this.collator.GetCollationKey(this.SecondRangeBeginningOriginal).KeyData.ToSByteArray()));
			this.secondRangeEnd = new BytesRef(this.EncodeCollationKey(this.collator.GetCollationKey(this.SecondRangeEndOriginal).KeyData.ToSByteArray()));
		}

		// the sort order of Ø versus U depends on the version of the rules being used
		// for the inherited root locale: Ø's order isnt specified in Locale.US since 
		// its not used in english.
		internal bool oStrokeFirst = Collator.GetInstance(CultureInfo.GetCultureInfo("")).Compare("Ø", "U") < 0;

		// Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
		// RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
		// characters properly.
		private readonly Collator collator = Collator.GetInstance(CultureInfo.GetCultureInfo("ar"));
		private Analyzer analyzer;

		private BytesRef firstRangeBeginning;
		private BytesRef firstRangeEnd;
		private BytesRef secondRangeBeginning;
		private BytesRef secondRangeEnd;
		
		public sealed class TestAnalyzer : Analyzer
		{
			private readonly TestCollationKeyFilter outerInstance;

			internal Collator _collator;

			internal TestAnalyzer(TestCollationKeyFilter outerInstance, Collator collator)
			{
				this.outerInstance = outerInstance;
				this._collator = collator;
			}

			public override TokenStreamComponents CreateComponents(String fieldName, TextReader reader)
			{
				Tokenizer result = new KeywordTokenizer(reader);
				return new TokenStreamComponents(result, new CollationKeyFilter(result, this._collator));
			}
		}

		[Test]
		public virtual void TestFarsiRangeFilterCollating()
		{
			this.TestFarsiRangeFilterCollating(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
		}

		[Test]
		public virtual void TestFarsiRangeQueryCollating()
		{
			this.TestFarsiRangeQueryCollating(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
		}

		[Test]
		public virtual void TestFarsiTermRangeQuery()
		{
			this.TestFarsiTermRangeQuery(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
		}

		[Test]
		public virtual void TestCollationKeySort()
		{
			var usAnalyzer = new TestAnalyzer(this, Collator.GetInstance(CultureInfo.GetCultureInfo("us")));
			var franceAnalyzer = new TestAnalyzer(this, Collator.GetInstance(CultureInfo.GetCultureInfo("fr")));
			var swedenAnalyzer = new TestAnalyzer(this, Collator.GetInstance(CultureInfo.GetCultureInfo("sv-SE")));
			var denmarkAnalyzer = new TestAnalyzer(this, Collator.GetInstance(CultureInfo.GetCultureInfo("da-DK")));

			// The ICU Collator and Sun java.text.Collator implementations differ in their
			// orderings - "BFJDH" is the ordering for java.text.Collator for Locale.US.
			this.TestCollationKeySort(usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer, this.oStrokeFirst ? "BFJHD" : "BFJDH", "EACGI", "BJDFH", "BJDHF");
		}
	}
}