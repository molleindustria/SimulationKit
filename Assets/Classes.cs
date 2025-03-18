using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


/// <summary>
/// Variable is basically a string - float pair with some extra fancy
/// things to make it visible in the inspector and track the changes as they happen
/// </summary>

[Serializable]
public class Variable
{
    [Tooltip("Variable name, one word")]
    public string name;

    // Use a private backing field so we can control how changes are made.
    [Tooltip("Initial value")]
    [SerializeField] private float _value;
    
    [Tooltip("Maximum value (minimum is assumed 0)")]
    [SerializeField] private float max = 100;

    // Property to encapsulate the value. This is what you’d use in code.
    public float Value
    {
        get => _value;
        set
        {
            // Check if the value has really changed (using Mathf.Approximately for floats)
            if (!Mathf.Approximately(_value, value))
            {
                float oldValue = _value;  // Capture the old value
                _value = value;           // Update the value
                OnValueChanged(oldValue, _value);

            }
        }
    }

    [Tooltip("[Optional] Associate a visualizer script on a textfield or image")]
    public Visualizer visualizer;

    // Function that gets called whenever the value changes via code
    public void OnValueChanged(float oldValue, float newValue)
    {
        //cap
        if (max != -1)
            newValue = Mathf.Clamp(newValue, 0, max);

        // If there's an associated visualizer, update it.
        if (visualizer != null)
        {
            visualizer.UpdateVariable(oldValue, newValue, max);
        }
    }
}


[Serializable]
public class Action
{
    [Tooltip("The description of the action")]
    public string description;
    [Tooltip("[Optional] condition for the action to be available Eg: money >= 100")]
    public string condition;
    [Tooltip("An array of string expressions affecting global variables. Eg: money -= 100")]
    public string[] effects;
    [Tooltip("An array of non mathematical effects resolved through custom functions")]
    public string[] specialEffects;
}


