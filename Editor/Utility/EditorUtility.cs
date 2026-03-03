using System.Collections;
using System.Reflection;
using Knitting.Interfaces;
using UnityEditor;
using UnityEngine;

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

        public static void DrawDictionary(string headerName, IDictionary dictionaryToDisplay, bool playModeOnly, ref Vector2 scrollPosition)
        {
            EditorGUILayout.Space();
            if (playModeOnly && !Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(headerName, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Enter Play Mode to inspect runtime dictionary.", MessageType.None);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Variables", EditorStyles.boldLabel);

            if (dictionaryToDisplay == null)
            {
                EditorGUILayout.HelpBox("Dictionary is null (probably not initialized yet).", MessageType.Warning);
                return;
            }

            if (dictionaryToDisplay.Count == 0)
            {
                EditorGUILayout.HelpBox("Dictionary is empty.", MessageType.None);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (DictionaryEntry entry in dictionaryToDisplay)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(entry.Key?.ToString(), GUILayout.Width(150));
                EditorGUILayout.LabelField(entry.Value?.ToString());

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        
        public static void DrawVariableContext(string headerName, IVariableContext variableContext, bool playModeOnly, ref Vector2 scrollPosition)
        {
            EditorGUILayout.Space();

            if (playModeOnly && !Application.isPlaying)
            {
                EditorGUILayout.LabelField(headerName, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to inspect runtime variables.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.LabelField(headerName, EditorStyles.boldLabel);

            if (variableContext == null)
            {
                EditorGUILayout.HelpBox(
                    "Variable context is null.",
                    MessageType.Warning);
                return;
            }

            // Attempt to extract backing dictionary via reflection
            IDictionary dictionary = ExtractDictionary(variableContext);

            if (dictionary == null)
            {
                EditorGUILayout.HelpBox(
                    "Unable to extract variables (implementation does not expose enumerable data).",
                    MessageType.Warning);
                return;
            }

            if (dictionary.Count == 0)
            {
                EditorGUILayout.HelpBox("No variables present.", MessageType.None);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (DictionaryEntry entry in dictionary)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(entry.Key?.ToString(), GUILayout.Width(150));
                EditorGUILayout.LabelField(entry.Value?.ToString());

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
        
        public static IDictionary ExtractDictionary(IVariableContext context)
        {
            var fields = context.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (typeof(IDictionary).IsAssignableFrom(field.FieldType))
                {
                    return field.GetValue(context) as IDictionary;
                }
            }

            return null;
        }
    }
}