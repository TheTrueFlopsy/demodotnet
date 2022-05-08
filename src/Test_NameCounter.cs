
/**
	File: Test_NameCounter.cs
	Contains self-test code for the NameCounter tool.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace NameCounter {
	/**
		Class: SadUnit
		Sick and dirty improvised unit tester.
	*/
	static class SadUnit {
		/**
			Method: AssertTrue
			Tests whether a *bool* is true.
			
			Parameters:
				b - The *bool* to test.
				testName - Test name to print in the test log.
				failCond - Informational string to print when the test fails. Defaults to "false".
			
			Returns:
				Zero (0) if the test succeeds, one (1) if it fails.
		*/
		public static int AssertTrue(bool b, string testName, string failCond="false") {
			if (b) {
				Console.WriteLine($"{testName}: SUCCESS");
				return 0;
			}
			else {
				Console.WriteLine($"{testName}: FAILED ({failCond})");
				return 1;
			}
		}
		
		/**
			Method: AssertEqual
			Tests whether two objects are equal (as determined by EqualityComparer<T>.Default).
			
			Parameters:
				o1 - The first object to compare.
				o2 - The second object to compare.
				testName - Test name to print in the test log.
			
			Returns:
				Zero (0) if the test succeeds, one (1) if it fails.
		*/
		public static int AssertEqual<T>(T o1, T o2, string testName) {
			var comparer = EqualityComparer<T>.Default;
			return AssertTrue(comparer.Equals(o1, o2), testName, $"{o1} != {o2}");
		}
		
		/**
			Method: AssertArrayEqual
			Tests whether two arrays are equal. The arrays are considered equal if and only if
			they have the same length and their elements are pairwise equal (as determined by
			EqualityComparer<T>.Default).
			
			Parameters:
				a1 - The first array to compare.
				a2 - The second array to compare.
				testName - Test name to print in the test log.
			
			Returns:
				Zero (0) if the test succeeds, one (1) if it fails.
		*/
		public static int AssertArrayEqual<T>(T[] a1, T[] a2, string testName) {
			// NOTE: To be honest, I find it deplorable that to this day there is
			//       apparently no convenient, built-in way to compare the content
			//       of two arrays in C# (e.g. "a1.Equals(a2)").
			//
			//       It is also my strong opinion that one shouldn't have to
			//       resort to cruft like "EqualityComparer<T>.Default"
			//       to check two objects of the same known type for value equality.
			var comparer = EqualityComparer<T>.Default;
			string failCond = "false";
			
			bool success = (a1.Length == a2.Length);
			if (!success)
				failCond = $"a1.Length={a1.Length:d} != a2.Length={a2.Length:d}";
			
			for (int i = 0; success && i < a1.Length; i++) {
				success &= comparer.Equals(a1[i], a2[i]);
				if (!success)
					failCond = $"a1[{i:d}]={a1[i]} != a2[{i:d}]={a2[i]}";
			}
			
			return AssertTrue(success, testName, failCond);
		}
	}
	
	
	// Unit test code for the StreamSearcher.
	static partial class StreamSearcher {
		public static int Test_FindPrefixes() {
			int res = 0;
			string inputStr;
			PrefixData[] expected, actual;
			
			inputStr = "";
			expected = new PrefixData[] {};
			actual = FindPrefixes(inputStr);
			res += SadUnit.AssertArrayEqual(expected, actual, $"FindPrefixes({inputStr})");
			
			inputStr = "abcd";
			expected = new PrefixData[] {};
			actual = FindPrefixes(inputStr);
			res += SadUnit.AssertArrayEqual(expected, actual, $"FindPrefixes({inputStr})");
			
			inputStr = "xxx";
			expected = new PrefixData[] { new PrefixData(1, 2) };
			actual = FindPrefixes(inputStr);
			res += SadUnit.AssertArrayEqual(expected, actual, $"FindPrefixes({inputStr})");
			
			inputStr = "aabaaabaaa";
			expected = new PrefixData[] {
				new PrefixData(1, 1), // "a"
				new PrefixData(3, 2), // "aa"
				new PrefixData(4, 6)  // "aabaaa"
			};
			actual = FindPrefixes(inputStr);
			res += SadUnit.AssertArrayEqual(expected, actual, $"FindPrefixes({inputStr})");
			
			inputStr = "abCabDabE";
			expected = new PrefixData[] {
				new PrefixData(3, 2), // "ab"
				new PrefixData(6, 2)  // "ab"
			};
			actual = FindPrefixes(inputStr);
			res += SadUnit.AssertArrayEqual(expected, actual, $"FindPrefixes({inputStr})");
			
			return res;
		}
		
		public static int Test_GetPrefixPos() {
			int res = 0;
			int actual;
			PrefixData[] prefixes = new PrefixData[] {
				new PrefixData(1, 1), // "a"
				new PrefixData(3, 2), // "aa"
				new PrefixData(4, 6), // "aabaaa"
				new PrefixData(5, 1), // "a"
				new PrefixData(7, 2), // "aa"
				new PrefixData(8, 2), // "aa"
				new PrefixData(9, 1)  // "a"
			};
			
			actual = GetPrefixPos(prefixes, 2);
			res += SadUnit.AssertEqual(1, actual, "GetPrefixPos(aabaaabaaa, 2)");
			
			actual = GetPrefixPos(prefixes, 3);
			res += SadUnit.AssertEqual(0, actual, "GetPrefixPos(aabaaabaaa, 3)");
			
			actual = GetPrefixPos(prefixes, 4);
			res += SadUnit.AssertEqual(1, actual, "GetPrefixPos(aabaaabaaa, 4)");
			
			actual = GetPrefixPos(prefixes, 7);
			res += SadUnit.AssertEqual(3, actual, "GetPrefixPos(aabaaabaaa, 7)");
			
			actual = GetPrefixPos(prefixes, 9);
			res += SadUnit.AssertEqual(5, actual, "GetPrefixPos(aabaaabaaa, 9)");
			
			return res;
		}
		
		public static int Test_TryReadChar() {
			return 0;  // TODO: Implement this.
		}
		
		public static int Test_CountString() {
			int res = 0;
			string inputPath;
			int actual;
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			using (FileStream f = File.Open(inputPath, FileMode.Open, FileAccess.Read)) {
				actual = CountString(f, "period", false);
			}
			res += SadUnit.AssertEqual(6, actual, "CountString(prefixes.txt, period, false)");
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			using (FileStream f = File.Open(inputPath, FileMode.Open, FileAccess.Read)) {
				actual = CountString(f, "aa", false);
			}
			res += SadUnit.AssertEqual(81, actual, "CountString(prefixes.txt, aa, false)");
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			using (FileStream f = File.Open(inputPath, FileMode.Open, FileAccess.Read)) {
				actual = CountString(f, "aa", true);
			}
			res += SadUnit.AssertEqual(112, actual, "CountString(prefixes.txt, aa, true)");
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			using (FileStream f = File.Open(inputPath, FileMode.Open, FileAccess.Read)) {
				actual = CountString(f, "aabaaabaaa", false);
			}
			res += SadUnit.AssertEqual(1, actual, "CountString(prefixes.txt, aabaaabaaa, false)");
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			using (FileStream f = File.Open(inputPath, FileMode.Open, FileAccess.Read)) {
				actual = CountString(f, "abCabDabE", false);
			}
			res += SadUnit.AssertEqual(3, actual, "CountString(prefixes.txt, abCabDabE, false)");
			
			inputPath = Path.Join("..", "test", "unicode.txt");
			using (FileStream f = File.Open(inputPath, FileMode.Open, FileAccess.Read)) {
				actual = CountString(f, "ий", false);
			}
			res += SadUnit.AssertEqual(4, actual, "CountString(unicode.txt, ий, false)");
			
			inputPath = Path.Join("..", "test", "unicode.txt");
			using (FileStream f = File.Open(inputPath, FileMode.Open, FileAccess.Read)) {
				actual = CountString(f, "😃", false);
			}
			res += SadUnit.AssertEqual(1, actual, "CountString(unicode.txt, 😃, false)");
			
			return res;
		}
		
		public static int Test_AllTests() {
			int res = 0;
			res += Test_FindPrefixes();
			res += Test_GetPrefixPos();
			res += Test_TryReadChar();
			res += Test_CountString();
			return res;
		}
	}
	
	
	// Self-test code for the NameCounter.
	static partial class NameCounterMain {
		private static int Test_DoWork() {
			int res = 0;
			string inputPath, inputPath2;
			string[] args;
			int actual;
			
			inputPath = "NameCounter.cs";
			args = new string[] { inputPath };  // Search own source for own name.
			actual = DoWork(args);
			res += SadUnit.AssertEqual(0, actual, "DoWork(NameCounter.cs)");
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			args = new string[] { "-q", "aba", inputPath };
			actual = DoWork(args);
			res += SadUnit.AssertEqual(0, actual, "DoWork(-q, aba, prefixes.txt)");
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			args = new string[] { "-oq", "aba", inputPath };
			actual = DoWork(args);
			res += SadUnit.AssertEqual(0, actual, "DoWork(-oq, aba, prefixes.txt)");
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			args = new string[] { "-#oq", "aba", inputPath };
			actual = DoWork(args);
			res += SadUnit.AssertEqual(2, actual, "DoWork(-#oq, aba, prefixes.txt)");
			
			inputPath = Path.Join("..", "test", "prefixes.txt");
			inputPath2 = Path.Join("..", "test", "newton.txt");
			args = new string[] { "-q", "aba", inputPath, inputPath2 };
			actual = DoWork(args);
			res += SadUnit.AssertEqual(0, actual, "DoWork(-q, aba, (prefixes.txt, newton.txt))");
			
			inputPath = Path.Join("..", "test", "not_there.txt");
			args = new string[] { inputPath };
			actual = DoWork(args);
			res += SadUnit.AssertEqual(1, actual, "DoWork(not_there.txt)");
			
			inputPath = Path.Join("..", "test", "not_there.txt");
			args = new string[] { "-z", inputPath };
			actual = DoWork(args);
			res += SadUnit.AssertEqual(0, actual, "DoWork(-z, not_there.txt)");
			
			return res;
		}
	}
}
