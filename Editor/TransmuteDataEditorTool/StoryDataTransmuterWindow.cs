using System.Collections.Generic;
using Knitting.Types;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Knitting.TransmuteDataEditorTool
{
    public class StoryDataTransmuterWindow : EditorWindow
    {
        private StoryDataTransmuter _target;
        private SerializedObject _serializedObject;

        private GUIStyle _sectionStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _bigButtonStyle;
        private SerializedProperty _folderPathToSaveTransmutationProperty;
        private SerializedProperty _allowOverwriteProperty;
        private SerializedProperty _makeStorySOProperty;
        private SerializedProperty _storyDataNewTypeProperty;
        private SerializedProperty _makeStoryNodeSOProperty;
        private SerializedProperty _storyNodeDataNewTypeProperty;
        private SerializedProperty _saveChoicesAsReferencesProperty;

        [MenuItem("Knitting/Story Data Transmuter")]
        public static void Open()
        {
            var window = GetWindow<StoryDataTransmuterWindow>();
            window.titleContent = new GUIContent("Story Transmuter");
            window.minSize = new Vector2(500, 600);
        }
        
        public static void Open(StoryDataTransmuter transmuterToUse)
        {
            var window = GetWindow<StoryDataTransmuterWindow>();
            window.titleContent = new GUIContent("Story Transmuter");
            window.minSize = new Vector2(500, 600);
            window._target = transmuterToUse;
        }

        private void CacheProperties()
        {
            if (_serializedObject == null) return;
            
            _folderPathToSaveTransmutationProperty = _serializedObject.FindProperty(nameof(StoryDataTransmuter.FolderPathToSaveTransmutation));
            _allowOverwriteProperty = _serializedObject.FindProperty(nameof(StoryDataTransmuter.AllowOverwrite));
            
            _makeStorySOProperty = _serializedObject.FindProperty(nameof(StoryDataTransmuter.MakeStoryDataScriptableObject));
            _storyDataNewTypeProperty = _serializedObject.FindProperty(nameof(StoryDataTransmuter.StoryDataNewType));
            
            _makeStoryNodeSOProperty = _serializedObject.FindProperty(nameof(StoryDataTransmuter.MakeStoryDataNodeScriptableObjects));
            _storyNodeDataNewTypeProperty = _serializedObject.FindProperty(nameof(StoryDataTransmuter.StoryNodeDataNewType));
            
            _saveChoicesAsReferencesProperty = _serializedObject.FindProperty(nameof(StoryDataTransmuter.SaveChoicesAsReferences));
        }

        private void CreateStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft
            };

            _sectionStyle = new GUIStyle("HelpBox")
            {
                padding = new RectOffset(15, 15, 12, 12)
            };

            _bigButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fixedHeight = 40
            };
        }

        private void OnGUI()
        {
            CreateStyles();
            CacheProperties();
            
            DrawTopBar();

            EditorGUILayout.Space(10);

            DrawAssetSelector();

            if (_target == null)
                return;

            if (_serializedObject == null || _serializedObject.targetObject != _target)
                _serializedObject = new SerializedObject(_target);

            _serializedObject.Update();

            CacheProperties();
            
            DrawInputSection();
            DrawOutputSection();
            DrawExecutionSection();

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Story Data Transmuter", _headerStyle);
            EditorGUILayout.LabelField(
                "Generate baked ScriptableObjects from Twine story data.",
                EditorStyles.miniLabel);
        }

        private void DrawAssetSelector()
        {
            EditorGUILayout.Space(10);

            _target = (StoryDataTransmuter)EditorGUILayout.ObjectField(
                "Configuration Asset",
                _target,
                typeof(StoryDataTransmuter),
                false);
        }

        private void DrawInputSection()
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_serializedObject.FindProperty("TwineTextFile"));

            EditorGUILayout.EndVertical();
        }

        private void DrawOutputSection()
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_folderPathToSaveTransmutationProperty);
            EditorGUILayout.PropertyField(_allowOverwriteProperty);

            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(_makeStorySOProperty);
            if (_makeStorySOProperty.boolValue)
            {
                EditorGUILayout.PropertyField(_storyDataNewTypeProperty);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_makeStoryNodeSOProperty);
            if (_makeStoryNodeSOProperty.boolValue)
            {
                EditorGUILayout.PropertyField(_storyNodeDataNewTypeProperty);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_saveChoicesAsReferencesProperty);

            EditorGUILayout.EndVertical();
        }

        private void DrawExecutionSection()
        {
            EditorGUILayout.Space(20);

            bool valid = Validate();

            List<string> warnings = Warnings();

            if (!valid)
            {
                EditorGUILayout.HelpBox(
                    "Missing required configuration before transmutation.",
                    MessageType.Warning);
            }
            else
            {
                foreach (string warning in warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }

            GUI.enabled = valid;

            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);

            if (GUILayout.Button("TRANSMUTE STORY DATA", _bigButtonStyle))
            {
                ExecuteTransmutation();
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }

        private List<string> Warnings()
        {
            List<string> warnings = new();
            
            if (_makeStorySOProperty.boolValue)
            {
                ScriptableObjectTypeReference typeReference = _storyDataNewTypeProperty.boxedValue as ScriptableObjectTypeReference;
                
                if (typeReference.Type != null && typeReference.Type.FullName.Contains("Editor"))
                {
                    warnings.Add("The type used for the transmutation Story SO uses or references UnityEditor, which won't compile in a build.");
                }
            }
            
            if (_makeStoryNodeSOProperty.boolValue)
            {
                ScriptableObjectTypeReference typeReference = _storyNodeDataNewTypeProperty.boxedValue as ScriptableObjectTypeReference;
                if (typeReference.Type != null && typeReference.Type.FullName.Contains("Editor"))
                {
                    warnings.Add("The type used for the transmutation Story Node SO uses or references UnityEditor, which won't compile in a build.");
                }
            }

            return warnings;
        }

        private bool Validate()
        {
            if (_target.TwineTextFile == null)
                return false;

            if (_target.MakeStoryDataScriptableObject &&
                _target.StoryDataNewType?.Type == null)
                return false;

            if (_target.MakeStoryDataNodeScriptableObjects &&
                _target.StoryNodeDataNewType?.Type == null)
                return false;

            return true;
        }

        private void ExecuteTransmutation()
        {
            try
            {
                UnityEditor.EditorUtility.DisplayProgressBar(
                    "Transmuting Story",
                    "Generating ScriptableObjects...",
                    0.5f);

                _target.Transmute();

                UnityEditor.EditorUtility.SetDirty(_target);
                AssetDatabase.SaveAssets();

                Debug.Log("Transmutation completed successfully.");
            }
            finally
            {
                UnityEditor.EditorUtility.ClearProgressBar();
            }
        }
    }
}