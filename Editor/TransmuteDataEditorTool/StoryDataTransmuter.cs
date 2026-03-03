using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AdriKat.Toolkit.Attributes;
using Knitting.Attributes;
using Knitting.Types;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using EditorUtility = Knitting.Utility.EditorUtility;

namespace Knitting.TransmuteDataEditorTool
{
    /// <summary>
    /// Uses reflection to transmute the Story datas into your custom ScriptableObjects.
    /// In other words, this generates baked ScriptableObjects of the type of your choice referencing each others.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryDataTransmuter", menuName = "Knitting/StoryDataTransmuter", order = 2)]
    public class StoryDataTransmuter : ScriptableObject
    {
        [Header("Transmuter Settings")]
        public string FolderPathToSaveTransmutation = "Assets/Resources/Dialogues";
        public bool AllowOverwrite;

        [Space]
    
        public bool MakeStoryDataScriptableObject = true;
        [ShowIf(nameof(MakeStoryDataScriptableObject))]
        public string StoryDataName = "STORY_DATA";
        [ShowIf(nameof(MakeStoryDataScriptableObject))]
        [Tooltip("The new type that will be used to hold the global story data.")]
        public ScriptableObjectTypeReference StoryDataNewType;
        
        [Space]
    
        public bool MakeStoryDataNodeScriptableObjects = true;
        [ShowIf(nameof(MakeStoryDataNodeScriptableObjects))]
        [Tooltip("The new type that will be used to hold the dialogue data.")]
        public ScriptableObjectTypeReference StoryNodeDataNewType;

        [Space]
    
        [Tooltip("Nodes will reference each others instead of just having a list of string.")]
        public bool SaveChoicesAsReferences;

        [Header("Input")]
        public TextAsset TwineTextFile;

        [Header("Pipeline")]
        [Tooltip("Event that will fire once the transmutation is finished.")]
        public UnityEvent<ScriptableObject[]> OnTransmutationComplete;
        
        private Dictionary<string, ScriptableObjectData> allScriptableObjects;
        private ScriptableObject[] readyScriptableObjectList;
        private List<FieldAttribute> StoryFieldAttributes;
        private List<FieldAttribute> StoryNodeFieldAttributes;
        private List<MethodAttribute> StoryNodeMethodAttributes;
        private Type AvailableChoiceCustomType;
    
        [ContextMenu("Transmute")]
        [StandaloneButtonAction("Transmute")]
        public void Transmute()
        {
            allScriptableObjects = new();
            readyScriptableObjectList = null;
            
            StoryFieldAttributes = new();
            StoryNodeMethodAttributes = new();
            StoryNodeFieldAttributes = new();

            AvailableChoiceCustomType = null;
        
            // Using the story as a plain C# class (it's fine since we do not intent to use any Unity functionality)
            Story story = new Story();
            story.SetUpStory(TwineTextFile.text);

            if (MakeStoryDataScriptableObject)
            {
                Debug.Log("Saving Story as ScriptableObjects...");
                var storyScriptableObject = MakeAndSaveStoryDataScriptableObject(story);
                Debug.Log("Saved Story.", storyScriptableObject);
            }

            if (MakeStoryDataNodeScriptableObjects)
            {
                MakeStoryNodeScriptableObjects(story, allScriptableObjects);
                
                if (SaveChoicesAsReferences)
                {
                    LinkScriptableObjects(allScriptableObjects, ref readyScriptableObjectList);
                }
                
                Debug.Log("Saving StoryNodes as ScriptableObjects...");
                
                SaveScriptableObjectsAndTriggerPostDataHook(allScriptableObjects);
                
                Debug.Log("Triggered PostTransmute event.");
                
                Debug.Log("Triggering OnTransmutationComplete event.");
                
                try
                {
                    OnTransmutationComplete.Invoke(readyScriptableObjectList);
                }
                catch (Exception)
                {
                    Debug.LogError($"Exception occured during OnTransmutationComplete Event");
                    throw;
                }
            }
        }

        private ScriptableObject MakeAndSaveStoryDataScriptableObject(Story story)
        {
            ScriptableObject storyScriptableObject = CreateInstance(StoryDataNewType.Type);
            
            MakeFieldInfosCache(storyScriptableObject, StoryFieldAttributes, ref AvailableChoiceCustomType);
            
            foreach (var storyFieldAttribute in StoryFieldAttributes)
            {
                storyFieldAttribute.FieldInfo.SetValue(storyScriptableObject, GetValueFromStory(story, storyFieldAttribute.TransmuteFromAttribute.StoryData));
            }
            
            AssetDatabase.CreateAsset(storyScriptableObject, $"{FolderPathToSaveTransmutation}/{StoryDataName}.asset");
            AssetDatabase.SaveAssets();

            return storyScriptableObject;
        }
        
        private void MakeStoryNodeScriptableObjects(Story story, Dictionary<string, ScriptableObjectData> scriptableObjectDatas)
        {
            List<StoryNode> storyNodesleft = new();
        
            StoryNode currentStoryNode = story.GetCurrentNode();
            storyNodesleft.Add(currentStoryNode);
        
            while (storyNodesleft.Count > 0)
            {
                currentStoryNode = storyNodesleft[0];
                storyNodesleft.RemoveAt(0);

                if (currentStoryNode == null) continue;
            
                string nodeTitle = currentStoryNode.GetTitle();
                story.SetNextNode(nodeTitle);
            
                // Create the custom object and copy the data.
                ScriptableObject scriptableObject = CreateInstance(StoryNodeDataNewType.Type);
                scriptableObject.name = nodeTitle;
                CopyStoryNodeData(currentStoryNode, scriptableObject);
            
                // Save the choices and the custom object in the dictionary.
                List<ChoiceData> choiceDatas = currentStoryNode.GetNextNodes();
                scriptableObjectDatas[nodeTitle] = new() { Next = choiceDatas , Node = scriptableObject };
            
                if (choiceDatas.Count > 1)
                {
                    // Multiple choice, add all possibilities to the list.
                    for (var index = 0; index < choiceDatas.Count; index++)
                    {
                        ChoiceData choiceData = choiceDatas[index];
                        story.SetNextNode(nodeTitle); // Always ensure to be here
                        story.ChooseNextNode(index);
                    
                        StoryNode nextNode = story.GetCurrentNode();
                    
                        if (scriptableObjectDatas.ContainsKey(nextNode.GetTitle())) continue;
                    
                        storyNodesleft.Add(nextNode);
                    }
                }
                else if (choiceDatas.Count == 1)
                {
                    // Single choice (linear)
                    story.NextNode();
                    StoryNode nextNode = story.GetCurrentNode();

                    if (!scriptableObjectDatas.ContainsKey(nextNode.GetTitle()))
                    {
                        storyNodesleft.Add(nextNode);
                    }
                }
            }
        }

        private void LinkScriptableObjects(Dictionary<string, ScriptableObjectData> scriptableObjectDatas, ref ScriptableObject[] scriptableObjects)
        {
            scriptableObjects = new ScriptableObject[scriptableObjectDatas.Count];

            int i = 0;
            foreach ((string _, ScriptableObjectData scriptableObjectData) in scriptableObjectDatas)
            {
                ReferenceChoiceData[] refChoices = scriptableObjectData.Next.Select(data => new ReferenceChoiceData(allScriptableObjects[data.nodeTitle].Node, data.display)).ToArray();
                InsertChoice(scriptableObjectData.Node, refChoices);
                scriptableObjects[i] = scriptableObjectData.Node;
                i++;
            }
        }

        private void SaveScriptableObjectsAndTriggerPostDataHook(Dictionary<string, ScriptableObjectData> scriptableObjectDatas)
        {
            EditorUtility.CreateFoldersRecursively(FolderPathToSaveTransmutation);

            foreach ((string _, ScriptableObjectData scriptableObject) in scriptableObjectDatas)
            {
                AssetDatabase.CreateAsset(scriptableObject.Node, $"{FolderPathToSaveTransmutation}/{scriptableObject.Node.name}.asset");
                
                TriggerPostDataAttribute(scriptableObject.Node);
            }
        }

        private void InsertChoice(ScriptableObject scriptableObject, ReferenceChoiceData[] refChoices)
        {
            // Localise the AssociatedChoices Enum as attribute.
            MakeFieldInfosCache(scriptableObject, StoryNodeFieldAttributes, ref AvailableChoiceCustomType);
            
            FieldInfo choiceReferenceField = null;
            FieldInfo displayChoiceField = null;
            
            Type elementType = AvailableChoiceCustomType.IsArray ?
                AvailableChoiceCustomType.GetElementType() :
                AvailableChoiceCustomType.GenericTypeArguments[0];
            
            FieldInfo[] fields = elementType.GetFields();
            foreach (FieldInfo fieldInfo in fields)
            {
                if (fieldInfo.GetCustomAttribute(typeof(TransmuteFromAttribute)) is not TransmuteFromAttribute transmuteFromAttribute) continue;
                
                if (transmuteFromAttribute.ChoiceData == ChoiceDataEnum.NextNodeReference)
                {
                    choiceReferenceField = fieldInfo;
                }
                else if (transmuteFromAttribute.ChoiceData == ChoiceDataEnum.ChoiceDisplay)
                {
                    displayChoiceField = fieldInfo;
                }
            }
            
            // Create the collection instance
            object refChoicesObject;

            if (AvailableChoiceCustomType.IsArray)
            {
                // Array version
                Array array = Array.CreateInstance(elementType, refChoices.Length);

                for (int i = 0; i < refChoices.Length; i++)
                {
                    object element = Activator.CreateInstance(elementType);

                    choiceReferenceField?.SetValue(element, refChoices[i].ChoiceReference);
                    displayChoiceField?.SetValue(element, refChoices[i].ChoiceDisplay);

                    array.SetValue(element, i);
                }

                refChoicesObject = array;
            }
            else
            {
                // List version
                
                refChoicesObject = Activator.CreateInstance(AvailableChoiceCustomType);
                
                if (refChoicesObject is not IList list) throw new InvalidOperationException("AvailableChoiceCustomType must implement IList");
                
                // Populate it
                for (int i = 0; i < refChoices.Length; i++)
                {
                    object customRefChoice = Activator.CreateInstance(elementType);

                    choiceReferenceField?.SetValue(customRefChoice, refChoices[i].ChoiceReference);
                    displayChoiceField?.SetValue(customRefChoice, refChoices[i].ChoiceDisplay);

                    list.Add(customRefChoice);
                }
            }
            
            foreach ((FieldInfo fieldInfo, TransmuteFromAttribute transmuteFromAttribute) in StoryNodeFieldAttributes)
            {
                if (transmuteFromAttribute.StoryNodeData == StoryNodeData.AvailableChoices)
                {
                    fieldInfo.SetValue(scriptableObject, refChoicesObject);
                    return;
                }
            }
        }
    
        private void CopyStoryNodeData(StoryNode storyNode, object newStoryNode)
        {
            MakeFieldInfosCache(newStoryNode, StoryNodeFieldAttributes, ref AvailableChoiceCustomType);

            foreach ((FieldInfo fieldInfo, TransmuteFromAttribute transmuteFromAttribute) in StoryNodeFieldAttributes)
            {
                if (SaveChoicesAsReferences && transmuteFromAttribute.StoryNodeData == StoryNodeData.AvailableChoices) continue;
                fieldInfo.SetValue(newStoryNode, GetValueFromStoryNode(storyNode, transmuteFromAttribute.StoryNodeData));
            }
        }

        private void TriggerPostDataAttribute(object scriptableObject)
        {
            MakeMethodInfosCache(scriptableObject, StoryNodeMethodAttributes);

            foreach ((MethodInfo methodInfo, PostTransmuteAttribute postTransmuteAttribute) in StoryNodeMethodAttributes)
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 0)
                {
                    methodInfo.Invoke(scriptableObject, Array.Empty<object>());
                }
                else
                {
                    Debug.LogWarning($"{nameof(PostTransmuteAttribute)}: {methodInfo.Name} can only be called if it has no parameter.");
                }
            }
        }
        
        #region Helpers
        
        private static object GetValueFromStory(Story story, StoryData storyData)
        {
            return storyData switch
            {
                StoryData.Title => story.GetTitle(),
                StoryData.Ifid => story.GetIfid(),
                StoryData.Format => story.GetFormat(),
                StoryData.FormatVersion => story.GetFormatVersion(),
                StoryData.Zoom => story.GetZoom(),
                StoryData.StartNodeId => story.GetStartNodeId(),
                StoryData.TagColors => story.GetTagColors(),
                StoryData.Nodes => null,
                _ => throw new ArgumentOutOfRangeException(nameof(storyData), storyData, null)
            };
        }
    
        private static object GetValueFromStoryNode(StoryNode storyNode, StoryNodeData storyNodeData)
        {
            return storyNodeData switch
            {
                StoryNodeData.NodeTitle => storyNode.GetTitle(),
                StoryNodeData.AttachedTags => storyNode.GetTags(),
                StoryNodeData.Position => storyNode.GetPosition(),
                StoryNodeData.Size => storyNode.GetSize(),
                StoryNodeData.RawText => storyNode.GetRawText(),
                StoryNodeData.ParsedText => storyNode.GetText(),
                StoryNodeData.AvailableChoices => storyNode.GetNextNodes(),
                StoryNodeData.ParentStory => storyNode.GetParentStory(),
                StoryNodeData.Sprite => storyNode.GetSprite(),
                StoryNodeData.SoundIds => storyNode.GetSoundIds(),
                StoryNodeData.VariableChanges => storyNode.GetVarChanges(),
                _ => throw new ArgumentOutOfRangeException(nameof(storyNodeData), storyNodeData, null)
            };
        }
        
        private static void MakeFieldInfosCache(object newObject, List<FieldAttribute> fieldAttributes, ref Type AvailableChoiceCustomType)
        {
            if (fieldAttributes.Count > 0) return;

            Type type = newObject.GetType();
            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        
            // Get all the fields with the TransmuteFromAttribute
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                TransmuteFromAttribute transmuteFromAttribute = fieldInfo.GetCustomAttribute<TransmuteFromAttribute>();

                if (transmuteFromAttribute == null) continue;
            
                fieldAttributes.Add(new(fieldInfo, transmuteFromAttribute));

                if (transmuteFromAttribute.StoryNodeData == StoryNodeData.AvailableChoices)
                {
                    AvailableChoiceCustomType = fieldInfo.FieldType;
                }
            }
        }

        private static void MakeMethodInfosCache(object newObject, List<MethodAttribute> methodAttributes)
        {
            if (methodAttributes.Count > 0) return;
        
            Type type = newObject.GetType();
            MethodInfo[] methodInfos = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        
            // Get all the fields with the TransmuteFromAttribute
            foreach (MethodInfo methodInfo in methodInfos)
            {
                PostTransmuteAttribute postTransmuteAttribute = methodInfo.GetCustomAttribute<PostTransmuteAttribute>();

                if (postTransmuteAttribute == null) continue;
            
                methodAttributes.Add(new(methodInfo, postTransmuteAttribute));
            }
        }
    
        #endregion
        
        #region Structs
        private struct FieldAttribute
        {
            public FieldInfo FieldInfo;
            public TransmuteFromAttribute TransmuteFromAttribute;

            public FieldAttribute(FieldInfo fieldInfo, TransmuteFromAttribute transmuteFromAttribute)
            {
                FieldInfo = fieldInfo;
                TransmuteFromAttribute = transmuteFromAttribute;
            }

            public readonly void Deconstruct(out FieldInfo fieldInfo, out TransmuteFromAttribute transmuteFromAttribute)
            {
                fieldInfo = FieldInfo;
                transmuteFromAttribute = TransmuteFromAttribute;
            }
        }
    
        private struct MethodAttribute
        {
            public MethodInfo MethodInfo;
            public PostTransmuteAttribute PostTransmuteAttribute;

            public MethodAttribute(MethodInfo methodInfo, PostTransmuteAttribute postTransmuteAttribute)
            {
                MethodInfo = methodInfo;
                PostTransmuteAttribute = postTransmuteAttribute;
            }

            public readonly void Deconstruct(out MethodInfo methodInfo, out PostTransmuteAttribute postTransmuteAttribute)
            {
                methodInfo = MethodInfo;
                postTransmuteAttribute = PostTransmuteAttribute;
            }
        }

        private struct ReferenceChoiceData
        {
            public ScriptableObject ChoiceReference;
            public string ChoiceDisplay;

            public ReferenceChoiceData(ScriptableObject choiceReference, string choiceDisplay)
            {
                ChoiceReference = choiceReference;
                ChoiceDisplay = choiceDisplay;
            }
        }
    
        private struct ScriptableObjectData
        {
            public ScriptableObject Node;
            public List<ChoiceData> Next;
        }
        
        #endregion
    }
}
