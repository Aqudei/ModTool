using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;

namespace TextTool.ViewModels
{

    class ShellViewModel : Screen
    {
        private string destinationFolder = "D:\\Downloads\\Compressed\\Upwork Reference Material\\dbout";
        private string inputFolder = "D:\\Downloads\\Compressed\\Upwork Reference Material\\db";
        private string modId = "5a0abb6e1526d8000a025282";
        private string newName;

        public string DestinationFolder { get => destinationFolder; set => Set(ref destinationFolder, value.Trim("\\/ ".ToCharArray())); }
        public string InputFolder { get => inputFolder; set => Set(ref inputFolder, value.Trim("\\/ ".ToCharArray())); }
        public string ModId { get => modId; set => Set(ref modId, value); }
        public string NewName { get => newName; set => Set(ref newName, value); }


        public void Run()
        {
            var files = Directory.EnumerateFiles(InputFolder, $"*{ModId}*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Console.WriteLine($"Copying {file}");
                var basename = Path.GetFileNameWithoutExtension(file);
                var destiFile = file.Replace(inputFolder, DestinationFolder).Replace(basename, NewName);
                Console.WriteLine($"\tTo {destiFile}");
                File.Copy(file, destiFile, true);

                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(destiFile));
                if (json.ContainsKey("_id"))
                {
                    json["_id"] = newName;
                }
                if (json.ContainsKey("_tpl"))
                {
                    json["_tpl"] = newName;
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
