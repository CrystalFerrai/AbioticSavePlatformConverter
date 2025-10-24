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

using System.Data;
using System.Text;

namespace AbioticSavePlatformConverter.Xbox
{
	/// <summary>
	/// An XBox save game container index which represents the entire set of save containers for a specific game
	/// </summary>
	internal class XBoxContainerIndex
	{
		public string PackageName { get; set; }

		public string AppName { get; set; }

		public DateTime Timestamp { get; set; }

		public Guid Guid { get; set; }

		public XBoxContainer[] Containers { get; set; }

		public XBoxContainerIndex(string packageName, string appName)
		{
			PackageName = packageName;
			AppName = appName;
			Timestamp = DateTime.UtcNow;
			Guid = Guid.Empty;
			Containers = Array.Empty<XBoxContainer>();
		}

		public static XBoxContainerIndex Load(string directory)
		{
			string containerIndexPath = Path.Combine(directory, "containers.index");
			if (!File.Exists(containerIndexPath))
			{
				throw new ArgumentException($"Unable to locate '{containerIndexPath}'");
			}

			int count;
			string packageName;
			string appName;
			DateTime timestamp;
			Guid guid;
			XBoxContainerHeader[] containerHeaders;

			using (FileStream file = File.OpenRead(containerIndexPath))
			using (BinaryReader reader = new(file, Encoding.Unicode, true))
			{
				int unknown1 = reader.ReadInt32(); // 14

				count = reader.ReadInt32();

				_ = reader.ReadInt32(); // 0

				string packageId = reader.ReadXboxString();
				string[] packageIdParts = packageId.Split('!');
				packageName = packageIdParts[0];
				appName = packageIdParts[1];

				timestamp = DateTime.FromFileTimeUtc(reader.ReadInt64());

				int unknown2 = reader.ReadInt32();

				guid = Guid.Parse(reader.ReadXboxString());

				int unknown3 = reader.ReadInt32();
				_ = reader.ReadInt32(); // 0

				containerHeaders = new XBoxContainerHeader[count];
				for (int i = 0; i < count; ++i)
				{
					containerHeaders[i] = XBoxContainerHeader.Load(reader);
				}
			}

			XBoxContainer[] containers = new XBoxContainer[count];
			for (int i = 0; i < count; ++i)
			{
				containers[i] = XBoxContainer.Load(containerHeaders[i], directory);
			}

			return new(packageName, appName)
			{
				Timestamp = timestamp,
				Guid = guid,
				Containers = containers
			};
		}

		public override string ToString()
		{
			return $"{Guid} : {PackageName} ({AppName})";
		}
	}
}
