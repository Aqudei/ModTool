using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Injection;

namespace TextTool.ViewModels
{

    class ShellViewModel : Screen
    {
#if DEBUG
        private string destinationFolder = "D:\\Downloads\\Compressed\\Upwork Reference Material\\dbout";
        private string inputFolder = "D:\\Downloads\\Compressed\\Upwork Reference Material\\db";
        private string modId = "5a0abb6e1526d8000a025282";
#else
        private string destinationFolder;
        private string inputFolder;
        private string modId;
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

        public IEnumerable<IResult> Run()
        {

            yield return Task.Run(() =>
            {
                try
                {
                    Execute.OnUIThread(() => IsBusy = true);
                    var files = Directory.EnumerateFiles(InputFolder, $"*{ModId}*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        Console.WriteLine($"Copying {file}");
                        var basename = Path.GetFileNameWithoutExtension(file);
                        var destiFile = file.Replace(inputFolder, DestinationFolder).Replace(basename, NewName);
                        Console.WriteLine($"\tTo {destiFile}");
                        Directory.CreateDirectory(Path.GetDirectoryName(destiFile));
                        File.Copy(file, destiFile, true);

                        var content = File.ReadAllText(destiFile);
                        content = Replace(content, "_id", NewName);
                        content = Replace(content, "_tpl", NewName);
                        content = Replace(content, "Id", NewName);
                        content = Replace(content, "_name", NewName);
                        content = Replace(content, "ShortName", NewName);
                        content = Replace(content, "Description", Description);
                        File.WriteAllText(destiFile, content);
                    }
                }
                finally
                {
                    Execute.OnUIThread(() => IsBusy = false);
                    Process.Start("explorer.exe", $"\"{DestinationFolder}\"");
                }
            }).AsResult();

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
