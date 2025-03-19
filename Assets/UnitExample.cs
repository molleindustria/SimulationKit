using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitExample : MonoBehaviour
{
    public ControlManager manager;

    //the graphics is assumed to be just a sprite
    public SpriteRenderer spriteRenderer;

    public Color normalTint = Color.white;
    public Color errorTint = Color.red;

    public int life = 5;

    void Awake()
    {
        //find daddy
        manager = FindObjectOfType<ControlManager>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        normalTint = spriteRenderer.color;
    }

    //this is called by the manager every turn on every unit via SendMessage
    public void OnTurn()
    {
        //example of function using the evaluator class
        Evaluator.Evaluate("money+=50", manager.vars);

        //get all the units within a manhattan distance
        List<GameObject> nearUnits = manager.GetObjectsInRange(gameObject, 3);

        //sutracting one because the function doesn't exclude the central object
        print(gameObject.name + " found "+ (nearUnits.Count-1)+" units nearby");

        //example of mixing and matching evaluated function and normal code
        Evaluator.Evaluate("research+=0.1*"+(nearUnits.Count-1), manager.vars);

        //You can use local variables as well
        life--;

        //to destroy a unit use the manager function that takes care of the map reference as well
        if (life <= 0)
            manager.DestroyUnit(gameObject);

    }

    //this is called by the manager
    public void ValidPosition()
    {
        spriteRenderer.color = normalTint;
    }

    public void InvalidPosition()
    {
        spriteRenderer.color = errorTint;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
