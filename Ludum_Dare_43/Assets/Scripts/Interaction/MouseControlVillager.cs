using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contains different mouse instructions for villager unit control
/// </summary>
public class MouseControlVillager : MonoBehaviour
{
    public float normalizedCellOnScreenWidth; // The normalized width of how much one cell occupies the game screen
    public float normalizedCellOnScreenHeight; // The normalized height of how much one cell occupies the game screen
    public float minMouseDragDistanceToShowSelection; // The minimum distance the player has to drag the mouse to show the selection rectangle
    public GameObject mouseDragAreaSprite; // The UI sprite shows the area the player is dragging over

    public MapMaker.Cell mouseHoveringCell; // The cell that is currently under the player's mouse cursor
    public MapMaker mapGrid; // The game's map data
    public Vector3 mouseDownScreenPosition; // The screen position when the player pressed down LMB
    public bool isMouseDownOnEmptyCell; // Is the LMB pressed down on cells beside log/flood/house
    public bool hasMouseDragged; // Did the player dragged the mouse cursor since last pressed down LMB
    public int selectedVillagers; // How many villagers are selected

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // If the player pressed down LMB
        if (Input.GetButtonDown("Fire1"))
        {
            LMBdown();
        }

        // If the player released LMB
        if (Input.GetButtonUp("Fire1"))
        {
            LMBup();
        }

        ShowMouseSelectionArea();
    }

    /// <summary>
    /// When the player press down LMB
    /// </summary>
    public void LMBdown()
    {
        // if (pressed on house)
        // send a villager to the house

        // if (pressed on log/flood)
        // send a villager to the log
        if (false)
        {

        }

        else
        {
            isMouseDownOnEmptyCell = true;
            // Record the position where the player pressed down LMB
            mouseDownScreenPosition = Input.mousePosition;
        }
    }

    /// <summary>
    /// When the player released LMB
    /// </summary>
    public void LMBup()
    {
        // If the mouse start press on empty cell
        if (isMouseDownOnEmptyCell)
        {
            // If the player just clicked and released mouse without drag
            if (!hasMouseDragged)
            {
                // Move the main group to that position
            }

            hasMouseDragged = false;
        }

        isMouseDownOnEmptyCell = false;
    }

    public void ShowMouseSelectionArea()
    {
        // If the player pressed down LMB on a cell that's not log/flood/house and start to drag the mouse while pressing down LMB
        if (isMouseDownOnEmptyCell &&
            Vector3.Distance(mouseDownScreenPosition, Input.mousePosition) > minMouseDragDistanceToShowSelection)
        {
            // Show the mouse drag area UI
            if (!mouseDragAreaSprite.activeInHierarchy)
            {
                mouseDragAreaSprite.SetActive(true);
                hasMouseDragged = true;
            }

            // Update the selection rectangle's transform
            mouseDragAreaSprite.GetComponent<RectTransform>().position = new Vector3((mouseDownScreenPosition.x + Input.mousePosition.x) / 2f, (mouseDownScreenPosition.y + Input.mousePosition.y) / 2f, 0);
            mouseDragAreaSprite.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Abs(mouseDownScreenPosition.x - Input.mousePosition.x), Mathf.Abs(mouseDownScreenPosition.y - Input.mousePosition.y));
        }
        else
        {
            // Hide the mouse drag area UI
            if (mouseDragAreaSprite.activeInHierarchy)
            {
                mouseDragAreaSprite.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Update the cell that the cursor is currently hover above
    /// </summary>
    public void UpdateCursorHoverCell()
    {
        mouseHoveringCell = mapGrid.gridArray[Mathf.FloorToInt(Input.mousePosition.y / Screen.height / normalizedCellOnScreenHeight), Mathf.FloorToInt(Input.mousePosition.x / Screen.width / normalizedCellOnScreenWidth)];
    }

    /// <summary>
    /// Stores information about a group of villagers
    /// </summary>
    public class VillagerGroup
    {


        public List<Villager> villagers; // The villagers in this group
        public int groupFrontRowCellIndex; // The cell index of the first row of the group

        /// <summary>
        /// Updates the group's shape
        /// </summary>
        public void UpdateGroupShape()
        {

        }

        /// <summary>
        /// Move the group around
        /// </summary>
        public void MoveGroup()
        {

        }
    }
}
