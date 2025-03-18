using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Tweens;

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

    //some examples of special effects 
    //determining by parsing a string
    void SpecialEffect(string effect)
    {
        print("Executing special effect " + effect);

        //try to parse command:argument syntax
        string[] eff = effect.Split(":");
        string command = "";
        string argument = "";

        if (eff.Length == 2)
        {
            command = eff[0].Trim();
            argument = eff[1].Trim();
            print("Splitting as command " + command + " argument " + argument);
        }
        else
        {   
            //if no ":" consider it a single word command
            command = effect.Trim();
        }

        //I'm just reusing these variables below
        EventSO e;
        

        switch (command)
        {
            //destroy (the current event happens once in the game)
            //removes the current card from the game
            case "destroy":
                EventSO currentEvent = nextEvents[0];
                if (allEvents.Contains(currentEvent))
                    allEvents.Remove(currentEvent);
                break;

            //add:event
            //finds the first inactive instance of a card by name and activates it + adds it to the deck
            case "add":
                e = GetInactiveEventByName(argument);
                if (e != null)
                {
                    e.active = true;
                    nextEvents.Add(e);
                }
                break;

            //add:event
            //finds the first event by name in the draw deck removes and deactivates it
            case "remove":
                e = GetActiveEventByName(argument);
                if (e != null)
                {
                    e.active = false;
                    nextEvents.Remove(e);
                }
                break;

            

            //chain:event
            //finds the first event by name and draw it immediately after the current one
            //can be used to trigger end states for example
            case "chain":

                //this function can be used to find multiple instances
                List<EventSO> es = GetEventsByName(argument);
                if (es.Count > 0 && nextEvents.Count>0)
                {
                    //insert in the the next spot
                    nextEvents.Insert(1, es[0]);
                }
                break;

            default:
                Debug.LogWarning("Warning I didn't recognize the special effect " + effect);
                break;
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
            Debug.Log("Warning: I can't find an event named "+eventName);

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
