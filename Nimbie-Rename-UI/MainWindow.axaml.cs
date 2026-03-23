using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nimbie_Rename_UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private async void PickImageFolder(object? sender, RoutedEventArgs e)
        {
            if (StorageProvider is null)
            {
                Log("StorageProvider is not available.");
                return;
            }

            var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false
            });

            if (folder != null && folder.Count > 0)
                ImagePathBox.Text = folder[0].Path.LocalPath;
        }

        private async void PickManifestFile(object? sender, RoutedEventArgs e)
        {
            if (StorageProvider is null)
            {
                Log("StorageProvider is not available.");
                return;
            }

            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = null // You can add filters if needed
            });

            if (files != null && files.Count > 0)
                ManifestPathBox.Text = files[0].Path.LocalPath;
        }

        private void Log(string message)
        {
            OutputBox.Text += $"{DateTime.Now:HH:mm:ss}  {message}\n";
        }

        private void ClearLog(object? sender, RoutedEventArgs e)
        {
            OutputBox.Text = "";   
        }

        private void SaveLog(object? sender, RoutedEventArgs e)
        {
            var imagePath = ImagePathBox.Text;
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                Log("image path is empty. cannot save log.");
                return;
            }

            // Ensure the directory exists before creating the file
            if (!Directory.Exists(imagePath))
            {
                Log("image path does not exist. cannot save log.");
                return;
            }

            var logFilePath = Path.Combine(imagePath, "nimbie-rename.log");
            using var outputFile = File.Create(logFilePath);
            using var writer = new StreamWriter(outputFile);
            writer.Write(OutputBox.Text);
            
            Log($"log saved to {logFilePath}");
        }

        private void RunProcess(object? sender, RoutedEventArgs e)
        {
            var imageLocation = ImagePathBox.Text;
            var manifestFile = ManifestPathBox.Text;

            if (DryRunCheckBox.IsChecked == true)
            {
                Log("running in test mode");
            }

            if (!Directory.Exists(imageLocation) || !File.Exists(manifestFile))
            {
                Log("Invalid paths.");
                return;
            }

            var filenames = File.ReadLines(manifestFile).ToList();
            
            HashSet<string> directories = new HashSet<String>();
            foreach(string filename in filenames)
            {
                directories.Add(Path.Combine(imageLocation, filename));
               
            }

            foreach (string directoryName in directories) {
                var targetDirectory = Path.Combine(imageLocation, directoryName);
                Log($"creating directory: {targetDirectory}");
                Directory.CreateDirectory(targetDirectory);
            }


            var allowedExtensions = new[] { ".iso", ".img" };

            var ImageFiles = Directory
                .GetFiles(imageLocation)
                .Where(f => allowedExtensions.Contains(
                    Path.GetExtension(f),
                    StringComparer.OrdinalIgnoreCase))
                .ToDictionary(f => f, f => File.GetLastWriteTime(f));


            Log($"found {filenames.Count} filenames and {ImageFiles.Count} iso files");
            var sortedFileDates = ImageFiles.OrderBy(x => x.Value).ToList();               
            
            for (int i = 0; i < sortedFileDates.Count; i++)
            {
                var fileDate = sortedFileDates[i];
                var path = Path.GetDirectoryName(fileDate.Key)!;
                var filename = Path.GetFileName(fileDate.Key);
                var ext = Path.GetExtension(filename).ToLower();
                var newPath = Path.Combine(path, filenames[i] + ext);
                var basename = filename.Replace(ext, "");

                if (DryRunCheckBox.IsChecked == true)
                {
                    Log($"TEST: renaming {fileDate.Key} ({fileDate.Value}) to {newPath}");
                }
                else
                {
                    RenameFile(fileDate.Key, newPath, fileDate.Value);
                }
                if (ext == ".img")
                {
                    var files = Directory
                        .EnumerateFiles(path)
                        .Where(f => Path.GetFileName(f).StartsWith(basename));
                    foreach (string artifactPath in files)
                    {
                        var artifactFilename = Path.GetFileName(artifactPath);
                        if (artifactFilename != filename)
                        {
                            var artifactExtension = Path.GetExtension(artifactFilename).ToLower();
                            var newArtifactPath = Path.Combine(path, filenames[i] + artifactExtension);
                            if (DryRunCheckBox.IsChecked == true)
                            {
                                Log($"TEST: renaming {artifactPath} to {newArtifactPath}");
                            }
                            else
                            {
                                RenameFile(artifactPath, newArtifactPath, File.GetLastWriteTime(artifactPath));    
                            }
                        }
                    }
                }
            }

            //move the files
            allowedExtensions = allowedExtensions.Append(".cue")
                .Append(".ccd")
                .Append(".cdt")
                .ToArray();

            var renamedFiles = Directory
                .GetFiles(imageLocation)
                .Where(f => allowedExtensions.Contains(
                    Path.GetExtension(f),
                    StringComparer.OrdinalIgnoreCase));

            foreach(string renamedFile in renamedFiles)
            {
                var filename = Path.GetFileName(renamedFile);
                var extension = Path.GetExtension(filename);
                var basename = filename.Replace(extension, "");
                var targetFile = Path.Combine(imageLocation, basename, filename);
                RenameFile(renamedFile, targetFile, File.GetLastWriteTime(filename));
            }


            Log("done.");
        }

        private void RenameFile(string originalPath, string newPath, DateTime timeStamp)
        {
            Log($"renaming {originalPath} (timestamp) to {newPath}");
            try
            {
                File.Move(originalPath, newPath);
            }
            catch (IOException ioEx)
            {
                Log($"IO error: {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Log($"Access denied: {uaEx.Message}");
            }
            catch (Exception ex)
            {
                Log($"Unexpected error: {ex.Message}");
            }
        }
    }
}