using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using BDCCChineseLocalization;
using BDCCChineseLocalization.Paratranz;
using CommandDotNet;
using GDShrapt.Reader;
using Newtonsoft.Json;
using Paratranz.NET;
using Python.Deployment;
using Python.Runtime;
using Installer = Python.Included.Installer;
using TscnParser = BDCCChineseLocalization.TscnParser;

public class ErrorFile
{
    public ErrorFile(string file, Exception exception)
    {
        File = file;
        Exception = exception.ToString();
    }
    public ErrorFile(string file, string exception)
    {
        File = file;
        Exception = exception;
    }
    public ErrorFile()
    {
        File = string.Empty;
        Exception = string.Empty;
    }
    public string File      { get; set; }
    public string Exception { get; set; }
}

[Command(
    Description = "提取字典和翻译BDCC项目"
)]
public class Program
{
    private static readonly HashSet<string> BanPath = new HashSet<string>()
    {
        "GameUI",
        "gdunzip",
        "Polygon2dWeightsFlipper",
        "FertilityBetterOvulationV2",
        "NpcLikesLineUI",
        "OptionPriorityListType",
        "RichTextCorrupt",
        "RichTextCuss",
        "AutoTranslation",
        "SexActivityCreator",
        "StrugglingGame",
        "HK_DatapadHackComputer",
        "ColorUtils",
        "RopesOralSex",
        "RopesSex",
        "RopesSolo",
        "FetishesWithNumbers",
        "Strings",
        "AddRemoveListVarUI",
        "CapEnergyBlast"
    };
    private static int Main(string[] args) =>
        new AppRunner<Program>().Run(args);

    [Command(
        Description = "提取项目",
        UsageLines =
        [
            "Extract -p /path/to/project -o /path/to/output",
            "%AppName% %CmdPath% -p ./BDCC -o ./BDCCChineseLocalization"
        ]
    )]
    public async Task Extract(
        [Option('p', "path",   Description = "项目路径")]   string path   = "BDCC",
        [Option('o', "output", Description = "输出项目路径")] string output = "Output"
    )
    {
        var currentDir = Path.GetFullPath(".");
        // 判断是否绝对路径
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path);
        }
        if (!Path.IsPathRooted(output))
        {
            output = Path.GetFullPath(output);
        }
        Console.WriteLine($"Extracting project at {path} to {output}");
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Project path {path} does not exist");
            return;
        }
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }
        var paratranzPath = Path.Combine(output, "paratranz");
        // var tscnDirPath   = Path.Combine(output, "tscn");
        var gdFiles   = Directory.GetFiles(path, "*.gd",   SearchOption.AllDirectories);
        var tscnFiles = Directory.GetFiles(path, "*.tscn", SearchOption.AllDirectories);


        var errorFiles   = new List<ErrorFile>();
        var skippedFiles = new List<string>();
        var completed    = 0;
        TranslationHashIndexFile.SetDir(currentDir);
        TranslationHashIndexFile.Instance.Load();
        foreach (var gdFile in gdFiles)
        {
            // Console.WriteLine($"Extracting file {file}");
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(gdFile);
                if (BanPath.Contains(fileName))
                {
                    skippedFiles.Add(gdFile);
                    // errorFiles.Add(new ErrorFile(file, "File is banned"));
                    continue;
                }
                var parser = GdScriptParser.Parse(await File.ReadAllTextAsync(gdFile), Path.GetRelativePath(path, gdFile));
                parser.Parse();
                if (!parser.HasTokens)
                {
                    continue;
                }
                var paratranzFilePath  = Path.ChangeExtension(gdFile.Replace(path, paratranzPath), "json");
                var paratranzDirectory = Path.GetDirectoryName(paratranzFilePath)!;


                if (!Directory.Exists(paratranzDirectory))
                {
                    Directory.CreateDirectory(paratranzDirectory);
                }
                await File.WriteAllTextAsync(paratranzFilePath, ParatranzConverter.Serialize(parser.Tokens));
                completed++;
            }
            catch (Exception e)
            {
                errorFiles.Add(new ErrorFile(gdFile, e));

            }

        }

        foreach (var tscnFile in tscnFiles)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(tscnFile);

                var parser = new TscnParser(await File.ReadAllTextAsync(tscnFile), Path.GetRelativePath(path, tscnFile));
                parser.Parse();
                if (!parser.HasTokens)
                {
                    continue;
                }
                var paratranzFilePath  = Path.ChangeExtension(tscnFile.Replace(path, paratranzPath), "tscn.json");
                var paratranzDirectory = Path.GetDirectoryName(paratranzFilePath)!;
                if (!Directory.Exists(paratranzDirectory))
                {
                    Directory.CreateDirectory(paratranzDirectory);
                }
                await File.WriteAllTextAsync(paratranzFilePath, ParatranzConverter.Serialize(parser.Tokens));


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        TranslationHashIndexFile.Instance.Save();
        Console.WriteLine("Extraction complete, files processed: " + completed);
        var sb = new StringBuilder();
        if (errorFiles.Count > 0)
        {
            Console.WriteLine("Error files:");
            sb.AppendLine("Error files:");
            foreach (var errorFile in errorFiles)
            {
                Console.WriteLine($"\"{Path.GetFileNameWithoutExtension(errorFile.File)}\",");
                sb.AppendLine($"File: {errorFile.File}");
                sb.AppendLine($"Exception:\n{errorFile.Exception.ToString()}");
            }


            await File.WriteAllTextAsync(Path.Combine(output, "error.txt"),  sb.ToString());
            await File.WriteAllTextAsync(Path.Combine(output, "error.json"), JsonConvert.SerializeObject(errorFiles));
        }
        if (skippedFiles.Count > 0)
        {
            Console.WriteLine("Skipped files:");
            foreach (var skippedFile in skippedFiles)
            {
                Console.WriteLine(skippedFile);
            }
        }
    }

    [Command(
        Description = "翻译项目",
        UsageLines =
        [
            "Translate -p /path/to/project -t /path/to/translation",
            "%AppName% %CmdPath% -p ./BDCC -t ./BDCCChineseLocalization"
        ]
    )]
    public async Task Translate(
        [Operand("api", Description = "Paratranz API Key")] string api,
        [CommandDotNet.Operand("projectID", Description = "Paratranz Project ID")]
        int projectId,
        [Option('i', "input",       Description = "项目路径")]   string inputPath   = "BDCC",
        [Option('t', "translation", Description = "翻译项目路径")] string translation = "Output"
    )
    {
        var currentDir = Path.GetFullPath(".");
        if (!Path.IsPathRooted(inputPath))
        {
            inputPath = Path.GetFullPath(inputPath);
        }
        if (!Path.IsPathRooted(translation))
        {
            translation = Path.GetFullPath(translation);
        }

        var paratranzPath = Path.GetFullPath("Paratranz");
        if (Directory.Exists(paratranzPath))
        {
            Directory.Delete(paratranzPath, true);
        }

        var artifactDirPath  = Path.GetFullPath("Artifact");
        var artifactFilePath = Path.Combine(artifactDirPath, "artifact.zip");

        if (Directory.Exists(artifactDirPath))
        {
            Directory.Delete(artifactDirPath, true);
        }
        Directory.CreateDirectory(artifactDirPath);
        // Directory.CreateDirectory(paratranzPath);

        if (File.Exists(artifactFilePath))
        {
            File.Delete(artifactFilePath);
        }
        TranslationHashIndexFile.SetDir(currentDir);
        TranslationHashIndexFile.Instance.Load();
        using var client            = new ParatranzClient(api);
        var       cancellationToken = new CancellationToken();
        await client.BuildArtifactAsync(projectId, cancellationToken);
        var             downloadStream = await client.DownloadArtifactAsync(projectId, cancellationToken);
        await using var fs             = File.Open(artifactFilePath, FileMode.Create);
        Console.WriteLine($"Downloading artifact to {artifactFilePath}");
        await downloadStream.CopyToAsync(fs, cancellationToken);
        fs.Close();
        downloadStream.Close();
        ZipFile.ExtractToDirectory(artifactFilePath, artifactDirPath);
        var extractedDirPath = Path.Combine(artifactDirPath, "utf8");
        Directory.Move(extractedDirPath, paratranzPath);


        Console.WriteLine($"Translating project at {inputPath} with translations at {translation}");
        if (!Directory.Exists(inputPath))
        {
            // Console.WriteLine($"Project path {inputPath} does not exist");
            return;
        }
        if (!Directory.Exists(translation))
        {
            // Console.WriteLine($"Translation path {translation} does not exist");
            return;
        }
        var missingTranslation   = Path.Combine(currentDir, "missing");
        var completedTranslation = Path.Combine(currentDir, "completed");
        var translateCache       = Path.Combine(currentDir, "translateCache");
        if (Directory.Exists(missingTranslation))
        {
            Directory.Delete(missingTranslation, true);
        }
        if (Directory.Exists(completedTranslation))
        {
            Directory.Delete(completedTranslation, true);
        }
        if (Directory.Exists(translateCache))
        {
            Directory.Delete(translateCache, true);
        }
        Directory.CreateDirectory(missingTranslation);
        Directory.CreateDirectory(completedTranslation);
        Directory.CreateDirectory(translateCache);
        var files = Directory.GetFiles(paratranzPath, "*.json", SearchOption.AllDirectories);

        var completed = 0;
        foreach (var file in files)
        {
            // Console.WriteLine($"Translating file {file}");
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                var scriptFilePath = Path.ChangeExtension(file.Replace(paratranzPath, inputPath), "gd");
                // Console.WriteLine($"Translating file {scriptFilePath}");
                if (!File.Exists(scriptFilePath))
                {
                    // errorFiles.Add(new ErrorFile(file, "Script file does not exist"));
                    continue;
                }

                var parser = GdScriptParser.Parse(await File.ReadAllTextAsync(scriptFilePath, cancellationToken), fileName);
                parser.Parse();
                if (!parser.HasTokens)
                {
                    continue;
                }
                var translateToken = ParatranzConverter.Deserialize(await File.ReadAllTextAsync(file, cancellationToken));
                var complete       = parser.Translate(translateToken);
                // if (missingTranslationTokens.Count > 0)
                // {
                //     ParatranzConverter.WriteFile(Path.ChangeExtension(Path.Combine(missingTranslation, Path.GetRelativePath(paratranzPath, file)), "json"), missingTranslationTokens);
                // }
                if (complete)
                {
                    // ParatranzConverter.WriteFile(Path.ChangeExtension(Path.Combine(completedTranslation, Path.GetRelativePath(paratranzPath, file)), "json"), completeTranslationTokens);
                    Console.WriteLine($"Translation complete for {scriptFilePath}");
                    scriptFilePath = scriptFilePath.Replace(inputPath, translateCache);
                    var dir = Path.GetDirectoryName(scriptFilePath)!;
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    await File.WriteAllTextAsync(scriptFilePath, parser.Content, cancellationToken);
                    completed++;
                }


                // Console.WriteLine($"Translation complete for {file}");

            }
            catch (Exception e)
            {
                // errorFiles.Add(new ErrorFile(file, e));
            }
        }
        TranslationHashIndexFile.Instance.Save();
        Console.WriteLine("Translation complete, files processed: " + completed);
        // if (errorFiles.Count > 0)
        // {
        //     Console.WriteLine("Error files:");
        //     foreach (var errorFile in errorFiles)
        //     {
        //         Console.WriteLine(errorFile);
        //     }
        // }
    }

    public async Task TestTscn([Option('i', "input", Description = "项目路径")] string inputPath = "BDCC")
    {
        if (!Path.IsPathRooted(inputPath))
        {
            inputPath = Path.GetFullPath(inputPath);
        }
        var files = Directory.GetFiles(inputPath, "*.tscn", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var text = await File.ReadAllTextAsync(file);
            var tscn = new TscnParser(text, file);
            tscn.Parse();

            if (tscn.Tokens.Count > 0)
            {
                Console.WriteLine(new string('=', 80));
                Console.WriteLine(ParatranzConverter.Serialize(tscn.Tokens));
            }
        }

    }

    public async Task TestScript()
    {
        // Installer.InstallPath = Path.GetFullPath("Python");
        await Installer.SetupPython(true);
        var requirements = (await File.ReadAllTextAsync(Path.GetFullPath("BDCC-Localization-Replacer/requirements.txt"))).Replace("\r\n", "\n").Split("\n");
        foreach (var requirement in requirements.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")))
        {
            if (requirement.Contains("==") || requirement.Contains(">=") || requirement.Contains("<="))
            {
                var index = requirement.IndexOf("==", StringComparison.Ordinal);
                if (index == -1)
                {
                    index = requirement.IndexOf(">=", StringComparison.Ordinal);
                }
                if (index == -1)
                {
                    index = requirement.IndexOf("<=", StringComparison.Ordinal);
                }
                var module  = requirement[..index].Trim();
                var version = requirement[(index + 2)..].Trim();
                await Installer.PipInstallModule(module, version);
                continue;
            }
            await Installer.PipInstallModule(requirement);
        }
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();
        PythonEngine.PythonPath += ";" + Path.GetFullPath("BDCC-Localization-Replacer");
        Console.WriteLine(PythonEngine.PythonPath);
        var src     = Path.GetFullPath("BDCC-Localization-Replacer/src/replacer.py");
        var mainSrc = Path.GetFullPath("BDCC-Localization-Replacer/main.py");
        Environment.SetEnvironmentVariable("BDDC_DIR", Path.GetFullPath("BDCC"));
        using (Py.GIL())
        {

            // var main = PythonEngine.Compile(code, src, RunFlagType.File);

            dynamic os  = Py.Import("os");
            dynamic sys = Py.Import("sys");
            // // Console.WriteLine(os.path.dirname(os.path.expanduser(filePath)));
            sys.path.append(os.path.dirname(os.path.expanduser(src)));
            sys.path.append(os.path.dirname(os.path.expanduser(mainSrc)));
            // main.Invoke();
            // Console.WriteLine(sys.path);
            var fromFile = Py.Import("main");
            //
            fromFile.InvokeMethod("main");
        }
        // PythonEngine.Shutdown();
        Console.WriteLine("Done");
    }
    public async Task TestErrorFiles(string path = "output")
    {

        // var script = """
        //              tool
        //              extends Polygon2D
        //              
        //              				
        //              func SetFlipLegPos(_newvalue):
        //              	if(color.r >= 0.99):
        //              		# was left, became right
        //              		color = Color("#AAAAAA")
        //              		global_position -= legsSwitchDifference
        //              	else:
        //              		# was right, became left
        //              		color = Color.white
        //              		global_position += legsSwitchDifference
        //              """;
        // try
        // {
        //     var reader = new GDScriptReader();
        //     reader.ParseFileContent(script);
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e);
        //     throw;
        // }
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path);
        }
        var errorFiles = JsonConvert.DeserializeObject<List<ErrorFile>>(await File.ReadAllTextAsync(Path.Combine(path, "error.json")));
        if (errorFiles == null) return;
        foreach (var errorFile in errorFiles)
        {
            Console.WriteLine(errorFile.File);
            // Console.WriteLine(errorFile.Exception);
        }

    }
}