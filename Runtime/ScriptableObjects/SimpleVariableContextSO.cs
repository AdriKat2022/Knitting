using System.Collections.Generic;
using Knitting.Interfaces;
using UnityEngine;

namespace Knitting.ScriptableObjects
{
    /// <summary>
    /// This class DOES NOT SAVE THE VARIABLES BETWEEN RELOADS.
    /// The used dictionary is NOT SERIALIZABLE.
    /// You can create a similar type implementing IVariableContextReceiver
    /// and use a serialized dictionary or something to better fit your needs.
    /// </summary>
    [CreateAssetMenu(menuName = "Knitting/VariableContext", fileName = "New Variable Context")]
    public class SimpleVariableContextSO : ScriptableObject, IVariableContext
    {
        protected Dictionary<string, string> _variableDictionary;

        public void Initialize()
        {
            _variableDictionary = new Dictionary<string, string>();
        }

        public bool Contains(string variableName)
        {
            return  _variableDictionary.ContainsKey(variableName);
        }

        public string this[string variableName]
        {
            get => _variableDictionary.ContainsKey(variableName) ? _variableDictionary[variableName] : null;
            set => _variableDictionary[variableName] = value;
        }
    }
}