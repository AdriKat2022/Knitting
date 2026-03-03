using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using EditorUtility = Knitting.Utility.EditorUtility;

namespace Knitting.ScriptableObjects
{
    [CustomEditor(typeof(SimpleVariableContextSO), false)]
    public class SimpleVariableContextSOEditor : Editor
    {
        private Vector2 _scroll;
        private FieldInfo _dictionaryField;
        private bool _enableMonitoring;
        
        private void OnEnable()
        {
            _dictionaryField = typeof(SimpleVariableContextSO)
                .GetField("_variableDictionary", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.HelpBox(
                "This scriptable object serves as a data source for runtime.\n" +
                "Note that this variable context does NOT save anything on its own, and " +
                "the variable values will be reset each time the application restarts.",
                MessageType.Info);

            if (_dictionaryField == null)
            {
                EditorGUILayout.HelpBox("Could not find _variableDictionary field via reflection.", MessageType.Error);
                return;
            }

            IDictionary dictionaryToDisplay = _dictionaryField.GetValue(target) as IDictionary;

            EditorUtility.DrawDictionary("Runtime Variables", dictionaryToDisplay, true, ref _scroll);

            // Repaint continuously during play mode so values update live
            if (Application.isPlaying) Repaint();
        }
    }
}