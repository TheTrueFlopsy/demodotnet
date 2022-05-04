
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NameCounter {
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
		
		private static int GetPrefixPos(PrefixData[] prefixes, int pos) {
			// If no matching prefix is found, abort the current matching attempt and
			// look for the next partial match (starting with the current char).
			int prefixPos = 0;
			
			// Check each repeated prefix in the query string.
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
		
		public static int CountString(StreamReader f, string s, bool overlapMode) {
			if (s.Length == 0) {
				Console.WriteLine("Cannot search for the empty string.");
				return -1;
			}
			
			PrefixData[] prefixes = FindPrefixes(s);
			
			bool hasChar;
			char c = ReadChar(f, out hasChar);  // Read the first character from the stream.
			int pos = 0, count = 0;
			
			while (hasChar) {
				if (c == s[pos]) {  // Current char matches, continue matching.
					pos++;  // Increment length of partial match.
					
					if (pos >= s.Length) {  // Successful match!
						// Look for the next match.
						pos = (overlapMode) ? GetPrefixPos(prefixes, pos) : 0;
						
						if (count == int.MaxValue) {
							Console.WriteLine("Maximum count exceeded.");
							return -1;
						}
						else
							count++;  // Increment match count.
					}
				}
				else if (pos > 0) {  // Failed partial match, check for repeated prefix match.
					// NOTE: We have to consider repeated prefixes even when doing
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
		
		public static int CountString(Stream f, string s, bool overlapMode) {
			int count;
			
			using (StreamReader sr = new StreamReader(f)) {
				count = CountString(sr, s, overlapMode);
			}
			
			return count;
		}
	}
	
	static class NameCounterMain {
		private static bool printUsage = false;
		private static bool overlapMode = false;
		private static string queryString = null;
		private static bool acceptMissing = false;
		
		private static string GetOptionArgument(string[] args, ref int argIndex, ref int strIndex) {
			// Use the rest of the current CLI argument as the option argument.
			string arg = args[argIndex].Substring(strIndex);
			
			// Continue looking for options in the next CLI argument.
			argIndex++;
			strIndex = 1;
			
			if (arg.Length == 0) {  // At end of current CLI argument. Try the next one.
				if (argIndex >= args.Length)  // No more CLI arguments.
					return null;  // No option argument. Not the same thing as an empty argument.
				
				arg = args[argIndex];
				
				if (arg.StartsWith('-')) // Next CLI argument is an option.
					return null;  // No option argument.
				
				argIndex++;
			}
			
			return arg;
		}
		
		private static int ParseArgs(string[] args) {
			int argIndex = 0, strIndex = 1;
			bool endOfOptions = false;
			
			while (argIndex < args.Length && args[argIndex].StartsWith('-')) {
				if (strIndex >= args[argIndex].Length) {
					if (args[argIndex].Length == 1) {  // The argument is just a minus sign.
						Console.WriteLine("Missing option symbol.");
						return -1;
					}
					
					// No more options in this CLI argument.
					argIndex++;
					strIndex = 1;
					
					if (endOfOptions)
						break;  // Treat any remaining arguments as input files.
					else
						continue;  // Continue with the next argument.
				}
				
				char optionSymbol = args[argIndex][strIndex++];
				
				switch (optionSymbol) {
				case '-':
					endOfOptions = true;
					break;
				case 'h':
					printUsage = true;
					break;
				case 'o':
					overlapMode = true;
					break;
				case 'q':
					queryString = GetOptionArgument(args, ref argIndex, ref strIndex);
					break;
				case 'z':
					acceptMissing = true;
					break;
				default:
					Console.WriteLine($"Unrecognized option {optionSymbol}.");
					return -1;
				}
			}
			
			return argIndex;
		}
		
		private static void PrintUsage() {
			Console.WriteLine(@"Usage: NameCounter [OPTION]... FILE...
Count instances of a string in the specified text files. The default
behavior is to search for the name of each input file (sans extension).

Options (may be combined in a single argument (e.g. '-ozq hello')):
  --        End option parsing, treat remaining arguments as input files.
  -h        Print this message and exit.
  -o        Include overlapping instances (default: no).
  -q STR    Query string (default: input filename without extension).
  -z        Treat nonexistent input files as empty instead of having them
            trigger an error (default: no).");
		}
		
		static void Main(string[] args) {
			// Parse CLI arguments.
			int inputArgIndex = ParseArgs(args);
			
			if (inputArgIndex < 0) {  // Argument parsing failed.
				PrintUsage();
				Environment.Exit(2);
			}
			
			if (printUsage) {
				PrintUsage();
				return;
			}
			
			if (queryString != null && queryString.Length == 0) {
				Console.WriteLine("Cannot search for the empty string.");
				Environment.Exit(2);
			}
			
			if (inputArgIndex >= args.Length) {
				Console.WriteLine("No input file specified.");
				PrintUsage();
				Environment.Exit(2);
			}
			
			int count = 0;
			
			// Process the input file(s).
			while (inputArgIndex < args.Length) {
				string inputPath = args[inputArgIndex];
				string inputQuery;
				
				if (queryString == null)
					inputQuery = Path.GetFileNameWithoutExtension(inputPath);
				else
					inputQuery = queryString;
				
				// Open input file.
				bool notFound = false;
				FileStream f = null;
				
				try {
					f = File.Open(inputPath, FileMode.Open, FileAccess.Read);
				}
				catch (FileNotFoundException) {
					Console.WriteLine($"File not found: {inputPath}");
					notFound = true;
				}
				catch (DirectoryNotFoundException) {
					Console.WriteLine($"Directory not found: {inputPath}");
					notFound = true;
				}
				catch (ArgumentException) {
					Console.WriteLine($"Invalid path: {inputPath}");
				}
				catch (NotSupportedException) {
					Console.WriteLine($"Invalid path: {inputPath}");
				}
				catch (PathTooLongException) {
					Console.WriteLine($"Path is too long: {inputPath}");
				}
				catch (UnauthorizedAccessException) {
					Console.WriteLine($"File not accessible: {inputPath}");
				}
				
				// Count instances of the filename in the file contents.
				int inputCount = -1;
				
				if (f != null) {
					using (f) {
						inputCount = StreamSearcher.CountString(f, inputQuery, overlapMode);
					}
				}
				
				if (notFound && acceptMissing)
					inputCount = 0;
				
				if (inputCount < 0)  // Something went wrong.
					Environment.Exit(1);
				else if (inputCount > int.MaxValue - count) {
					Console.WriteLine("Maximum total count exceeded.");
					Environment.Exit(1);
				}
				else
					count += inputCount;
				
				inputArgIndex++;
			}
			
			// Print the count on standard output.
			Console.WriteLine($"Found {count:d} instances of the query string.");
		}
	}
}
