using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;

namespace ModItemCreationTool.ViewModels
{

    class ShellViewModel : Screen
    {
#if DEBUG
        private string destinationFolder = "D:\\Downloads\\Compressed\\Upwork Reference Material\\dbout";
        private string inputFolder = "D:\\Downloads\\Compressed\\Upwork Reference Material\\db";
        private string modId = "5a0abb6e1526d8000a025282";
        private string modConfigPath = @"D:\Downloads\Compressed\StraitSix-GS18M-1.0.0\mod.config.json";
#else
        private string destinationFolder;
        private string inputFolder;
        private string modId;
        private string modConfigPath;
#endif

        private string newName;
        private string description;
        private bool isBusy;


        public string DestinationFolder { get => destinationFolder; set => Set(ref destinationFolder, value.Trim("\\/ ".ToCharArray())); }
        public string InputFolder { get => inputFolder; set => Set(ref inputFolder, value.Trim("\\/ ".ToCharArray())); }
        public string ModId { get => modId; set => Set(ref modId, value); }
        public string NewName { get => newName; set => Set(ref newName, value); }
        public string Description { get => description; set => Set(ref description, value); }
        public bool IsBusy { get => isBusy; set => Set(ref isBusy, value); }
        public string ModConfigFilePath { get => modConfigPath; set => Set(ref modConfigPath, value); }

        private string Replace(string content, string key, string value)
        {
            var regex = new Regex($"\"{key}\"\\s*:.+\"", RegexOptions.IgnoreCase);
            var match = regex.Match(content);
            if (match.Success)
            {
                return content.Replace(match.Value, $"\"{key}\": \"{value}\"");
            }
            return content;
        }

        private bool IsValidDirectory(IEnumerable<string> lookup, string dirName)
        {
            foreach (var item in lookup)
            {
                if (dirName.StartsWith(item))
                    return true;
            }

            return false;
        }

        private void GenModConfig(string dir, string modName)
        {
            var json = JObject.Parse(File.ReadAllText(ModConfigFilePath));

            json["db"]["items"][modName] = $"db/items/{modName}.json";
            json["db"]["locales"]["en"]["templates"][modName] = $"db/locales/en/templates/{modName}.json";
            json["db"]["assort"]["ragfair"]["items"][modName] = $"db/assort/ragfair/items/{modName}.json";
            json["db"]["templates"]["items"][modName] = $"db/templates/items/{modName}.json";

            File.WriteAllText(ModConfigFilePath, json.ToString());
        }

        public void BrowseConfigFile()
        {
            var dlg = new CommonOpenFileDialog();
            var rslt = dlg.ShowDialog();
            if (rslt != CommonFileDialogResult.Ok)
            {
                return;
            }
            ModConfigFilePath = dlg.FileName;
        }


        public IEnumerable<IResult> Run()
        {
            var folders = new string[] { "templates", "locales", "items", "assort" };
            var tempDirectory = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString()
                .Replace("/", "-").Replace(" ", "-").Replace(":", "-"));
            yield return Task.Run(() =>
            {
                try
                {
                    Execute.OnUIThread(() => IsBusy = true);
                    var files = Directory.EnumerateFiles(InputFolder, $"*{ModId}*.json", SearchOption.AllDirectories);
                    var validDirectories = new List<string>();

                    foreach (var folderName in folders)
                    {
                        validDirectories.Add(Path.Combine(InputFolder, folderName).Trim(" \\/".ToCharArray()));
                    }

                    foreach (var file in files)
                    {
                        var directoryName = Path.GetDirectoryName(file).Trim(" \\/".ToCharArray());
                        if (!IsValidDirectory(validDirectories, directoryName))
                            continue;

                        Console.WriteLine($"Copying {file}");
                        var basename = Path.GetFileNameWithoutExtension(file);
                        var destiFile = file.Replace(inputFolder, tempDirectory).Replace(basename, NewName);
                        Console.WriteLine($"\tTo {destiFile}");
                        Directory.CreateDirectory(Path.GetDirectoryName(destiFile));
                        File.Copy(file, destiFile, true);

                        var content = File.ReadAllText(destiFile);
                        content = Replace(content, "_id", NewName);
                        content = Replace(content, "_tpl", NewName);
                        content = Replace(content, "Id", NewName);
                        content = Replace(content, "_name", NewName);
                        content = Replace(content, "ShortName", NewName);
                        content = Replace(content, "Name", NewName);
                        content = Replace(content, "Description", Description);
                        File.WriteAllText(destiFile, content);
                    }

                    GenModConfig(tempDirectory, newName);
                    CleanUp(tempDirectory);
                    Process.Start("xcopy", $"\"{tempDirectory}\" \"{DestinationFolder}\" /s /i /Y");
                }
                finally
                {
                    Execute.OnUIThread(() => IsBusy = false);
                    Process.Start("explorer.exe", $"\"{DestinationFolder}\"");
                }
            }).AsResult();

        }

        private void CleanUp(string tempDirectory)
        {
            var locales = Path.Combine(tempDirectory, "locales");
            foreach (var directory in Directory.GetDirectories(locales))
            {
                var directoryName = Path.GetFileName(directory);
                if (directoryName != "en")
                {
                    Directory.Delete(directory, true);
                }
            }

            var assort = Path.Combine(tempDirectory, "assort");
            foreach (var directory in Directory.GetDirectories(assort))
            {
                var directoryName = Path.GetFileName(directory);
                if (directoryName != "ragfair")
                {
                    Directory.Delete(directory, true);
                }
            }

            var assortRagfair = Path.Combine(tempDirectory, "assort", "ragfair");
            foreach (var directory in Directory.GetDirectories(assortRagfair))
            {
                var directoryName = Path.GetFileName(directory);
                if (directoryName != "items")
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        public void BrowseDestinationFolder()
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            var rslt = dlg.ShowDialog();
            if (rslt == CommonFileDialogResult.Ok)
            {
                DestinationFolder = dlg.FileName;
            }
        }

        public void BrowseInputFolder()
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            var rslt = dlg.ShowDialog();
            if (rslt == CommonFileDialogResult.Ok)
            {
                InputFolder = dlg.FileName;
            }
        }
    }
}
