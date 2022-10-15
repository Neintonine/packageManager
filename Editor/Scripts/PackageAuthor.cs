using UnityEngine;

namespace Package_Manager
{
    [CreateAssetMenu(fileName = "Author", menuName = "Package Folder/Author", order = 0)]
    public class PackageAuthor : ScriptableObject
    {
        public bool HasEMail => !string.IsNullOrEmpty(_email);
        public bool HasURL => !string.IsNullOrEmpty(_url);

        public string Name => _name;
        public string EMail => _email;
        public string URL => _url;
        
        [SerializeField] private string _name;
        [SerializeField] private string _email;
        [SerializeField] private string _url;
    }
}