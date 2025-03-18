using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//this script is meant to be on a UI prefab and moves all the game logic on ControlManager
//it has handlers for common pointer events declared here which activate the functions OnPointerDown etc
public class ControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Description")]
    public string label;
    [Tooltip("[Optional] Condition for it being clickable. Eg: a >= b AND NOT(b) OR (a + b + 3) >= c")]
    public string condition;
    [Tooltip("[Optional] Cool off time, after the click it's unclickable for x seconds")]
    public float coolOff = 0;
    [Tooltip("If not interactable it will not be in the menu at the beginning")]
    public bool interactable = true;
    [Tooltip("An array of string expressions affecting global variables. Eg: money -= 100")]
    public string[] effects;
    [Tooltip("An array of non mathematical effects resolved through custom functions")]
    public string[] specialEffects;

    //this will be assigned automatically
    private ControlManager manager;

    [Space(10)]
    [Tooltip("The background behind the image")]
    public Image background;

    [Tooltip("Where the icon/illustration is going to appear")]
    public Image image;

    [Tooltip("The overlay on the top of the image")]
    public Image overlay;

    //set automatically based on the initial bg color
    private Color backgroundNormal = Color.gray;
    [Tooltip("BG tint when mouse rolls over")]
    public Color backgroundOver = Color.white;
    [Tooltip("BG tint when mouse is pressed")]
    public Color backgroundPressed = Color.white;
    [Tooltip("BG tint during the cool off")]
    public Color backgroundNonInteractable = Color.white;

    //set automatically based on the initial image color
    private Color interactableTint = Color.gray;
    [Tooltip("Image tint during the cool off")]
    public Color nonInteractableTint = Color.gray;

    public float coolOffTimer = 0;

    public void Awake()
    {
        manager = FindObjectOfType<ControlManager>();

        if (manager == null)
            Debug.LogWarning("Warning: the button " + gameObject.name + " can't find a control manager. It's meant to work with it");

        backgroundNormal = background.color;

        interactableTint = image.color;

        SetInteractable(interactable);
    }

    // Called when the pointer is pressed down.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactable && coolOffTimer <= 0)
        {
            manager.OnControlPress(this);

            coolOffTimer = coolOff;

            background.color = backgroundPressed;
        }
    }

    public void Update()
    {
        //it's not very efficient to do it every frame but whatever 
        if (condition != "")
        {
            //the condition is true
            if (Evaluator.EvaluateBool(condition, manager.vars))
            {
                SetInteractable(true);
            }
            else
            {
                SetInteractable(false);
            }
        }

        //if there is a cool off value and it's greater than 0
        //visualize the time as an animated fill
        if (interactable && coolOffTimer > 0)
        {
            background.color = backgroundNormal;

            coolOffTimer -= Time.deltaTime;
            if (coolOffTimer < 0)
                coolOffTimer = 0;

            float fillAmount = Map(coolOffTimer, 0, coolOff, 0, 1);

            overlay.fillAmount = fillAmount;
        }
        else
        {
            overlay.fillAmount = 0;
        }
    }

    public void SetInteractable(bool newState)
    {
        //just changed
        if (newState != interactable)
        {
            //visualizing non interactable as greyed out image
            if (newState == false)
            {
                image.color = nonInteractableTint;
                background.color = backgroundNonInteractable;
            }
            else
            {
                image.color = Color.white;
                background.color = backgroundNormal;
            }
        }

        interactable = newState;

    }



    // Called when the pointer is released.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (interactable && coolOffTimer <= 0)
        {
            manager.OnControlRelease(this);
            background.color = backgroundOver;
        }
    }

    // Called when the pointer enters the element.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (interactable && coolOffTimer <= 0)
        {
            background.color = backgroundOver;
        }

        manager.OnControlEnter(this);
    }

    // Called when the pointer exits the element.
    public void OnPointerExit(PointerEventData eventData)
    {
        if (interactable && coolOffTimer <= 0)
        {
            background.color = backgroundNormal;
        }

        manager.OnControlExit(this);
    }


    public static float Map(float value, float fromLow, float fromHigh, float toLow, float toHigh, bool clamped = true)
    {
        float mapped = toLow + (toHigh - toLow) * ((value - fromLow) / (fromHigh - fromLow));

        if (clamped)
            mapped = Mathf.Clamp(mapped, Mathf.Min(toLow, toHigh), Mathf.Max(toLow, toHigh));

        return mapped;
    }
}
