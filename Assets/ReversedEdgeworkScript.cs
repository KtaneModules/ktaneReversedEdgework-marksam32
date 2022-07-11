using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;
using rnd = UnityEngine.Random;

public class ReversedEdgeworkScript : MonoBehaviour
{
    public KMAudio ModuleAudio;
    public KMNeedyModule Module;
    public KMBombInfo BombInfo;

    private int _moduleId;
    public KMSelectable[] Buttons;
    public TextMesh[] texts;
    
    private readonly ReversedEdgeworkQuestion[] _assignedQuestions = new ReversedEdgeworkQuestion[4];
    private List<ReversedEdgeworkQuestion> _questions;
    private ReversedEdgeworkQuestion _mainQuestion;


    public TextMesh screenText;

    private int _startmin;
    private int _startsec;

    private bool _active;

    // Use this for initialization
    void Start()
    {
        _moduleId++;

        _startmin = int.Parse(BombInfo.GetFormattedTime().Split(':').First());
        _startsec = int.Parse(BombInfo.GetFormattedTime().Split(':').Last());

        LogTheFile(string.Format("The bomb's starting time was {0} minutes and {1} seconds.", _startmin, _startsec));

        _questions = new List<ReversedEdgeworkQuestion>
        {
            new ReversedEdgeworkQuestion("# of INDs", BombInfo.GetIndicators().Count()),
            new ReversedEdgeworkQuestion("# of lit INDs", BombInfo.GetOnIndicators().Count()),
            new ReversedEdgeworkQuestion("# of off INDs", BombInfo.GetOffIndicators().Count()),
            new ReversedEdgeworkQuestion("# of ports", BombInfo.GetPortCount()),
            new ReversedEdgeworkQuestion("# of port p.s", BombInfo.GetPortPlateCount()),
            new ReversedEdgeworkQuestion("# of batts", BombInfo.GetBatteryCount()),
            new ReversedEdgeworkQuestion("# of D batts", BombInfo.GetBatteryCount(Battery.D)),
            new ReversedEdgeworkQuestion("# of AA batts", BombInfo.GetBatteryCount(Battery.AA)),
            new ReversedEdgeworkQuestion("# of batt hds", BombInfo.GetBatteryHolderCount()),
            new ReversedEdgeworkQuestion("First # of SN", BombInfo.GetSerialNumberNumbers().First()),
            new ReversedEdgeworkQuestion("Last # of SN", BombInfo.GetSerialNumberNumbers().Last()),
            new ReversedEdgeworkQuestion("# of mod", BombInfo.GetModuleNames().Count),
            new ReversedEdgeworkQuestion("# of r. mod", BombInfo.GetSolvableModuleNames().Count),
            new ReversedEdgeworkQuestion("# of n. mod",
                BombInfo.GetModuleNames().Count - BombInfo.GetSolvableModuleNames().Count),
            new ReversedEdgeworkQuestion("b. st. min", _startmin),
            new ReversedEdgeworkQuestion("b. st. sec", _startsec)
        };
        for (var i = 0; i < 4; ++i)
        {
            int index = i;
            Buttons[index].OnInteract += delegate
            {
                Buttons[index].AddInteractionPunch();
                ModuleAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[index].transform);
                if (!_active)
                {
                    return false;
                }

                if (_mainQuestion.Answer != _assignedQuestions[index].Answer)
                {
                    _active = false;
                    foreach (var text in texts)
                    {
                        text.text = "";
                    }

                    screenText.text = "";
                    LogTheFile(string.Format("You pressed {0}({1}), which was wrong. Handling Strike and going to sleep.",
                        _assignedQuestions[index].Question, _assignedQuestions[index].Answer.ToString()));
                    Module.HandleStrike();
                    Module.HandlePass();
                }
                else
                {
                    _active = false;
                    foreach (var text in texts)
                    {
                        text.text = "";
                    }

                    screenText.text = "";
                    Module.HandlePass();
                    LogTheFile("Correct button pressed, going to sleep.");
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
            _active = false;
            foreach (var text in texts)
            {
                text.text = "";
            }
            screenText.text = "";
        };
        Module.OnTimerExpired += delegate
        {
            foreach (var text in texts)
            {
                text.text = "";
            }

            screenText.text = "";
            LogTheFile("Timer expired, handling Strike");
            Module.HandleStrike();
        };
    }

    void GenerateQuestion()
    {
        _active = true;
        var questionsChosen = new List<int>
        {
            rnd.Range(0, _questions.Count),
            rnd.Range(0, _questions.Count),
            rnd.Range(0, _questions.Count),
            rnd.Range(0, _questions.Count)
        };

        while (questionsChosen.GroupBy(x => x).Any(x => x.Count() > 1))
        {
            questionsChosen = new List<int>()
            {
                rnd.Range(0, _questions.Count),
                rnd.Range(0, _questions.Count),
                rnd.Range(0, _questions.Count),
                rnd.Range(0, _questions.Count)
            };
        }

        _mainQuestion = _questions[questionsChosen[0]];

        var possibleQuestions = new List<ReversedEdgeworkQuestion>()
            {_mainQuestion, _questions[questionsChosen[1]], _questions[questionsChosen[2]], _questions[questionsChosen[3]]};

        screenText.text = _mainQuestion.Answer.ToString();

        possibleQuestions.Shuffle();

        for (int i = 0; i < 4; ++i)
        {
            _assignedQuestions[i] = possibleQuestions[i];
            texts[i].text = _assignedQuestions[i].Question;
        }

        LogTheFile(string.Format("I've chosen the following questions: {0}.",
            string.Join(", ", _assignedQuestions.Select(x => x.Question).ToArray())));
        LogTheFile(string.Format("The number on the display is: {0}", _mainQuestion.Answer.ToString()));
        LogTheFile(string.Format("One correct answer is: {0}", _mainQuestion.Question));
    }

    void LogTheFile(string logMessage)
    {
        Debug.LogFormat("[Reversed Edgework #{0}] {1}", _moduleId, logMessage);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage =
        @"Use “!{0} tr/br/tl/bl” to press the button in the corresponding location.";
#pragma warning restore 414
    
    public IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();

        if (!_active)
        {
            yield return "sendtochaterror How do you think pressing me when I'm not active will benefit you and your bomb? 4Head";
            yield break;
        }

        if (command.StartsWith("press"))
        {
            command = string.Join(" ", command.Split(' ').Skip(1).ToArray());
        }

        switch (command)
        {
            case "tl":
            case "lt":
            case "top left":
            case "top-left":
            case "left top":
            case "left-top":
                yield return null;
                Buttons[0].OnInteract();   
                yield return new WaitForSeconds(.1f);
                Buttons[0].OnInteractEnded();
                yield break;
            case "tr":
            case "rt":
            case "top right":
            case "top-right":
            case "right top":
            case "right-top":
                yield return null;
                Buttons[2].OnInteract();
                yield return new WaitForSeconds(.1f);
                Buttons[2].OnInteractEnded();
                yield break;
            case "bl":
            case "lb":
            case "bottom left":
            case "bottom-left":
            case "left bottom":
            case "left-bottom":
                yield return null;
                Buttons[1].OnInteract();
                yield return new WaitForSeconds(.1f);
                Buttons[1].OnInteractEnded();
                yield break;
            case "br":
            case "rb":
            case "bottom right":
            case "bottom-right":
            case "right bottom":
            case "right-bottom":
                yield return null;
                Buttons[3].OnInteract();
                yield return new WaitForSeconds(.1f);
                Buttons[3].OnInteractEnded();
                yield break;
            default:
                yield return string.Format("sendtochaterror Do you honestly expect me to know what {0} means? 4Head", command != string.Empty ? command : "an empty command");
                yield break;
        }
    }
}

public class ReversedEdgeworkQuestion
{
    public string Question { get; private set; }
    public int Answer { get; private set; }

    public ReversedEdgeworkQuestion(string question, int answer)
    {
        this.Question = question;
        this.Answer = answer;
    }
}