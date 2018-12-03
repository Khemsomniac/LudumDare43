﻿using System.Collections;
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
        UpdateCursorHoverCell();
        UpdateBridgePreview();
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
    /// Show a preview of the bridge when the player is hovering the cursor above a river while some villagers are selected
    /// </summary>
    public void ShowBridgePreview()
    {
        // Also show a number of how many villagers will be used to form the bridge

        // Store the number of villagers that's being selected
        int selectedVillagerCount = selectedVillagers;

        int villagerNeedForBridgeCount = 0; // The number of villagers that need to sacrifice to make the bridge

        MapMaker.Cell startingCell = mouseHoveringCell;

        // Flag the water cells for bridge preview
        while (selectedVillagerCount > 0)
        {
            int thisColumnBridgeWidth = 0;

            // If the tile under
            if (startingCell.tileType == 1)
            {
                thisColumnBridgeWidth = MakeBridgeColumn(startingCell.iCoord, startingCell.jCoord);

                // Move starting cell to the right
                startingCell = mapGrid.gridArray[startingCell.iCoord, startingCell.jCoord + 1];
            }
            else if (mapGrid.gridArray[startingCell.iCoord + 1, startingCell.jCoord].tileType == 1)
            {
                thisColumnBridgeWidth = MakeBridgeColumn(startingCell.iCoord + 1, startingCell.jCoord);

                // Move starting cell to the right
                startingCell = mapGrid.gridArray[startingCell.iCoord + 1, startingCell.jCoord + 1];
            }
            else
            {
                thisColumnBridgeWidth = MakeBridgeColumn(startingCell.iCoord - 1, startingCell.jCoord);

                // Move starting cell to the right
                startingCell = mapGrid.gridArray[startingCell.iCoord - 1, startingCell.jCoord + 1];
            }

            villagerNeedForBridgeCount += thisColumnBridgeWidth; // Increase required villager count
            selectedVillagerCount -= thisColumnBridgeWidth; // Decrease remained villager count

            print("cell coord: " + startingCell.iCoord + ", " + startingCell.jCoord);
        }
    }

    /// <summary>
    /// Update bridge preview
    /// </summary>
    public void UpdateBridgePreview()
    {
        foreach (MapMaker.Cell c in mapGrid.gridArray)
        {
            if (c.showBridgePreview && c.bridgePreview == null)
            {
                c.bridgePreview = Instantiate(mapGrid.GroundTile, new Vector3(0.5f + c.jCoord, 0.01f, 0.5f + c.iCoord + mapGrid.offset), Quaternion.Euler(new Vector3(90, 0, 0)));
                c.bridgePreview.GetComponent<MeshRenderer>().material.color = Color.red;
            }
            else if (!c.showBridgePreview && c.bridgePreview != null)
            {
                Destroy(c.bridgePreview);
                c.bridgePreview = null;
            }
        }
    }

    /// <summary>
    /// Get the width (along the z axis) of a river column, then fill the column with bridge preview
    /// </summary>
    /// <param name="iCoord"></param>
    /// <param name="jCoord"></param>
    /// <returns></returns>
    public int MakeBridgeColumn(int iCoord, int jCoord)
    {
        ClearBridgePreviewFlag();

        int columnWidth = 1;
        int startZ = iCoord; // Get the starting i coord

        int whileStopper = 0;
        // Find top border
        while (!mapGrid.gridArray[startZ, jCoord].border && whileStopper < 1000)
        {
            startZ++;
            columnWidth++;
            mapGrid.gridArray[startZ, jCoord].showBridgePreview = true;

            whileStopper++;
        }

        startZ = iCoord;
        // Find bottom border
        while (!mapGrid.gridArray[startZ, jCoord].border && whileStopper < 1000)
        {
            startZ--;
            columnWidth++;
            mapGrid.gridArray[startZ, jCoord].showBridgePreview = true;

            whileStopper++;
        }

        return columnWidth;
    }

    /// <summary>
    /// Clear the bridge preview flag on the cells
    /// </summary>
    public void ClearBridgePreviewFlag()
    {
        foreach (MapMaker.Cell c in mapGrid.gridArray)
        {
            c.showBridgePreview = false;
        }
    }

    /// <summary>
    /// Update the cell that the cursor is currently hover above
    /// </summary>
    public void UpdateCursorHoverCell()
    {
        mouseHoveringCell = mapGrid.gridArray[Mathf.FloorToInt(Input.mousePosition.y / Screen.height / normalizedCellOnScreenHeight), Mathf.FloorToInt(Input.mousePosition.x / Screen.width / normalizedCellOnScreenWidth)];
        //print("cell type: " + mouseHoveringCell.tileType);

        // Show the bridge preview if the player is hovering cursor above river while selected some villagers
        if (mouseHoveringCell.tileType == 1 && selectedVillagers > 0)
        {
            ShowBridgePreview();
        }
        else
        {
            ClearBridgePreviewFlag();
        }
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
