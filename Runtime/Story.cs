using System.Collections.Generic;
using System.Text.RegularExpressions;
using AdriKat.Toolkit.Attributes;
using Knitting.Interfaces;
using UnityEngine;

namespace Knitting
{
    public class Story : MonoBehaviour
    {
        private const string SCRIPTABLE_OBJECT_INTERFACE_MISSING_WARNING =
            "The ScriptableObject must implement the IVariableContextReceiver interface to function properly.";
        
        public TextAsset twineText;
        public bool UseSeparateObjectForVariableContext;
        [ShowIf(nameof(UseSeparateObjectForVariableContext), showDisabledField: true)]
        [WarnIf(nameof(IsScriptableObjectVariableContextInvalid), SCRIPTABLE_OBJECT_INTERFACE_MISSING_WARNING, showAfter: true)]
        public ScriptableObject ScriptableObjectVariableContext;
        private IVariableContext _variableContext;
        
        [SerializeField]
        private bool printVariableWhenSet;

        // Story datas
        private string title;
        private string ifid;
        private string format;
        private string formatVersion;
        private float zoom;
        private string startNodeId;
        private readonly Dictionary<string, Color> tagColors = new();

        // UserScript

        // UserStylesheet

        // Nodes
        private readonly Dictionary<string, StoryNode> nodes = new();
        private readonly Dictionary<string, string> nodeVariables = new();
        private StoryNode currentNode;

        // Customisation
        private readonly Dictionary<string, Color> possibleColors = new();

        [System.Serializable]
        public class DescribedSprite
        {
            public string title;
            public Sprite sprite;
        }

        [SerializeField] private bool usePath;
        [SerializeField] private string path;
        [SerializeField] private List<DescribedSprite> sprites = new();
        
        public void Awake()
        {
            if (usePath) twineText = Resources.Load(path) as TextAsset;

            if (!twineText)
            {
                Debug.LogWarning("No twine file.");
            }

            SetUpStory(twineText.text);

            if (IsScriptableObjectVariableContextInvalid())
            {
                Debug.LogWarning("Variable context is invalid! Switched to using a default variable context without a scriptable object.");
                UseSeparateObjectForVariableContext = false;
            }
            
            if (UseSeparateObjectForVariableContext)
            {
                _variableContext.Initialize();
            }
        }

        // ########## Setup ##########

        public void SetUpStory(string text)
        {
            SetupPossiblesColors();
            string RGX_componentExtraction = @":: (?<title>.*)\n(?<data>((?:(?!^::).|\n)*))";
            foreach (Match component in Regex.Matches(text, RGX_componentExtraction, RegexOptions.Multiline))
            {
                if (!component.Success) continue;
                string componentTitle = component.Groups["title"].Value.Trim();
                string componentData = component.Groups["data"].Value.Trim();

                switch (componentTitle)
                {
                    case "StoryTitle":
                        title = componentData;
                        break;
                    case "StoryData":
                        ParseStoryData(componentData);
                        break;
                    case "UserScript [script]":
                        // TODO
                        break;
                    case "UserStylesheet [stylesheet]":
                        // TODO
                        break;
                    default:
                        StoryNode node = new StoryNode(componentTitle, componentData, this);
                        nodes.Add(node.GetTitle(), node);
                        break;
                }

            }

            currentNode = nodes[startNodeId];
        }

        private void ParseStoryData(string data)
        {
            Regex RGX_parseData = new Regex(@"""(?<name>[^""]*)"": (({(?<colors>[^}]*)})|(""?(?<value>[^""\n,]*)""?))", RegexOptions.Multiline);

            MatchCollection datas = RGX_parseData.Matches(data);
            foreach (Match dataMatch in datas)
            {
                switch (dataMatch.Groups["name"].Value)
                {
                    case "ifid":
                        ifid = dataMatch.Groups["value"].Value;
                        break ;
                    case "format":
                        format = dataMatch.Groups["value"].Value;
                        break;
                    case "format-version":
                        formatVersion = dataMatch.Groups["value"].Value;
                        break;
                    case "start":
                        startNodeId = dataMatch.Groups["value"].Value;
                        break;
                    case "zoom":
                        zoom = float.Parse(dataMatch.Groups["value"].Value.Replace('.',','));
                        break;
                    case "tag-colors":
                        Regex RGX_parseColor = new Regex(@"[^""]*""(?<tag>[^""]*)"": ""(?<color>[^""]*)""");

                        MatchCollection pairs = RGX_parseColor.Matches(dataMatch.Groups["colors"].Value);
                        foreach (Match colorMatch in pairs)
                        {
                            string tag = colorMatch.Groups["tag"].Value;
                            Color color = possibleColors[colorMatch.Groups["color"].Value];
                            tagColors.Add(tag, color);
                        }
                        break;
                }
            }
        }

        private void SetupPossiblesColors()
        {
            possibleColors.Add("red", Color.red);
            possibleColors.Add("orange", new Color(1f, 0.498f, .0f));
            possibleColors.Add("yellow", Color.yellow);
            possibleColors.Add("green", Color.green);
            possibleColors.Add("blue", Color.blue);
            possibleColors.Add("purple", new Color(0.502f, 0f, 0.502f));
        }

        // ########## node variable ########## 

        public IVariableContext GetVarContext()
        {
            return _variableContext ?? new DictionaryVariableContext(nodeVariables);
        }
        
        public string GetVariable(string variableName)
        {
            if (UseSeparateObjectForVariableContext && _variableContext != null)
            {
                return _variableContext[variableName];
            }
            
            bool isSet = nodeVariables.TryGetValue(variableName, out string returnValue);
            //Assert.IsTrue(isSet, variableName + " is not defined");
            return returnValue;
        }

        public void SetVariable(string variableName, string value)
        {
            if(printVariableWhenSet) Debug.Log("set " + variableName + " to " + value);

            if (UseSeparateObjectForVariableContext && _variableContext != null)
            {
                _variableContext[variableName] = value;
                return;
            }
            
            nodeVariables[variableName] = value;
        }

        // ########## Node changement ########## 

        public void NextNode()
        {
            List<ChoiceData> nextNodes = currentNode.GetNextNodes();
            SetNextNode(nextNodes[0].nodeTitle);
        }

        public void ChooseNextNode(int index)
        {
            List<ChoiceData> nextNodes = currentNode.GetNextNodes();
            //Assert.IsTrue(index < nextNodes.Count, "Index out of range");
            SetNextNode(nextNodes[index].nodeTitle);
        }

        public void SetNextNode(string title)
        {
            nodes.TryGetValue(title, out StoryNode next);
            //Assert.IsTrue(nodes.TryGetValue(title, out next), title + " doesn't existe");
            currentNode = next;
        }

        public void SetToStart()
        {
            SetNextNode(startNodeId);
        }
        
        // ########## GETTER / SETTER ########## 

        public string GetTitle() { return title; }

        public string GetIfid() { return ifid; }

        public string GetFormat() { return format; }

        public string GetFormatVersion() { return formatVersion; }

        public float GetZoom() { return zoom; }

        public string GetStartNodeId() { return startNodeId; }

        public Dictionary<string, Color> GetTagColors() { return tagColors; }

        public StoryNode GetCurrentNode() { return currentNode; }

        public Sprite GetSprite(string spriteName)
        {
            foreach(DescribedSprite sprite in sprites) if (sprite.title == spriteName) return sprite.sprite;
            return null;
        }

        public bool IsScriptableObjectVariableContextInvalid()
        {
            if (!UseSeparateObjectForVariableContext) return false;
            
            _variableContext = ScriptableObjectVariableContext as IVariableContext;
            
            return _variableContext == null;
        }

        public class DictionaryVariableContext : IVariableContext
        {
            private readonly Dictionary<string, string> _variables = new();
            
            public DictionaryVariableContext() { }
            
            public DictionaryVariableContext(Dictionary<string, string> variableContext)
            {
                _variables = variableContext;
            }
            
            public void Initialize()
            { } // No need for initialization.

            public bool Contains(string variableName)
            {
                return _variables.ContainsKey(variableName);
            }

            public string this[string variableName]
            {
                get => _variables[variableName];
                set => _variables[variableName] = value;
            }
        }
        
        // ########## Debug ########## 
        /*
    [ContextMenu("nextNode")]
    private void Debug_NextNode() 
    {
        NextNode();
        Debug.Log(currentNode.getText());
    }

    [ContextMenu("ChooseNextNode")]
    private void Debug_ChooseNextNode() 
    { 
        ChooseNextNode(index);
        Debug.Log(currentNode.getText());
    }


    [ContextMenu("SkipMultipleNextNode")]
    private void Debug_SkipMultipleNextNode()
    {
        for (int i = 0; i < index; i++)
        {
            NextNode();
            Debug.Log(currentNode.getText());
        }
    }
    */
    }
    
    /// <summary>
    /// Represents passable data.
    /// </summary>
    public enum StoryData
    { 
        Title,
        Ifid,
        Format,
        FormatVersion,
        Zoom,
        StartNodeId,
        TagColors,
        Nodes
    }
}
