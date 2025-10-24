// Copyright 2025 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics.CodeAnalysis;

namespace AbioticSavePlatformConverter
{
	/// <summary>
	/// Utility for parsing and validating command line arguments
	/// </summary>
	internal class Options
	{
		/// <summary>
		/// Specifies what the program should do
		/// </summary>
		public ProgramMode Mode { get; }

		/// <summary>
		/// Options for ProgramMode.Convert
		/// </summary>
		public ConvertOptions ConvertOptions { get; }

		/// <summary>
		/// Options for ProgramMode.CreateHeader
		/// </summary>
		public string? HeaderSource { get; }

		public Options(ProgramMode mode, ConvertOptions convertOptions, string? headerSource)
		{
			Mode = mode;
			ConvertOptions = convertOptions;
			HeaderSource = headerSource;
		}

		/// <summary>
		/// Parse and validate command line arguments
		/// </summary>
		/// <param name="args">Command line arguments to parse</param>
		/// <param name="logger">For logging messages</param>
		/// <param name="result">The resulting options if parsing and validation were successful</param>
		/// <returns>Whether parsing and validation were successful. On failure, specific errors will be printed to the passed in logger.</returns>
		public static bool TryParse(string[] args, Logger logger, [NotNullWhen(true)] out Options? result)
		{
			result = null;

			if (args.Length == 0)
			{
				return false;
			}

			string? inputPath = null;
			string? outputPath = null;
			bool find = false;
			string? profileId = null;
			string? headerSource = null;
			bool version = false;

			for (int i = 0; i < args.Length; ++i)
			{
				if (args[i].StartsWith("--"))
				{
					string arg = args[i][2..].ToLowerInvariant();
					switch (arg)
					{
						case "in":
							inputPath = GetArgParam(args, ref i);
							if (inputPath is null)
							{
								logger.Error("Missing argument for option '--in'");
								return false;
							}
							break;
						case "out":
							outputPath = GetArgParam(args, ref i);
							if (outputPath is null)
							{
								logger.Error("Missing argument for option '--out'");
								return false;
							}
							break;
						case "find":
							find = true;
							break;
						case "profile":
							profileId = GetArgParam(args, ref i);
							if (profileId is null)
							{
								logger.Error("Missing argument for option '--profile'");
								return false;
							}
							break;
						case "create-header":
							headerSource = GetArgParam(args, ref i);
							if (headerSource is null)
							{
								logger.Error("Missing argument for option '--create-header'");
								return false;
							}
							break;
						case "version":
							version = true;
							break;
						default:
							logger.Error($"Unrecognized option: {args[i]}");
							return false;
					}
				}
				else
				{
					logger.Error($"Unexpected parameter: {args[i]}");
					return false;
				}
			}

			bool[] oneRequired = [inputPath is not null, find, profileId is not null, headerSource is not null, version];
			int count = oneRequired.Count(b => b);

			if (count == 0)
			{
				logger.Error("Must specify at least one of [--in, --find, --profile, --create-header, --version]");
				return false;
			}
			if (count > 1)
			{
				logger.Error("Must specify only one of [--in, --find, --profile, --create-header, --version]");
				return false;
			}

			if (find)
			{
				if (args.Length > 1)
				{
					logger.Error("Should not specify any additional parameters along with --find");
					return false;
				}
				result = new(ProgramMode.Find, default, null);
				return true;
			}

			if (headerSource is not null)
			{
				result = new(ProgramMode.CreateHeader, default, headerSource);
				return true;
			}

			if (version)
			{
				result = new(ProgramMode.PrintVersion, default, null);
				return true;
			}

			if (outputPath is null)
			{
				logger.Error($"Must pass --output parameter when using --in or --profile to perform a conversion");
				return false;
			}

			if (inputPath is not null)
			{
				try
				{
					inputPath = Path.GetFullPath(inputPath);
				}
				catch (Exception ex)
				{
					logger.Error($"Unable to interpret input path. Value: '{inputPath}'. [{ex.GetType().FullName}] {ex.Message}");
					return false;
				}

				if (!Directory.Exists(inputPath))
				{
					logger.Error($"Input directory does not exist or is not accessible. Value: '{inputPath}'");
					return false;
				}
			}

			try
			{
				outputPath = Path.GetFullPath(outputPath);
			}
			catch (Exception ex)
			{
				logger.Error($"Unable to interpret output path. Value: '{outputPath}'. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			result = new(ProgramMode.Convert, new() { InputPath = inputPath, ProfileId = profileId, OutputPath = outputPath }, null);
			return true;
		}

		/// <summary>
		/// Print program usage to the passed in logger
		/// </summary>
		public static void PrintUsage(Logger logger)
		{
			logger.Information(
				"Converts an Abiotic Factor world save from [XBox / MS Store] format to\n" +
				"[Steam / Dedicated Server] format.\n" +
				"\n" +
				"Usage: AbioticSavePlatformConverter [options]\n" +
				"\n" +
				"Options\n" +
				"\n" +
				"  --in [path]     The path to a directory containing a save to convert.\n" +
				"\n" +
				"  --out [path]    The path to the directory to output the converted save.\n" +
				"\n" +
				"  --find          Search the system for save game files created by the\n" +
				"                  MS Store version of Abiotic Factor and list the results.\n" +
				"\n" +
				"  --profile [id]  The ID of a user profile containing Abiotic Factor saves\n" +
				"                  to use as the input. Use this in place of --in.\n" +
				"                  To find profile IDs, run with the --find option.\n" +
				"\n" +
				"  --create-header [source]  Generates a SaveHeader.dat file using the\n" +
				"                            given Steam save file as a template.\n" +
				"\n" +
				"  --version       Print program version information.\n" +
				"\n" +
				"Note: If the game updates and the Steam save file header changes, you may\n" +
				"need to generate a new SaveHeader.dat to replace the one that ships with\n" +
				"this tool. You can do this by running with the option --create-header and\n" +
				"passing in the path to any .sav file from a world save recently updated by\n" +
				"the Steam version of Abiotic Factor, or by a dedicated server. The Steam\n" +
				"version saves to '%LocalAppData%\\AbioticFactor\\Saved\\SaveGames\\[SteamID]'.\n" +
				"Usually this should not be required for most game updates as the game\n" +
				"should be able to read earlier saves."
			);
		}

		private static string? GetArgParam(string[] args, ref int index)
		{
			++index;
			if (index >= args.Length)
			{
				return null;
			}
			string result = args[index];
			if (result.StartsWith("--"))
			{
				--index;
				return null;
			}
			return args[index];
		}
	}

	/// <summary>
	/// Specifies what the program should do when run
	/// </summary>
	internal enum ProgramMode
	{
		/// <summary>
		/// Find MS Store Abiotic Factor saves and print information about them, including the associated profile IDs
		/// </summary>
		Find,

		/// <summary>
		/// Perform a conversion from an XBox formatted save to a Steam formatted save
		/// </summary>
		Convert,

		/// <summary>
		/// Generate a new SaveHeader.dat file from an existing Steam save file
		/// </summary>
		CreateHeader,

		/// <summary>
		/// Prints the program version
		/// </summary>
		PrintVersion
	}

	/// <summary>
	/// Options for the Convert program mode
	/// </summary>
	internal struct ConvertOptions
	{
		/// <summary>
		/// Path to the source save data directory, if specified
		/// </summary>
		public string? InputPath;

		/// <summary>
		/// The XUID of the profile to lookup source save data from
		/// </summary>
		public string? ProfileId;

		/// <summary>
		/// Path to where converted save data should be written
		/// </summary>
		public string OutputPath;
	}
}
