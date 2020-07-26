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
        private string _destinationFolder = "D:\\Downloads\\Compressed\\Upwork Reference Material";
        private string _inputFolder = "D:\\Downloads\\Compressed\\Upwork Reference Material";
        private string _modId = "5a0abb6e1526d8000a025282";
        private string _modConfigPath = @"D:\Downloads\Compressed\StraitSix-GS18M-1.0.0\mod.config.json";
#else
        private string _destinationFolder;
        private string _inputFolder;
        private string _modId;
        private string _modConfigPath;
#endif

        private string _newName;
        private string _description;
        private bool _isBusy;


        public string DestinationFolder
        {
            get => _destinationFolder;
            set
            {
                Set(ref _destinationFolder, value.Trim("\\/ ".ToCharArray()));
                NotifyOfPropertyChange(nameof(CanRun));
            }
        }

        public string InputFolder
        {
            get => _inputFolder;
            set
            {
                Set(ref _inputFolder, value.Trim("\\/ ".ToCharArray()));
                NotifyOfPropertyChange(nameof(CanRun));
            }
        }

        public string ModId
        {
            get => _modId;
            set
            {
                Set(ref _modId, value);
                NotifyOfPropertyChange(nameof(CanRun));
            }
        }

        public string NewName
        {
            get => _newName;
            set
            {
                Set(ref _newName, value);
                NotifyOfPropertyChange(nameof(CanRun));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                Set(ref _description, value);
                NotifyOfPropertyChange(nameof(CanRun));
            }
        }

        public bool IsBusy { get => _isBusy; set => Set(ref _isBusy, value); }
        public string ModConfigFilePath { get => _modConfigPath; set => Set(ref _modConfigPath, value); }

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
            if (!File.Exists(ModConfigFilePath))
            {
                var configJson = File.ReadAllText("./mod-config.template");
                configJson = configJson.Replace("{modname}", modName);
                File.WriteAllText(Path.Combine(DestinationFolder, "mod.config.json"), configJson);
                return;
            }
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
                        var destiFile = file.Replace(_inputFolder, tempDirectory).Replace(basename, NewName);
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

                    GenModConfig(tempDirectory, _newName);
                    CleanUp(tempDirectory);
                    var dbDirectory = Path.Combine(DestinationFolder, "db");
                    if (!Directory.Exists(dbDirectory))
                    {
                        Directory.CreateDirectory(dbDirectory);
                    }

                    Process.Start("xcopy", $"\"{tempDirectory}\" \"{dbDirectory}\" /s /i /Y");
                }
                finally
                {
                    Execute.OnUIThread(() => IsBusy = false);
                    Process.Start("explorer.exe", $"\"{DestinationFolder}\"");
                }
            }).AsResult();
        }

        public bool CanRun => !string.IsNullOrWhiteSpace(DestinationFolder) &&
                              !string.IsNullOrWhiteSpace(InputFolder) &&
                              !string.IsNullOrWhiteSpace(NewName) &&
                              !string.IsNullOrWhiteSpace(ModId);

        public void Reset()
        {
            ModConfigFilePath = "";
            Description = "";
            NewName = "";
            ModId = "";
            InputFolder = "";
            DestinationFolder = "";
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
