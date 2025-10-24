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

using EpicGames.Compression;
using System.Text;
using UeSaveGame;
using UeSaveGame.Util;

namespace AbioticSavePlatformConverter
{
	/// <summary>
	/// The data for an Abiotic Factor world save in the XBox save data format
	/// </summary>
	internal class AbioticWorldContainer
	{
		private static readonly FString VersionPropertyName = new("ABF_SAVE_VERSION");

		public int Version { get; }

		public AbioticSaveFileHeader[] FileHeaders { get; }

		public byte[] Data { get; }

		private AbioticWorldContainer(int version, AbioticSaveFileHeader[] fileHeaders, byte[] data)
		{
			Version = version;
			FileHeaders = fileHeaders;
			Data = data;
		}

		/// <summary>
		/// Load a world container
		/// </summary>
		/// <param name="path">The path to the file containting the world data</param>
		/// <param name="logger">For logging messages</param>
		/// <returns>The loaded container</returns>
		/// <exception cref="InvalidDataException">Unable to parse the input file</exception>
		public static AbioticWorldContainer Load(string path, Logger logger)
		{
			using FileStream file = File.OpenRead(path);
			using BinaryReader reader = new(file, Encoding.ASCII, true);

			FString? versionPropertyName = reader.ReadUnrealString();
			if (versionPropertyName is null || versionPropertyName != VersionPropertyName)
			{
				throw new InvalidDataException($"Save file is missing expected header {VersionPropertyName}");
			}

			int version = reader.ReadInt32(); // 3
			int decompressedSize = reader.ReadInt32();

			_ = reader.ReadInt32(); // 16

			int count = reader.ReadInt32();
			AbioticSaveFileHeader[] fileHeaders = new AbioticSaveFileHeader[count];
			for (int i = 0; i < count; ++i)
			{
				fileHeaders[i] = AbioticSaveFileHeader.Read(reader);
			}

			_ = reader.ReadInt32(); // 1

			int compressedSize = reader.ReadInt32();

			byte[] compressedData = reader.ReadBytes(compressedSize);
			byte[] decompressedData = new byte[decompressedSize];

			Oodle.Decompress(compressedData, decompressedData);

			return new(version, fileHeaders, decompressedData);
		}

		/// <summary>
		/// Exports the world container to a directory containing the world in the Steam save format
		/// </summary>
		/// <param name="directory">The output directory path (must exist)</param>
		/// <param name="profileFolder">The name of the profile folder within the output directory to export to (must exist)</param>
		/// <param name="fileHeader">The Steam file header to prepend to all output .sav files</param>
		/// <param name="logger">For logging messages</param>
		public void ExportForSteam(string directory, string profileFolder, byte[] fileHeader, Logger logger)
		{
			using MemoryStream stream = new(Data, false);

			foreach (AbioticSaveFileHeader header in FileHeaders)
			{
				string outPath;

				if (header.Class is null)
				{
					// This entry is likely not included in the save, but pulled in from an external directory.
					// I have only seen this occur with the SandboxSettings.ini file.
					if (header.Path.Value.EndsWith("SandboxSettings.ini", StringComparison.InvariantCultureIgnoreCase))
					{
						string inPath = Path.GetFullPath(header.Path);
						if (File.Exists(inPath))
						{
							outPath = Path.Combine(directory, profileFolder, inPath.Substring(inPath.IndexOf("\\Profile\\") + 9));
							File.Copy(inPath, outPath);
						}
						else
						{
							logger.Warning($"Unable to include SandboxSettings.ini because input file is not accessible at: {inPath}");
						}
					}

					stream.Seek(header.Size, SeekOrigin.Current);
					continue;
				}

				string sourcePath = header.Path;
				if (sourcePath.StartsWith("Profile/", StringComparison.InvariantCultureIgnoreCase))
				{
					sourcePath = sourcePath[8..];
				}
				else
				{
					logger.Debug($"Source path does not begin with 'Profile/' as expected: {sourcePath}");
				}
				sourcePath = $"{sourcePath}.sav";

				outPath = Path.Combine(directory, profileFolder, sourcePath);
				Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

				byte[] fileData = new byte[header.Size];
				stream.Read(fileData, 0, header.Size);

				using (FileStream outFile = File.Create(outPath))
				using (BinaryWriter writer = new(outFile, Encoding.ASCII, true))
				{
					writer.Write(fileHeader);
					writer.WriteUnrealString(header.Class);

					switch (header.Class)
					{
						case "/Game/Blueprints/Saves/Abiotic_WorldSave.Abiotic_WorldSave_C":
						case "/Game/Blueprints/Saves/Abiotic_WorldMetadataSave.Abiotic_WorldMetadataSave_C":
							writer.WriteUnrealString(VersionPropertyName);
							writer.Write(Version);
							writer.Write(1);
							writer.Write(fileData.Length);
							break;
						case "/Game/Blueprints/Saves/Abiotic_CharacterSave.Abiotic_CharacterSave_C":
							writer.Write(1);
							writer.Write(fileData.Length);
							break;
					}

					writer.Write(fileData);
				}
			}
		}
	}

	/// <summary>
	/// Metadata for a specific file contained within a world container
	/// </summary>
	internal struct AbioticSaveFileHeader
	{
		public FString Path;
		public int Size;
		public FString Class;
		public int Unknown;

		public static AbioticSaveFileHeader Read(BinaryReader reader)
		{
			return new AbioticSaveFileHeader
			{
				Path = reader.ReadUnrealString()!,
				Size = reader.ReadInt32(),
				Class = reader.ReadUnrealString()!,
				Unknown = reader.ReadInt32()
			};
		}

		public override string ToString()
		{
			return Path;
		}
	}
}
