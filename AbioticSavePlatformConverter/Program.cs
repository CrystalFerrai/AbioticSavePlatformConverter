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

namespace AbioticSavePlatformConverter
{
	internal class Program
	{
		/// <summary>
		/// Entry point
		/// </summary>
		private static int Main(string[] args)
		{
			Logger logger = new ConsoleLogger();

			if (args.Length == 0)
			{
				Options.PrintUsage(logger);
				return OnExit(0);
			}

			Options? options;
			if (!Options.TryParse(args, logger, out options))
			{
				return OnExit(1);
			}

			Converter converter = new(options);
			if (!converter.Run(logger))
			{
				return OnExit(1);
			}

			return OnExit(0);
		}

		private static int OnExit(int code)
		{
			if (System.Diagnostics.Debugger.IsAttached)
			{
				Console.ReadKey(true);
			}
			return code;
		}
	}
}
