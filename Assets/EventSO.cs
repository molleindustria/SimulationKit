using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Event", menuName = "New Event", order = 1)]
public class EventSO : ScriptableObject
{
    [Tooltip("Title")]
    public string title;
    [Tooltip("Description")]
    [TextArea]
    public string description;
    [Tooltip("[Optional] Condition for its presence in the deck as string expression using global variables. Eg: a >= b AND NOT(b) OR (a + b + 3) >= c")]
    public string condition;
    [Tooltip("Number of cards in the deck / probability")]
    public int number = 1;
    [Tooltip("If not active it will not be in the deck")]
    public bool active = true;
    [Tooltip("This can be used to keep track of its use")]
    public bool discarded = false;
    [Tooltip("Event illustration")]
    public Sprite illustration;
    [Tooltip("Actions the player can take when the event happens")]
    public Action[] actions;
}
