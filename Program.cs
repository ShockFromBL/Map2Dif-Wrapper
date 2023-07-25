using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// TODO: Get texture paths from .map files instead using regex?
// The regex in question: /^(?:\s+)?(?:\(\s(?:\-?\d+(?:\.\d+(?:e\-\d+)?)?\s){3}\)\s?){3}\s([A-z\d\.\_\-\/]+)/gm

// TODO: Copy .map file to %TEMP% so that stuff like "auxiliary\NULL" can be changed to "NULL" instead without affecting original file?

namespace Map2DifWrapper
{
    class Config
    {
        public string TexturesPath { get; set; }

        public bool CopyTextures { get; set; }

        public bool SilentMode { get; set; }
    }

    class Program
    {
        public static string version = "1.0.0";

        public static Config config;

        static int Main(string[] args)
        {
            if (!File.Exists("map2dif_wrapper.yaml"))
            {
                Debug.WriteLine("Config file not found.");

                if (!CreateConfigFile())
                {
                    ShowMessage("Failed to create config file in the current directory.");
                    return -1;
                }

                Debug.WriteLine("Config file created.");
            }

            if (!ReadConfigFile())
            {
                ShowMessage("The config file is invalid.");
                return -1;
            }

            if (!ValidateConfig())
            {
                return -1;
            }

            string[] fileNames = { "map2dif_plus.exe", "map2dif_plus_MBG.exe", "map2dif.exe", "map2dif_DEBUG.exe" };
            string selectedFileName = null;

            foreach (string fileName in fileNames)
            {
                if (File.Exists(fileName))
                {
                    Debug.WriteLine("Found " + fileName);

                    selectedFileName = fileName;

                    break;
                }
            }

            if (String.IsNullOrEmpty(selectedFileName))
            {
                ShowMessage("Could not find a map2dif executable in the current directory.");
                return -1;
            }

            Process map2dif = new Process();
            map2dif.StartInfo.FileName = selectedFileName;
            map2dif.StartInfo.UseShellExecute = false;
            map2dif.StartInfo.RedirectStandardOutput = true;

            string textureArg = null;

            if (args.Length > 0)
            {
                bool isTextureArgNext = false;

                foreach (string arg in args)
                {
                    if (isTextureArgNext)
                    {
                        textureArg = arg.Trim().Replace('/', '\\');

                        break;
                    }

                    if (arg == "-t")
                    {
                        isTextureArgNext = true;
                    }
                }

                string map2difArgs = String.Join(" ", args).Trim();

                Regex isPath = new Regex(@"^[A-z]\:\\", RegexOptions.Compiled);

                if (isPath.Match(args[0]).Success)
                {
                    map2difArgs = "\"" + map2difArgs.Trim('"') + "\"";
                }

                if (!config.SilentMode)
                {
                    Console.WriteLine(selectedFileName + " " + map2difArgs);
                }

                Debug.WriteLine(selectedFileName + " " + map2difArgs);

                map2dif.StartInfo.Arguments = map2difArgs;
            }
            else
            {
                if (!config.SilentMode)
                {
                    Console.WriteLine(selectedFileName);
                }

                Debug.WriteLine(selectedFileName);
            }

            map2dif.Start();

            string output = map2dif.StandardOutput.ReadToEnd();

            string[] lines = output.Split('\n');

            Regex isTexture = new Regex(@"^\s+Unable\ to\ load\ texture\ (.+)$", RegexOptions.Compiled);

            bool rerun = false;

            foreach (string line in lines)
            {
                MatchCollection matches = isTexture.Matches(line);

                if (matches.Count > 0)
                {
                    rerun = true;

                    foreach (Match match in matches)
                    {
                        string texture = match.Groups[1].Value.Replace('/', '\\');
                        string texturePath = config.TexturesPath + "\\" + texture;

                        CopyTexture(texturePath, textureArg); // FIXME: Why doesn't textureArg work? Torque or Map2Dif isn't honoring the -t flag and still reads the textures from the root regardless of what it is set to.
                    }
                }
                else
                {
                    Console.WriteLine(line);
                    Debug.Write(line);
                }
            }

            map2dif.WaitForExit();

            int exitCode = map2dif.ExitCode;

            if (rerun && exitCode == 0)
            {
                map2dif.Start();
                map2dif.WaitForExit();

                exitCode = map2dif.ExitCode;
            }

            if (exitCode != 0)
            {
                if (args.Length > 0)
                {
                    string reason = "Generic error";

                    if (exitCode == -2147483645)
                    {
                        reason = "Invalid argument supplied";
                    }

                    ShowMessage("" + selectedFileName + " exited with error code " + map2dif.ExitCode.ToString() + " (" + reason + ")");
                }

                return map2dif.ExitCode;
            }

            Debug.WriteLine("" + selectedFileName + " exited with code 0 (Success)");

            return 0;
        }

        static void CopyTexture(string source, string texturesPath = null)
        {
            if (!config.CopyTextures)
            {
                return;
            }

            string[] extensions = { "jpg", "jpeg", "png", "bmp", "gif" };
            int fileCount = 0;

            foreach (string extension in extensions)
            {
                string texturePath = source.Trim() + "." + extension;

                if (File.Exists(texturePath))
                {
                    string textureFileName = Path.GetFileName(texturePath);

                    fileCount++;

                    Debug.WriteLine(texturePath);

                    if (!String.IsNullOrEmpty(texturesPath))
                    {
                        textureFileName = texturesPath + "\\" + textureFileName;

                        Directory.CreateDirectory(Path.GetDirectoryName(textureFileName));
                    }

                    File.Copy(texturePath, textureFileName, true);
                }
            }

            if (fileCount > 1)
            {
                // DisplayMessage("Warning, texture with more than one file of the same name detected.");
                Debug.WriteLine("Duplicate texture found.");
            }
        }

        static bool CreateConfigFile()
        {
            var config = new Config {
                TexturesPath = null,
                CopyTextures = true,
                SilentMode = false
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(config);

            using (StreamWriter file = new StreamWriter("map2dif_wrapper.yaml"))
            {
                file.Write(yaml);
            }

            if (File.Exists("map2dif_wrapper.yaml"))
            {
                return true;
            }

            return false;
        }

        static bool ReadConfigFile()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            string yaml = null;

            using (StreamReader file = new StreamReader("map2dif_wrapper.yaml"))
            {
                yaml = file.ReadToEnd();
            }

            try
            {
                var config = deserializer.Deserialize<Config>(yaml);

                Program.config = config;
            }
            catch // TODO: lazy.
            {
                return false;
            }

            return true;
        }

        static bool ValidateConfig()
        {
            if (String.IsNullOrEmpty(config.TexturesPath))
            {
                ShowMessage("texturesPath has not been set.");
                return false;
            }

            if (!Directory.Exists(config.TexturesPath))
            {
                ShowMessage("texturesPath \"" + config.TexturesPath + "\" could not be found.");
                return false;
            }

            return true;
        }

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr h, string m, string c, int type);

        static void ShowMessage(string text)
        {
            if (!config.SilentMode)
            {
                MessageBox((IntPtr)0, text, "Map2Dif Wrapper", 0);
            }
            else
            {
                Console.WriteLine(text);
            }

            Debug.WriteLine(text);
        }
    }
}
