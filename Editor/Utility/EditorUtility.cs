using UnityEditor;

namespace Knitting.Utility
{
    public static class EditorUtility
    {
        public static void CreateFoldersRecursively(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            string[] folders = path.Split(new[] { '/', '\\' });
            string currentPath = "";

            foreach (string folder in folders)
            {
                if (string.IsNullOrEmpty(folder)) continue;

                currentPath = string.IsNullOrEmpty(currentPath) ? folder : $"{currentPath}/{folder}";

                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    string parentPath = System.IO.Path.GetDirectoryName(currentPath);
                    string folderName = System.IO.Path.GetFileName(currentPath);
                    AssetDatabase.CreateFolder(parentPath, folderName);
                }
            }
        }
    }
}