using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Knitting.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Knitting
{
    [Serializable]
    public class ChoiceData
    {
        public string nodeTitle;
        public string display;

        public ChoiceData(string _title, string _display)
        {
            nodeTitle = _title;
            display = _display;
        }
    }

    [Serializable]
    public class VariableUpdate
    {
        public string VariableName;
        public string VariableValue;

        public VariableUpdate(string variableName, string variableValue)
        {
            VariableName = variableName;
            VariableValue = variableValue;
        }

        public void ApplyChangeToContext(IVariableContext varContext)
        {
            varContext[VariableName] = VariableValue;
        }
    }

    [Serializable]
    public class StoryNode
    {
        // ######### Classes and Structs #########
        
        #region Text Parsing Data
        
        /// <summary>
        /// Represents the data computed from the main text.
        /// Those fields are dependent on the variableContext.
        /// </summary>
        [Serializable]
        public class TextParsingData
        { 
            public string DisplayText;
            public string SpriteId;
            public List<string> SoundIds = new();
            /// <summary>
            /// All variable updates that should happen when this node is attaigned.
            /// </summary>
            public List<VariableUpdate> VariableUpdates = new();
            
            public override string ToString() => DisplayText;
        }
        
        #endregion
        
        private Story ParentStory;
        private IVariableContext VariableContext;
        private string Title = string.Empty;
        private string RawText = string.Empty;
        private List<string> Tags = new();
        private Vector2Int Position;
        private Vector2Int Size;
        private List<ChoiceData> AvailableChoices = new();
        private TextParsingData DynamicData;

        private bool isNodeComputed;
        
        // ########## SETUP + CONSTRUCTOR ##########
        
        #region Setup/Constructor
        
        public StoryNode(string _title, string _nodeData, Story _parentStory)
        {
            ParseTitle(_title);
            ParseNodeData(_nodeData);
            ParentStory = _parentStory;
            VariableContext = _parentStory.GetVarContext();
        }

        public StoryNode(string _title, List<string> _tags, Vector2Int _position, Vector2Int _size, string _data, Story _parentStrory)
        {
            Title = _title;
            Tags = _tags;
            Position = _position;
            Size = _size;
            ParseNodeData(_data);
            ParentStory = _parentStrory;
        }
        
        private void ParseTitle(string _title)
        {
            Regex regex = new Regex(@"(?<title>[^{[]*)(\[)?(?<tags>[^]]*)(\])? {""position"":""(?<posX>\d*),(?<posY>\d*)"",""size"":""(?<sizeX>\d*),(?<sizeY>\d*)""}");

            Match match = regex.Match(_title);
            //Assert.IsTrue(match.Success, _title + " is not a valid title");

            Title = match.Groups["title"].Value.Trim();

            string tagsString = match.Groups["tags"].Value;
            
            if (tagsString.Length != 0) foreach (string tag in tagsString.Split(" ")) Tags.Add(tag);

            Position.x = int.Parse(match.Groups["posX"].Value);
            Position.y = int.Parse(match.Groups["posY"].Value);

            Size.x = int.Parse(match.Groups["sizeX"].Value);
            Size.y = int.Parse(match.Groups["sizeY"].Value);
        }

        private void ParseNodeData(string _data)
        {
            Regex destinationChoice = new Regex(@"^\[\[(((?<textChoicePrefix>.*)->)|)(?<destination>[^]<]*)((<-(?<textChoiceSufix>.*))|)]]");
            foreach (string line in _data.Split("\n"))
            {
                Match match = destinationChoice.Match(line);
                if (!match.Success)
                {
                    RawText += line + "\n";
                    continue;
                }

                AvailableChoices.Add(new ChoiceData(match.Groups["destination"].Value, match.Groups["textChoicePrefix"].Success ? match.Groups["textChoicePrefix"].Value : match.Groups["textChoiceSufix"].Value));
            }

            RawText = RawText.Trim();
        }

        #endregion
        
        // ########## OVERRIDES ########
        
        #region Overrides
        
        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(StoryNode)) return false;
            StoryNode node = (StoryNode)obj;

            if (node == null) return false;

            if (node.Title != Title) return false;
            if (node.Position != Position) return false;
            if (node.Size != Size) return false;
            if (node.Tags.Count != Tags.Count) return false;

            for (int i = 0; i < Tags.Count; i++)
                if (node.Tags[i] != Tags[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Tags, Position, Size);
        }
        
        #endregion

        // ########## Execute commandes ########## 
        private enum COMMAND_TYPE
        {
            SET,
            IF,
            ELSEIF,
            ELSE,
            SOUND,
            UNLESS,
            EITHER,
            COND,
            NTH,
            PRINT,
            IMAGE,
            DEFAULT
        }

        #region Helper Functions
        
        public void PrintNodeData()
        {
            string output = "Title : " + Title + '\n';

            output += "Tags : ";

            if (Tags.Count == 0) output += "NO TAGS\n";
            else
            {
                output += "[";
                foreach (string tag in Tags) output += tag + " ";
                output = output.Trim();
                output += "]\n";
            }

            output += "Position : " + Position;
            output += "\nSize : " + Size;

            Debug.Log(output);
        }

        /// <summary>
        /// Execute all variable events and bake the displayText.
        /// </summary>
        public void ComputeNode(bool forceRecompute = false)
        {
            if (isNodeComputed && !forceRecompute) return;

            isNodeComputed = true;
            
            DynamicData = ParseText(RawText, ParentStory.GetVarContext());
        }
        
        public static TextParsingData ParseText(string textToParse, IVariableContext varsContext)
        {
            TextParsingData existingData = new();
            
            //Regex patternExtract = new Regex(@"\((?<commande>[^)]*)\)\n?([^[\n]*\[(?<text>[^]]*)\]\n?|)");
            bool isConditionAlreadyValide = false;

            List<ParsingTools.ParsingResult> commands = ParsingTools.GetAllCommandes(textToParse);

            List<string> notCommands = ParsingTools.GetOpposit(textToParse, commands);

            existingData.DisplayText = "";

            for (int i = 0; i < notCommands.Count; i++)
            {
                string text = ReplaceVariablesByValues(notCommands[i], "", varsContext);
                if (text.Length > 0 && text[0] == '\n') text = text[1..];
                existingData.DisplayText += text;
                //Debug.Log("not commande " + i + "\"" + text + "\"");

                if (i >= commands.Count) continue;
                
                ParsingTools.ParsingResult match = commands[i];
                //Debug.Log("commande " + i + "\"" + match.Value + "\"");

                string matchedCommand = match.Groups["commande"];
                string matchedText = match.Groups.GetValueOrDefault("text");
                
                COMMAND_TYPE commandType = GetCommandType(matchedCommand);
                
                switch (commandType)
                {
                    case COMMAND_TYPE.SET:
                        ParsingTools.ParsingResult variableMatch = ParsingTools.GetSet(matchedCommand);
                        string name = variableMatch.Groups["variableName"];
                        string value = variableMatch.Groups["value"];
                        existingData.VariableUpdates.Add(new(name, value));
                        varsContext[name] = value;
                        break;

                    case COMMAND_TYPE.IF:
                        isConditionAlreadyValide = false;
                        if (TestCondition(matchedCommand, varsContext))
                        {
                            isConditionAlreadyValide = true;
                            existingData.DisplayText += ParseText(matchedText, varsContext);
                        }
                        break;
                    
                    case COMMAND_TYPE.ELSEIF:
                        if (isConditionAlreadyValide) break;

                        if (TestCondition(matchedCommand, varsContext))
                        {
                            isConditionAlreadyValide = true;
                            existingData.DisplayText += ParseText(matchedText, varsContext);
                        }
                        break;
                    
                    case COMMAND_TYPE.ELSE:
                        if (isConditionAlreadyValide) break;
                        existingData.DisplayText += ParseText(matchedText, varsContext);
                        break;
                    
                    case COMMAND_TYPE.SOUND:
                        Regex patternSound = new Regex(@"(sound): (?<soundId>.*)");
                        Match soundMatch = patternSound.Match(matchedCommand);
                        string soundId = soundMatch.Groups["soundId"].Value;
                        existingData.SoundIds.Add(soundId);
                        break;
                    
                    case COMMAND_TYPE.UNLESS:
                        if (TestCondition(matchedCommand, varsContext)) break;
                        existingData.DisplayText += ParseText(matchedText, varsContext);
                        break;
                    
                    case COMMAND_TYPE.EITHER:
                        string formatedText = ReplaceVariablesByValues(matchedCommand, "", varsContext);
                        List<ParsingTools.ParsingResult> matches = ParsingTools.GetAllWords(formatedText);
                        existingData.DisplayText += matches[Random.Range(0, matches.Count)].Groups["Inside"];
                        break;
                    
                    case COMMAND_TYPE.COND:
                        string arguments = matchedCommand.Remove(0, 5);

                        List<string> splitedArguments = ParsingTools.CarefullSplit(arguments, ',');

                        bool foundValidCondition = false;

                        for (int j = 0; j < splitedArguments.Count / 2; j += 2)
                        {
                            if (!foundValidCondition && TestCondition(splitedArguments[j], varsContext))
                            {
                                existingData.DisplayText += ReplaceVariablesByValues(splitedArguments[j + 1], "", varsContext).Substring(2, splitedArguments[j + 1].Length - 3);
                                foundValidCondition = true;
                                break;
                            }
                        }

                        if (!foundValidCondition)
                            existingData.DisplayText += ReplaceVariablesByValues(splitedArguments[splitedArguments.Count - 1], "", varsContext)
                                .Substring(2, splitedArguments[splitedArguments.Count - 1].Length - 3);

                        break;
                    
                    case COMMAND_TYPE.NTH:
                        string arguments2 = matchedCommand.Remove(0, 4);

                        List<string> splitedArguments2 = ParsingTools.CarefullSplit(ReplaceVariablesByValues(arguments2, "", varsContext), ',');

                        int nth = 0;
                        int.TryParse(splitedArguments2[0], out nth);
                        //Assert.IsTrue(int.TryParse(splitedArguments2[0], out nth), splitedArguments2[0] + " is not a number");

                        // Debug.Log("splitedArguments2.Count : " + splitedArguments2.Count);
                        nth = (nth - 1) % (splitedArguments2.Count - 1) + 1;
                        // Debug.Log("nth : " + nth);
                        existingData.DisplayText += ReplaceVariablesByValues(splitedArguments2[nth], "", varsContext).Substring(2, splitedArguments2[nth].Length - 3);
                        break;
                    
                    case COMMAND_TYPE.PRINT:
                        string argument = matchedCommand.Remove(0, 6);
                        argument = argument.Trim();
                        existingData.DisplayText += ReplaceVariablesByValues(argument, "", varsContext);
                        break;
                    
                    case COMMAND_TYPE.IMAGE:
                        existingData.SpriteId = ParsingTools.GetWord(matchedCommand, 6).Groups["Inside"];
                        break;
                    
                    case COMMAND_TYPE.DEFAULT:
                        // Debug.Log("false positve");
                        existingData.DisplayText += ReplaceVariablesByValues(match.Value, "", varsContext);
                        break;
                }
            }


            return existingData;
        }
        
        private static COMMAND_TYPE GetCommandType(string command)
        {
            if (command.StartsWith("set:")) return COMMAND_TYPE.SET;
            if (command.StartsWith("if:")) return COMMAND_TYPE.IF;
            if (command.StartsWith("else-if:")) return COMMAND_TYPE.ELSEIF;
            if (command.StartsWith("else:")) return COMMAND_TYPE.ELSE;
            if (command.StartsWith("sound:")) return COMMAND_TYPE.SOUND;
            if (command.StartsWith("unless:")) return COMMAND_TYPE.UNLESS;
            if (command.StartsWith("either:")) return COMMAND_TYPE.EITHER;
            if (command.StartsWith("cond:")) return COMMAND_TYPE.COND;
            if (command.StartsWith("nth:")) return COMMAND_TYPE.NTH;
            if (command.StartsWith("print:")) return COMMAND_TYPE.PRINT;
            if (command.StartsWith("image:")) return COMMAND_TYPE.IMAGE;
            return COMMAND_TYPE.DEFAULT;
        }

        private static bool TestCondition(string condition, IVariableContext varsContext)
        {
            // replace variable by real value
            condition = ReplaceVariablesByValues(condition, "'", varsContext);

            // evaluate
            Regex RGX_extractValues = new Regex(@"((('|"")(?<leftValue>[^'""]*?)('|"") *(?<operator>[^'""\n]*?) *('|"")(?<rightValue>[^'""]*?)('|""))|('|"")(true|false)('|"")|not *('|"")(?<uniqueValue>[^'""]*?)('|""))");
            Match match = RGX_extractValues.Match(condition);

            //Assert.IsTrue(match.Success, "syntaxe error in condition : " + condition);

            if (condition.StartsWith("not"))
            {
                bool isUniqueValueABool = match.Groups["uniqueValue"].Value == "true" || match.Groups["uniqueValue"].Value == "false";
                //Assert.IsTrue(isUniqueValueABool, "invalide argument " + match.Groups["uniqueValue"].Value + "is not a boolean");

                return match.Groups["uniqueValue"].Value == "false";
            }

            if (match.Value == "'true'" || match.Value == "'false'") return match.Value == "'true'";

            bool isLeftValueANumber = float.TryParse(match.Groups["leftValue"].Value, out float leftValue);
            bool isRightValueANumber = float.TryParse(match.Groups["rightValue"].Value, out float rightValue);

            bool isLeftValueABool = !isLeftValueANumber && (match.Groups["leftValue"].Value == "true" || match.Groups["leftValue"].Value == "false");
            bool isRightValueABool = !isRightValueANumber && (match.Groups["rightValue"].Value == "true" || match.Groups["rightValue"].Value == "false");


            switch (match.Groups["operator"].Value)
            {
                case "is":
                    return match.Groups["leftValue"].Value == match.Groups["rightValue"].Value;
                case "is not":
                    return match.Groups["leftValue"].Value != match.Groups["rightValue"].Value;
                case "contains":
                    return match.Groups["leftValue"].Value.Contains(match.Groups["rightValue"].Value);
                case "does not contain":
                    return !match.Groups["leftValue"].Value.Contains(match.Groups["rightValue"].Value);
                case "is in":
                    return match.Groups["rightValue"].Value.Contains(match.Groups["leftValue"].Value);
                case "is not in":
                    return !match.Groups["rightValue"].Value.Contains(match.Groups["leftValue"].Value);
                case ">":
                    if (isLeftValueANumber && isRightValueANumber) return leftValue > rightValue;
                    else
                    {
                        //Assert.IsTrue(false, "invalide argument " + (isLeftValueANumber ? "right value" : "left value") + "is not a number");
                        return false;
                    }
                case ">=":
                    if (isLeftValueANumber && isRightValueANumber) return leftValue >= rightValue;
                    else
                    {
                        //Assert.IsTrue(false, "invalide argument " + (isLeftValueANumber ? "right value" : "left value") + "is not a number");
                        return false;
                    }
                case "<":
                    if (isLeftValueANumber && isRightValueANumber) return leftValue < rightValue;
                    else
                    {
                        //Assert.IsTrue(false, "invalide argument " + (isLeftValueANumber ? "right value" : "left value") + "is not a number");
                        return false;
                    }
                case "<=":
                    if (isLeftValueANumber && isRightValueANumber) return leftValue <= rightValue;
                    else
                    {
                        //Assert.IsTrue(false, "invalide argument " + (isLeftValueANumber ? "right value" : "left value") + "is not a number");
                        return false;
                    }
                case "and":
                    if (isLeftValueABool && isRightValueABool) return match.Groups["leftValue"].Value == "true" && match.Groups["rightValue"].Value == "true";
                    else
                    {
                        //Assert.IsTrue(false, "invalide argument " + (isLeftValueABool ? "right value" : "left value") + "is not a boolean");
                        return false;
                    }
                case "or":
                    if (isLeftValueABool && isRightValueABool) return match.Groups["leftValue"].Value == "true" || match.Groups["rightValue"].Value == "true";
                    else
                    {
                        //Assert.IsTrue(false, "invalide argument " + (isLeftValueABool ? "right value" : "left value") + "is not a boolean");
                        return false;
                    }
            }

            return false;
        }

        private static string ReplaceVariablesByValues(string text, string delimiter, IVariableContext varsContext)
        {
            Regex RGX_FindVariable = new Regex(@"\$\D[^\s,]*");

            Match match = RGX_FindVariable.Match(text);
            while (match.Success)
            {
                text = text.Replace(match.Value, delimiter + varsContext.GetValueOrDefault(match.Value.Substring(1)) + delimiter);
                match = RGX_FindVariable.Match(text);
            }

            return text;
        }

        #endregion

        // ########## GETTER / SETTER ##########
        
        #region Getter / Setter
        public string GetTitle()
        {
            return Title;
        }
        
        public string GetText()
        {
            ComputeNode();
            return DynamicData.DisplayText;
        }
        
        public List<ChoiceData> GetNextNodes()
        {
            return AvailableChoices;
        }

        public List<string> GetTags()
        {
            return Tags;
        }

        public Vector2Int GetPosition()
        {
            return Position;
        }

        public Vector2Int GetSize()
        {
            return Size;
        }

        public string GetRawText()
        {
            return RawText;
        }

        public Story GetParentStory()
        {
            return ParentStory;
        }
        
        public bool HasTag(string tag)
        {
            return Tags.Contains(tag);
        }

        public string GetSprite()
        {
            ComputeNode();
            return DynamicData.SpriteId;
        }

        public string[] GetSoundIds()
        {
            ComputeNode();
            return DynamicData.SoundIds.ToArray();
        }
        
        public VariableUpdate[] GetVarChanges()
        {
            ComputeNode();
            return DynamicData.VariableUpdates.ToArray();
        }
        
        #endregion
    }

    public enum StoryNodeData
    {
        NodeTitle,
        AttachedTags,
        Position,
        Size,
        RawText,
        ParsedText,
        AvailableChoices,
        ParentStory,
        Sprite,
        SoundIds,
        VariableChanges
    }

    public enum ChoiceDataEnum
    {
        NextNodeReference,
        ChoiceDisplay
    }
}