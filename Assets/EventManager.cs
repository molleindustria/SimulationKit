using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Tweens;
using System.Text.RegularExpressions;


public class EventManager : MonoBehaviour
{
    [Tooltip("The global variables used by the expressions. + to create a new one.")]
    [SerializeField]
    public List<Variable> vars = new List<Variable>();

    [Tooltip("The whole deck of events.")]
    public List<EventSO> allEvents = new List<EventSO>();
    [Tooltip("The draw deck of event (excluding past and inactive)")]
    public List<EventSO> nextEvents = new List<EventSO>();

    private IEnumerator turnCoroutine;

    [Tooltip("the prefabs for the event UI that will be created dynamically")]
    public GameObject eventPanelPrefab;
    [Tooltip("each available action will have to create a button")]
    public GameObject actionButtonPrefab;

    //the current panel
    private GameObject currentEventUI;
    public Canvas canvas;

    public string CONTINUE_BUTTON_TEXT = "Continue";

    //block the button inputs to prevent clicks during transitions
    public bool inputActive = true;

    // Start is called before the first frame update
    void Start()
    {
        turnCoroutine = Turn();
        StartGame();
    }

    void StartGame()
    {
        //Load all the scriptable Objects (SO) in Resources/Event
        EventSO[] allSO = Resources.LoadAll<EventSO>("Events");

        //create a "deck" of events by cloning the scriptable object
        //cloning allows me to modify a SO without permanently changing the asset

        //this is the actual events "deck"
        allEvents = new List<EventSO>();

        foreach (EventSO e in allSO)
        {
            //for each type of event I may have multiples (copies of the same card in a deck)
            for (int i = 0; i < e.number; i++)
            {
                // Creates a runtime copy
                EventSO eventInstance = Instantiate(e);

                // Rename to match the original asset for easy identification
                eventInstance.name = e.name;

                // Add it to the full deck
                allEvents.Add(eventInstance);

                // Add it to the draw deck if start active
                if (e.active)
                    nextEvents.Add(eventInstance);
            }
        }


        //shuffle the decks
        Shuffle(allEvents);
        Shuffle(nextEvents);

        //set all the variables to trigger UI updates
        foreach (Variable v in vars)
            v.OnValueChanged(0, v.Value);

        NextTurn();
    }

    //turn sequence, it's a co-routine so I can do timed animations
    IEnumerator Turn()
    {
        CheckConditions();

        //deactivate input until the animations are done
        inputActive = false;

        //example of tween animation using the library
        //https://github.com/jeffreylanters/unity-tweens

        //destroy the old event panel if it exists
        if (currentEventUI != null)
        {
            //animate a movement
            currentEventUI.transform.localScale = Vector3.one;
            var tween = new AnchoredPositionTween
            {
                to = new Vector2(0, -1000),
                duration = 0.5f,
                easeType = EaseType.CubicIn
            };
            currentEventUI.AddTween(tween);

            //I wait the same amount of the tween
            yield return new WaitForSeconds(0.5f);

            //destroy after the delay
            Destroy(currentEventUI);
        }


        //If there are events in the draw deck
        if (nextEvents.Count > 0)
        {
            //dynamically populates a prefab based on the first event in the deck
            currentEventUI = CreateEventPanel(nextEvents[0], canvas.transform);

            //example of tween animation using the library
            //https://github.com/jeffreylanters/unity-tweens

            currentEventUI.transform.localScale = Vector3.zero;
            var tween = new LocalScaleTween
            {
                to = Vector3.one,
                duration = 0.5f,
                easeType = EaseType.BackOut
            };
            currentEventUI.AddTween(tween);

            //I wait the same amount of the tween
            yield return new WaitForSeconds(0.5f);

            //animation is over enable input
            inputActive = true;

        }
        else
        {

            //reached the end of the deck, reshuffle or end the game
            print("End of the deck, reshuffle");

            nextEvents = new List<EventSO>();

            //an example of special card marked inactive that always shows up after the reshuffle
            if (EventExists("last_card"))
            {
                EventSO e = GetInactiveEventByName("last_card");
                nextEvents.Add(e);
            }

            //reshuffle
            Shuffle(allEvents);

            //add all the active events
            foreach (EventSO e in allEvents)
            {
                if (e.active)
                {
                    //reset the discarded status
                    e.discarded = false;
                    nextEvents.Add(e);
                }
            }



            //check this limit case
            if (nextEvents.Count == 0)
            {
                print("There are no active events even after the reshuffle");
            }
            else
            {
                NextTurn();
            }
        }
        //if reaching the end of the deck reshuffle
    }


    public void CheckConditions()
    {
        //since things may have changed, check the conditions of each event
        foreach (EventSO evt in allEvents)
        {
            //checking discarded prevents cards with conditions to keep respawning
            if (!evt.discarded)
            {
                //if the event has a condition enable or disable it accordingly
                //and add it to the drawing deck
                if (evt.condition != "")
                {
                    //the condition is true
                    if (Evaluator.EvaluateBool(evt.condition, vars))
                    {
                        //the event is not in the draw deck -> add it                    
                        if (!nextEvents.Contains(evt))
                        {
                            nextEvents.Add(evt);
                            print("adding event " + evt.name + " to the draw deck");
                        }
                    }
                    else
                    {
                        //the condition is false and the event is in the draw deck -> remove it
                        if (nextEvents.Contains(evt))
                        {
                            nextEvents.Remove(evt);
                            print("removing event " + evt.name + " from the draw deck");
                        }
                    }
                }
            }
        }
    }

    //this function presents the event panel and it's strictly related to the structure of the UI prefab
    GameObject CreateEventPanel(EventSO e, Transform parent)
    {
        //create a new instance of the panel prefab
        GameObject newPanel = Instantiate(eventPanelPrefab, canvas.transform);

        //assign all the values from the current event
        newPanel.transform.Find("Title").GetComponent<TMP_Text>().text = e.title;
        newPanel.transform.Find("Description").GetComponent<TMP_Text>().text = e.description;

        if (e.illustration != null)
            newPanel.transform.Find("Illustration").GetComponent<Image>().sprite = e.illustration;

        RectTransform actionContainer = newPanel.transform.Find("Actions").GetComponent<RectTransform>();

        int activeActions = 0;

        //create as many buttons as the actions
        foreach (Action a in e.actions)
        {
            //create a new instance of the button inside the button container for proper placement
            GameObject actionUI = Instantiate(actionButtonPrefab, actionContainer);
            actionUI.transform.Find("ActionDescription").GetComponent<TMP_Text>().text = a.description;

            Button btn = actionUI.GetComponent<Button>();

            //the action can have a condition eg: you need x money to do that so 
            //I have to decide how to present this visually
            //In this case I'm showing a grayed out inactive button but I could also show no button at all
            if (a.condition == "" || (a.condition != "" && Evaluator.EvaluateBool(a.condition, vars)))
            {
                activeActions++;
                // Assign a click function with parameters using a lambda expression
                btn.onClick.AddListener(() => DoAction(a));
            }
            else
            {
                //disable and grey out the button
                btn.interactable = false;
                ColorBlock colors = btn.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f); // Adjust RGB values as needed
                btn.colors = colors;
            }
        }

        //if there are no action buttons or they are all disabled
        //I decided to create a default "continue button" to avoid a dead end
        if (activeActions == 0)
        {
            GameObject actionUI = Instantiate(actionButtonPrefab, actionContainer);
            actionUI.transform.Find("ActionDescription").GetComponent<TMP_Text>().text = CONTINUE_BUTTON_TEXT;

            Button btn = actionUI.GetComponent<Button>();
            btn.onClick.AddListener(() => Continue());
        }

        return newPanel;

    }

    //this is called from the button
    void DoAction(Action a)
    {
        if (inputActive)
        {
            print("Executing action " + a.description);

            //for each of the effects
            foreach (string e in a.effects)
            {
                //I use a global class that "evaluates" mathematical expressions from string
                //all the variables must be contained in vars otherwise it will create new ones
                if (e != "")
                    Evaluator.Evaluate(e, vars);
            }

            //for each of the special effects
            foreach (string e in a.specialEffects)
            {
                if (e != "")
                    SpecialEffect(e);
            }


            //mark the top event as discarded
            nextEvents[0].discarded = true;
            //remove it from the draw pile
            nextEvents.RemoveAt(0);

            NextTurn();
        }
    }

    //This is called from the automatically created button
    void Continue()
    {
        if (inputActive)
        {
            //mark the top event as discarded
            nextEvents[0].discarded = true;
            //remove it from the draw pile
            nextEvents.RemoveAt(0);

            NextTurn();
        }
    }

    void NextTurn()
    {
        StopCoroutine(turnCoroutine);
        turnCoroutine = Turn(); // Create a new instance of the coroutine
        StartCoroutine(turnCoroutine);
    }

    
    void SpecialEffect(string effect)
    {
        Debug.Log("Executing special effect: " + effect);

        // Regex pattern explanation:
        // ^(?<effectName>\w+)         : capture the effectName at the start (one or more word characters)
        // (?:\((?<args>.*)\))?      : optionally, capture everything between '(' and ')' as 'args'
        // $                        : end of the string.
        Regex regex = new Regex(@"^(?<effectName>\w+)(?:\((?<args>.*)\))?$", RegexOptions.IgnoreCase);
        Match match = regex.Match(effect);
        if (!match.Success)
        {
            Debug.LogWarning("Effect string not in expected format: " + effect);
            return;
        }

        string effectName = match.Groups["effectName"].Value;
        string argsContent = match.Groups["args"].Value;

        // Split the arguments by comma if any.
        string[] arguments = new string[0];
        if (!string.IsNullOrEmpty(argsContent))
        {
            arguments = argsContent.Split(new char[] { ',' });
            for (int i = 0; i < arguments.Length; i++)
            {
                arguments[i] = arguments[i].Trim();
            }
        }

        
        // Process the effect
        switch (effectName.ToLower())
        {
            case "destroy":
                {
                    // Destroy() - removes an event from the game entirely (single use event)
                    EventSO currentEvent = nextEvents[0];
                    if (allEvents.Contains(currentEvent))
                        allEvents.Remove(currentEvent);
                    break;
                }
            case "add":
                {
                    //Add(eventName) - adds an inactive event to the draw deck
                    if (arguments.Length >= 1)
                    {
                        EventSO e = GetInactiveEventByName(arguments[0]);
                        if (e != null)
                        {
                            int randomIndex = Random.Range(1, nextEvents.Count + 1); // +1 so you can insert at the end too.
                            nextEvents.Insert(randomIndex, e);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Add effect is missing an argument.");
                    }
                    break;
                }
            case "remove":
                {
                    // Remove(eventName) - removes an active event from the draw deck and sets
                    if (arguments.Length >= 1)
                    {
                        EventSO e = GetActiveEventByName(arguments[0]);
                        if (e != null)
                        {
                            nextEvents.Remove(e);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Remove effect is missing an argument.");
                    }
                    break;
                }

            case "activate":
                {
                    // Activate(eventName) - sets event active, eg to be added in the next reshuffle
                    if (arguments.Length >= 1)
                    {
                        EventSO e = GetInactiveEventByName(arguments[0]);
                        if (e != null)
                            e.active = true;
                    }
                    else
                    {
                        Debug.LogWarning("Activate effect is missing an argument.");
                    }
                    break;
                }

            case "deactivate":
                {
                    // Activate(eventName) - sets event inactive, eg to be removed in the next reshuffle (single use event)
                    if (arguments.Length >= 1)
                    {
                        EventSO e = GetActiveEventByName(arguments[0]);
                        if (e != null)
                            e.active = false;
                    }
                    else
                    {
                        Debug.LogWarning("Activate effect is missing an argument.");
                    }
                    break;
                }

            case "chain":
                {
                    // For 'chain', we expect one argument: the event name.
                    if (arguments.Length >= 1)
                    {
                        var es = GetEventsByName(arguments[0]);
                        if (es.Count > 0 && nextEvents.Count > 0)
                        {
                            nextEvents.Insert(1, es[0]);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Chain effect is missing an argument.");
                    }
                    break;
                }

            //RandomChain(event1, event2, 30) means 30% chance of chaining event1 after this one 70% of event2
            //both events have to be
            case "randomchain":
                {
                    if (arguments.Length == 3)
                    {
                        if (float.TryParse(arguments[2], out float percent))
                        {
                            Debug.Log("RandomChain: "+ percent + "% chance of chaining event " + arguments[0] +" otherwise chain " + arguments[1]);

                            string eventName = "";

                            if(Random.Range(0, 100) < percent)
                                eventName = arguments[0];
                            else
                                eventName = arguments[1];

                            var es = GetEventsByName(eventName);

                            if (es.Count > 0 && nextEvents.Count > 0)
                            {
                                nextEvents.Insert(1, es[0]);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("RandomChain: the 3 argument must be a number");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("RandomChain effect requires 3 arguments: Event1, Event2, percent");
                    }
                    break;
                }

            default:
                {
                    Debug.LogWarning("Unrecognized special effect: " + effectName);
                    break;
                }
        }
    }

    //check if an event with that name exists
    public bool EventExists(string eventName)
    {
        bool result = false;
        for (int i = 0; i < allEvents.Count; i++)
        {
            if (allEvents[i].name == eventName)
                result = true;
        }

        return result;
    }

    //get all the events with the name
    public List<EventSO> GetEventsByName(string eventName)
    {
        List<EventSO> results = new List<EventSO>();

        for (int i = 0; i < allEvents.Count; i++)
        {
            if (allEvents[i].name == eventName)
            {
                results.Add(allEvents[i]);
            }
        }

        if (results.Count == 0)
            Debug.Log("Warning: I can't find an event named " + eventName);

        return results;
    }

    //get the first inactive event with that
    public EventSO GetActiveEventByName(string eventName)
    {
        EventSO e = null;

        for (int i = 0; i < nextEvents.Count; i++)
        {
            if (allEvents[i].name == eventName && nextEvents[i].active)
                e = allEvents[i];
        }

        if (e == null)
            Debug.Log("Warning: I can't find an event named " + eventName);

        return e;
    }

    //get the first inactive event with that
    public EventSO GetInactiveEventByName(string eventName)
    {
        EventSO e = null;

        for (int i = 0; i < allEvents.Count; i++)
        {

            if (allEvents[i].name == eventName && allEvents[i].active == false)
                e = allEvents[i];
        }

        if (e == null)
            Debug.Log("Warning: I can't find an event named " + eventName);


        return e;
    }



    //shuffles a list regardless of its type
    public void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]); // Swap
        }
    }

}
