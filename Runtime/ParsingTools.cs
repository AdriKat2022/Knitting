using System;
using System.Collections.Generic;

namespace Knitting
{
    public static class ParsingTools
    {
        public class ParsingResult
        {
            public Dictionary<string, string> Groups = new();
            public string Value;
            public bool Success;

            public int StartIndex;
            public int EndIndex;

            public override bool Equals(object other)
            {
                if(other.GetType() != typeof(ParsingResult)) return false;
                ParsingResult otherParsingResult = (ParsingResult)other;
                if (this.Value != otherParsingResult.Value) return false;
                if (this.Groups.Count != otherParsingResult.Groups.Count) return false;
                foreach (KeyValuePair<string, string> pair in this.Groups) 
                {
                    string value;
                    if(!otherParsingResult.Groups.TryGetValue(pair.Key, out value)) return false;
                    if (value != pair.Value) return false;
                }
                if (this.Success != otherParsingResult.Success) return false;
                return true;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Groups, Value, Success);
            }

            public override string ToString() {  return Value; }
        }

        public static int FindInWord(string text, char separator, int offset = 0, int toIgnore = 0)
        {
            bool isInGuillemet = false;
            bool isInApostrophe = false;
            bool skipnext = false;

            for (int i = offset; i < text.Length; i++)
            {
                if (!isInApostrophe && !isInGuillemet && text[i] == separator) if (toIgnore == 0) return i; else toIgnore--;
                if (!skipnext && !isInGuillemet && text[i] == '\'') isInApostrophe = !isInApostrophe;
                if (!skipnext && !isInApostrophe && text[i] == '"') isInGuillemet = !isInGuillemet;
            }

            return -1;
        }

        public static List<string> CarefullSplit(string text, char separator, int offset = 0)
        {
            List<string> splited = new List<string>();

            int index = FindInWord(text, separator, offset);
            while (index != -1)
            {
                splited.Add(text.Substring(offset,index - offset));
                offset = index + 1;
                index = FindInWord(text, separator, offset);
            }

            if (offset < text.Length) splited.Add(text.Substring(offset, text.Length - offset));
            else if (offset == text.Length) splited.Add("");

            return splited;
        }

        public static ParsingResult GetBetween(string text, char begin, char end, int offset = 0) 
        {
            int startIndex = FindInWord(text, begin, offset);
            int endIndex = FindInWord(text, end, startIndex + 1);
            int firstEndIndex = endIndex;


            ParsingResult result = new ParsingResult();
            result.Success = false;

            if (startIndex < 0 || endIndex < 0)return result;

            string potentialValue = text.Substring(startIndex, endIndex - startIndex + 1);

            while(!(CountOf(potentialValue, begin) == CountOf(potentialValue, end) || endIndex < firstEndIndex))
            {             
                endIndex = FindInWord(text, end, endIndex + 1);
                //Debug.Log(endIndex + " " + potentialValue);
                potentialValue = text.Substring(startIndex, endIndex - startIndex + 1);
            }

            if(endIndex < firstEndIndex) return GetBetween(text, begin, end, startIndex + 1);

            result.Value = potentialValue;
            //Debug.Log("end : " + begin + " " + CountOf(result.Value,begin) + "," + end + " " + CountOf(result.Value,end));
            result.StartIndex = startIndex;
            result.EndIndex = endIndex + 1;
            result.Success = true;
            result.Groups.Add("Inside",text.Substring(startIndex + 1, endIndex - startIndex - 1));

            return result;
        }

    
        public static ParsingResult GetCommande(string text, int offset = 0)
        {
            ParsingResult result = new ParsingResult();

            ParsingResult macro = GetBetween(text, '(', ')', offset);
            if (!macro.Success) return macro;
            result.StartIndex = macro.StartIndex;
            result.EndIndex = macro.EndIndex;
            result.Groups.Add("commande", macro.Groups["Inside"]);
            result.Success = true;

            if (macro.EndIndex + 1 < text.Length && text[macro.EndIndex] == '[')
            {
                ParsingResult hook = GetBetween(text, '[', ']', macro.EndIndex);

                if (hook.Success)
                {
                    result.Groups.Add("text", hook.Groups["Inside"]);
                    result.EndIndex = hook.EndIndex;
                }
            }

            result.Value = text.Substring(result.StartIndex, result.EndIndex - result.StartIndex);

            return result ;
        }

        public static List<ParsingResult> GetAllCommandes(string text, int offset = 0)
        {
            List<ParsingResult> result = new List<ParsingResult>();

            ParsingResult match = GetCommande(text, offset);

            while (match.Success) 
            {
                result.Add(match);
                match = GetCommande(text,match.EndIndex + 1);
            }

            return result ;

        }

        public static List<string> GetOpposit(string text, List<ParsingResult> complement)
        {
            List<string> result = new List<string>();

            int start = 0;

            foreach (ParsingResult complementItem in complement)
            {
                result.Add(text.Substring(start,complementItem.StartIndex - start ));
                start = complementItem.EndIndex;
            }

            if (start < text.Length) result.Add(text.Substring(start, text.Length - start));

            return result ;
        }

        public static ParsingResult GetSet(string text, int offset = 0) 
        {
            ParsingResult result = new ParsingResult();

            int variableNameStart = -1;
            int variableNameEnd = -1;

            for (int i = offset; i < text.Length; i++) 
            {
                if (text[i] == '$' && variableNameStart == -1) variableNameStart = i + 1;
                else if (text[i] == ' ' && variableNameStart != -1 && variableNameEnd == -1) variableNameEnd = i;
                else if (variableNameEnd != -1) break;
            }

            if (variableNameStart == -1 || variableNameEnd == -1) return result ;

            result.Groups.Add("variableName", text.Substring(variableNameStart, variableNameEnd - variableNameStart));

            ParsingResult value = GetWord(text,variableNameEnd);
            if (!value.Success) return result ;

            result.Groups.Add("value", value.Groups["Inside"]);

            result.Value = text;
            result.Success = true ;
            result.StartIndex = 0;
            result.EndIndex = text.Length;

            return result;
        }

        public static ParsingResult GetWord(string text, int offset = 0)
        {
            ParsingResult valueGuillemet = GetBetween(text, '"', '"', offset);
            ParsingResult valueApostrophe = GetBetween(text, '\'', '\'', offset);

            return valueGuillemet.Success ? valueGuillemet : valueApostrophe;
        }


        public static List<ParsingResult> GetAllWords(string text, int offset = 0)
        {
            List<ParsingResult> result = new List<ParsingResult>();

            ParsingResult match = GetWord(text, offset);

            while (match.Success)
            {
                result.Add(match);
                match = GetWord(text, match.EndIndex + 1);
            }

            return result;

        }

        public static int CountOf(string text, char c)
        {
            int retour = 0;
            foreach (char ch in text) if (ch == c) retour++;
            return retour;
        }

    }
}
