using System.IO.Compression;
using System.Text;
using BDCCChineseLocalization;
using BDCCChineseLocalization.Paratranz;
using CommandDotNet;
using GDShrapt.Reader;
using Newtonsoft.Json;
using Paratranz.NET;

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
    private static int Main(string[] args) =>
        new AppRunner<Program>().Run(args);
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
        var paratranzJson = Path.Combine(output, "paratranzJson");
        var files         = Directory.GetFiles(path, "*.gd", SearchOption.AllDirectories);
        var errorFiles    = new List<ErrorFile>();
        var skippedFiles  = new List<string>();
        var completed     = 0;
        TranslationHashIndexFile.SetDir(currentDir);
        TranslationHashIndexFile.Instance.Load();
        foreach (var file in files)
        {
            Console.WriteLine($"Extracting file {file}");
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (_banPath.Contains(fileName))
                {
                    skippedFiles.Add(file);
                    errorFiles.Add(new ErrorFile(file, "File is banned"));
                    continue;
                }
                var parser = GDScriptParser.ParseFile(file, Path.GetRelativePath(path, file));
                parser.Parse();
                if (!parser.HasTokens)
                {
                    continue;
                }
                var paratranzFilePath  = Path.ChangeExtension(file.Replace(path, paratranzPath), "json");
                var paratranzDirectory = Path.GetDirectoryName(paratranzFilePath)!;


                if (!Directory.Exists(paratranzDirectory))
                {
                    Directory.CreateDirectory(paratranzDirectory);
                }
                File.WriteAllText(paratranzFilePath, ParatranzConverter.Serialize(parser.Tokens));
                completed++;
            }
            catch (Exception e)
            {
                errorFiles.Add(new ErrorFile(file, e));

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
                Console.WriteLine(errorFile.File);
                sb.AppendLine($"File: {errorFile.File}");
                sb.AppendLine($"Exception:\n{errorFile.Exception.ToString()}");
            }


            File.WriteAllText(Path.Combine(output, "error.txt"),  sb.ToString());
            File.WriteAllText(Path.Combine(output, "error.json"), JsonConvert.SerializeObject(errorFiles));
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
        [Operand("api",         Description = "Paratranz API Key")]    string api,
        [CommandDotNet.Operand("projectID",   Description = "Paratranz Project ID")] int    projectId,
        [Option('i', "input",       Description = "项目路径")]                 string inputPath   = "BDCC",
        [Option('t', "translation", Description = "翻译项目路径")]               string translation = "Output"
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
            Console.WriteLine($"Project path {inputPath} does not exist");
            return;
        }
        if (!Directory.Exists(translation))
        {
            Console.WriteLine($"Translation path {translation} does not exist");
            return;
        }
        var missingTranslation   = Path.Combine(currentDir, "missing");
        var completedTranslation = Path.Combine(currentDir, "completed");
        if (Directory.Exists(missingTranslation))
        {
            Directory.Delete(missingTranslation, true);
        }
        if (Directory.Exists(completedTranslation))
        {
            Directory.Delete(completedTranslation, true);
        }
        Directory.CreateDirectory(missingTranslation);
        Directory.CreateDirectory(completedTranslation);
        var files                = Directory.GetFiles(paratranzPath, "*.json", SearchOption.AllDirectories);
        var errorFiles           = new List<ErrorFile>();
        var completed            = 0;
        foreach (var file in files)
        {
            Console.WriteLine($"Translating file {file}");
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                var scriptFilePath = Path.ChangeExtension(file.Replace(paratranzPath, inputPath), "gd");
                Console.WriteLine($"Translating file {scriptFilePath}");
                if (!File.Exists(scriptFilePath))
                {
                    errorFiles.Add(new ErrorFile(file, "Script file does not exist"));
                    continue;
                }
               
                var parser         = GDScriptParser.ParseFile(scriptFilePath, fileName);
                parser.Parse();
                if (!parser.HasTokens)
                {
                    continue;
                }
                var translateToken = ParatranzConverter.Deserialize(await File.ReadAllTextAsync(file, cancellationToken));
                var (missingTranslationTokens, completeTranslationTokens)       = parser.Translate(translateToken);
                if (missingTranslationTokens.Count > 0)
                {
                    ParatranzConverter.WriteFile( Path.ChangeExtension(Path.Combine(missingTranslation, Path.GetRelativePath(paratranzPath, file)), "json"), missingTranslationTokens);
                }
                if (completeTranslationTokens.Count > 0)
                {
                    ParatranzConverter.WriteFile(Path.ChangeExtension(Path.Combine(completedTranslation, Path.GetRelativePath(paratranzPath, file)), "json"), completeTranslationTokens);
                    await File.WriteAllTextAsync(scriptFilePath, parser.ClassDeclaration!.ToString(), cancellationToken);
                    completed++;
                }

            
                Console.WriteLine($"Translation complete for {file}");
               
            }
            catch (Exception e)
            {
                errorFiles.Add(new ErrorFile(file, e));
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

    public void TestScript()
    {
        var script = """
                     func getDefaultEquipment():
                     	return ["EngineerClothesAlex", "plainBriefs"]

                     func _getAttacks():
                     	return ["NpcScratch", "StrongBite", "simplekickattack", "HeatGrenade", "BolaThrow", "ForceBlindfoldPC", "trygetupattack"]

                     func getFightIntro(_battleName):
                     	return "Alex grunts as he gets into a fighting stance, his prosthetic spine is not meant for combat. But he seems tough even with that handicap."

                     func getLootTable(_battleName):
                     	return EngineerLoot.new()

                     func reactRestraint(restraintType, restraintAmount, isGettingForced):
                     	if(!isGettingForced):
                     		if(restraintAmount == 0):
                     			return RNG.pick([
                     				"You can't tie me up so easily",
                     				"Try harder next time",
                     				"What now?",
                     				"You can't tie up a rigger",
                     			])
                     		
                     		return RNG.pick([
                     			"I will break your toys",
                     			"These can't hold me back forever",
                     		])
                     	
                     	if(isGettingForced):
                     		if(restraintAmount > 2 && RNG.chance(30)):
                     			return RNG.pick([
                     				"Stop with this crap",
                     				"Enough, save these for lilacs and reds",
                     				"How many more do you have?",
                     			])
                     		
                     		if(restraintType == RestraintType.Gag):
                     			return RNG.pick([
                     				"Hey! Don't fucking gag me",
                     				"You think gagging me will save you?",
                     			])
                     		if(restraintType == RestraintType.Muzzle):
                     			return RNG.pick([
                     				"Hey! Don't fucking muzzle me",
                     				"You think muzzling me will save you?",
                     			])
                     		if(restraintType == RestraintType.ButtPlug):
                     			return RNG.pick([
                     				"Hey! Not my ass!",
                     				"Don't touch my fucking ass",
                     			])
                     	
                     		return RNG.pick([
                     			"Hey! Restraints are my thing!",
                     			"The fuck are you doing?",
                     			"You are making me real mad",
                     			"You can't win like this",
                     			"Fight me instead of this shit",
                     			"Keep that shit away from me",
                     		])
                     	return null
                     """;
        var parser = GDScriptParser.Parse(script, "测试");
        parser.Parse();
        Console.WriteLine(ParatranzConverter.Serialize(parser.Tokens));
        var translateJson = """
                            [
                              {
                                "key": "_1",
                                "original": "return [\"EngineerClothesAlex\", \"plainBriefs\"]",
                                "translation": "return [\"测试\", \"测试\"]"
                              },
                              {
                                "key": "_4",
                                "original": "return [\"NpcScratch\", \"StrongBite\", \"simplekickattack\", \"HeatGrenade\", \"BolaThrow\", \"ForceBlindfoldPC\", \"trygetupattack\"]",
                                "translation": "return [\"测试\", \"测试\", \"测试\", \"测试\", \"测试\", \"测试\", \"测试\"]"
                              },
                              {
                                "key": "_7_9",
                                "original": "Alex grunts as he gets into a fighting stance, his prosthetic spine is not meant for combat. But he seems tough even with that handicap.",
                                "translation": "测试",
                                "context": "return \"Alex grunts as he gets into a fighting stance, his prosthetic spine is not meant for combat. But he seems tough even with that handicap.\""
                              },
                              {
                                "key": "_16_5",
                                "original": "You can't tie me up so easily",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"You can't tie me up so easily\",\n\t\t\t\t\"Try harder next time\",\n\t\t\t\t\"What now?\",\n\t\t\t\t\"You can't tie up a rigger\",\n\t\t\t])"
                              },
                              {
                                "key": "_17_5",
                                "original": "Try harder next time",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"You can't tie me up so easily\",\n\t\t\t\t\"Try harder next time\",\n\t\t\t\t\"What now?\",\n\t\t\t\t\"You can't tie up a rigger\",\n\t\t\t])"
                              },
                              {
                                "key": "_18_5",
                                "original": "What now?",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"You can't tie me up so easily\",\n\t\t\t\t\"Try harder next time\",\n\t\t\t\t\"What now?\",\n\t\t\t\t\"You can't tie up a rigger\",\n\t\t\t])"
                              },
                              {
                                "key": "_19_5",
                                "original": "You can't tie up a rigger",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"You can't tie me up so easily\",\n\t\t\t\t\"Try harder next time\",\n\t\t\t\t\"What now?\",\n\t\t\t\t\"You can't tie up a rigger\",\n\t\t\t])"
                              },
                              {
                                "key": "_23_4",
                                "original": "I will break your toys",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\"I will break your toys\",\n\t\t\t\"These can't hold me back forever\",\n\t\t])"
                              },
                              {
                                "key": "_24_4",
                                "original": "These can't hold me back forever",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\"I will break your toys\",\n\t\t\t\"These can't hold me back forever\",\n\t\t])"
                              },
                              {
                                "key": "_30_5",
                                "original": "Stop with this crap",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Stop with this crap\",\n\t\t\t\t\"Enough, save these for lilacs and reds\",\n\t\t\t\t\"How many more do you have?\",\n\t\t\t])"
                              },
                              {
                                "key": "_31_5",
                                "original": "Enough, save these for lilacs and reds",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Stop with this crap\",\n\t\t\t\t\"Enough, save these for lilacs and reds\",\n\t\t\t\t\"How many more do you have?\",\n\t\t\t])"
                              },
                              {
                                "key": "_32_5",
                                "original": "How many more do you have?",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Stop with this crap\",\n\t\t\t\t\"Enough, save these for lilacs and reds\",\n\t\t\t\t\"How many more do you have?\",\n\t\t\t])"
                              },
                              {
                                "key": "_37_5",
                                "original": "Hey! Don't fucking gag me",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Hey! Don't fucking gag me\",\n\t\t\t\t\"You think gagging me will save you?\",\n\t\t\t])"
                              },
                              {
                                "key": "_38_5",
                                "original": "You think gagging me will save you?",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Hey! Don't fucking gag me\",\n\t\t\t\t\"You think gagging me will save you?\",\n\t\t\t])"
                              },
                              {
                                "key": "_42_5",
                                "original": "Hey! Don't fucking muzzle me",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Hey! Don't fucking muzzle me\",\n\t\t\t\t\"You think muzzling me will save you?\",\n\t\t\t])"
                              },
                              {
                                "key": "_43_5",
                                "original": "You think muzzling me will save you?",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Hey! Don't fucking muzzle me\",\n\t\t\t\t\"You think muzzling me will save you?\",\n\t\t\t])"
                              },
                              {
                                "key": "_47_5",
                                "original": "Hey! Not my ass!",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Hey! Not my ass!\",\n\t\t\t\t\"Don't touch my fucking ass\",\n\t\t\t])"
                              },
                              {
                                "key": "_48_5",
                                "original": "Don't touch my fucking ass",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\t\"Hey! Not my ass!\",\n\t\t\t\t\"Don't touch my fucking ass\",\n\t\t\t])"
                              },
                              {
                                "key": "_52_4",
                                "original": "Hey! Restraints are my thing!",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\"Hey! Restraints are my thing!\",\n\t\t\t\"The fuck are you doing?\",\n\t\t\t\"You are making me real mad\",\n\t\t\t\"You can't win like this\",\n\t\t\t\"Fight me instead of this shit\",\n\t\t\t\"Keep that shit away from me\",\n\t\t])"
                              },
                              {
                                "key": "_53_4",
                                "original": "The fuck are you doing?",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\"Hey! Restraints are my thing!\",\n\t\t\t\"The fuck are you doing?\",\n\t\t\t\"You are making me real mad\",\n\t\t\t\"You can't win like this\",\n\t\t\t\"Fight me instead of this shit\",\n\t\t\t\"Keep that shit away from me\",\n\t\t])"
                              },
                              {
                                "key": "_54_4",
                                "original": "You are making me real mad",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\"Hey! Restraints are my thing!\",\n\t\t\t\"The fuck are you doing?\",\n\t\t\t\"You are making me real mad\",\n\t\t\t\"You can't win like this\",\n\t\t\t\"Fight me instead of this shit\",\n\t\t\t\"Keep that shit away from me\",\n\t\t])"
                              },
                              {
                                "key": "_55_4",
                                "original": "You can't win like this",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\"Hey! Restraints are my thing!\",\n\t\t\t\"The fuck are you doing?\",\n\t\t\t\"You are making me real mad\",\n\t\t\t\"You can't win like this\",\n\t\t\t\"Fight me instead of this shit\",\n\t\t\t\"Keep that shit away from me\",\n\t\t])"
                              },
                              {
                                "key": "_56_4",
                                "original": "Fight me instead of this shit",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\"Hey! Restraints are my thing!\",\n\t\t\t\"The fuck are you doing?\",\n\t\t\t\"You are making me real mad\",\n\t\t\t\"You can't win like this\",\n\t\t\t\"Fight me instead of this shit\",\n\t\t\t\"Keep that shit away from me\",\n\t\t])"
                              },
                              {
                                "key": "_57_4",
                                "original": "Keep that shit away from me",
                                "translation": "测试",
                                "context": "return RNG.pick([\n\t\t\t\"Hey! Restraints are my thing!\",\n\t\t\t\"The fuck are you doing?\",\n\t\t\t\"You are making me real mad\",\n\t\t\t\"You can't win like this\",\n\t\t\t\"Fight me instead of this shit\",\n\t\t\t\"Keep that shit away from me\",\n\t\t])"
                              }
                            ]


                            """;

        var translationList = JsonConvert.DeserializeObject<List<TranslationToken>>(translateJson);
        parser.Translate(translationList);
        Console.WriteLine(parser.ClassDeclaration);
    }
    public void TestErrorFiles(string path = "output")
    {
        var script = """
                     tool
                     extends Polygon2D
                     
                     				
                     func SetFlipLegPos(_newvalue):
                     	if(color.r >= 0.99):
                     		# was left, became right
                     		color = Color("#AAAAAA")
                     		global_position -= legsSwitchDifference
                     	else:
                     		# was right, became left
                     		color = Color.white
                     		global_position += legsSwitchDifference
                     """;
        try
        {
            var reader = new GDScriptReader();
            reader.ParseFileContent(script);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }
}