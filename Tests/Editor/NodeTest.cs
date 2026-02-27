using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Knitting.Tests
{
    public class NodeTest
    {
        private GameObject storyHolder = new GameObject();
        private Story story;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            story = storyHolder.AddComponent<Story>();
        }


        [Test]
        public void NodeTestWithTags()
        {
            Debug.Log("==========\nNodeTestWithTags\n==========");

            string componentTitle = "Cassandre_6 [PNJ QUESTION] {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentData = "(set: $A2 to '')\n" +
                                   "Dis-moi machine savante. Quelle est la voie � suivre pour atteindre mes ambitions ?\n" +
                                   "\n" +
                                   "La Papesse[[A2HighPriestess]]\n" +
                                   "L'Empereur[[A2Emperor]]\n" +
                                   "L'Hermite[[A2Hermit]]\n" +
                                   "Les Amoureux[[A2Lovers]]";

            StoryNode nodeA = new StoryNode(componentTitle, componentData, story);

            List<string> tags = new List<string>();
            tags.Add("PNJ");
            tags.Add("QUESTION");
            StoryNode nodeB = new StoryNode("Cassandre_6", tags, new Vector2Int(850,5350), new Vector2Int(100,100), componentData, story);

            nodeA.PrintNodeData();
            nodeB.PrintNodeData();

            Assert.IsTrue(nodeA.Equals(nodeB));
            Debug.Log("====================");
        }


        [Test]
        public void NodeTestWithoutTags()
        {
            Debug.Log("==========\nNodeTestWithoutTags\n==========");

            string componentTitle = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentData = "(set: $A2 to '')\n" +
                                   "Dis-moi machine savante. Quelle est la voie � suivre pour atteindre mes ambitions ?\n" +
                                   "\n" +
                                   "La Papesse[[A2HighPriestess]]\n" +
                                   "L'Empereur[[A2Emperor]]\n" +
                                   "L'Hermite[[A2Hermit]]\n" +
                                   "Les Amoureux[[A2Lovers]]";

            StoryNode nodeA = new StoryNode(componentTitle, componentData, story);

            List<string> tags = new List<string>();
            StoryNode nodeB = new StoryNode("Cassandre_6", tags, new Vector2Int(850, 5350), new Vector2Int(100, 100), componentData, story);

            nodeA.PrintNodeData();
            nodeB.PrintNodeData();

            Assert.IsTrue(nodeA.Equals(nodeB));

            Debug.Log("====================");
        }


        [Test]
        public void NodeTestDataParsing()
        {
            Debug.Log("==========\nNodeTestDataParsing\n==========");

            string componentTitle = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentData = "(set: $A2 to '')\n" +
                                   "Dis-moi machine savante. Quelle est la voie � suivre pour atteindre mes ambitions ?\n" +
                                   "\n" +
                                   "[[La Papesse->A2HighPriestess]]\n" +
                                   "[[A2Emperor<-L'Empereur]]\n" +
                                   "[[L'Hermite->A2Hermit]]\n" +
                                   "[[Les Amoureux->A2Lovers]]";


            string outputedText = "Dis-moi machine savante. Quelle est la voie � suivre pour atteindre mes ambitions ?";
            List<ChoiceData> goodNextNodes = new List<ChoiceData>();
            goodNextNodes.Add(new ChoiceData("A2HighPriestess", "La Papesse"));
            goodNextNodes.Add(new ChoiceData("A2Emperor", "L'Empereur"));
            goodNextNodes.Add(new ChoiceData("A2Hermit", "L'Hermite"));
            goodNextNodes.Add(new ChoiceData("A2Lovers", "Les Amoureux"));


            StoryNode nodeA = new StoryNode(componentTitle, componentData, story);

            Debug.Log(nodeA.GetText());

            Assert.AreEqual(outputedText,nodeA.GetText());

            List<ChoiceData> nextNodes = nodeA.GetNextNodes();
            Assert.AreEqual(nextNodes.Count, goodNextNodes.Count);
            for(int i = 0; i < goodNextNodes.Count; i++)
            {
                Assert.AreEqual(goodNextNodes[i].nodeTitle, nextNodes[i].nodeTitle);
                Assert.AreEqual(goodNextNodes[i].display, nextNodes[i].display);
            }


            Debug.Log("====================");
        }

        [Test]
        public void NodeTestSetVariable()
        {
            Debug.Log("==========\nNodeTestSetVariable\n==========");

            string componentTitle = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentData = "(set: $Variable to \"ThisIsAVariableValue\")\n";

            StoryNode nodeA = new StoryNode(componentTitle, componentData, story);
            nodeA.GetText();

            string value = story.GetVariable("Variable");

            Assert.AreEqual(value, "ThisIsAVariableValue");

            Debug.Log("====================");
        }

        [Test]
        public void NodeTestSimpleIf()
        {
            Debug.Log("==========\nNodeTestSimpleIf\n==========");

            string componentTitleA = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataA = "(set: $Variable to 'ThisIsAVariableValue')\n";
            StoryNode nodeA = new StoryNode(componentTitleA, componentDataA, story);


            string componentTitleB = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataB = "(if: $Variable is \"ThisIsAVariableValue\")[Vous aviez raison, la Nouvelle Lune n�est que mensonge. Ma vie n�a cess� de s�am�liorer depuis que je l�ai quitt�e.]\n" +
                                    "(else-if: $Variable is 'ThisIsAnOtherVariableValue')[La vache... CETTE vache !! Elle est en train de tout me prendre ! Mon poste, mon parti, tous mes �lecteurs...]\n" +
                                    "(else:)[La Nouvelle Lune m�a guid�e dans l�ascension au sein de l�ordre. Je n�aurai jamais pu esp�rer une �volution plus fulgurante.]";
            StoryNode nodeB = new StoryNode(componentTitleB, componentDataB, story);

            string componentTitleC = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataC = "(set: $Variable to 'ThisIsAnOtherVariableValue')\n";
            StoryNode nodeC = new StoryNode(componentTitleC, componentDataC, story);

            string componentTitleD = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataD = "(set: $Variable to '')\n";
            StoryNode nodeD = new StoryNode(componentTitleD, componentDataD, story);
            nodeD.GetText();

            Debug.Log(nodeB.GetText());
            Assert.AreEqual("La Nouvelle Lune m�a guid�e dans l�ascension au sein de l�ordre. Je n�aurai jamais pu esp�rer une �volution plus fulgurante.", nodeB.GetText());
        
            nodeA.GetText();
            string value = story.GetVariable("Variable");
            Assert.AreEqual(value, "ThisIsAVariableValue");

            Debug.Log(nodeB.GetText());
            Assert.AreEqual("Vous aviez raison, la Nouvelle Lune n�est que mensonge. Ma vie n�a cess� de s�am�liorer depuis que je l�ai quitt�e.", nodeB.GetText());
        
            nodeC.GetText();
            value = story.GetVariable("Variable");
            Assert.AreEqual(value, "ThisIsAnOtherVariableValue");

            Debug.Log(nodeB.GetText());
            Assert.AreEqual("La vache... CETTE vache !! Elle est en train de tout me prendre ! Mon poste, mon parti, tous mes �lecteurs...", nodeB.GetText());

            Debug.Log("====================");
        }

        [Test]
        public void NodeTestUnless()
        {
            Debug.Log("==========\nNodeTestUnless\n==========");


            string componentTitleA = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataA = "(set: $Variable to \"\")\n";
            StoryNode nodeA = new StoryNode(componentTitleA, componentDataA, story);

            string componentTitleB = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataB = "(unless: $Variable is 'ThisIsAVariableValue')[Vous aviez raison, la Nouvelle Lune n�est que mensonge.]";
            StoryNode nodeB = new StoryNode(componentTitleB, componentDataB, story);

            string componentTitleC = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataC = "(set: $Variable to 'ThisIsAVariableValue')\n";
            StoryNode nodeC = new StoryNode(componentTitleC, componentDataC, story);

            nodeA.GetText();
            Debug.Log(nodeB.GetText());
            Assert.AreEqual("Vous aviez raison, la Nouvelle Lune n�est que mensonge.", nodeB.GetText());

            nodeC.GetText();
            Debug.Log(nodeB.GetText());
            Assert.AreEqual("", nodeB.GetText());

            Debug.Log("====================");
        }

        [Test]
        public void NodeTestEither()
        {
            Debug.Log("==========\nNodeTestEither\n==========");


            string componentTitle = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentData = "this test is (either: \"good\", 'bad', 'ug,ly').";
            StoryNode node = new StoryNode(componentTitle, componentData, story);

            int[] count = {0,0,0};

            for (int i = 0; i < 1000; i++)
            {
                Debug.Log(node.GetText());
                switch (node.GetText())
                {
                    case "this test is good.":
                        count[0]++;
                        break;
                    case "this test is bad.":
                        count[1]++;
                        break;
                    case "this test is ug,ly.":
                        count[2]++;
                        break;
                }
            }

            Debug.Log(node.GetText());
            Assert.AreEqual(1000, count[0] + count[1] + count[2]);
            Assert.Greater(count[0], 300);
            Assert.Greater(count[1], 300);
            Assert.Greater(count[2], 300);

            Debug.Log("====================");
        }

        [Test]
        public void NodeTestCond()
        {
            Debug.Log("==========\nNodeTestCond\n==========");


            string componentTitleA = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataA = "(set: $Variable to 'false')\n";
            StoryNode nodeA = new StoryNode(componentTitleA, componentDataA, story);

            string componentTitleB = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataB = "Your (cond: $Variable, \"gasps of triumph\", \"wheezes of defeat\") drown out all other noise.";
            StoryNode nodeB = new StoryNode(componentTitleB, componentDataB, story);

            string componentTitleC = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataC = "(set: $Variable to 'true')\n";
            StoryNode nodeC = new StoryNode(componentTitleC, componentDataC, story);

            nodeA.GetText();
            Debug.Log(nodeB.GetText());
            Assert.AreEqual("Your wheezes of defeat drown out all other noise.", nodeB.GetText());

            nodeC.GetText();
            Debug.Log(nodeB.GetText());
            Assert.AreEqual("Your gasps of triumph drown out all other noise.", nodeB.GetText());

            Debug.Log("====================");
        }

        [Test]
        public void NodeTestNth()
        {
            Debug.Log("==========\nNodeTestNth\n==========");


            string componentTitleA = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataA = "(set: $Variable to '1')";
            StoryNode nodeA = new StoryNode(componentTitleA, componentDataA, story);

            string componentTitleB = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataB = "(nth: $Variable, \"Hi!\", \"Hello again!\", \"Oh, it's you!\", \"Hey!\")";
            StoryNode nodeB = new StoryNode(componentTitleB, componentDataB, story);

            string componentTitleC = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataC = "(set: $Variable to '3')";
            StoryNode nodeC = new StoryNode(componentTitleC, componentDataC, story);

            string componentTitleD = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentDataD = "(set: $Variable to '5')";
            StoryNode nodeD = new StoryNode(componentTitleD, componentDataD, story);

            nodeA.GetText();
            // Debug.Log(nodeB.getText());
            Assert.AreEqual("Hi!", nodeB.GetText());

            nodeC.GetText();
            // Debug.Log(nodeB.getText());
            Assert.AreEqual("Oh, it's you!", nodeB.GetText());

            nodeD.GetText();
            // Debug.Log(nodeB.getText());
            Assert.AreEqual("Hi!", nodeB.GetText());

            Debug.Log("====================");
        }
    
        [Test]
        public void NodeTestPrint()
        {
            Debug.Log("==========\nNodeTestPrint\n==========");

            string componentTitle = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentData = "(set: $Variable to 'successful')this is a (print: $Variable) test !";
            StoryNode node = new StoryNode(componentTitle, componentData, story);

            Debug.Log(node.GetText());
            Assert.AreEqual("this is a successful test !", node.GetText());

            Debug.Log("====================");
        }
    
        [Test]
        public void NodeTestRecursive()
        {
            Debug.Log("==========\nNodeTestPrint\n==========");

            string componentTitle = "Cassandre_6 {\"position\":\"850,5350\",\"size\":\"100,100\"}";
            string componentData = "(if: $isOpen is \"false\")[...]\n" +
                                   "(else:)[C��tait notre tableau pr�f�r� � l��cole d�art...(if: $isHappy is \"false\")[(set: $oneTime to \"true\")]]\n" +
                                   "[[Ce_P_Tableau_Reponse]]";

            StoryNode node = new StoryNode(componentTitle, componentData, story);

            story.SetVariable("isOpen", "false");
            story.SetVariable("isHappy", "false");
            story.SetVariable("oneTime", "false");

            Debug.Log(node.GetText());
            Assert.AreEqual("...", node.GetText());

            story.SetVariable("isOpen", "true");
            Debug.Log(node.GetText());
            Assert.AreEqual("C��tait notre tableau pr�f�r� � l��cole d�art...", node.GetText());
            Assert.AreEqual("true",story.GetVariable("oneTime"));

            Debug.Log("====================");
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        /*
    [UnityTest]
    public IEnumerator NodeTestWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
    */
    }
}

/*
 'zefef' is'ThisIsAVariableValue'
'fvc' is not   'ThisIsAVariableValue'
'cvd'contains'ThisIsAVariableValue'
'' does not contain 'ThisIsAVariableValue'
'' is in 'ThisIsAVariableValue'
'' > 'ThisIsAVariableValue'
'' >= 'ThisIsAVariableValue'
'' < 'ThisIsAVariableValue'
'' <= 'ThisIsAVariableValue'
'' and 'ThisIsAVariableValue'
'' or 'ThisIsAVariableValue'
'true'
'false'
 */