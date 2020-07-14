using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;
using rnd = UnityEngine.Random;

public class ReversedEdgeworkScript : MonoBehaviour 
{
    public ReversedEdgeworkQuestion[] questions;
    public ReversedEdgeworkQuestion mainQuestion;

    public KMAudio ModuleAudio;
    public KMNeedyModule Module;
    public KMBombInfo BombInfo;

    private int _moduleId;
    public KMSelectable[] Buttons;
    public TextMesh[] texts;
    public ReversedEdgeworkQuestion[] assignedQuestions = new ReversedEdgeworkQuestion[4];

    public TextMesh screenText;

    private int startmin;
    private int startsec;

    private bool active = false;
    
	// Use this for initialization
	void Start () {
        _moduleId++;

        startmin = int.Parse(BombInfo.GetFormattedTime().Split(':').First());
        startsec = int.Parse(BombInfo.GetFormattedTime().Split(':').Last());

        LogTheFile("The bomb's starting time was " + startmin + " minutes and " + startsec + " seconds.");

        questions = new [] {
            new ReversedEdgeworkQuestion{question = "# of INDs", answer = BombInfo.GetIndicators().Count() },
            new ReversedEdgeworkQuestion{question = "# of lit INDs", answer = BombInfo.GetOnIndicators().Count()},
            new ReversedEdgeworkQuestion{question = "# of off INDs", answer = BombInfo.GetOffIndicators().Count()},
            new ReversedEdgeworkQuestion{question = "# of ports", answer = BombInfo.GetPortCount()},
            new ReversedEdgeworkQuestion{question = "# of port p.s", answer = BombInfo.GetPortPlateCount()},
            new ReversedEdgeworkQuestion{question = "# of batts", answer = BombInfo.GetBatteryCount()},
            new ReversedEdgeworkQuestion{question = "# of D batts", answer = BombInfo.GetBatteryCount(Battery.D)},
            new ReversedEdgeworkQuestion{question = "# of AA batts", answer = BombInfo.GetBatteryCount(Battery.AA)},
            new ReversedEdgeworkQuestion{question = "# of batt hds", answer = BombInfo.GetBatteryHolderCount()},
            new ReversedEdgeworkQuestion{question = "First # of SN", answer = BombInfo.GetSerialNumberNumbers().First()},
            new ReversedEdgeworkQuestion{question = "Last # of SN", answer = BombInfo.GetSerialNumberNumbers().Last()},
            new ReversedEdgeworkQuestion{question = "# of mod", answer = BombInfo.GetModuleNames().Count()},
            new ReversedEdgeworkQuestion{question = "# of r. mod", answer = BombInfo.GetSolvableModuleNames().Count()},
            new ReversedEdgeworkQuestion{question = "# of n. mod", answer = BombInfo.GetModuleNames().Count() - BombInfo.GetSolvableModuleNames().Count()},
            new ReversedEdgeworkQuestion{question = "b. st. min", answer = startmin},
            new ReversedEdgeworkQuestion{question = "b. st. sec", answer = startsec}
        };
        for (var i = 0; i < 4; ++i) 
        {
            int index = i;
            Buttons[index].OnInteract += delegate 
            {
                Buttons[index].AddInteractionPunch();
                ModuleAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[index].transform);
                if (!active)
                {
                    return false;
                }
                if (mainQuestion.answer != assignedQuestions[index].answer)
                {
                    active = false;
                    foreach (TextMesh text in texts) 
                    {
                        text.text = "";
                    }
                    screenText.text = "";
                    LogTheFile("You pressed " + assignedQuestions[index].question + ", which was wrong. Handling Strike and going to sleep.");
                    Module.HandleStrike();
                    Module.HandlePass();
                } 
                else 
                {
                    active = false;
                    foreach (TextMesh text in texts) 
                    {
                        text.text = "";
                    }
                    screenText.text = "";
                    Module.HandlePass();
                    LogTheFile("Correct question pressed, going to sleep.");
                }
                return false;
            };
            Buttons[index].OnInteractEnded += delegate
            {
                ModuleAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease,
                    Buttons[index].transform);
            };
        }
        Module.OnNeedyActivation += GenerateQuestion;
        Module.OnNeedyDeactivation += delegate
        {
            active = false;
            foreach (TextMesh text in texts) 
            {
                text.text = "";
            }
            screenText.text = "";
        };
        Module.OnTimerExpired += delegate {
            LogTheFile("Timer expired, handling Strike");
            Module.HandleStrike();
        };
	}

    void GenerateQuestion()
    {
        active = true;
        List<int> questionsChosen;
        do
        {
            questionsChosen = new List<int>()
            {
                rnd.Range(0, questions.Length), 
                rnd.Range(0, questions.Length), 
                rnd.Range(0, questions.Length), 
                rnd.Range(0, questions.Length)
            }; 
        } while (questionsChosen.GroupBy(x => x).Any(x => x.Count() > 1));

        mainQuestion = questions[questionsChosen[0]];
        ReversedEdgeworkQuestion question2 = questions[questionsChosen[1]];
        ReversedEdgeworkQuestion question3 = questions[questionsChosen[2]];
        ReversedEdgeworkQuestion question4 = questions[questionsChosen[3]];

        List<ReversedEdgeworkQuestion> possible = new List<ReversedEdgeworkQuestion>() { mainQuestion, question2, question3, question4 };

        screenText.text = mainQuestion.answer.ToString();

        for (int i = 0; i < 4; ++i) {
            assignedQuestions[i] = possible.PickRandom();
            possible.Remove(assignedQuestions[i]);
            texts[i].text = assignedQuestions[i].question;
        }
        LogTheFile("I've chosen the following questions: " + assignedQuestions[0].question + ", " + assignedQuestions[1].question + ", " + assignedQuestions[2].question + ", " + assignedQuestions[3].question + ".");
    }

    void LogTheFile(string logMessage) {
        Debug.LogFormat("[Reversed Edgework #{0}] {1}", _moduleId, logMessage);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use “!{0} tr/br/tl/bl” to press the button in the corresponding location.";
#pragma warning restore 414
    public IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        if (!active)
        {
            yield return "sendtochaterror You can't press me when I am not active!";
        }
        switch (command)
        {
            case "tl":
            case "top left":
            case "top-left":
            case "left top":
            case "left-top":
                yield return null;
                Buttons[0].OnInteract();
                yield break;
            case "tr":
            case "top right":
            case "top-right":
            case "right top":
            case "right-top":
                yield return null;
                Buttons[2].OnInteract();
                yield break;
            case "bl":
            case "bottom left":
            case "bottom-left":
            case "left bottom":
            case "left-bottom":
                yield return null;
                Buttons[1].OnInteract();
                yield break;
            case "br":
            case "bottom right":
            case "bottom-right":
            case "right bottom":
            case "right-bottom":
                yield return null;
                Buttons[3].OnInteract();
                yield break;
            default:
                yield return "sendtochaterror You can only press tl/tr/bl/br";
                yield break; 
        }
    }
}

public class ReversedEdgeworkQuestion{
    public string question;
    public int answer;
}
