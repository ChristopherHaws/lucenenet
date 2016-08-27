/*
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Analysis
{

	/// <summary>
	/// base test class for testing Unicode collation.
	/// </summary>
	public abstract class CollationTestBase : LuceneTestCase
	{
		protected internal string FirstRangeBeginningOriginal = "\u062F";
		protected internal string FirstRangeEndOriginal = "\u0698";

		protected internal string SecondRangeBeginningOriginal = "\u0633";
		protected internal string SecondRangeEndOriginal = "\u0638";

		/// <summary>
		/// Convenience method to perform the same function as CollationKeyFilter.
		/// </summary>
		/// <param name="keyBits"> the result from
		///  collator.getCollationKey(original).toByteArray() </param>
		/// <returns> The encoded collation key for the original String </returns>
		/// @deprecated only for testing deprecated filters
		[Obsolete("only for testing deprecated filters")]
		protected internal virtual string EncodeCollationKey(sbyte[] keyBits)
		{
			// Ensure that the backing char[] array is large enough to hold the encoded
			// Binary String
			var encodedLength = IndexableBinaryStringTools.GetEncodedLength(keyBits, 0, keyBits.Length);
			var encodedBegArray = new char[encodedLength];
			IndexableBinaryStringTools.Encode(keyBits, 0, keyBits.Length, encodedBegArray, 0, encodedLength);
			return new string(encodedBegArray);
		}

		public virtual void TestFarsiRangeFilterCollating(Analyzer analyzer, BytesRef firstBeg, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
		{
			Directory dir = NewDirectory();
			var writer = new IndexWriter(dir, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, analyzer));
			var doc = new Document();
			doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
			doc.Add(new StringField("body", "body", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(dir);
			var searcher = new IndexSearcher(reader);
			Search.Query query = new TermQuery(new Term("body", "body"));

			// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
			// orders the U+0698 character before the U+0633 character, so the single
			// index Term below should NOT be returned by a TermRangeFilter with a Farsi
			// Collator (or an Arabic one for the case when Farsi searcher not
			// supported).
			var result = searcher.Search(query, new TermRangeFilter("content", firstBeg, firstEnd, true, true), 1).ScoreDocs;
			AreEqual(0, result.Length, "The index Term should not be included.");

			result = searcher.Search(query, new TermRangeFilter("content", secondBeg, secondEnd, true, true), 1).ScoreDocs;
			AreEqual(1, result.Length, "The index Term should be included.");

			reader.Dispose();
			dir.Dispose();
		}

		public virtual void TestFarsiRangeQueryCollating(Analyzer analyzer, BytesRef firstBeg, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
		{
			Directory dir = NewDirectory();
			var writer = new IndexWriter(dir, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, analyzer));
			var doc = new Document();

			// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
			// orders the U+0698 character before the U+0633 character, so the single
			// index Term below should NOT be returned by a TermRangeQuery with a Farsi
			// Collator (or an Arabic one for the case when Farsi is not supported).
			doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(dir);
			var searcher = new IndexSearcher(reader);

			Search.Query query = new TermRangeQuery("content", firstBeg, firstEnd, true, true);
			var hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual(0, hits.Length, "The index Term should not be included.");

			query = new TermRangeQuery("content", secondBeg, secondEnd, true, true);
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			AreEqual(1, hits.Length, "The index Term should be included.");
			reader.Dispose();
			dir.Dispose();
		}

		public virtual void TestFarsiTermRangeQuery(Analyzer analyzer, BytesRef firstBeg, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
		{
			Directory farsiIndex = NewDirectory();
			var writer = new IndexWriter(farsiIndex, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, analyzer));
			var doc = new Document();
			doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
			doc.Add(new StringField("body", "body", Field.Store.YES));
			writer.AddDocument(doc);
			writer.Dispose();

			IndexReader reader = DirectoryReader.Open(farsiIndex);
			var search = this.NewSearcher(reader);

			// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
			// orders the U+0698 character before the U+0633 character, so the single
			// index Term below should NOT be returned by a TermRangeQuery
			// with a Farsi Collator (or an Arabic one for the case when Farsi is
			// not supported).
			Search.Query csrq = new TermRangeQuery("content", firstBeg, firstEnd, true, true);
			var result = search.Search(csrq, null, 1000).ScoreDocs;
			AreEqual(0, result.Length, "The index Term should not be included.");

			csrq = new TermRangeQuery("content", secondBeg, secondEnd, true, true);
			result = search.Search(csrq, null, 1000).ScoreDocs;
			AreEqual(1, result.Length, "The index Term should be included.");
			reader.Dispose();
			farsiIndex.Dispose();
		}

		// Test using various international locales with accented characters (which
		// sort differently depending on locale)
		//
		// Copied (and slightly modified) from
		// Lucene.Net.Search.TestSort.testInternationalSort()
		//
		// TODO: this test is really fragile. there are already 3 different cases,
		// depending upon unicode version.
		public virtual void TestCollationKeySort(Analyzer usAnalyzer, Analyzer franceAnalyzer, Analyzer swedenAnalyzer, Analyzer denmarkAnalyzer, string usResult, string frResult, string svResult, string dkResult)
		{
			Directory indexStore = NewDirectory();
			var writer = new IndexWriter(indexStore, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false)));

			// document data:
			// the tracer field is used to determine which document was hit
			var sortData = new string[][] { new string[] { "A", "x", "p\u00EAche", "p\u00EAche", "p\u00EAche", "p\u00EAche" }, new string[] { "B", "y", "HAT", "HAT", "HAT", "HAT" }, new string[] { "C", "x", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9" }, new string[] { "D", "y", "HUT", "HUT", "HUT", "HUT" }, new string[] { "E", "x", "peach", "peach", "peach", "peach" }, new string[] { "F", "y", "H\u00C5T", "H\u00C5T", "H\u00C5T", "H\u00C5T" }, new string[] { "G", "x", "sin", "sin", "sin", "sin" }, new string[] { "H", "y", "H\u00D8T", "H\u00D8T", "H\u00D8T", "H\u00D8T" }, new string[] { "I", "x", "s\u00EDn", "s\u00EDn", "s\u00EDn", "s\u00EDn" }, new string[] { "J", "y", "HOT", "HOT", "HOT", "HOT" } };

			var customType = new FieldType();
			customType.Stored = true;

			for (var i = 0; i < sortData.Length; ++i)
			{
				var doc = new Document();
				doc.Add(new Field("tracer", sortData[i][0], customType));
				doc.Add(new TextField("contents", sortData[i][1], Field.Store.NO));
				if (sortData[i][2] != null)
				{
					doc.Add(new TextField("US", usAnalyzer.TokenStream("US", new StringReader(sortData[i][2]))));
				}
				if (sortData[i][3] != null)
				{
					doc.Add(new TextField("France", franceAnalyzer.TokenStream("France", new StringReader(sortData[i][3]))));
				}
				if (sortData[i][4] != null)
				{
					doc.Add(new TextField("Sweden", swedenAnalyzer.TokenStream("Sweden", new StringReader(sortData[i][4]))));
				}
				if (sortData[i][5] != null)
				{
					doc.Add(new TextField("Denmark", denmarkAnalyzer.TokenStream("Denmark", new StringReader(sortData[i][5]))));
				}
				writer.AddDocument(doc);
			}
			writer.ForceMerge(1);
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(indexStore);
			var searcher = new IndexSearcher(reader);

			var sort = new Sort();
			Search.Query queryX = new TermQuery(new Term("contents", "x"));
			Search.Query queryY = new TermQuery(new Term("contents", "y"));

			sort.SetSort(new SortField("US", SortField.Type_e.STRING));
			this.AssertMatches(searcher, queryY, sort, usResult);

			sort.SetSort(new SortField("France", SortField.Type_e.STRING));
			this.AssertMatches(searcher, queryX, sort, frResult);

			sort.SetSort(new SortField("Sweden", SortField.Type_e.STRING));
			this.AssertMatches(searcher, queryY, sort, svResult);

			sort.SetSort(new SortField("Denmark", SortField.Type_e.STRING));
			this.AssertMatches(searcher, queryY, sort, dkResult);
			reader.Dispose();
			indexStore.Dispose();
		}

		// Make sure the documents returned by the search match the expected list
		// Copied from TestSort.java
		private void AssertMatches(IndexSearcher searcher, Search.Query query, Sort sort, string expectedResult)
		{
			var result = searcher.Search(query, null, 1000, sort).ScoreDocs;
			var buff = new StringBuilder(10);
			var n = result.Length;
			for (var i = 0; i < n; ++i)
			{
				var doc = searcher.Doc(result[i].Doc);
				var v = doc.GetFields("tracer");
				for (var j = 0; j < v.Length; ++j)
				{
					buff.Append(v[j].StringValue);
				}
			}
			AreEqual(expectedResult, buff.ToString());
		}

		public virtual void AssertThreadSafe(Analyzer analyzer)
		{
			var numTestPoints = 100;
			var numThreads = TestUtil.NextInt(Random(), 3, 5);
			var map = new Dictionary<string, BytesRef>();

			// create a map<String,SortKey> up front.
			// then with multiple threads, generate sort keys for all the keys in the map
			// and ensure they are the same as the ones we produced in serial fashion.

			for (var i = 0; i < numTestPoints; i++)
			{
				var term = TestUtil.RandomSimpleString(Random());
				IOException priorException = null;
				var ts = analyzer.TokenStream("fake", new StreamReader(term));
				try
				{
					var termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
					var bytes = termAtt.BytesRef;
					ts.Reset();
					IsTrue(ts.IncrementToken());
					termAtt.FillBytesRef();
					// ensure we make a copy of the actual bytes too
					map[term] = BytesRef.DeepCopyOf(bytes);
					IsFalse(ts.IncrementToken());
					ts.End();
				}
				catch (IOException e)
				{
					priorException = e;
				}
				finally
				{
					IOUtils.CloseWhileHandlingException(priorException, ts);
				}
			}

			var threads = new ThreadClass[numThreads];
			for (var i = 0; i < numThreads; i++)
			{
				threads[i] = new ThreadAnonymousInnerClassHelper(this, analyzer, map);
			}
			for (var i = 0; i < numThreads; i++)
			{
				threads[i].Start();
			}
			for (var i = 0; i < numThreads; i++)
			{
				threads[i].Join();
			}
		}

		private class ThreadAnonymousInnerClassHelper : ThreadClass
		{
			private readonly CollationTestBase OuterInstance;

			private Analyzer Analyzer;
			private Dictionary<string, BytesRef> Map;

			public ThreadAnonymousInnerClassHelper(CollationTestBase outerInstance, Analyzer analyzer, Dictionary<string, BytesRef> map)
			{
				this.OuterInstance = outerInstance;
				this.Analyzer = analyzer;
				this.Map = map;
			}

			public override void Run()
			{
				try
				{
					foreach (var mapping in this.Map)
					{
						var term = mapping.Key;
						var expected = mapping.Value;
						IOException priorException = null;
						var ts = this.Analyzer.TokenStream("fake", new StreamReader(term));
						try
						{
							var termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
							var bytes = termAtt.BytesRef;
							ts.Reset();
							IsTrue(ts.IncrementToken());
							termAtt.FillBytesRef();
							AreEqual(expected, bytes);
							IsFalse(ts.IncrementToken());
							ts.End();
						}
						catch (IOException e)
						{
							priorException = e;
						}
						finally
						{
							IOUtils.CloseWhileHandlingException(priorException, ts);
						}
					}
				}
				catch (IOException e)
				{
					throw (Exception)e;
				}
			}
		}
	}
}