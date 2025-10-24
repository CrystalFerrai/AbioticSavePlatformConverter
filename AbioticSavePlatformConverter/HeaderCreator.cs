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

using System.Reflection;
using UeSaveGame;

namespace AbioticSavePlatformConverter
{
	/// <summary>
	/// Helper for creating a Steam save file header
	/// </summary>
	internal static class HeaderCreator
	{
		/// <summary>
		/// Create a Steam save file header using an existing save file as a template
		/// </summary>
		/// <remarks>
		/// The file will be output to Resources\SaveHeader.dat within the assembly directory. This is where
		/// the program expects to find it when running save conversions.
		/// </remarks>
		/// <param name="sourceSavePath">The Steam save file to use as a template</param>
		/// <param name="logger">For logging messages</param>
		/// <returns>Whether the file was successfully created</returns>
		public static bool Run(string sourceSavePath, Logger logger)
		{
			logger.Information("Generating a new SaveHeader.dat using the passed in Steam save file as a template.");

			if (!File.Exists(sourceSavePath))
			{
				logger.Error($"Unable to find or access source save file: {sourceSavePath}");
				return false;
			}

			SaveGame save;
			try
			{
				using FileStream file = File.OpenRead(sourceSavePath);
				save = SaveGame.LoadFrom(file);
			}
			catch (Exception ex)
			{
				logger.Error($"An error occurred while loading the save file at '{sourceSavePath}'. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			string outPath;
			try
			{
				outPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Resources/SaveHeader.dat"));
				using FileStream file = File.Create(outPath);
				using BinaryWriter writer = new(file);

				save.WritePart(SaveGamePart.Magic, writer);
				save.WritePart(SaveGamePart.Header, writer);
				save.WritePart(SaveGamePart.CustomFormats, writer);
			}
			catch (Exception ex)
			{
				logger.Error($"An error occurred while writing the output file. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			logger.Information($"Save file header created at: {outPath}");

			return true;
		}
	}
}
