#r "System.IO.FileSystem"
#r "System.IO.Compression.FileSystem"
#r "System.Linq"
#r "System.Text.RegularExpressions"
#r "nuget: GitignoreParserNet, 0.2.0.13"

using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitignoreParserNet;

// Define source and destination paths
string srcStandalone = @".\src\ReadieFur.SourceAnalyzer.Standalone\bin\Debug\net472";
string srcVSIX = @".\src\ReadieFur.SourceAnalyzer.VSIX\bin\Debug";
string srcReport = @".\report\Main.pdf";
string srcDir = @".\src";

string dstStandalone = @".\dist\Standalone";
string dstVSIX = @".\dist\VSIX";
string dstReport = @".\dist\Report.pdf";
string dstSubmission = @".\submission_files";

// Create directories if they don't exist
if (!Directory.Exists(dstStandalone)) { Directory.CreateDirectory(dstStandalone); }
if (!Directory.Exists(dstVSIX)) { Directory.CreateDirectory(dstVSIX); }
if (!Directory.Exists(dstSubmission)) { Directory.CreateDirectory(dstSubmission); }

// Clear existing contents of dist directory
TryDeleteContents(dstStandalone);
TryDeleteContents(dstVSIX);
TryDeleteFile(dstReport);

// Copy contents to destination directories
CopyFiles(srcStandalone, dstStandalone);
CopyFiles(srcVSIX, dstVSIX);
File.Copy(srcReport, dstReport);

// Copy Report.pdf to submission_files directory
File.Copy(dstReport, Path.Combine(dstSubmission, "Report.pdf"), true);

// Compress the src directory into a single archive excluding .gitignore
string srcZipPath = Path.Combine(dstSubmission, "src.zip");
if (File.Exists(srcZipPath)) { File.Delete(srcZipPath); }
ArchiveFiles();

void TryDeleteContents(string path)
{
    try { Directory.Delete(path, true); }
    catch (DirectoryNotFoundException) { /*Do nothing.*/ }
    catch (IOException ex) { Console.WriteLine(ex.Message); }
}

void TryDeleteFile(string path)
{
    try { File.Delete(path); }
    catch (FileNotFoundException) { Console.WriteLine($"{path} does not exist."); }
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

void ArchiveFiles()
{
    string[] excludeOverrides = { "[Bb]in/", "[Rr]elease/", "[Dd]ebug/" };
    string[] additionalExcludes = { "TestResults/" };

    IEnumerable<string> gitignoreContents = File.ReadAllText(".gitignore")
        .Split('\n')
        .Where(line =>
            (!string.IsNullOrWhiteSpace(line)
            && !line.StartsWith("#")
            && !excludeOverrides.Any(line.Contains))
            || additionalExcludes.Any(line.Contains));
    
    GitignoreParser parser = new(string.Join('\n', gitignoreContents));
    
    IEnumerable<string> archiveFiles = Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories)
        .Where(file => parser.Accepts(file));

    using (ZipArchive archive = ZipFile.Open(srcZipPath, ZipArchiveMode.Create))
    {
        foreach (string file in archiveFiles)
        {
            string relativePath = Path.GetRelativePath(srcDir, file);
            archive.CreateEntryFromFile(file, relativePath);
        }
    }
}
