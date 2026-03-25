open System.IO

// Set your directories here
let sourceDir = @"Z:\Utilities\Nimbie\ISO-COPY"
let targetDir = @"Z:\Utilities\Nimbie\ISO"

// Delete all files in the target directory
if Directory.Exists(targetDir) then
    Directory.GetFiles(targetDir)
    |> Array.iter (fun file ->
        printfn "Deleting: %s" file
        File.Delete(file)
    )

    Directory.GetDirectories(targetDir)
    |> Array.iter (fun dir ->
        printfn "Deleting: %s" dir
        Directory.Delete(dir, true);
    )
else
    Directory.CreateDirectory(targetDir) |> ignore

// Copy all files from source to target
if Directory.Exists(sourceDir) then
    Directory.GetFiles(sourceDir)
    |> Array.iter (fun file ->
        let fileName = Path.GetFileName(file)
        let destFile = Path.Combine(targetDir, fileName)
        printfn "Copying: %s" file
        File.Copy(file, destFile, true) // overwrite if exists
    )
    printfn "Done"
else
    printfn "Source directory does not exist: %s" sourceDir