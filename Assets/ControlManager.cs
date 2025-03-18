using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ControlManager : MonoBehaviour
{
    //centralized class for the game logic

    [Tooltip("The global variables used by the expressions. + to create a new one.")]
    [SerializeField]
    public List<Variable> vars = new List<Variable>();

    public int TILE_SIZE = 1; // in unity units not pixels
    public GameObject[,] map; //x, y

    public UnitButton selectedUnitButton = null;
    //creates an instance of the unit for preview without putting it on the map 
    public GameObject unitPreview;

    [Tooltip("The where all the unit gameObjects will be instantiated (NOT UI)")]
    public GameObject mapContainer;

    [Tooltip("A field to display contextual information to the player")]
    public TMP_Text labelField;



    // Start is called before the first frame update
    void Start()
    {
        //create an empty container if not specified
        if(mapContainer == null)
            mapContainer = new GameObject("mapContainer");

        if (labelField != null)
            labelField.text = "";

        //set all the variables to trigger UI updates
        foreach (Variable v in vars)
            v.OnValueChanged(0, v.Value);
    }

    // Update is called once per frame
    void Update()
    {
        if(unitPreview != null)
        {
            //convert screen position to world position since it depends on the camera
            Vector2 pointerPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //move the preview
            unitPreview.transform.position = pointerPosition;
        }
    }



    public void Turn()
    {
        
    }


    public void OnControlPress(ControlButton btn)
    {
        print(btn.name + " has been pressed");

        //for each of the effects
        foreach (string e in btn.effects)
        {
            //I use a global class that "evaluates" mathematical expressions from string
            //all the variables must be contained in vars otherwise it will create new ones
            if (e != "")
                Evaluator.Evaluate(e, vars);
        }

        //for each of the special effects
        foreach (string e in btn.specialEffects)
        {
            if (e != "")
                SpecialEffect(e);
        }
    }


    void SpecialEffect(string effect)
    {
        print("Executing special effect " + effect);


        switch (effect)
        {
            case "example":
                print("This is a non arithmetical operation called from a button");
                break;
            default:
                Debug.LogWarning("Warning: I didn't find the special effect " + effect);
                break;
        }
    }


    public void OnControlRelease(ControlButton btn)
    {
    }

    //rollover action
    public void OnControlEnter(ControlButton btn)
    {
        if (btn.label != "")
            labelField.text = btn.label;
    }

    //rollout action
    public void OnControlExit(ControlButton btn)
    {
        labelField.text = "";
    }

    ///////////////////////////
    ///UNIT BUTTONS
    public void OnUnitButtonPress(UnitButton btn)
    {
        //if this is selected, pressing again deselects
        if (selectedUnitButton == btn)
        {
            selectedUnitButton.Deselect();
            selectedUnitButton = null;

            if(unitPreview != null)
                Destroy(unitPreview);
        }
        else
        {
            //if this is another deselect the previous one

            if (selectedUnitButton != null)
            {
                selectedUnitButton.Deselect();

                if (unitPreview != null)
                    Destroy(unitPreview);
            }

            selectedUnitButton = btn;
            selectedUnitButton.Select();

            if (btn.unit != null) {
                unitPreview = Instantiate(btn.unit);
            }
        }
        /*
        //for each of the effects
        foreach (string e in btn.effects)
        {
            //I use a global class that "evaluates" mathematical expressions from string
            //all the variables must be contained in vars otherwise it will create new ones
            if (e != "")
                Evaluator.Evaluate(e, vars);
        }

        //for each of the special effects
        foreach (string e in btn.specialEffects)
        {
            if (e != "")
                SpecialEffect(e);
        }*/
    }

    //the button deselected itself due to conditions 
    public void DeselectUnitButton(UnitButton btn) {
        
        if(selectedUnitButton == btn)
            selectedUnitButton = null;

    }

    public void OnUnitButtonRelease(UnitButton btn)
    {
    }

    //rollover action
    public void OnUnitButtonEnter(UnitButton btn)
    {
        if (btn.label != "")
            labelField.text = btn.label;
    }

    //rollout action
    public void OnUnitButtonExit(UnitButton btn)
    {
        labelField.text = "";
    }


}
