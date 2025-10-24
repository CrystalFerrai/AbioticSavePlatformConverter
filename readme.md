# AbioticSavePlatformConverter

A command line program that converts Abiotic Factor game saves from the [XBox / Microsoft Store] version to the [Steam / Dedicated Server] version.

NOTE: Conversion from Steam back to XBox is not currently implemented.

## Releases

Releases can be found [here](https://github.com/CrystalFerrai/AbioticSavePlatformConverter/releases). There is no installer, just unzip the contents to a location on your hard drive.

You will need to have the .NET Runtime 8.0 x64 installed. You can find the latest .NET 8 downloads [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Look for ".NET Runtime" or ".NET Desktop Runtime" (which includes .NET Runtime). Download and install the x64 version for your OS.

## How to Use

Prerequisite: You should have some familiarity with using command line programs or you may struggle to run this. You will need to pass various command line arguments to the program to tell it what you want to do.

### With MS Store Saves

If you have the Miscrosoft Store version of Abiotic Factor installed and want to extract and convert save games from it, then follow these steps.

#### Step 1: Get Profile ID

Run AbioticSavePlatformConverter with the `--find` option to locate your profile saves.

Example

```
AbioticSavePlatformConverter --find
```

```
Profile ID: 901000ABCDEFG
  MyCoolSave [2025-10-06 18:57:03]

Profile ID: 901110GFDECBA
  FirstSave [2024-11-09 12:37:02]
  CoolWorld [2025-10-22 16:48:49]
```

In the above example, you can see that there are two profiles (two different user accounts) that each have some Abiotic Factor world saves. Copy the profile ID for the one that looks like the profile you care about. For this example, we will use `901110GFDECBA`.

#### Step 2: Create an output folder

Create an empty folder somewhere on your PC where the converted save files can be stored. For this example, we will make a folder on our desktop named `AbioticSaves`.

#### Step 3: Run the conversion

Using the information you have from the previous steps, run AbioticSavePlatformConverter again with the following options, substituting your profile ID and output path.

```
AbioticSavePlatformConverter --profile 901110GFDECBA --out "%UserProfile%\Desktop\AbioticSaves"
```

Make sure it says "Conversion successful" when it is done. If there are errors, you will need to address them and try again.

### With other saves

If you have an XBox or Microsoft store save that is not connected to your profile, such as a save someone sent you for example, follow these steps.

#### Step 1: Extract/place the saves

If the save file is zipped, you will first want to extract it somewhere you can find it. If it is already extracted, then copy the folder path to use in step 3. Make sure the path you copy contains a `containers.index` file along with one or more folders that have long machine names.

For this example, we will assume the input saves are extracted to a folder on the desktop named `InputSave` (or in our home directory if on Linux) which directly contains the `containers.index` file and related folders.

#### Step 2: Create an output folder

Create an empty folder somewhere on your PC where the converted save files can be stored. For this example, we will make a folder on our desktop named `OutputSave` (or in our home directory if on Linux).

#### Step 3: Run the conversion

Using the information you have from the previous steps, run AbioticSavePlatformConverter with the following options, substituting your input and output paths.

**Windows**

```
AbioticSavePlatformConverter --in "%UserProfile%\Desktop\InputSave" --out "%UserProfile%\Desktop\OutputSave"
```

**Linux**

```
dotnet AbioticSavePlatformConverter.dll --in "~/InputSave" --out "~/OutputSave"
```

Make sure it says "Conversion successful" when it is done. If there are errors, you will need to address them and try again.

### After conversion

Verify that the folder you created now contains converted save files. You should see a folder inside named with your profile ID which contains a `Worlds` folder. Inside the `Worlds` folder will be each of the individual worlds as their own folders.

Copy each world folder to wherever you want to use it.

#### Steam game

If you want to load it locally in your Steam version of the game, then copy it to the following location.

```
%LocalAppData%\AbioticFactor\Saved\SaveGames\[Your Steam ID]\Worlds
```

#### Local dedicated server

If you want to copy it to a self-hosted dedicated server, it should go inside your server installation folder at the following relative location. Make sure the server is stopped before copying it.

```
AbioticFactor\Saved\SaveGames\Server\Worlds
```

#### Remote dedicated server

If your dedicated server is hosted remotely by a service provider, check their documentation for how to upload save files. They may have a web interface or provide you with FTP credentials.

### More options

To see the full list of options, run the program in a command window with no parameters. Here is what currently prints at the time of writing this:
```
Converts an Abiotic Factor world save from [XBox / MS Store] format to
[Steam / Dedicated Server] format.

Usage: AbioticSavePlatformConverter [options]

Options

  --in [path]     The path to a directory containing a save to convert.

  --out [path]    The path to the directory to output the converted save.

  --find          Search the system for save game files created by the
                  MS Store version of Abiotic Factor and list the results.

  --profile [id]  The ID of a user profile containing Abiotic Factor saves
                  to use as the input. Use this in place of --in.
                  To find profile IDs, run with the --find option.

  --create-header [source]  Generates a SaveHeader.dat file using the
                            given Steam save file as a template.

  --version       Print program version information.

Note: If the game updates and the Steam save file header changes, you may
need to generate a new SaveHeader.dat to replace the one that ships with
this tool. You can do this by running with the option --create-header and
passing in the path to any .sav file from a world save recently updated by
the Steam version of Abiotic Factor, or by a dedicated server. The Steam
version saves to '%LocalAppData%\AbioticFactor\Saved\SaveGames\[SteamID]'.
Usually this should not be required for most game updates as the game
should be able to read earlier saves.
```

## Technical Details

For anyone who wants to know how the conversion works at a high level, this is the process:

1. Load the save header data from a Steam save file because the XBox save format does not include this information.
2. Parse the XBox containers and search for Abiotic Factor world save containers. These are single file archives of all save data associated with a specific world save. Process each one that is found.
3. Parse the world save container header to get information about the file names and sizes of each file stored within.
4. Decompress the data blob that comes after the header using Oodle (an Unreal Engine compression format).
5. Export the data for each file to its own output file, prepending the Steam save header data.

The tool ships with a file named `SaveHeader.dat` which contains the Steam header data. The tool also supports generating a new version of the file if necessary using a passed in Steam world save file. Updating is not likely to be necessary usually since the game is capable of loading save files from older versions, but the feature is supported in case it becomes necessary at some point.

## How to Build

If you want to build, from source, follow these steps.
1. Clone the repo, including submodules.
    ```
    git clone --recursive https://github.com/CrystalFerrai/AbioticSavePlatformConverter.git
    ```
2. Open the file `AbioticSavePlatformConverter.sln` in Visual Studio.
3. Right click the solution in the Solution Explorer panel and select "Restore NuGet Dependencies".
4. Build the solution.

## Support

If you encounter errors when converting a save file, you can [submit as issue on Github](https://github.com/CrystalFerrai/AbioticSavePlatformConverter/issues). Please attach a zip containing the save file that is causing problems so that I can test it.

This is just one of my many free time projects. As such, I make no promises about when or if I will address submitted issues.
