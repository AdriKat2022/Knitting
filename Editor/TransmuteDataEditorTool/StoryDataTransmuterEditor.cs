using UnityEditor;
using UnityEngine;

namespace Knitting.TransmuteDataEditorTool
{
    [CustomEditor(typeof(StoryDataTransmuter))]
    public class StoryDataTransmuterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (GUILayout.Button("Open In Tool Window"))
            {
                StoryDataTransmuterWindow.Open(serializedObject.targetObject as StoryDataTransmuter);
            }
        }
    }
}