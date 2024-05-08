#r "System.IO.FileSystem"
#r "System.IO.Compression.FileSystem"
#r "System.Linq"

using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;

// Define source and destination paths
string srcStandalone = @".\src\ReadieFur.SourceAnalyzer.Standalone\bin\Debug\net472";
string srcVSIX = @".\src\ReadieFur.SourceAnalyzer.VSIX\bin\Debug";
string srcDir = @".\src";

string dstStandalone = @".\dist\Standalone";
string dstVSIX = @".\dist\VSIX";

// Create directories if they don't exist
if (!Directory.Exists(dstStandalone)) { Directory.CreateDirectory(dstStandalone); }
if (!Directory.Exists(dstVSIX)) { Directory.CreateDirectory(dstVSIX); }

// Clear existing contents of dist directory
TryDeleteContents(dstStandalone);
TryDeleteContents(dstVSIX);

// Copy contents to destination directories
CopyFiles(srcStandalone, dstStandalone);
CopyFiles(srcVSIX, dstVSIX);

void TryDeleteContents(string path)
{
    try { Directory.Delete(path, true); }
    catch (DirectoryNotFoundException) { /*Do nothing.*/ }
    catch (IOException ex) { Console.WriteLine(ex.Message); }
}

void CopyFiles(string sourceDir, string destDir)
{
    foreach (string file in Directory.EnumerateFiles(sourceDir))
    {
        string destFile = Path.Combine(destDir, Path.GetFileName(file));
        Directory.CreateDirectory(destDir);
        File.Copy(file, destFile, true);
    }
}
