#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Debug = UnityEngine.Debug;
namespace Package_Manager
{
    class PackageImporter : EditorWindow
    {
        enum PackageType
        {
            None,
            Registry,
            OpenUPM,
            Git,
            PackageFile,
        }

        class PackageTypeInformation
        {
            public PackageType Type;
            public string Identifier;
            public string Name;
        }
    
        class ParsingContext
        {
            public string OriginPath;
            public string CurrentPath;
        
            public List<string> Defines = new List<string>();
            public string Name;
            public Dictionary<PackageType, List<string>> Data = new Dictionary<PackageType, List<string>>();
        }
    
        private ParsingContext _context;
        private static bool _read;
        private static Dictionary<string, Action<ParsingContext, int, string>> _preProcesses = new Dictionary<string, Action<ParsingContext, int, string>>()
        {
            {
                "name",
                (context, index, s) => {
                    if (string.IsNullOrWhiteSpace(context.Name)) context.Name = s;
                }
            },
            {
                "if",
                (context,index, s) => {
                    _read = !context.Defines.Contains(s.ToUpper());
                }
            },
            {
                "nif",
                (context,index, s) => {
                    _read = context.Defines.Contains(s.ToUpper());
                }
            },
            {
                "endif",
                ((context,index, s) => _read = true)
            },
            {
                "define",
                (context,index, s) => {
                    if (context.Defines.Contains(s.ToUpper()))
                    {
                        SendWarning(context.CurrentPath, index, "the 'define'-preprocessor", $"{s.ToUpper()} is already set.");
                        return;
                    }
                
                    context.Defines.Add(s.ToUpper());
                }
            },
            {
                "include",
                (context,index, s) => {
                    string path = Path.Combine(Path.GetDirectoryName(context.CurrentPath), s);
                    if (!File.Exists(path))
                    {
                        SendWarning(context.CurrentPath, index, "the 'include'-preprocessor", $"Path {path} was not found.");
                        return;
                    }
                
                    ParseFile(path, context);
                }
            }
        };
        private static List<PackageTypeInformation> _packageInformations = new List<PackageTypeInformation>()
        {
            new PackageTypeInformation
            {
                Type = PackageType.Registry,
                Identifier = "registry",
                Name = "Registry"
            },
            new PackageTypeInformation()
            {
                Type = PackageType.OpenUPM,
                Identifier = "openupm",
                Name = "UPM"
            },
            new PackageTypeInformation()
            {
                Type = PackageType.Git,
                Identifier = "git",
                Name = "Git"
            },
            new PackageTypeInformation()
            {
                Type = PackageType.PackageFile,
                Identifier = "file",
                Name = "Package Folder"
            }
        };

        [MenuItem("Tools/Import Package Pack")]
        private static void ShowWindow()
        {
            string result = EditorUtility.OpenFilePanelWithFilters("Select the package pack", Path.Combine(Application.dataPath, "../.."), new [] {"Package Pack", "packagePack"});
       
            if (string.IsNullOrEmpty(result)) return;
        
            ParsingContext context = new ParsingContext()
            {
                OriginPath = result,
                CurrentPath = result,
                Name = ""
            };
            ParseFile(result, context);
            EditorUtility.ClearProgressBar();
        
            var window = GetWindow<PackageImporter>();
            window._context = context;
            window.titleContent = new GUIContent($"Import '{context.Name}'");
            window.Show();
        }

        private static void ParseFile(string file, ParsingContext context)
        {
            EditorUtility.DisplayProgressBar("Prepare package pack", $"Read File '{Path.GetFileName(file)}'", 0);
            string[] lines = File.ReadAllLines(file);
            float max = lines.Length;
            _read = true;

            string lastPath = context.CurrentPath;
            context.CurrentPath = file;
        
            for (int i = 0; i < max; i++)
            {
                EditorUtility.DisplayProgressBar("Prepare package pack", $"Interpret Line {i} - {max} from File '{Path.GetFileName(file)}'", i / max);
            
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

            
                if (line.StartsWith("#"))
                {
                    line = line.TrimStart(' ', '#');
                    string[] split = line.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);

                    if (!_read && split[0] == "endif") continue;
                
                    if (!_preProcesses.ContainsKey(split[0]))
                    {
                        SendWarning(context.CurrentPath, i, "the preprocesses", $"Command '{split[0]}' doesn't exist.");
                        continue;
                    }

                    _preProcesses[split[0]].Invoke(context, i, split.Length > 1 ? split[1] : "");
                
                    continue;
                }
                if (!_read) continue;

                PackageType type = PackageType.None;
                foreach (PackageTypeInformation packageInformation in _packageInformations)
                {
                    if (line.StartsWith(packageInformation.Identifier + ":"))
                    {
                        type = packageInformation.Type;
                        break;
                    }
                }

                if (type == PackageType.None)
                {
                    SendWarning(context.CurrentPath, i, "the type of package", "No Identifier was found.");
                
                    continue;
                }

                if (!context.Data.ContainsKey(type)) context.Data.Add(type, new List<string>());
            
                string content = line.Split(new[] {':'}, 2)[1];
                if (context.Data[type].Contains(content)) 
                    continue;
            
                context.Data[type].Add(content);

            }

            context.CurrentPath = lastPath;
            _read = true;
        }

        private static void SendWarning(string file, int line, string part, string message)
        { 
            Debug.LogWarning($"[{file}:{(line + 1)}]\nDuring processing of {part} went something wrong.\n{message}");
        }

        private bool[] _foldouts;
    
        private void OnGUI()
        {
            GUILayout.Label("Found Packages", EditorStyles.largeLabel);

            _foldouts = new bool[_context.Data.Count];
            foreach (PackageType type in _context.Data.Keys)
            {
                GUILayout.Label(_packageInformations.Find(a => a.Type == type).Name, EditorStyles.boldLabel);
                List<string> value = _context.Data[type];
                foreach (var s in value)
                {
                    EditorGUILayout.LabelField(" - " + s);
                }
                GUILayout.Space(10);
            }

            if (GUILayout.Button("Import")) ImportContext();
        }
        private async void ImportContext()
        {
            Close();

            if (_context.Data.ContainsKey(PackageType.OpenUPM))
            {
                string batPath = Path.Combine(Application.dataPath, "..", "openupmJob.bat");
                StringBuilder builder = new StringBuilder();
                foreach (string s in _context.Data[PackageType.OpenUPM])
                {
                    builder.Append($"{s} ");
                }
                builder.AppendLine("\nexit");
            
                File.WriteAllText(batPath, "openupm add "+builder.ToString());

                Process process = Process.Start(batPath);
                while (true)
                {
                    if (process.HasExited) break;
                    await Task.Yield();
                }
                File.Delete(batPath);
            }
        
            EditorUtility.DisplayProgressBar("Appling packages", $"Adding packages via Unity", 1);
            foreach (KeyValuePair<PackageType,List<string>> pair in _context.Data)
            {
                if (pair.Key == PackageType.None || pair.Key == PackageType.OpenUPM) continue;

                foreach (string s in pair.Value)
                {
                    string result = s;
                    switch (pair.Key)
                    {

                        case PackageType.Registry:
                            break;
                        case PackageType.Git:
                            break;
                        case PackageType.PackageFile:
                            result = result.Replace("package.json", "");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    EditorUtility.DisplayProgressBar("Appling packages", $"Adding package '{result}' via Unity", 1);
                    Debug.Log($"Installing {result}");
                
                    AddRequest request = Client.Add(result);
                    while (true)
                    {
                        if (!request.IsCompleted)
                        {
                            await Task.Yield();
                            continue;
                        }

                        if (request.Error != null)
                        {
                            Debug.LogError($"[{result}]\nError while adding Package:\n{request.Error.message}");
                        }

                        break;
                    }
                }
            }
            EditorUtility.ClearProgressBar();

        
            Debug.Log("Compiling Done");
        }
    }
}

#endif