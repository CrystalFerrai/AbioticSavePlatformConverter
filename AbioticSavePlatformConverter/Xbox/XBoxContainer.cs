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

using System.Globalization;
using System.Text;

namespace AbioticSavePlatformConverter.Xbox
{
	/// <summary>
	/// An XBox save game container which represents a specific group of save files
	/// </summary>
	internal class XBoxContainer
	{
		public XBoxContainerHeader Header { get; }

		public string ContainerDirectory { get; }

		public XBoxContainerFile[] Files { get; }

		public XBoxContainer(XBoxContainerHeader header, string containerDirectory, XBoxContainerFile[] files)
		{
			Header = header;
			ContainerDirectory = containerDirectory;
			Files = files;
		}

		public static XBoxContainer Load(XBoxContainerHeader header, string indexDirectory)
		{
			string containerDirectory = header.Guid.ToString("N").ToUpperInvariant();
			string fullDirectory = Path.Combine(indexDirectory, containerDirectory);
			string containerPath = Path.Combine(fullDirectory, $"container.{header.Index}");

			XBoxContainerFile[] files;

			using (FileStream file = File.OpenRead(containerPath))
			using (BinaryReader reader = new(file, Encoding.Unicode, true))
			{
				int containerVersion = reader.ReadInt32();
				if (containerVersion != 4)
				{
					throw new InvalidDataException($"Unknown container version '{containerVersion}' in '{containerPath}'");
				}

				int fileCount = reader.ReadInt32();
				files = new XBoxContainerFile[fileCount];
				for (int i = 0; i < fileCount; ++i)
				{
					files[i] = XBoxContainerFile.Load(reader);
				}
			}

			return new(header, containerDirectory, files);
		}

		public override string ToString()
		{
			return Header.ToString();
		}
	}

	/// <summary>
	/// The header for an XBoxContainer
	/// </summary>
	internal class XBoxContainerHeader
	{
		public XBoxContainerId Id { get; set; }

		public byte Index { get; set; }

		public Guid Guid { get; set; }

		public DateTime Timestamp { get; set; }

		public long Size { get; set; }

		internal static XBoxContainerHeader Load(BinaryReader reader)
		{
			XBoxContainerId id = XBoxContainerId.Load(reader);

			byte index = reader.ReadByte();

			_ = reader.ReadInt32(); // 1

			Guid guid = new(reader.ReadBytes(16));

			DateTime timestamp = DateTime.FromFileTimeUtc(reader.ReadInt64());

			_ = reader.ReadInt32(); // 0
			_ = reader.ReadInt32(); // 0

			long size = reader.ReadInt64();

			return new()
			{
				Id = id,
				Index = index,
				Guid = guid,
				Timestamp = timestamp,
				Size = size
			};
		}

		public override string ToString()
		{
			return $"{Guid} [{Id}]";
		}
	}

	/// <summary>
	/// Metadata for a file within an XBoxContainer
	/// </summary>
	internal class XBoxContainerFile
	{
		public string Metadata { get; }

		public Guid Guid { get; }

		public string DataFileName { get; }
		
		public XBoxContainerFile(string metadata, Guid guid, string dataFileName)
		{
			Metadata = metadata;
			Guid = guid;
			DataFileName = dataFileName;
		}

		internal static XBoxContainerFile Load(BinaryReader reader)
		{
			// Fixed length string padded with null bytes
			string metadata = Encoding.Unicode.GetString(reader.ReadBytes(128));

			Guid guid = new(reader.ReadBytes(16));

			string dataFileName = new Guid(reader.ReadBytes(16)).ToString("N").ToUpperInvariant();

			return new(metadata, guid, dataFileName);
		}

		public override string ToString()
		{
			return Guid.ToString();
		}
	}

	internal struct XBoxContainerId
	{
		public string Name1;
		public string Name2;
		public ulong Unknown;

		public static XBoxContainerId Load(BinaryReader reader)
		{
			XBoxContainerId instance = new();
			instance.Name1 = reader.ReadXboxString();
			instance.Name2 = reader.ReadXboxString();

			string unknown = reader.ReadXboxString();
			unknown = unknown.Trim('\"')[2..];
			instance.Unknown = ulong.Parse(unknown, NumberStyles.HexNumber);

			return instance;
		}

		public override string ToString()
		{
			return Name1;
		}
	}
}
