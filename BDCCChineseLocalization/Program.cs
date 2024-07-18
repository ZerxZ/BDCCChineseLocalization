using System.Text;
using BDCCChineseLocalization;
using BDCCChineseLocalization.Paratranz;
using CommandDotNet;
using GDShrapt.Reader;
using Newtonsoft.Json;

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
        var paratranzPath = Path.Combine(output, "paratranz");
        var paratranzJson = Path.Combine(output, "paratranzJson");
        var files         = Directory.GetFiles(path, "*.gd", SearchOption.AllDirectories);
        var errorFiles    = new List<ErrorFile>();
        var skippedFiles  = new List<string>();
        var completed     = 0;
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
                var parser = GDScriptParser.ParseFile(file, fileName);
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
        var files      = Directory.GetFiles(translation, "*.json", SearchOption.AllDirectories);
        var errorFiles = new List<ErrorFile>();
        var completed  = 0;
        foreach (var file in files)
        {
            Console.WriteLine($"Translating file {file}");
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                var scriptFilePath = Path.ChangeExtension(file.Replace(translation, path), "gd");
                var parser         = GDScriptParser.ParseFile(scriptFilePath, fileName);
                parser.Parse();
                if (!parser.HasTokens)
                {
                    continue;
                }
                var translateToken = ParatranzConverter.Deserialize(File.ReadAllText(file));
                parser.Translate(translateToken);
                File.WriteAllText(scriptFilePath, parser.ClassDeclaration!.ToString());
                completed++;
            }
            catch (Exception e)
            {
                errorFiles.Add(new ErrorFile(file, e));
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
//
//     public void TestScript()
//     {
//         var script = """
//                      extends ItemBase
//
//                      var prisonerNumber = ""
//                      var inmateType = InmateType.General
//
//                      func _init():
//                      	id = "inmateuniform"
//
//                      func getVisibleName():
//                      	return InmateType.getOfficialName(inmateType).capitalize() + " inmate uniform"
//                      	
//                      func setPrisonerNumber(newnumber):
//                      	prisonerNumber = newnumber
//                      	
//                      func setInmateType(newtype):
//                      	inmateType = newtype
//                      	
//                      func getDescription():
//                      	var text = "A short sleeved shirt and some shorts, both are made out of black cloth with "+InmateType.getColorName(inmateType)+" trim."
//                      
//                      	if(prisonerNumber != null && prisonerNumber != ""):
//                      		text += " The shirt has a prisoner number attached to it that says \""+prisonerNumber+"\""
//                      	
//                      	return text
//
//                      func getClothingSlot():
//                      	return InventorySlot.Body
//
//                      func getBuffs():
//                      	return [
//                      		]
//
//                      func getTags():
//                      	return [
//                      		ItemTag.GeneralInmateUniform,
//                      		]
//
//                      func saveData():
//                      	var data = .saveData()
//                      	
//                      	data["prisonerNumber"] = prisonerNumber
//                      	data["inmateType"] = inmateType
//                      	
//                      	return data
//                      	
//                      func loadData(data):
//                      	.loadData(data)
//                      	
//                      	prisonerNumber = SAVE.loadVar(data, "prisonerNumber", "")
//                      	inmateType = SAVE.loadVar(data, "inmateType", InmateType.General)
//
//                      func getTakingOffStringLong(withS):
//                      	if(withS):
//                      		return "takes off your inmate shirt and pulls down the shorts"
//                      	else:
//                      		return "take off your inmate shirt and pull down the shorts"
//
//                      func getPuttingOnStringLong(withS):
//                      	if(withS):
//                      		return "puts on your inmate shirt and the shorts"
//                      	else:
//                      		return "put on your inmate shirt and the shorts"
//
//                      func generateItemState():
//                      	itemState = ShirtAndShortsState.new()
//                      	itemState.canActuallyBeDamaged = true # Is hack because there are many clothes that use this state already and don't support damaging..
//
//                      func getRiggedParts(_character):
//                      	if(itemState.isRemoved()):
//                      		return null
//                      	if(inmateType == InmateType.SexDeviant):
//                      		if(itemState.isSuperDamaged()):
//                      			return {
//                      				"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/LilacInmateUniformSuperDamaged.tscn",
//                      			}
//                      		if(itemState.isDamaged()):
//                      			return {
//                      				"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/LilacInmateUniformDamaged.tscn",
//                      			}
//                      		if(itemState.isHalfDamaged()):
//                      			return {
//                      				"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/LilacInmateUniformHalfDamaged.tscn",
//                      			}
//                      		return {
//                      			"clothing": "res://Inventory/RiggedModels/InmateUniform/LilacInmateUniform.tscn",
//                      		}
//                      	elif(inmateType == InmateType.HighSec):
//                      		if(itemState.isSuperDamaged()):
//                      			return {
//                      				"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/RedInmateUniformSuperDamaged.tscn",
//                      			}
//                      		if(itemState.isDamaged()):
//                      			return {
//                      				"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/RedInmateUniformDamaged.tscn",
//                      			}
//                      		if(itemState.isHalfDamaged()):
//                      			return {
//                      				"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/RedInmateUniformHalfDamaged.tscn",
//                      			}
//                      		return {
//                      			"clothing": "res://Inventory/RiggedModels/InmateUniform/RedInmateUniform.tscn",
//                      		}
//                      	
//                      	if(itemState.isSuperDamaged()):
//                      		return {
//                      			"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/OrangeInmateUniformSuperDamaged.tscn",
//                      		}
//                      	if(itemState.isDamaged()):
//                      		return {
//                      			"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/OrangeInmateUniformDamaged.tscn",
//                      		}
//                      	if(itemState.isHalfDamaged()):
//                      		return {
//                      			"clothing": "res://Inventory/RiggedModels/InmateUniform/damaged/OrangeInmateUniformHalfDamaged.tscn",
//                      		}
//                      	return {
//                      		"clothing": "res://Inventory/RiggedModels/InmateUniform/OrangeInmateUniform.tscn",
//                      	}
//
//                      func getInventoryImage():
//                      	if(inmateType == InmateType.SexDeviant):
//                      		return "res://Images/Items/equipment/shirtlilac.png"
//                      	if(inmateType == InmateType.HighSec):
//                      		return "res://Images/Items/equipment/shirtred.png"
//                      	return "res://Images/Items/equipment/shirtorange.png"
//
//                      """;
//         var parser = GDScriptParser.Parse(script);
//         parser.Parse();
//         Console.WriteLine(ParatranzConverter.Serialize(parser.Tokens));
//     }
//     public void TestErrorFiles(string path = "output")
//     {
//         var script = """
//                      tool
//                      extends Polygon2D
//                      
//                      				
//                      func SetFlipLegPos(_newvalue):
//                      	if(color.r >= 0.99):
//                      		# was left, became right
//                      		color = Color("#AAAAAA")
//                      		global_position -= legsSwitchDifference
//                      	else:
//                      		# was right, became left
//                      		color = Color.white
//                      		global_position += legsSwitchDifference
//                      """;
//         try
//         {
//             var reader = new GDScriptReader();
//             reader.ParseFileContent(script);
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine(e);
//             throw;
//         }
//
//     }
}