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

using AbioticSavePlatformConverter.Xbox;
using System.Reflection;
using System.Text;
using UeSaveGame;
using UeSaveGame.Util;

namespace AbioticSavePlatformConverter
{
	/// <summary>
	/// Helper for running the program
	/// </summary>
	internal class Converter
	{
		private Options mOptions;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Configures what the converter will do when calling Run</param>
		public Converter(Options options)
		{
			mOptions = options;
		}

		/// <summary>
		/// Runs the converter using the options passed into the constructor
		/// </summary>
		/// <param name="logger">For logging messages</param>
		/// <returns>Whether the program ran successfully</returns>
		/// <exception cref="InvalidOperationException">Unrecognized mode specified in the program options</exception>
		public bool Run(Logger logger)
		{
			switch (mOptions.Mode)
			{
				case ProgramMode.Find:
					return RunFind(logger);
				case ProgramMode.Convert:
					return RunConvert(logger);
				case ProgramMode.CreateHeader:
					return HeaderCreator.Run(mOptions.HeaderSource!, logger);
				case ProgramMode.PrintVersion:
					logger.Information(Assembly.GetExecutingAssembly().GetName().ToString());
					return true;
				default:
					throw new InvalidOperationException($"Invalid program mode value: {mOptions.Mode}");
			}
		}

		private bool RunFind(Logger logger)
		{
			string saveFolder = GetXboxSaveFolder();
			if (!Directory.Exists(saveFolder))
			{
				logger.Error("Unable to locate Microsoft Store save data folder for Abiotic Factor. This probably means that either the game is not installed from the Microsoft Store, or that it has not generated any save data yet while playing the game.");
				return false;
			}

			try
			{
				foreach (string profileDir in Directory.GetDirectories(saveFolder))
				{
					string profileId = Path.GetFileName(profileDir);
					profileId = profileId[..profileId.IndexOf('_')].TrimStart('0');

					XBoxContainerIndex containerIndex;
					try
					{
						containerIndex = XBoxContainerIndex.Load(profileDir);
					}
					catch (Exception ex)
					{
						logger.Warning($"Unable to load save data for profile {profileId}.");
						logger.Debug($"[{ex.GetType().FullName}] {ex.Message}");
						continue;
					}

					logger.Information($"Profile ID: {profileId}");
					foreach (XBoxContainer container in containerIndex.Containers)
					{
						// World saves end with '-WC'. Copies/backups end with '-WC-B' but we are skipping those
						if (container.Header.Id.Name1.EndsWith("-WC"))
						{
							logger.Information($"  {container.Header.Id.Name1[..^3]} [{container.Header.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")}]");
						}
					}
					logger.LogEmptyLine(LogLevel.Information);
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading save game folder. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			return true;
		}

		private bool RunConvert(Logger logger)
		{
			string inputPath, outputPath, profileFolder;
			if (mOptions.ConvertOptions.InputPath is not null)
			{
				inputPath = mOptions.ConvertOptions.InputPath;
				outputPath = mOptions.ConvertOptions.OutputPath;
				profileFolder = "Profile";
			}
			else if (mOptions.ConvertOptions.ProfileId is not null)
			{
				string saveFolder = GetXboxSaveFolder();
				if (!Directory.Exists(saveFolder))
				{
					logger.Error("Unable to locate Microsoft Store save data folder for Abiotic Factor. This probably means that either the game is not installed from the Microsoft Store, or that it has not generated any save data yet while playing the game.");
					return false;
				}

				string paddedProfileId = mOptions.ConvertOptions.ProfileId.PadLeft(16, '0');
				string profileDir = $"{paddedProfileId}_0000000000000000000000007B483EAA";
				inputPath = Path.Combine(saveFolder, profileDir);

				if (!Directory.Exists(inputPath))
				{
					logger.Error($"Unable to locate Abiotic Factor save data for profile id: {mOptions.ConvertOptions.ProfileId}");
					return false;
				}

				profileFolder = paddedProfileId;
				outputPath = mOptions.ConvertOptions.OutputPath;
			}
			else
			{
				throw new InvalidOperationException("ConvertOptions is missing data");
			}

			return DoConvert(inputPath, outputPath, profileFolder, logger);
		}

		private static bool DoConvert(string inputPath, string outputPath, string profileFolder, Logger logger)
		{
			try
			{
				string profileOutputPath = Path.Combine(outputPath, profileFolder);
				if (Directory.Exists(profileOutputPath))
				{
					Directory.Delete(profileOutputPath, true);
				}
				Directory.CreateDirectory(profileOutputPath);
			}
			catch (Exception ex)
			{
				logger.Error($"Unable to create or empty output directory '{outputPath}'. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			string headerPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources\\SaveHeader.dat");
			if (!File.Exists(headerPath))
			{
				logger.Error($"Could not locate save header resource at '{headerPath}'");
				return false;
			}

			byte[] saveFileHeader;
			try
			{
				saveFileHeader = File.ReadAllBytes(headerPath);
			}
			catch (Exception ex)
			{
				logger.Error($"An error occurred while reading the save header resource at '{headerPath}'. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			XBoxContainerIndex containerIndex;
			try
			{
				containerIndex = XBoxContainerIndex.Load(inputPath);
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading containers.index. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			if (!containerIndex.AppName.Equals("AppAbioticFactorShipping"))
			{
				throw new InvalidDataException($"Found data for '{containerIndex.AppName}' at input location. Expected to find 'AppAbioticFactorShipping'");
			}

			string? worldSavePath = null;
			foreach (XBoxContainer container in containerIndex.Containers)
			{
				// World saves end with '-WC'. Copies/backups end with '-WC-B' but we are skipping those
				if (container.Header.Id.Name1.EndsWith("-WC"))
				{
					worldSavePath = Path.Combine(inputPath, container.ContainerDirectory, container.Files[0].DataFileName);

					logger.Information($"Converting world: {container.Header.Id.Name1[..^3]}");

					if (!File.Exists(worldSavePath))
					{
						logger.Error($"Unable to find or access world save file at '{worldSavePath}'");
						continue;
					}

					try
					{
						ConvertFile(worldSavePath, outputPath, profileFolder, saveFileHeader, logger);
					}
					catch (Exception ex)
					{
						logger.Error($"An error occured while converting the world save file at '{worldSavePath}'. [{ex.GetType().FullName}] {ex.Message}");
						continue;
					}
				}
			}

			if (worldSavePath is null)
			{
				logger.Error("Unable to locate a world save file within the input location");
				return false;
			}

			logger.Information("Conversion successful.");

			return true;
		}

		private static void ConvertFile(string inPath, string outPath, string profileDir, byte[] saveFileHeader, Logger logger)
		{
			AbioticWorldContainer archive = AbioticWorldContainer.Load(inPath, logger);
			archive.ExportForSteam(outPath, profileDir, saveFileHeader, logger);
		}

		private static string GetXboxSaveFolder()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages\\PlayStack.AbioticFactor_3wcqaesafpzfy\\SystemAppData\\wgs");
		}
	}
}
