
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NameCounter {
	
	/*struct PrefixString {
		public string Data;
		
		public int PrefixLength;
		
		public int RepeatLength;
		
		public int FullRepeatCount {
			get { return RepeatLength / PrefixLength; }
		}
		
		public int FullRepeatLength {
			get { return FullRepeatCount * PrefixLength; }
		}
		
		public bool HasPartialRepeat {
			get { return FullRepeatLength < RepeatLength; }
		}
		
		public int RepeatCount {
			get {
				if (HasPartialRepeat)
					return FullRepeatCount + 1;
				else
					return FullRepeatCount;
			}
		}
		
		PrefixString(string s, int pLen, int rLen) {
			Data = s;
			PrefixLength = pLen;
			RepeatLength = rLen;
		}
		
		PrefixString(string s) {
			Data = s;
			PrefixLength = s.Length;
			RepeatLength = s.Length;
			
			
		}
		
		private void UpdatePrefixFields() {
			// ISSUE: I just realized that a string can start with (but not consist of)
			//        several repeated prefixes (that are not multiples of each other),
			//        e.g. "aaabaaabaacc". WAT DO? Stop trying to analyze them all and
			//        resort to a dumb solution (e.g. restart from position 1)?
		}
	}*/
	
	static class StreamSearcher {
		private struct PrefixData {
			public int Index;
			
			public int Length;
			
			public PrefixData(int index, int len) {
				Index = index;
				Length = len;
			}
		}
		
		private static PrefixData[] FindPrefixes(string s) {
			var prefixes = new List<PrefixData>();
			
			for (int index = 1; index < s.Length; index++) {
				if (s[index] == s[0]) {  // Found a repeated prefix.
					int length = 1;
					
					while (index + length < s.Length && s[index + length] == s[length])
						length++;
					
					prefixes.Add(new PrefixData(index, length));
				}
			}
			
			return prefixes.ToArray();
		}
		
		private static char ReadChar(StreamReader f, out bool success) {
			int ci = f.Read();  // Read the next character from the stream.
			
			if (ci < 0) {  // Failed to read from the stream.
				success = false;
				return '\0';
			}
			else {
				success = true;
				return Convert.ToChar(ci);
			}
		}
		
		private static int GetPrefixPos(PrefixData[] prefixes, int pos) {
			// If no matching prefix is found, abort the current matching attempt and
			// look for the next partial match (starting with the current char).
			int prefixPos = 0;
			
			// Check each repeating prefix in the query string.
			foreach (var prefix in prefixes) {
				// Break when the remaining prefixes start after the current partial match.
				if (prefix.Index >= pos)
					break;
				else if (prefix.Length >= pos - prefix.Index) {  // Found matching prefix.
					// Set query string position to continue the matching attempt from
					// the position of the matching prefix.
					prefixPos = pos - prefix.Index;
					break;
				}
			}
			
			return prefixPos;
		}
		
		public static int CountString(StreamReader f, string s) {
			PrefixData[] prefixes = FindPrefixes(s);
			
			bool hasChar;
			char c = ReadChar(f, out hasChar);  // Read the first character from the stream.
			int pos = 0, count = 0;
			
			while (hasChar) {
				if (c == s[pos]) {  // Current char matches, continue matching.
					pos++;
					
					if (pos >= s.Length) {  // Successful match!
						pos = 0;  // Look for the next match.
						
						if (count == int.MaxValue) {
							// TODO: Report overflow.
						}
						else
							count++;  // Increment match count.
					}
				}
				else if (pos > 0) {  // Failed partial match, check for repeating prefix match.
					// NOTE: We have to consider repeating prefixes even when doing
					//       a non-overlapping search. If the query string is "ababaC",
					//       then we must restart the match at position 4 in the string
					//       after finding "ababab" in the input stream.
					pos = GetPrefixPos(prefixes, pos);
					continue;  // Test current char again at the new query string position.
				}
				// else: No match in progress, keep looking for a match.
				
				c = ReadChar(f, out hasChar);  // Read the next character from the stream.
			}
			
			return count;
		}
		
		public static int CountString(Stream f, string s) {
			int count;
			
			using (StreamReader sr = new StreamReader(f)) {
				count = CountString(sr, s);
			}
			
			return count;
		}
		
		public static int CountStringOverlapping(StreamReader f, string s) {
			return -1;  // TODO: Implement this.
		}
		
		public static int CountStringOverlapping(Stream f, string s) {
			int count;
			
			using (StreamReader sr = new StreamReader(f)) {
				count = CountStringOverlapping(sr, s);
			}
			
			return count;
		}
	}
	
	static class NameCounterMain {
		static void Main(string[] args) {
			// TODO: Parse option arguments.
			// Options:
			//   -o, --overlapping    - Include overlapping instances (def: no).
			//   -p, --include-path   - Include path in default query string (def: no).
			//   -q, --query STR      - Query string (def: filename sans extension).
			//   -x, --include-ext    - Include extension in default query string (def: no).
			//   -z, --accept-missing - Treat nonexistent input files as empty instead
			//                          of having them trigger an error (def: no).
			
			// TODO: Handle multiple input files. Output the count for each file.
			
			if (args.Length < 1) {
				Console.WriteLine("No input file specified.");
				Environment.Exit(1);
			}
			
			string filePath = args[0];
			string queryString;
			
			if (args.Length >= 2)
				queryString = args[1];
			else
				queryString = Path.GetFileNameWithoutExtension(filePath);
			
			int count = -1;
			
			// Open input file.
			// TODO: If no filename specified, read standard input.
			using (FileStream f = File.Open(filePath, FileMode.Open, FileAccess.Read)) {
				// Count instances of the filename in the file contents.
				count = StreamSearcher.CountString(f, queryString);
			}
			
			// Print the count on standard output.
			Console.WriteLine($"Found {count:d} instances of the query string.");
		}
	}
}
