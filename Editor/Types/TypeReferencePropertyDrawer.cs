using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Knitting.Types
{
    /// <summary>
    /// Property drawer for all TypeReference objects, also valid for children.
    /// Children should implement the _baseTypeSpecificationQualifiedName field to restrict
    /// the type selection to those extending the one provided in this field.
    /// </summary>
    [CustomPropertyDrawer(typeof(TypeReference), true)]
    public class TypeReferenceDrawer : PropertyDrawer
    {
        private const string SPECIFICATION_PROPERTY_NAME = "_baseTypeSpecificationQualifiedName";
        private const float SEARCH_FIELD_RATIO = 0.5f;
        private const float SPACING = 20;

        private Type[] _types;
        private string[] _typeNames;
        private string _lastSearch;

        private static Type GetTypeSpecification(SerializedProperty property)
        {
            var spec = property.FindPropertyRelative(SPECIFICATION_PROPERTY_NAME);

            string typeName = (string)spec?.boxedValue;

            return string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName);
        }
        
        private void FetchAndFilterTypes(string searching, Type currentlySelectedType, Type parentType)
        {
            if (_lastSearch == searching) return;
            
            _lastSearch = searching;
            
            _types = TypeCache.GetTypesDerivedFrom(parentType)
                .Where(t =>
                    !t.IsAbstract &&
                    (t.FullName.Contains(searching)))
                .ToArray();
            
            _typeNames = _types.Select(t => t.FullName).ToArray();
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var searchProperty = property.FindPropertyRelative("_search");
            var typeNameProperty = property.FindPropertyRelative("_typeName");
            
            var currentType = string.IsNullOrEmpty(typeNameProperty.stringValue)
                ? null
                : Type.GetType(typeNameProperty.stringValue);
            
            Type getParentType = GetTypeSpecification(property) ?? typeof(object);
            
            FetchAndFilterTypes(searchProperty.stringValue ?? string.Empty, currentType, getParentType);
            
            EditorGUIUtility.labelWidth = position.width * SEARCH_FIELD_RATIO * 0.65f;
            
            Rect searchRect = new Rect(position.x, position.y, position.width * SEARCH_FIELD_RATIO - SPACING/2, EditorGUIUtility.singleLineHeight);
            Rect popupRect = new Rect(position.x + searchRect.width + SPACING/2, position.y, position.width - searchRect.width, EditorGUIUtility.singleLineHeight);
            
            // Search
            EditorGUI.PropertyField(searchRect, searchProperty, label);

            int currentIndex = Array.FindIndex(_types, t => t == currentType);

            if (currentIndex < 0) currentIndex = 0;
            
            int selectedIndex = EditorGUI.Popup(popupRect, string.Empty, currentIndex, _typeNames);

            if (selectedIndex >= 0 && selectedIndex < _types.Length)
            {
                typeNameProperty.stringValue = _types[selectedIndex].AssemblyQualifiedName;
            }
        }
    }
}