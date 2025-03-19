using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ControlManager : MonoBehaviour
{
    //centralized class for the game logic all the "buttons" report to this script

    [Tooltip("The global variables used by the expressions. + to create a new one.")]
    [SerializeField]
    public List<Variable> vars = new List<Variable>();

    public float TILE_SIZE = 1f; // in unity units not pixels
    public int MAP_COLS = 1000;
    public int MAP_ROWS = 1000;
    public GameObject[,] map; //x, y

    public UnitButton selectedUnitButton = null;
    //creates an instance of the unit for preview without putting it on the map 
    public GameObject unitPreview;

    [Tooltip("The where all the unit gameObjects will be instantiated (NOT UI)")]
    public GameObject mapContainer;

    [Tooltip("A field to display contextual information to the player")]
    public TMP_Text labelField;

    [Tooltip("The preview Unit floats above the others")]
    public float previewUnitZ = -1; //unfortunately negative is close to the camera

    [Tooltip("The turn duration in seconds")]
    public float TURN_DURATION = 2;
    public int turns = 0;
    public float turnCounter = 0;
    public bool onUI = false;


    // Start is called before the first frame update
    void Start()
    {
        //create a map 2D array, it can't be resized later
        map = new GameObject[MAP_COLS, MAP_ROWS];

        //create an empty container at 0,0 if not specified
        if (mapContainer == null)
        {
            mapContainer = new GameObject("mapContainer");

            //position it on the top left position in the camera
            float zDistance = Mathf.Abs(Camera.main.transform.position.z - 0f);
            // In viewport coordinates, (0, 1) is the top left.
            Vector3 topLeftWorld = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, zDistance));
            // Set the mapContainer position to this top-left point.
            mapContainer.transform.position = SnapToTile(topLeftWorld);
        }

        if (labelField != null)
            labelField.text = "";

        //set all the variables to trigger UI updates
        foreach (Variable v in vars)
            v.OnValueChanged(0, v.Value);
    }

    //the main game loop can be put here 
    void Turn()
    {
        ////call "OnTurn" on every script of every gameObject in the map
        for (int col = 0; col < map.GetLength(0); col++)
        {
            for (int row = 0; row < map.GetLength(1); row++)
            {
                GameObject currentUnit = map[col, row];

                if (currentUnit != null) {
                    currentUnit.SendMessage("OnTurn", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        turnCounter -= Time.deltaTime;
        if(turnCounter < 0)
        {
            turnCounter = TURN_DURATION;
            Turn();
        }

        //if pointer is on ANY UI elements
        onUI = EventSystem.current.IsPointerOverGameObject();

        //dragging the unit around and calculating its position in the map
        if (unitPreview != null)
        {
            if (onUI)
            {
                //hide it entirely
                unitPreview.SetActive(false);
            }
            else
            {
                unitPreview.SetActive(true);

                //convert screen position to world position since it depends on the camera
                Vector2 pointerPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                //snap to closest tile
                pointerPosition = SnapToTile(pointerPosition);

                //move the preview to the closest tiles adjust the position to place in the center of the 
                unitPreview.transform.position = new Vector3(pointerPosition.x, pointerPosition.y, previewUnitZ) + new Vector3(TILE_SIZE / 2, -TILE_SIZE / 2, 0);

                // Convert the world position to the map container's local space
                Vector2 localPosition = mapContainer.transform.InverseTransformPoint(pointerPosition);

                // Calculate the column 
                int col = Mathf.FloorToInt(localPosition.x / TILE_SIZE);

                // Flip the row calculation by inverting the local y-coordinate.
                // When localPosition.y is 0 (top), row becomes 0; as the pointer moves down (localPosition.y becomes negative)
                int row = Mathf.FloorToInt(-localPosition.y / TILE_SIZE);


                //see if the position is valid (on the map and not taken)
                if (ValidatePosition(col, row))
                {
                    //send a message to all the scripts on the unit, they will take care of the visualization
                    unitPreview.SendMessage("ValidPosition", SendMessageOptions.DontRequireReceiver);

                    //left click on a valid position while carrying a unit "builds" the unit
                    if (Input.GetMouseButtonDown(0))
                    {
                        AddUnit(unitPreview, col, row);
                    }
                }
                else
                {
                    //send a message to all the scripts on the unit, they will take care of the visualization
                    unitPreview.SendMessage("InvalidPosition", SendMessageOptions.DontRequireReceiver);
                }

               
            }
        }

        //right click to deselect
        if (selectedUnitButton != null && Input.GetMouseButtonDown(1))
        {
            DeselectUnitButton(selectedUnitButton);
        }

    }


    //returns the newly created unit
    public GameObject AddUnit(GameObject instance, int col, int row)
    {
        //validate position again just in case
        if (ValidatePosition(col, row))
        {
            print("Adding unit " + instance.name);

            Vector3 newPosition = MapToPosition(col, row, 0);

            //in this case I assume the preview unit and the final unit are the same gameObject
            GameObject newUnit = Instantiate(instance, mapContainer.transform);
            newUnit.transform.localPosition = newPosition;

            //add it to the map
            map[col, row] = newUnit;

            //the effects on the button are called now (not upon press)
            foreach (string e in selectedUnitButton.effects)
            {
                //I use a global class that "evaluates" mathematical expressions from string
                //all the variables must be contained in vars otherwise it will create new ones
                if (e != "")
                    Evaluator.Evaluate(e, vars);
            }

            //for each of the special effects
            foreach (string e in selectedUnitButton.specialEffects)
            {
                if (e != "")
                    SpecialEffect(e);
            }

            return newUnit;
        }
        else
        {
            return null;
        }

    }

    public void DestroyUnit(GameObject instance)
    {
        bool found = false;
        for (int col = 0; col < map.GetLength(0); col++)
        {
            for (int row = 0; row < map.GetLength(1); row++)
            {
                GameObject currentUnit = map[col, row];

                if (currentUnit == instance)
                {
                    print("Destroying unit "+instance.name);
                    //empty the slot
                    map[col, row] = null;
                    found = true;
                }
            }
        }

        //destroy the actual game object
        if(found)
            Destroy(instance);
    }

    //example of utility function gets all the game object within a certain range (manhattan distance)
    public List<GameObject> GetObjectsInRange(GameObject centerObject, int manhattanDistance)
    {
        Vector2 cell = GetColRow(centerObject);
        print(cell);
        if (cell.x == -1)
            return null;

        return GetObjectsInRange((int)cell.x, (int)cell.y, manhattanDistance);
    }

    //overload specifying columns
    public List<GameObject> GetObjectsInRange(int centerCol, int centerRow, int manhattanDistance)
    {
        List<GameObject> objectsAtDistance = new List<GameObject>();
        int cols = map.GetLength(0);
        int rows = map.GetLength(1);

        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                // Calculate the Manhattan distance from the center position.
                int distance = Mathf.Abs(col - centerCol) + Mathf.Abs(row - centerRow);
                if (distance <= manhattanDistance)
                {
                    // Optionally check if the GameObject is not null.
                    if (map[col, row] != null)
                    {
                        objectsAtDistance.Add(map[col, row]);
                    }
                }
            }
        }

        return objectsAtDistance;
    }

    //from an object on the map get the row and column -1, -1 if not found
    public Vector2 GetColRow(GameObject instance)
    {
        Vector2 cell = new Vector2(-1, -1);

        for (int col = 0; col < map.GetLength(0); col++)
        {
            for (int row = 0; row < map.GetLength(1); row++)
            {
                if (map[col, row] == instance)
                {
                    cell = new Vector2(col, row);
                }
            }
        }

        return cell;
    }

    public Vector3 MapToPosition(int col, int row, float z = 0)
    {
        return new Vector3(col * TILE_SIZE + TILE_SIZE / 2f, -row * TILE_SIZE - TILE_SIZE / 2f, z);
    }

    //snap to the closest tile keeping z the same
    public Vector3 SnapToTile(Vector3 point)
    {
        // Snap x and y coordinates to the nearest multiple of TILE_SIZE
        point.x = Mathf.Floor(point.x / TILE_SIZE) * TILE_SIZE;
        //CEILING because of the flipped coordinate
        point.y = Mathf.Ceil(point.y / TILE_SIZE) * TILE_SIZE;
        return point;
    }

    //make sure tile is on the map
    public bool TileOnTheMap(int col, int row)
    {
        if (col < 0 || col > map.GetLength(0) - 1 || row < 0 || row > map.GetLength(1) - 1)
            return false;
        else
            return true;
    }

    //right now the validation is just: make sure there isn't another unit in that cell
    public bool ValidatePosition(int col, int row)
    {
        bool valid = true;
        if (!TileOnTheMap(col, row))
        {
            valid = false;
        }
        else if (map[col, row] != null)
        {
            //check if it's occupied
            valid = false;
        }

        return valid;
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
            DeselectUnitButton(selectedUnitButton);
        }
        else
        {
            //if this is another deselect the previous one

            if (selectedUnitButton != null)
            {
                DeselectUnitButton(selectedUnitButton);
            }

            selectedUnitButton = btn;
            selectedUnitButton.Select();

            if (btn.unit != null)
            {
                unitPreview = Instantiate(btn.unit);
            }
        }

    }

    //the button deselected itself due to conditions 
    public void DeselectUnitButton(UnitButton btn)
    {

        btn.Deselect();
        selectedUnitButton = null;

        if (unitPreview != null)
            Destroy(unitPreview);
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
