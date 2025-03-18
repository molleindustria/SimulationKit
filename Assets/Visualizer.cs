using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Tweens;

/// <summary>
/// This is a utility class to add effects to the UI
/// Attach it to a TMPro textfield or an Image
/// Then assign the Visualizer object to your Variable in the inspector
/// when the variable changes it should update the image or the field
/// The animations (Tweens) are handled by
/// https://github.com/jeffreylanters/unity-tweens
/// </summary>

public class Visualizer : MonoBehaviour
{
    [Tooltip("Duration of the tween in second")]
    public float duration = 0.5f;
    [Tooltip("Easing type")]
    public EaseType easing = EaseType.SineInOut;
    [Space(10)]
    [Tooltip("Increase or decrease the text gradually (only numbers)")]
    public bool animateText = true;
    [Space(10)]
    [Tooltip("Animate the fillAmount of an Image")]
    public bool animateFill = true;
    [Space(10)]
    [Tooltip("Change the color for the duration of the tween")]
    public bool animateColor = true;
    [Tooltip("Color when the value increases")]
    public Color positiveColor = Color.green;
    [Tooltip("Color when the value decreases")]
    public Color negativeColor = Color.red;
    [Space(10)]
    [Tooltip("Round the displayed number to a set number of decimals after the point -1 for no rounding")]
    public int decimals = -1;
    [Space(10)]
    [Tooltip("automatically set to the image on the same object")]
    public Image image;
    [Tooltip("automatically set to the field on the same object")]
    public TMP_Text field;

    private Color initialTextColor;
    private Color initialImageColor;

    public void Awake()
    {
        if(image == null)
            image = GetComponent<Image>();
        
        if(field == null)
            field = GetComponent<TMP_Text>();

        if (image == null && field == null)
            Debug.LogWarning("Warning: Visualizer is meant to be attached/associated to a UI image or a TMPro textfield");

        if (image != null)
            initialImageColor = image.color;

        if (field != null)
            initialTextColor = field.color;

    }

    public void UpdateVariable(float oldValue, float newValue, float maxValue)
    {
        //print("Visualizer changed from " + oldValue + " to " + newValue);

        //round if necessary
        if (decimals >= 0)
        {
            newValue = RoundToDecimals(newValue, decimals);
        }

        if (image != null)
        {
            if (animateFill)
            {
                AnimateFill(image, newValue, maxValue);
            }
            else
            {
                //assign fill without tweens
                float fill = Map(newValue, 0, maxValue, 0, 1);
                image.fillAmount = fill;
            }

            if (animateColor)
            {
                //change color based on the 
                Color colorHighlight = (newValue > oldValue) ? positiveColor : negativeColor;
                AnimateImageColor(image, colorHighlight);
            }
        }

        if (field != null)
        {
            if (animateText)
            {
                AnimateNumber(field, newValue);
            }
            else
            {
                //assign it without tweens
                field.text = newValue.ToString();
            }

            if (animateColor)
            {
                //change color based on the increase or decrease
                Color colorHighlight = (newValue > oldValue) ? positiveColor : negativeColor;
                AnimateTextColor(field, colorHighlight);
            }

        }

        

    }

    public void AnimateNumber(TMP_Text field, float value)
    {
        float oldValue = float.Parse(field.text);

        if (oldValue != value)
        {
            //Tween a NUMBER!
            var tween = new FloatTween
            {
                from = oldValue,
                to = value,
                duration = duration,
                easeType = easing,
                onUpdate = (instance, tweenedValue) =>
                {
                    /*a bit convoluted but basically every time the tween
                     * updates get the instance on which it's added get the text field 
                     * and set the tweening value to it but round it first so I don't get long decimals
                     */
                    instance.target.gameObject.GetComponent<TMP_Text>().text = Mathf.Floor(tweenedValue).ToString();
                },

            };

            field.gameObject.AddTween(tween);
        }
    }

    public void AnimateTextColor(TMP_Text field, Color targetColor)
    {
        
        //Tween a NUMBER!
        var tween = new ColorTween
        {
            from = targetColor,
            to = initialTextColor,
            duration = duration,
            easeType = easing,
            onUpdate = (instance, tweenedColor) => {
                instance.target.gameObject.GetComponent<TMP_Text>().color = tweenedColor;
            },

        };

        field.gameObject.AddTween(tween);

    }

    public void AnimateImageColor(Image image, Color targetColor)
    {
        
        //Tween a NUMBER!
        var tween = new ColorTween
        {
            from = targetColor,
            to = initialImageColor,
            duration = duration,
            easeType = easing,
            onUpdate = (instance, tweenedColor) => {
                instance.target.gameObject.GetComponent<Image>().color = tweenedColor;
            },

        };

        image.gameObject.AddTween(tween);

    }

    public void AnimateFill(Image image, float value, float maxValue, float minValue = 0)
    {
       float fill = Map(value, minValue, maxValue, 0, 1);
       
        if (fill != image.fillAmount)
        {
            var tween = new ImageFillAmountTween
            {
                to = fill,
                duration = 0.5f,
                easeType = easing,
            };
            image.gameObject.AddTween(tween);
        }

        //return true if the value changed
    }

    public Color ColorFromHex(string hexColor)
    {
        Color newColor;

        if (ColorUtility.TryParseHtmlString(hexColor, out newColor))
        {
            return newColor;
        }
        else
        {
            Debug.LogError("Invalid hex color string.");
            return Color.black;
        }

    }

    public static float RoundToDecimals(float value, int decimals)
    {
        // If decimals is negative, return the original value without rounding.
        if (decimals < 0)
            return value;

        // Calculate the multiplier based on the number of decimals.
        float factor = Mathf.Pow(10f, decimals);

        // Multiply the value, round it, then divide to get the rounded number.
        return Mathf.Round(value * factor) / factor;
    }

    public static float Map(float value, float fromLow, float fromHigh, float toLow, float toHigh, bool clamped = true)
    {
        float mapped = toLow + (toHigh - toLow) * ((value - fromLow) / (fromHigh - fromLow));
        
        if(clamped)
            mapped = Mathf.Clamp(mapped, Mathf.Min(toLow, toHigh), Mathf.Max(toLow, toHigh));

        return mapped;
    }
}

