using System.Collections;
using System.Linq;
using Knitting.Interfaces;
using Knitting.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using EditorUtility = Knitting.Utility.EditorUtility;

namespace Knitting.Debugging
{
    public class StoryDebuggerWindow : EditorWindow
    {
        private Story _story;
        private StoryNode _currentNode;

        private int _selectedChoice;
        private Vector2 _scroll;
        private bool _debugWithoutPlayMode;

        [MenuItem("Knitting/Story Debugger")]
        public static void Open()
        {
            GetWindow<StoryDebuggerWindow>("Story Debugger");
        }

        public static void Open(Story story)
        {
            GetWindow<StoryDebuggerWindow>("Story Debugger")._story = story;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            _story = (Story)EditorGUILayout.ObjectField(
                "Story",
                _story,
                typeof(Story),
                true);

            if (_story == null)
            {
                EditorGUILayout.HelpBox("Assign a Story to begin debugging.", MessageType.Info);
                return;
            }

            if (!_debugWithoutPlayMode && GUILayout.Button("Enable Debugging Without Play Mode"))
            {
                _debugWithoutPlayMode = true;
                _story.Awake();
            }
            
            if (!_debugWithoutPlayMode && !Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to debug the story.", MessageType.Warning);
                return;
            }

            float totalWidth = EditorGUIUtility.currentViewWidth;
            float spacing = 10f;
            float usableWidth = totalWidth - spacing;
            float firstColumnWidth = usableWidth * 0.65f;
            float secondColumnWidth = usableWidth * 0.35f;
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(firstColumnWidth));
            DrawStoryStateAndControls();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(spacing);
            
            EditorGUILayout.BeginVertical(GUILayout.Width(secondColumnWidth));

            IVariableContext variableContext = _story.GetVarContext();

            if (variableContext is SimpleVariableContextSO simpleVariableContext)
            {
                Editor.CreateEditor(simpleVariableContext).OnInspectorGUI();
            }
            else if (variableContext is Story.DictionaryVariableContext dictionaryVariableContext)
            {
                EditorUtility.DrawVariableContext("Variable Context (in story)", variableContext, true, ref _scroll);
            }
            else if (variableContext != null)
            {
                EditorUtility.DrawVariableContext("Variable Context (scriptable object)", variableContext, true, ref _scroll);
            }
            else
            {
                EditorGUILayout.LabelField("No variable context was found.");
            }

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Live repaint in play mode
            Repaint();
        }

        private void DrawStoryStateAndControls()
        {
            if (GUILayout.Button("Restart"))
            {
                _story.SetToStart();
                UpdateNode();
            }

            EditorGUILayout.Space();

            UpdateNode();

            if (_currentNode == null)
            {
                EditorGUILayout.HelpBox("No current node available.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Current Node", EditorStyles.boldLabel);

            var text = _currentNode.GetText();
            var style = EditorStyles.textArea;

            float height = style.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth - 40f); // subtract padding/margins

            EditorGUILayout.TextArea(text, style, GUILayout.Height(height));

            EditorGUILayout.Space();

            var nextNodes = _currentNode.GetNextNodes();

            if (nextNodes == null || nextNodes.Count == 0)
            {
                EditorGUILayout.HelpBox("No further nodes.", MessageType.Info);
                return;
            }

            if (nextNodes.Count == 1)
            {
                if (GUILayout.Button("Go Next"))
                {
                    _story.NextNode();
                    UpdateNode();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);

                string[] choiceLabels = nextNodes
                    .Select((n, i) => $"Choice {i}: {n.display}")
                    .ToArray();

                _selectedChoice = EditorGUILayout.Popup(
                    "Select Choice",
                    _selectedChoice,
                    choiceLabels);

                if (GUILayout.Button("Confirm Choice"))
                {
                    _story.ChooseNextNode(_selectedChoice);
                    UpdateNode();
                }
            }
        }

        private void UpdateNode()
        {
            if (_story != null) _currentNode = _story.GetCurrentNode();
        }
    }
}