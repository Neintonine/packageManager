using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Package_Manager
{
    public class AddDependency : EditorWindow
    {
        private Object _packageFile;
        
        public static PackageFileAuthor.Dependency ShowWindow()
        {
            var window = GetWindow<AddDependency>();
            window.titleContent = new GUIContent("Add Dependency");
            window.ShowModal();
            Debug.Log("Modal closed");
            
            JObject json = JObject.Parse(((TextAsset)window._packageFile).text);
            return new PackageFileAuthor.Dependency()
            {
                PackageName = json.Value<string>("name"),
                PackageVersion = json.Value<string>("version")
            };
        }

        private void OnGUI()
        {
            EditorGUIExt.CheckedObjectField("Package File", _packageFile, typeof(TextAsset), o => AssetDatabase.GetAssetPath(o).EndsWith("package.json"), out _packageFile);

            if (GUILayout.Button("Create"))
            {
                Close();
            }
        }
    }
}