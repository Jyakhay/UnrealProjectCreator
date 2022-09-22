using Newtonsoft.Json;
using System.Reflection;

//TODO - Make it so bat files are filled with proper contents, then rejoice because it should be finished.

public struct ProjectModule
{
    public ProjectModule(string ModuleName)
    {
        Name = ModuleName;
    }

    [JsonProperty("Name")]
    string Name = "";

    [JsonProperty("Type")]
    string Type = "Runtime";

    [JsonProperty("LoadingPhase")]
    string LoadingPhase = "Default";

}

public struct UProject
{

    public UProject()
    {
    }

    [JsonProperty("FileVersion")]
    int FileVersion = 3;

    [JsonProperty("EngineAssociation")]
    public string EngineAssociation = "5.0";

    [JsonProperty("Category")]
    string Category = "";

    [JsonProperty("Description")]
    string Description = "";

    [JsonProperty("Modules")]
    public List<ProjectModule> Modules = new List<ProjectModule>();
    
}

public struct ProjectDefaults
{
    public ProjectDefaults()
    {

    }

    [JsonProperty("ProjectDirectory")]
    public string ProjectDirectory = "";

    [JsonProperty("EngineVersion")]
    public string EngineVersion = "";

    [JsonProperty("EpicDirectory")]
    public string EpicGamesDirectory = "";
}

internal class Test
{
    static string BuildBase = "using UnrealBuildTool;\r\n\r\npublic class BASENAME : ModuleRules\r\n{\r\n\tpublic BASENAME(ReadOnlyTargetRules Target) : base(Target)\r\n\t{\r\n\t\tPCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;\r\n\t\tbEnforceIWYU = true;\r\n\r\n\t\tPublicDependencyModuleNames.AddRange(new string[] { \"Core\", \"CoreUObject\", \"Engine\" });\r\n\t\tPrivateDependencyModuleNames.AddRange(new string[] { });\r\n\t}\r\n}";
    static string TargetBase = "using UnrealBuildTool;\r\n\r\npublic class BASENAME Target : TargetRules\r\n{\r\n\tpublic BASENAME Target(TargetInfo Target) : base(Target)\r\n\t{\r\n\t\tType = TargetType.Game;\r\n\t\tDefaultBuildSettings = BuildSettingsVersion.V2;\r\n\t\tExtraModuleNames.AddRange( new string[] { \"MODULENAME\" } );\r\n\t}\r\n}\r\n";
    static string EditorTargetBase = "using UnrealBuildTool;\r\n\r\npublic class BASENAME EditorTarget : TargetRules\r\n{\r\n\tpublic BASENAME EditorTarget(TargetInfo Target) : base(Target)\r\n\t{\r\n\t\tType = TargetType.Game;\r\n\t\tDefaultBuildSettings = BuildSettingsVersion.V2;\r\n\t\tExtraModuleNames.AddRange( new string[] { \"MODULENAME\" } );\r\n\t}\r\n}\r\n";
    static string ModuleCPPBase = "#include \"BASENAME.h\"\r\n#include \"Modules/ModuleManager.h\"\r\n\r\nvoid FBASENAME::StartupModule()\r\n{\r\n}\r\n\r\nvoid FBASENAME::ShutdownModule()\r\n{\r\n}\r\n\r\nIMPLEMENT_GAME_MODULE(FBASENAME, BASENAME);\r\n";
    static string ModuleHBase = "#pragma once\r\n\r\n#include \"CoreMinimal.h\"\r\n#include \"Modules/ModuleInterface.h\"\r\n\r\nclass FBASENAME : public IModuleInterface\r\n{\r\npublic:\r\n\tstatic inline FBASENAME& Get()\r\n\t{\r\n\t\treturn FModuleManager::LoadModuleChecked<FBASENAME>(\"BASENAME\");\r\n\t}\r\n\r\n\tstatic inline bool IsAvailable()\r\n\t{\r\n\t\treturn FModuleManager::Get().IsModuleLoaded(\"BASENAME\");\r\n\t}\r\n\r\n\tvirtual void StartupModule() override;\r\n\tvirtual void ShutdownModule() override;\r\n};\r\n;";
    static string VarsScriptBase = "@echo off\r\n\r\nset ROOTDIR=%~dp0\r\n\r\nset PROJECT=BASENAME\r\n\r\nset UE_VER=BASEVER\r\n\r\nset UPROJECT_PATH=%ROOTDIR%%PROJECT%.uproject\r\n\r\nset EPIC_GAMES_PATH=EGPATH\r\n\r\nset UE_DIR=%EPIC_GAMES_PATH%UE_%UE_VER%\\\r\n\r\nset UE_EDITOR_EXE_DIR=%UE_DIR%Engine\\Binaries\\Win64\\UE4Editor.exe\r\n\r\nset UE_BUILD=%UE_DIR%Engine\\Build\\BatchFiles\\Build.bat";
    static string BuildScriptBase = "@echo off\r\n\r\ncall vars.bat\r\n\r\ncall \"%UE_BUILD%\" %PROJECT%Editor Win64 Development \"%UPROJECT_PATH%\" -waitmutex -NoHotReload\r\n\r\npause";
    static string OpenScriptBase = "@echo off\r\n\r\ncall vars.bat\r\n\r\nstart \"%UE_EDITOR_EXE%\" \"%UPROJECT_PATH%\"";

    private static void Main(string[] args)
    {
        CreateProject();
    }

    static ProjectDefaults GetProjectDefaults()
    {

        string FileContent;

        
        using (StreamReader Reader = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Config.json"))
        {
            FileContent = Reader.ReadToEnd();
            Reader.Close();
        }

        return JsonConvert.DeserializeObject<ProjectDefaults>(FileContent);

    }

    static string CreateUproject(string ModuleName, string EngineVersion)
    {

        //Create uproject and primary module.
        ProjectModule MainModule = new ProjectModule(ModuleName);
        UProject NewProject = new UProject();

        List<ProjectModule> NewModules = new List<ProjectModule>();
        NewModules.Add(MainModule);

        NewProject.Modules = NewModules;
        NewProject.EngineAssociation = EngineVersion;

        return JsonConvert.SerializeObject(NewProject, Formatting.Indented);

    }

    static void CreateProject()
    {

        //Gather information about project.
        string ProjectDirectory = "", ProjectName = "", PrimaryModuleName = "", EngineVersion = "", EpicGamesDirectory = "";

        //Get the project defaults.
        ProjectDefaults Defaults = GetProjectDefaults();
        EngineVersion = Defaults.EngineVersion;
        ProjectDirectory = Defaults.ProjectDirectory;
        EpicGamesDirectory = Defaults.EpicGamesDirectory;

        PromptInformation(ref ProjectDirectory, ref ProjectName, ref PrimaryModuleName, ref EngineVersion, ref EpicGamesDirectory);

        //--FILE CONSTRUCTION--//

        if (Directory.Exists(ProjectDirectory + ProjectName))
        {
            Console.WriteLine("A directory of this name already exists.");
            return;
        }

        //The directory the uproject is located in.
        string UProjectDirectory = ProjectDirectory + ProjectName + "/";

        //The directory of the uproject.
        string UProjectFileDir = UProjectDirectory + ProjectName + ".uproject";

        //The directory of the project source.
        string SourceDirectory = UProjectDirectory + "Source/";

        //The directory of the primary module.
        string ModuleDirectory = SourceDirectory + PrimaryModuleName + "/";

        //Create project directory and uproject file.
        Directory.CreateDirectory(UProjectDirectory);
        File.Create(UProjectFileDir).Dispose();

        CreateProjectScripts(UProjectDirectory, ProjectName, EngineVersion, EpicGamesDirectory);

        //add uproject contents.
        using(StreamWriter Writer = new StreamWriter(UProjectFileDir, false))
        {
            Writer.WriteLine(CreateUproject(PrimaryModuleName, EngineVersion));

            Writer.Close();
        }

        //Create module directories.
        Directory.CreateDirectory(ModuleDirectory);
        Directory.CreateDirectory(ModuleDirectory + "Public");
        Directory.CreateDirectory(ModuleDirectory + "Private");

        //Create build information.
        CreateTargets(ProjectName, ProjectDirectory + ProjectName + "/Source/", PrimaryModuleName);
        CreateModuleBuild(PrimaryModuleName, ModuleDirectory);
    }

    static void CreateProjectScripts(string UProjectDirectory, string ProjectName, string UEVersion, string EGPath)
    {
        File.Create(UProjectDirectory + "build.bat").Dispose();
        File.Create(UProjectDirectory + "vars.bat").Dispose();
        File.Create(UProjectDirectory + "open.bat").Dispose();

        using (StreamWriter Writer = new StreamWriter(UProjectDirectory + "Vars.bat"))
        {
            Writer.WriteLine(VarsScriptBase.Replace("BASENAME", ProjectName).Replace("BASEVER", UEVersion).Replace("EGPATH", EGPath));
            Writer.Close();
        }

        using (StreamWriter Writer = new StreamWriter(UProjectDirectory + "Build.bat"))
        {
            Writer.WriteLine(BuildScriptBase);
            Writer.Close();
        }

        using (StreamWriter Writer = new StreamWriter(UProjectDirectory + "Open.bat"))
        {
            Writer.WriteLine(OpenScriptBase);
            Writer.Close();
        }
    }

    static void PromptInformation(ref string ProjectDir, ref string ProjectName, ref string PrimaryModuleName, ref string EngineVersion, ref string EpicGamesDirectory)
    {

        //Directory prompt.

        if (ProjectDir.Length == 0)
        {
            Console.WriteLine("Please enter a directory for the new project to be created in (this can be set in the config file): \n");
            ProjectDir = Console.ReadLine();
            Console.WriteLine("Chosen Project Directory: " + ProjectDir + "\n");
        }

        if(ProjectDir.Length != 0)
        {
            //Project name prompt.
            Console.WriteLine("Please input a project name: \n");
            ProjectName = Console.ReadLine().Replace(" ", "");
            Console.WriteLine("Chosen Project Name: " + ProjectName + "\n");

            if(ProjectName.Length != 0)
            {
                if (EngineVersion.Length == 0)
                {
                    Console.WriteLine("Please input an engine version (This can be set in the config file): \n");
                    EngineVersion = Console.ReadLine();
                    Console.WriteLine("Chosen Engine Version: " + EngineVersion + "\n");
                }

                if (EngineVersion.Length != 0)
                {
                    //Primary module name prompt.
                    Console.WriteLine("Please input a primary module name: \n");
                    PrimaryModuleName = Console.ReadLine();
                    Console.WriteLine("Chosen Module Name: " + PrimaryModuleName + "\n");

                    if (PrimaryModuleName.Length != 0)
                    {
                        //Add slash to end of directory if needed.
                        bool DirEndsInSlash = ProjectDir.EndsWith('\\') || ProjectDir.EndsWith('/');

                        if (!DirEndsInSlash)
                            ProjectDir += "/";

                        if(PrimaryModuleName.Length != 0)
                        {
                            if(EpicGamesDirectory.Length == 0)
                            {
                                Console.WriteLine("Please input the epic games directory (This can be set in the config file): \n");
                                EpicGamesDirectory = Console.ReadLine();
                                Console.WriteLine("Chosen Epic Directory");
                            }
                        }

                    }
                }
            }
        }
    }

    static void CreateTargets(string ProjectName, string SourceDir, string ModuleName)
    {

        //Create project targets.
        string EditorTargetDir = SourceDir + ProjectName + "Editor.Target.cs";
        string TargetDir = SourceDir + ProjectName + ".Target.cs";

        File.Create(TargetDir).Close();
        File.Create(EditorTargetDir).Close();

        //Add target contents
        using (StreamWriter Writer = new StreamWriter(EditorTargetDir))
        {
            Writer.WriteLine(EditorTargetBase.Replace("Game", "Editor").Replace("BASENAME ", ProjectName).Replace("MODULENAME", ModuleName));
            Writer.Close();
        }

        using(StreamWriter Writer = new StreamWriter(TargetDir))
        {
            Writer.WriteLine(TargetBase.Replace("BASENAME ", ProjectName).Replace("MODULENAME", ModuleName));
            Writer.Close();
        }
    }

    static void CreateModuleBuild(string ModuleName, string ModuleDirectory)
    {

        //Create build.cs file
        string BuildDir = ModuleDirectory + ModuleName + ".Build.cs";
        File.Create(BuildDir).Dispose();

        string BuildContent = BuildBase.Replace("BASENAME", ModuleName);

        //Add build.cs contents.
        using(StreamWriter Writer = new StreamWriter(BuildDir))
        {
            Writer.WriteLine(BuildContent);
            Writer.Close();
        }

        string CPPDir = ModuleDirectory + "Private/" + ModuleName + ".cpp";
        File.Create(CPPDir).Dispose();

        using(StreamWriter Writer = new StreamWriter(CPPDir))
        {
            Writer.WriteLine(ModuleCPPBase.Replace("BASENAME", ModuleName));
            Writer.Close();
        }

        string HDir = ModuleDirectory + "Public/" + ModuleName + ".h";
        File.Create(HDir).Dispose();

        using (StreamWriter Writer = new StreamWriter(HDir))
        {
            Writer.WriteLine(ModuleHBase.Replace("BASENAME", ModuleName));
            Writer.Close();
        }
    }
}