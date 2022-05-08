
/**
	File: NameCounter.cs
	Contains the implementation of the NameCounter tool.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace NameCounter {
	/**
		Class: StreamSearcher
		Implements an algorithm that counts instances of a given string
		in a text stream. Supports both non-overlapping (where "ABBABBA"
		contains a single instance of "ABBA") and overlapping (where "ABBABBA"
		contains two instances of "ABBA") search. This class is stateless.
	*/
	static partial class StreamSearcher {
		/**
			Inner Class: PrefixData
			Represents a substring as a start index and a length. Used to keep
			track of repeated prefixes in query strings.
		*/
		private struct PrefixData {
			public int Index;
			
			public int Length;
			
			public int EndIndex {
				get { return Index + Length; }
			}
			
			public PrefixData(int index, int len) {
				Index = index;
				Length = len;
			}
			
			public override string ToString() {
				return $"({Index:d}, {Length:d})";
			}
		}
		
		/**
			Method: FindPrefixes
			Finds all the repeated prefixes in a given string that are not contained
			in another repeated prefix. There is a repeated prefix of length *L* at
			index *i* in *s* if and only if *i > 0* and *s[i+k] == s[k]* for all *k*
			between zero (inclusive) and *L* (exclusive), but either *s[i+L] != s[L]*
			or *s* ends at index *i+L*.
			
			Parameters:
				s - The string to look for repeated prefixes in.
			
			Returns:
				An array containing all repeated prefixes of *s* that are not contained in
				another repeated prefix, in ascending start index order.
		*/
		private static PrefixData[] FindPrefixes(string s) {
			var prefixes = new List<PrefixData>();
			var prevPrefix = new PrefixData(0, 0);  // Dummy prefix.
			
			for (int index = 1; index < s.Length; index++) {
				if (s[index] == s[0]) {  // Found a repeated prefix.
					int length = 1;
					
					while (index + length < s.Length && s[index + length] == s[length])
						length++;
					
					// Prefixes that are contained in other prefixes will never be selected
					// by GetPrefixPos. Avoid generating those prefixes.
					var newPrefix = new PrefixData(index, length);
					
					if (newPrefix.EndIndex > prevPrefix.EndIndex) {
						// Prefix not contained in preceding prefix, add it to the list.
						prefixes.Add(newPrefix);
						prevPrefix = newPrefix;
					}
				}
			}
			
			return prefixes.ToArray();
		}
		
		/**
			Method: GetPrefixPos
			Takes a description of the repeated prefixes in a string and some
			index in that string as arguments. Returns the difference between the
			specified index and the start index of the first repeated prefix of the
			string that starts before the specified index and ends after or at the
			specified index, or zero, if no such prefix exists.
			
			Parameters:
				prefixes - An array of <PrefixData> that describes the repeated prefixes
				           in some string.
				pos - An index in the string described by *prefixes*.
			
			Returns:
				The length of the (truncated) repeated prefix of the described string
				that ends at index *pos* in the string, or zero, if there is no such prefix.
		*/
		private static int GetPrefixPos(PrefixData[] prefixes, int pos) {
			// If no matching repeated prefix is found, return zero.
			int prefixPos = 0;
			
			// Check each repeated prefix in the string.
			foreach (var prefix in prefixes) {
				// Break when the remaining prefixes start after the specified position.
				if (prefix.Index >= pos)
					break;
				else if (prefix.EndIndex >= pos) {  // Found matching prefix.
					// Return the length of the matching prefix, truncated to end at
					// the specified position.
					prefixPos = pos - prefix.Index;
					break;
				}
			}
			
			return prefixPos;
		}
		
		/**
			Method: TryReadChar
			Helper method that attempts to read a character from a *StreamReader* and
			returns true if successful.
			
			Parameters:
				f - The *StreamReader* to read from.
				c - *[Out]* The character that was read, or a null character if reading
				    failed.
			
			Returns:
				True if and only if a character was successfully read.
		*/
		private static bool TryReadChar(StreamReader f, out char c) {
			int ci = f.Read();  // Read the next character from the stream.
			
			if (ci < 0) {  // Failed to read from the stream.
				c = '\0';
				return false;
			}
			else {
				c = Convert.ToChar(ci);
				return true;
			}
		}
		
		/**
			Method: CountString
			Counts instances of a specified string in text read from a *StreamReader*.
			
			Parameters:
				f - The *StreamReader* to read from.
				s - The string to count instances of.
				overlapMode - Flag indicating whether to count overlapping instances
				              (e.g. the second "ABBA" in "ABBABBA").
			
			Returns:
				The number of instances of the specified string that were found.
		*/
		public static int CountString(StreamReader f, string s, bool overlapMode) {
			if (s.Length == 0) {
				Console.WriteLine("Cannot search for the empty string.");
				return -1;
			}
			
			// Identify and collect repeated prefixes in the query string.
			PrefixData[] prefixes = FindPrefixes(s);
			
			char c;
			bool hasChar = TryReadChar(f, out c);  // Read the first character from the stream.
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
				
				hasChar = TryReadChar(f, out c);  // Read the next character from the stream.
			}
			
			return count;
		}
		
		/**
			Method: CountString
			Counts instances of a specified string in text read from a *Stream*.
			Internally wraps the *Stream* in a *StreamReader*.
			
			Parameters:
				f - The *Stream* to read from.
				s - The string to count instances of.
				overlapMode - Flag indicating whether to count overlapping instances
				              (e.g. the second "ABBA" in "ABBABBA").
			
			Returns:
				The number of instances of the specified string that were found.
		*/
		public static int CountString(Stream f, string s, bool overlapMode) {
			int count;
			
			// This contructor assumes UTF-8 encoding.
			// ISSUE: Do we need to deal with other encodings? How?
			//        There could be an option to explicitly specify
			//        another encoding. Does the .NET platform provide
			//        auto-detection of encodings?
			using (StreamReader sr = new StreamReader(f)) {
				count = CountString(sr, s, overlapMode);
			}
			
			return count;
		}
	}
	
	
	/**
		Class: NameCounterMain
		Implements the command-line user interface of the filename counter.
		Supports multiple input files and searching for an arbitrary string
		instead of the file name. Provides a self-test mode. Note that the
		self-test mode expects to be run from the "src/" subdirectory of
		the Git repository and also that specific test input files are
		present in the "test/" subdirectory.
	*/
	static partial class NameCounterMain {
		private static bool printUsage = false;
		private static bool overlapMode = false;
		private static string queryString = null;
		private static bool acceptMissing = false;
		private static bool runTests = false;
		
		/**
			Method: ResetConfig
			Resets all configuration variables to their default values.
		*/
		private static void ResetConfig() {
			printUsage = false;
			overlapMode = false;
			queryString = null;
			acceptMissing = false;
			runTests = false;
		}
		
		/**
			Method: GetOptionArgument
			Attempts to get an option argument for a command-line option.
			First looks for the option argument in the remaining characters of the current
			CLI argument. If there are no remaining characters, then looks at the next CLI
			argument. If that argument doesn't look like it contains more options, it is
			assumed to be the sought option argument.
			
			Parameters:
				args - A CLI argument sequence.
				argIndex - *[Ref]* The index in *args* of the current CLI argument.
				strIndex - *[Ref]* The current character index in *args[argIndex]*.
			
			Returns:
				An option argument string, or *null* if no option argument was found.
		*/
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
		
		/**
			Method: ParseArgs
			Parses and handles a CLI argument sequence.
			
			Parameters:
				args - A CLI argument sequence.
			
			Returns:
				The index of the first non-option argument in the sequence, unless there were
				no non-option arguments or an error occurred. If there were no non-option
				arguments, returns the length of the sequence. If an error occurred, returns
				a negative value.
		*/
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
				case 'T':
					runTests = true;
					break;
				default:
					Console.WriteLine($"Unrecognized option {optionSymbol}.");
					return -1;
				}
			}
			
			return argIndex;
		}
		
		/**
			Method: DoPrintUsage
			Prints a brief help text for the *NameCounter* on standard output.
		*/
		private static void DoPrintUsage() {
			Console.WriteLine(@"Usage: NameCounter.exe [OPTION]... FILE...
Count instances of a string in the specified text files. The default
behavior is to search for the name of each input file (sans extension).

Options (may be combined in a single argument (e.g. '-ozq hello')):
  --        End option parsing, treat remaining arguments as input files.
  -h        Print this message and exit.
  -o        Include overlapping instances of the query string (default: no).
  -q STR    Use the specified query string (default: input filename without
            extension).
  -z        Treat nonexistent input files as empty instead of having them
            trigger an error (default: no).
  -T        Run self tests and exit.");
		}
		
		/**
			Method: DoRunTests
			Runs the *NameCounter* self-test suite.
			
			Returns:
				A process exit status value that is zero if and only if all tests
				were successful.
		*/
		private static int DoRunTests() {
			int res = 0;
			
			Console.WriteLine("Running self-test suite...");
			
			res += StreamSearcher.Test_AllTests();
			
			res += Test_DoWork();
			
			if (res == 0)
				Console.WriteLine("ALL TESTS SUCCESSFUL");
			else
				Console.WriteLine($"FAILURE, {res:d} TESTS FAILED");
			
			return (res == 0) ? 0 : 1;
		}
		
		/**
			Method: DoWork
			Runs the *NameCounter* as instructed by the specified CLI argument sequence.
			
			Parameters:
				args - A CLI argument sequence.
			
			Returns:
				A process exit status value that is zero if and only if execution finished
				without errors.
		*/
		private static int DoWork(string[] args) {
			// Parse CLI arguments.
			ResetConfig();
			int inputArgIndex = ParseArgs(args);
			
			if (inputArgIndex < 0) {  // Argument parsing failed.
				DoPrintUsage();
				return 2;
			}
			
			if (printUsage) {
				DoPrintUsage();
				return 0;
			}
			
			if (runTests)
				return DoRunTests();
			
			if (queryString != null && queryString.Length == 0) {
				Console.WriteLine("Cannot search for the empty string.");
				return 2;
			}
			
			if (inputArgIndex >= args.Length) {
				Console.WriteLine("No input file specified.");
				DoPrintUsage();
				return 2;
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
					return 1;
				else if (inputCount > int.MaxValue - count) {
					Console.WriteLine("Maximum total count exceeded.");
					return 1;
				}
				else
					count += inputCount;
				
				inputArgIndex++;
			}
			
			// Print the count on standard output.
			Console.WriteLine($"Found {count:d} instances of the query string.");
			return 0;
		}
		
		/**
			Method: Main
			Entry point of the *NameCounter* application. Lets <DoWork> do the heavy lifting.
			
			Parameters:
				args - A CLI argument sequence.
			
			Returns:
				A process exit status value that is zero if and only if execution finished
				without errors.
		*/
		public static int Main(string[] args) {
			return DoWork(args);
		}
	}
}
