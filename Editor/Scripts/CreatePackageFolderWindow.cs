using System.IO;
using System.Text;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Package_Manager
{
    public class CreatePackageFolderWindow : EditorWindow
    {
        
        private string _folder = "";

        private string _version = "";
        private string _identifier = "";

        private PackageAuthor _author;

        private string _unity;

        private bool _createEditorFolder;
        private bool _createRuntimeFolder;
        private bool _createTestFolders;
        private bool _createSampleFolder;

        [MenuItem("Assets/Create/Package Folder/Folder")]
        private static void ShowWindow()
        {
            
            
            var window = GetWindow<CreatePackageFolderWindow>();

            string[] version = Application.unityVersion.Split(new char[] {'.'}, 3);
            
            window._folder = GetSelectedPathOrFallback();
            window._version = "1.0.0";
            window._unity = string.Join(".", version[0], version[1]);

            window.titleContent = new GUIContent("Create Package Folder");
            window.Show();
        }

        private void OnEnable()
        {
            _createEditorFolder = true;
            _createRuntimeFolder = true;
        }

        private void OnGUI()
        {
            StringBuilder error = new StringBuilder();
            
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..",_folder, _identifier));
            EditorGUILayout.LabelField("Unity Folder: " + _folder + "/"+_identifier);

            GUIStyle layout = new GUIStyle(GUI.skin.label);

            bool allowButton = true;
            if (Directory.Exists(folder + Path.DirectorySeparatorChar))
            {
                layout.normal.textColor = Color.red;
                error.AppendLine("- Folder must be valid");
                allowButton = false;
            }
            EditorGUILayout.LabelField("Folder: " + folder, layout);
            
            EditorGUILayout.Space(2);
            EditorGUIExt.HeaderField("Package");
            _identifier = EditorGUILayout.TextField("Folder Name",_identifier);
            _author = EditorGUILayout.ObjectField("Author", _author, typeof(PackageAuthor), false) as PackageAuthor;
            _version = EditorGUILayout.TextField("Package Version", _version);
            
            EditorGUILayout.Space(2);
            _unity = EditorGUILayout.TextField("Unity Version", _unity);

            EditorGUILayout.Space(2);
            EditorGUIExt.HeaderField("Needed Folders");
            _createEditorFolder = GUILayout.Toggle(_createEditorFolder, "Editor Folder");
            _createRuntimeFolder = GUILayout.Toggle(_createRuntimeFolder, "Runtime Folder");
            _createTestFolders = GUILayout.Toggle(_createTestFolders, "Test Folders");
            _createSampleFolder = GUILayout.Toggle(_createSampleFolder, "Sample Folder");

            if (!_author)
            {
                allowButton = false;
                error.AppendLine("- Author required");
            }
            
            EditorGUILayout.Space(20);
            if (!allowButton)
            {
                EditorGUILayout.HelpBox(error.ToString(), MessageType.Info);
            }
            
            if (allowButton && GUILayout.Button("Create Folder"))
            {
                Close();
                CreateFolder();
            }
        }
        private void CreateFolder()
        {
            string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(_folder, _identifier));

            if (_createRuntimeFolder)
            {
                AssetDatabase.CreateFolder(AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(path, "Runtime")), "Scripts");
            }
            if (_createEditorFolder) AssetDatabase.CreateFolder(AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(path, "Editor")), "Scripts");


            if (_createTestFolders)
            {
                string testFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(path, "Tests"));

                if (_createRuntimeFolder) AssetDatabase.CreateFolder(testFolder, "Runtime");
                if (_createEditorFolder) AssetDatabase.CreateFolder(testFolder, "Editor");
                
            }
            if (_createSampleFolder) AssetDatabase.CreateFolder(path, "Samples~");

            JObject obj = JObject.FromObject(new
            {
                name = string.Join(".","com", _author.Name.ToLower(), _identifier.ToLower()),
                version = _version,
                unity = _unity,
                author = new {
                    name = _author.Name,
                    email = _author.EMail,
                    url = _author.URL
                }
            });

            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", _folder, _identifier, "package.json")), obj.ToString());
            AssetDatabase.ImportAsset(path);

        }
        static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
		
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if ( !string.IsNullOrEmpty(path) && File.Exists(path) ) 
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }

    }
}