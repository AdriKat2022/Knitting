using System;

namespace Knitting.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TransmuteFromAttribute : Attribute
    {
        public StoryData StoryData;
        public StoryNodeData StoryNodeData;
        public ChoiceDataEnum ChoiceData;

        private Type LinkerType;
        
        public TransmuteFromAttribute(StoryData storyData)
        {
            StoryData = storyData;
            LinkerType = typeof(StoryData);
        }

        public TransmuteFromAttribute(StoryNodeData storyNodeData)
        {
            StoryNodeData = storyNodeData;
            LinkerType = typeof(StoryNodeData);
        }

        public TransmuteFromAttribute(ChoiceDataEnum choiceData)
        {
            ChoiceData = choiceData;
            LinkerType = typeof(ChoiceDataEnum);
        }
    }
}