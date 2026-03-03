using UnityEditor;
using UnityEngine;

namespace Knitting.Debugging
{
    [CustomEditor(typeof(Story), true)]
    public class StoryEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            if (GUILayout.Button("Open Story Debugger"))
            {
                StoryDebuggerWindow.Open(serializedObject.targetObject as Story);
            }
        }
    }
}