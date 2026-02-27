using System;
using UnityEngine;

namespace Knitting.Types
{
    [Serializable]
    public class TypeReference
    {
        [SerializeField]
        private string _search;
        
        [SerializeField]
        private string _typeName;

        public Type Type
        {
            get => string.IsNullOrEmpty(_typeName) ? null : Type.GetType(_typeName);
            set => _typeName = value?.AssemblyQualifiedName;
        }
    }

    [Serializable]
    public class SpecificTypeReference<T> : TypeReference
    {
        [SerializeField]
        private string _baseTypeSpecificationQualifiedName = typeof(T).AssemblyQualifiedName;
    }
    
    [Serializable]
    public class ScriptableObjectTypeReference : SpecificTypeReference<ScriptableObject>
    { }
}