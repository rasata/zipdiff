﻿namespace ZipDiff.Core.UnitTests
{
	using System.IO;
	using System.Linq;
	using System.Text;
	using ICSharpCode.SharpZipLib.Zip;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using ZipDiff.Core.Output;

	[TestClass]
	public class DifferenceCalculatorTests
	{
		[TestClass]
		public class CalculateDifferences
		{
			const string ZipOneEntryA1 = "zip-one-entry-a1.zip";
			const string ZipOneEntryA1_Changed = "zip-one-entry-a1-changed.zip";
			const string ZipOneEntryA2 = "zip-one-entry-a2.zip";
			const string ZipOneEntryA3 = "zip-one-entry-a3.zip";
			const string ZipOneEntryB1 = "zip-one-entry-b1.zip";

			[TestInitialize]
			public void Init()
			{
				this.CreateZipOneEntry(ZipOneEntryA1);
				this.CreateZipOneEntry(ZipOneEntryA2);
				this.CreateZipOneEntry(ZipOneEntryA3, 'A');
				this.CreateZipOneEntryContentsChanged(ZipOneEntryA1_Changed, 'a', 'b');
				this.CreateZipOneEntry(ZipOneEntryB1, 'b');
			}

			[TestCleanup]
			public void Cleanup()
			{
				var zips = new[] { ZipOneEntryA1, ZipOneEntryA1_Changed, ZipOneEntryA2, ZipOneEntryA3, ZipOneEntryB1 };
				foreach (var zip in zips.Where(File.Exists))
					File.Delete(zip);
			}

			[TestMethod]
			public void CalculateDifferences_SameZip()
			{
				var calc = new DifferenceCalculator(ZipOneEntryA1, ZipOneEntryA1);
				var diff = calc.GetDifferences();

				Assert.IsFalse(diff.HasDifferences());
				Assert.IsTrue(diff.Added.Count == 0);
				Assert.IsTrue(diff.Removed.Count == 0);
				Assert.IsTrue(diff.Changed.Count == 0);

				this.ExerciseOutputBuilders(diff);
			}

			[TestMethod]
			public void CalculateDifferences_ZipsSameEntries()
			{
				var calc = new DifferenceCalculator(ZipOneEntryA1, ZipOneEntryA2);
				var diff = calc.GetDifferences();

				Assert.IsFalse(diff.HasDifferences());
				Assert.IsTrue(diff.Added.Count == 0);
				Assert.IsTrue(diff.Removed.Count == 0);
				Assert.IsTrue(diff.Changed.Count == 0);

				this.ExerciseOutputBuilders(diff);
			}

			[TestMethod]
			public void CalculateDifferences_ZipsDifferentEntries()
			{
				var calc = new DifferenceCalculator(ZipOneEntryA1, ZipOneEntryB1);
				var diff = calc.GetDifferences();

				Assert.IsTrue(diff.HasDifferences());
				Assert.IsTrue(diff.Added.ContainsKey("b"));
				Assert.IsTrue(diff.Removed.ContainsKey("a"));
				Assert.IsTrue(diff.Changed.Count == 0);

				this.ExerciseOutputBuilders(diff);
			}

			[TestMethod]
			public void CalculateDifferences_ZipsSameEntriesDifferentContent()
			{
				var calc = new DifferenceCalculator(ZipOneEntryA1, ZipOneEntryA1_Changed);
				var diff = calc.GetDifferences();

				Assert.IsTrue(diff.HasDifferences());
				Assert.IsTrue(diff.Added.Count == 0);
				Assert.IsTrue(diff.Removed.Count == 0);
				Assert.IsTrue(diff.Changed.ContainsKey("a"));

				this.ExerciseOutputBuilders(diff);
			}

			[TestMethod]
			public void CalculateDifferences_ZipsDifferentEntriesSameContent()
			{
				var calc = new DifferenceCalculator(ZipOneEntryA1, ZipOneEntryA3) { IgnoreCase = true };
				var diff = calc.GetDifferences();

				Assert.IsTrue(diff.HasDifferences());
				Assert.IsTrue(diff.Added.Count == 0);
				Assert.IsTrue(diff.Removed.Count == 0);
				Assert.IsTrue(diff.Changed.ContainsKey("a"));

				this.ExerciseOutputBuilders(diff);
			}

			private void CreateZipOneEntry(string filename, char c = 'a')
			{
				using (var writer = new StreamWriter(filename))
				{
					var testZipOS = new ZipOutputStream(writer.BaseStream);

					var entry1 = new ZipEntry(c.ToString());
					testZipOS.PutNextEntry(entry1);

					var data = new string(char.ToLower(c), 1024);
					var bytes = UTF8Encoding.UTF8.GetBytes(data);
					foreach (var bit in bytes)
						testZipOS.WriteByte(bit);

					testZipOS.Finish();
					testZipOS.Flush();
				}
			}

			private void CreateZipOneEntryContentsChanged(string filename, char c = 'a', char changed = 'b')
			{
				using (var writer = new StreamWriter(filename))
				{
					var testZipOS = new ZipOutputStream(writer.BaseStream);

					var entry1 = new ZipEntry(c.ToString());
					testZipOS.PutNextEntry(entry1);

					var data = new string(char.ToLower(c), 1024);
					data = string.Concat(data.Remove(data.Length - 1, 1), changed);

					var bytes = UTF8Encoding.UTF8.GetBytes(data);
					foreach (var bit in bytes)
						testZipOS.WriteByte(bit);

					testZipOS.Finish();
					testZipOS.Flush();
				}
			}

			private void ExerciseOutputBuilders(Differences diff)
			{
				Assert.IsNotNull(diff);

				this.ExerciseHtmlBuilder(diff);
				this.ExerciseJsonBuilder(diff);
				this.ExerciseXmlBuilder(diff);
				this.ExerciseXmlBuilder2(diff);
				this.ExerciseTextBuilder(diff);
				this.ExerciseZipBuilder(diff);
			}

			private void ExerciseHtmlBuilder(Differences diff)
			{
				Assert.IsNotNull(diff);

				using (var stream = new MemoryStream())
				using (var writer = new StreamWriter(stream))
				{
					var builder = new HtmlBuilder();
					builder.Build(writer, diff);

					Assert.IsTrue(writer.BaseStream.Length > 0);
				}
			}

			private void ExerciseJsonBuilder(Differences diff)
			{
				Assert.IsNotNull(diff);

				using (var stream = new MemoryStream())
				using (var writer = new StreamWriter(stream))
				{
					var builder = new JsonBuilder();
					builder.Build(writer, diff);

					Assert.IsTrue(writer.BaseStream.Length > 0);
				}
			}

			private void ExerciseXmlBuilder(Differences diff)
			{
				Assert.IsNotNull(diff);

				using (var stream = new MemoryStream())
				using (var writer = new StreamWriter(stream))
				{
					var builder = new XmlBuilder();
					builder.Build(writer, diff);

					Assert.IsTrue(writer.BaseStream.Length > 0);
				}
			}

			private void ExerciseXmlBuilder2(Differences diff)
			{
				Assert.IsNotNull(diff);

				using (var stream = new MemoryStream())
				using (var writer = new StreamWriter(stream))
				{
					var builder = new XmlBuilder2();
					builder.Build(writer, diff);

					Assert.IsTrue(writer.BaseStream.Length > 0);
				}
			}

			private void ExerciseTextBuilder(Differences diff)
			{
				Assert.IsNotNull(diff);

				using (var stream = new MemoryStream())
				using (var writer = new StreamWriter(stream))
				{
					var builder = new TextBuilder();
					builder.Build(writer, diff);

					Assert.IsTrue(writer.BaseStream.Length > 0);
				}
			}

			private void ExerciseZipBuilder(Differences diff)
			{
				Assert.IsNotNull(diff);

				using (var stream = new MemoryStream())
				using (var writer = new StreamWriter(stream))
				{
					var builder = new ZipBuilder();
					builder.Build(writer, diff);

					Assert.IsTrue(writer.BaseStream.Length > 0);
				}
			}
		}
	}
}