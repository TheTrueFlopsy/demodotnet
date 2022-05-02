
using System;
using System.IO;
using System.Text;

namespace NameCounter {
	
	struct PrefixString {
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
		
		PrefixInfo(string s, int pLen, int rLen) {
			Data = s;
			PrefixLength = pLen;
			RepeatLength = rLen;
		}
		
		PrefixInfo(string s) {
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
	}
	
	static class StreamSearcher {
		static 
		
		// ISSUE: How to deal with overlapping instances of the sought string
		//        (i.e. does "aaaa" contain one or two instances of "aaa"?)?
		//        I think I'll start by implementing non-overlapping search
		//        (because that's easier and sometimes what is intuitively
		//        expected) and then consider overlapping search as an option.
		// TODO: Implement this.
		static int CountString(StreamReader f, string s) {
			int ci = -1, k = 0, count = 0;
			
			while ((ci = f.Read()) >= 0) {
				char c = Convert.ToChar(ci);
				
				if (c == s[k]) {
					k++;
					
					if (k >= s.Length) {
						if (count == int.MaxValue) {
							// TODO: Report overflow.
						}
						else {
							count++;
							k = 0;
						}
					}
				}
				else {
					// FIXME: We have to consider repeating prefixes even when doing
					//        a non-overlapping search. If N is "ababaC", then we must
					//        restart the match at position 2 in N after finding "ababab" in H.
					// IDEA: Is it always sufficient to continue the match with the unmatching
					//       character, now at position k-|P| in N, unless k-|P| is not in the
					//       repeating-prefix part of N (then start at position 0)?
				}
			}
		}
		
		static int CountString(Stream f, string s) {
			int res;
			
			using (StreamReader sr = new StreamReader(f)) {
				res = CountString(sr, s);
			}
			
			return res;
		}
		
		// TODO: Implement this.
		static int CountStringOverlapping(StreamReader f, string s) {
			// Check whether the string 's' consists of a repeating prefix.
			// If it doesn't, then there are no overlapping instances and
			// a non-overlapping search is sufficient.
		}
		
		static int CountStringOverlapping(Stream f, string s) {
			int res;
			
			using (StreamReader sr = new StreamReader(f)) {
				res = CountStringOverlapping(sr, s);
			}
			
			return res;
		}
	}
	
	static class NameCounterMain {
		
		static void Main(string[] args) {
			// TODO: Parse arguments.
			// Options:
			//   -o, --overlapping    - Include overlapping instances (def: no).
			//   -p, --include-path   - Include path in default query string (def: no).
			//   -q, --query STR      - Query string (def: filename sans extension).
			//   -x, --include-ext    - Include extension in default query string (def: no).
			//   -z, --accept-missing - Treat nonexistent input files as empty instead
			//                          of having them trigger an error (def: no).
			
			// TODO: Open input file. (If no filename specified, read standard input.)
			// IDEA: Handle multiple input files. Output the count for each file.
			
			// TODO: Count instances of the filename in the file contents.
			
			// TODO: Print the count on standard output.
		}
	}
}
