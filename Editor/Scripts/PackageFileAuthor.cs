using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Package_Manager
{
    public class PackageFileAuthor : EditorWindow
    {
        public class Dependency
        {
            public string PackageName;
            public string PackageVersion;
        }

        private class Sample
        {
            public string DisplayName;
            public string Description;
            public string Path = "Samples~/";
        }

        private string _path;
        private bool _isPackage;
        private JObject _baseJson;

        private string _identifier;
        private string _version;
        private string _description;
        private string _displayName;
        private string _unity;
        private string _authorName;
        private string _authorEMail;
        private string _authorURL;
        private string _changelogUrl;
        private string _documentationUrl;
        private bool _hideInEditor;
        private List<string> _keywords;
        private string _license;
        private string _licenseUrl;
        private string _unityRelease;
        private List<Dependency> _dependencies;
        private List<Sample> _samples;

        private bool _keywordListFoldout;
        private bool _dependencyListFoldout;
        private bool _sampleListFoldout;
        private Vector2 _scrollPos;
        
        [MenuItem("Window/Package Author Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<PackageFileAuthor>();
            window.titleContent = new GUIContent("Package Author Tool");
            window.Show();
        }

        private void OnSelectionChange()
        {
            _dependencyListFoldout = false;
            _keywordListFoldout = false;
            _sampleListFoldout = false;
            _scrollPos = Vector2.zero;

            _isPackage = IsPackageFile(out Object obj);
            if (!_isPackage)
            {
                Repaint();
                return;
            }

            _path = AssetDatabase.GetAssetPath(obj);
            AssetDatabase.ImportAsset(_path);
            InterpretFile(obj);

            Repaint();
        }
        private void InterpretFile(Object obj)
        {
            _baseJson = JObject.Parse(obj.ToString());

            string[] version = Application.unityVersion.Split(new char[] {'.'}, 3);

            
            InputStringData( nameof(_identifier), "name", "<REQUIRED>");
            InputStringData( nameof(_version), "version", "1.0.0");
            InputStringData(nameof(_description), "description", "");
            InputStringData(nameof(_displayName), "displayName", _baseJson.Value<string>("name"));
            InputStringData(nameof(_unity), "unity", string.Join(".", version[0], version[1]));
            InputStringData(nameof(_changelogUrl), "changelogUrl", "");
            InputStringData(nameof(_documentationUrl), "documentationUrl", "");
            InputStringData(nameof(_license), "license", "MIT");
            InputStringData(nameof(_licenseUrl), "licenseURL", "");
            InputStringData(nameof(_unityRelease), "unityRelease", "");
            InputStringData(nameof(_authorName), "author.name", "");
            InputStringData(nameof(_authorEMail), "author.email", "");
            InputStringData( nameof(_authorURL), "author.url", "");

            _hideInEditor = true;
            if (_baseJson.ContainsKey("hideInEditor"))
                _hideInEditor = _baseJson.Value<bool>("hideInEditor");
            
            _keywords = new List<string>();
            if (_baseJson.ContainsKey("keywords"))
                _keywords = _baseJson.Values<string>("keywords").ToList();
            
            _dependencies = new List<Dependency>();
            if (_baseJson.ContainsKey("dependencies"))
            {
                JObject dependencyObj = _baseJson.GetValue("dependencies") as JObject;
                foreach (KeyValuePair<string,JToken> keyValuePair in dependencyObj)
                {
                    _dependencies.Add(new Dependency()
                    {
                        PackageName = keyValuePair.Key,
                        PackageVersion = keyValuePair.Value.ToString()
                    });
                }
            }
            
            _samples = new List<Sample>();
            if (_baseJson.ContainsKey("samples"))
            {
                _samples = _baseJson.Values<Sample>().ToList();
            }
        }
        private void InputStringData(string propertyName, string jsonName, string defaultValue)
        {
            string value = defaultValue;
            JToken token = _baseJson.SelectToken(jsonName);
            if (token != null) value = token.ToString();

            FieldInfo info = GetType().GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
            info.SetValue(this, value);
        }

        private void OnGUI()
        {
            if (!_isPackage)
            {
                EditorGUILayout.LabelField("Please select a package file");
                
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            EditorGUIExt.HeaderField("Data");
            
            _identifier = EditorGUILayout.TextField("Identifier", _identifier);
            _version = EditorGUILayout.TextField("Package Version", _version);
            _unity = EditorGUILayout.TextField("Unity Version", _unity);
            _unityRelease = EditorGUILayout.TextField("Unity Release", _unityRelease);
            
            EditorGUILayout.Space();
            EditorGUIExt.HeaderField("Display");
            
            _displayName = EditorGUILayout.TextField("Display Name", _displayName);
            EditorGUILayout.LabelField("Description");
            _description = EditorGUILayout.TextArea(_description, GUILayout.MaxHeight(75));
            _hideInEditor = EditorGUILayout.Toggle("Hide in Editor", _hideInEditor);
            
            EditorGUILayout.Space();
            EditorGUIExt.HeaderField("Author");
            _authorName = EditorGUILayout.TextField("Author Name", _authorName);
            _authorEMail = EditorGUILayout.TextField("Author E-Mail", _authorEMail);
            _authorURL = EditorGUILayout.TextField("Author URL", _authorURL);
            
            EditorGUILayout.Space();
            EditorGUIExt.HeaderField("Publications");
            _changelogUrl = EditorGUILayout.TextField("Changelog URL", _changelogUrl);
            _documentationUrl = EditorGUILayout.TextField("Documentation URL", _documentationUrl);
            _license = EditorGUILayout.TextField("License", _license);
            _license = EditorGUILayout.TextField("License URL", _licenseUrl);

            GUIStyle keywordStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Normal
            };

            _keywordListFoldout = EditorListDisplay.Display(_keywordListFoldout, "Keywords", keywordStyle,
                _keywords, s => {
                    _keywords[s] = EditorGUILayout.TextField(_keywords[s]);
                }, () => _keywords.Add(""), s => _keywords.Remove(s), Repaint);
            
            EditorGUILayout.Space();

            _dependencyListFoldout = EditorListDisplay.Display(_dependencyListFoldout, "Dependencies", _dependencies, i => {
                Dependency dependency = _dependencies[i];
                EditorGUILayout.LabelField("Dependency "+ (i+1) + (!string.IsNullOrEmpty(dependency.PackageName) ? " - " + dependency.PackageName : ""), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                dependency.PackageName = EditorGUILayout.TextField("Package Identifier", dependency.PackageName);
                dependency.PackageVersion = EditorGUILayout.TextField("Package Version", dependency.PackageVersion);
            }, () => {
                _dependencies.Add(AddDependency.ShowWindow());
            }, dependency => _dependencies.Remove(dependency), Repaint);
            
            _sampleListFoldout = EditorListDisplay.Display(_sampleListFoldout, "Samples", _samples, i => {
                Sample sample = _samples[i];
                EditorGUILayout.LabelField("Sample "+ (i+1) + (!string.IsNullOrEmpty(sample.DisplayName) ? " - " + sample.DisplayName : ""), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                sample.DisplayName = EditorGUILayout.TextField("Name", sample.DisplayName);
                EditorGUILayout.LabelField("Description");
                sample.Description = EditorGUILayout.TextArea(sample.Description, GUILayout.MaxHeight(75));
                
                sample.Path = EditorGUILayout.TextField("Path", sample.Path);
            }, () => _samples.Add(new Sample()), sample => _samples.Remove(sample), Repaint);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("");
            
            if (GUILayout.Button("Apply", GUILayout.MaxWidth(50)))
            {
                ApplyData();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
        private void ApplyData()
        {
            JObject jObject = new JObject();

            AddStringJSON(jObject, "name", _identifier);
            AddStringJSON(jObject, "version", _version);
            AddStringJSON(jObject, "description", _description);
            AddStringJSON(jObject, "displayName", _displayName);
            AddStringJSON(jObject, "unity", _unity);
            AddStringJSON(jObject, "unityRelease", _unityRelease);
            AddStringJSON(jObject, "changelogUrl", _changelogUrl);
            AddStringJSON(jObject, "documentationUrl", _documentationUrl);
            AddStringJSON(jObject, "license", _license);
            AddStringJSON(jObject, "licenseURL", _licenseUrl);
            jObject.Add("author", JObject.FromObject(
                new {
                    name = _authorName,
                    email = _authorEMail,
                    url = _authorURL
                })
            );
            jObject.Add("hideInEditor", _hideInEditor);

            if (_keywords.Count > 0)
            {
                JArray keywordList = new JArray(_keywords);
                jObject.Add("keywords", keywordList);
            }

            if (_dependencies.Count > 0)
            {
                JObject dependencies = new JObject();
                foreach (Dependency dependency in _dependencies) dependencies.Add(dependency.PackageName, dependency.PackageVersion);
                jObject.Add("dependencies", dependencies);
            }

            if (_samples.Count > 0)
            {
                JArray samples = new JArray();
                foreach (Sample sample in _samples)
                {
                    samples.Add(JObject.FromObject(sample));
                }
                jObject.Add("samples", samples);
            }
            
            Debug.Log(_path);
            Debug.Log(Path.GetFullPath(Path.Combine(Application.dataPath, "..", _path)));
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", _path)), jObject.ToString());
            AssetDatabase.ImportAsset(_path);
        }
        private void AddStringJSON(JObject jObject, string s, string obj)
        {
            if (string.IsNullOrWhiteSpace(obj)) return;
            
            jObject.Add(s, obj);
        }

        private bool IsPackageFile(out Object resultObject)
        {
            resultObject = null;
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);

                if (path.EndsWith("package.json"))
                {
                    resultObject = obj;
                    return true;
                }
            }
            return false;
        }
    }
}