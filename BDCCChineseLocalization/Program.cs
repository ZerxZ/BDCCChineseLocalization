using BDCCChineseLocalization;
using BDCCChineseLocalization.Paratranz;
using CommandDotNet;
using GDShrapt.Reader;

[Command(
    Description = "提取字典和翻译BDCC项目"
)]
public class Program
{
    private static int Main(string[] args) =>
        new AppRunner<Program>().Run(args);

    // [Command(
    //     Description = "Adds two numbers",
    //     UsageLines = new[] { "Add 1 2", "%AppName% %CmdPath% 1 2" },
    //     ExtendedHelpText = "single line of extended help here")]
    // public void Add(
    //     [Operand(Description = "first value")]  int x,
    //     [Operand(Description = "second value")] int y) => Console.WriteLine(x + y);
    //
    // // [Command(Description = "Subtracts two numbers")]
    // // public void Subtract(int x, int y) => Console.WriteLine(x - y);
    private readonly HashSet<string> _banPath = new HashSet<string>
    {
        "LoadGameScreen",
        "ModsMenu"
    };
    [Command(
        Description = "提取项目",
        UsageLines =
        [
            "Extract -p /path/to/project -o /path/to/output",
            "%AppName% %CmdPath% -p ./BDCC -o ./BDCCChineseLocalization"
        ]
    )]
    public void Extract(
        [Option('p', "path",   Description = "项目路径")]   string path   = "",
        [Option('o', "output", Description = "输出项目路径")] string output = ""
    )
    {
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
        var files        = Directory.GetFiles(path, "*.gd", SearchOption.AllDirectories);
        var errorFiles   = new List<string>();
        var skippedFiles = new List<string>();
        var completed    = 0;
        foreach (var file in files)
        {
            Console.WriteLine($"Extracting file {file}");
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (_banPath.Contains(fileName))
                {
                    skippedFiles.Add(file);
                    continue;
                }
                var parser = GDScriptParser.ParseFile(file, fileName);
                parser.Parse();
                if (!parser.HasTokens)
                {
                    continue;
                }
                var outputFilePath  = Path.ChangeExtension(file.Replace(path, output), "json");
                var outputDirectory = Path.GetDirectoryName(outputFilePath)!;
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                ParatranzConverter.WriteFile(outputFilePath, parser.Tokens);
                completed++;
            }
            catch (Exception)
            {
                errorFiles.Add(file);

            }

        }
        Console.WriteLine("Extraction complete, files processed: " + completed);
        if (errorFiles.Count > 0)
        {
            Console.WriteLine("Error files:");
            foreach (var errorFile in errorFiles)
            {
                Console.WriteLine(errorFile);
            }
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
    public void Translate(
        [Option('p', "path",        Description = "项目路径")]   string path        = "",
        [Option('t', "translation", Description = "翻译项目路径")] string translation = ""
    )
    {
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path);
        }
        if (!Path.IsPathRooted(translation))
        {
            translation = Path.GetFullPath(translation);
        }
        Console.WriteLine($"Translating project at {path} with translations at {translation}");
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Project path {path} does not exist");
            return;
        }
        if (!Directory.Exists(translation))
        {
            Console.WriteLine($"Translation path {translation} does not exist");
            return;
        }
        var files = Directory.GetFiles(translation, "*.json", SearchOption.AllDirectories);
        var errorFiles = new List<string>();
        var completed = 0;
        foreach (var file in files)
        {
            Console.WriteLine($"Translating file {file}");
            try
            {
                var fileName       = Path.GetFileNameWithoutExtension(file);
              
                var scriptFilePath = Path.ChangeExtension(file.Replace(translation, path), "gd");
                var parser         = GDScriptParser.ParseFile(scriptFilePath, fileName);
                parser.Parse();
                if (!parser.HasTokens)
                {
                    continue;
                }
                var translateToken = ParatranzConverter.Deserialize(File.ReadAllText(file));
                if (translateToken is null)
                {
                    continue;
                }
                parser.Translate(translateToken);
                File.WriteAllText(scriptFilePath, parser.ClassDeclaration!.ToString());
                completed++;
            }
            catch (Exception)
            {
                errorFiles.Add(file);
            }
        }
        Console.WriteLine("Translation complete, files processed: " + completed);
        if (errorFiles.Count > 0)
        {
            Console.WriteLine("Error files:");
            foreach (var errorFile in errorFiles)
            {
                Console.WriteLine(errorFile);
            }
        }
        
    }
}