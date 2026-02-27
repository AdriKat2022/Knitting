using AdriKat.Toolkit.Attributes;
using UnityEngine;

namespace Knitting.Tests
{
    public class TestInInspector : MonoBehaviour
    {
        public Story Story;
        // [ButtonAction(nameof(PrintParsedText))]
        public StoryNode CurrentStoryNode;
        
        [ShowIf(nameof(hasChoices))]
        [ButtonAction(nameof(GetNextNodeWithChoice), showButtonBelow = true)]
        public int NextChoice;

        private bool hasChoices;

        private void Start()
        {
            UpdateNodeAndChoices();
        }
        
        [StandaloneButtonAction("Go Next")]
        public void GetNextNode()
        {
            Story.NextNode();
            UpdateNodeAndChoices();
        }
        
        // [StandaloneButtonAction("Go Next With Choice")]
        public void GetNextNodeWithChoice()
        {
            Story.ChooseNextNode(NextChoice);
            UpdateNodeAndChoices();
        }

        private void UpdateNodeAndChoices()
        {
            CurrentStoryNode = Story.GetCurrentNode();
            hasChoices = CurrentStoryNode.GetNextNodes().Count > 1;
        }

        [StandaloneButtonAction("Print Text")]
        private void PrintParsedText() => Debug.Log(CurrentStoryNode.GetText());

        [StandaloneButtonAction("Restart")]
        public void Restart()
        {
            Story.SetToStart();
            UpdateNodeAndChoices();
        }
    }
}