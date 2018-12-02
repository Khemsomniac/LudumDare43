using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Cryptography;

public class MapMaker : MonoBehaviour
{

    public GameObject tileObject;           //gameobject variable to help instantiate the tile
    List<GameObject> TileQueue;

    public class Cell                       //A single cell in the 2D array defining the grid of the map
    {
        public int tileType;                //0=Ground, 1=Water, 2=Log, 3=Bridge, 4=House
        public bool border = false;         //if true, it means the terrain will change from land to water starting this cell, and the terrain will change from water to land from the next cell.

        public Cell(int tile, bool bord)         //constructor
        {
            tileType = tile;
            border = bord;
        }
    }

    public Cell[,] gridArray;               //This stores the interactive values of the tiles which are currently in play
    public int offset = 3;                      //This is the z-axis value added to the position of the new instantiated tiles

    public int gridLength;                  //Length of the active grid or the grid array
    public int gridBreadth;                 //Breadth of the active grid or the grid array

    public int maxLandLength;               //maximum length that one stretch of land can have
    public int minLandLength;               //minimum length that one stretch of land can have
    public int maxRiverWidth;               //maximum width that one river can have
    public int minRiverWidth;               //minimum width that one river can have

    //-----------------The algorithm will FAIL if minRiverWidth is less than or equal to 3 !!!!!!!!--------------------------

    public GameObject GroundTile;           //Tile prefabs to be specified by the designer
    public GameObject WaterTile;
    public GameObject LogTile;
    public GameObject BridgeTile;
    public GameObject HouseTile;

    private int i, j, k;                    //loop increment variables
    private int riverOffset;                //temporary variable for calculating random numbers for river boundaries
    private float randomHelper;             //temporary random variable for calculation purposes
    private int tempi, tempj;               //temporary coordinate variables for calculation purposes
    private bool withinTheLimits;           //temporary variable for helping run the while loops for checking if a certain value is under limits
    private int tempCount;                  //temporary variable for keeping a count for verification of certain conditions
    private bool tempFound;                 //temporary variable for helping run the while loops for checking if a certain value is found or not

    public struct Rivermaker
    {
        public int gridi;                   //These store the location in the grid from where the rivermaking needs to start
        public int gridj;
        public int mode;
        //0 = Rivermaker is on the left end and is gonna go up the stretch of land
        //1 = Rivermaker is going to the right, making the bottom border of the river
        //2 = Rivermaker is on the right end and is gonna go up the width of the river
        //3 = Rivermaker is going to the left, making the top border of the river
        //4 = Rivermaker is on standby

        public Rivermaker(int i, int j, int m)
        {
            gridi = i;
            gridj = j;
            mode = m;
        }
    }
    Rivermaker tracker;                             //This will probe the available empty grid to make rivers
    Rivermaker anchor;                              //This will store the tracker before rivermaking, for the purpose of assigning tiles from the anchor to the tracker

    public bool pauseFlood = false;                 //variable to control the flood, if true, then the flood will not ascend




    // Use this for initialization
    void Start()
    {


        //--Creating the stationary flood and making it a child of the gamecontroller object, to which this script is attached--
        for (i = 0; i < offset - 1; ++i)                                 //instantiating the first some rows of the flood tiles
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                Instantiate(WaterTile, new Vector3(0.5f + j, 0f, 0.5f + i), Quaternion.Euler(new Vector3(90, 0, 0)), gameObject.transform);
            }
        }

        for (j = 0; j < gridBreadth; ++j)                       //instantiating a row of log above the rows of flood
        {
            Instantiate(LogTile, new Vector3(0.5f + j, 0f, 0.5f + offset - 1), Quaternion.Euler(new Vector3(90, 0, 0)), gameObject.transform);
        }


        //--Creating the grid and a list to store the tile objects so that they can be manipulated later
        gridArray = new Cell[gridLength, gridBreadth];          //declaring the length and breadth of the 2 dimensional array

        TileQueue = new List<GameObject>();                     //the array to store the tiles instantiated, so that they can be shifted out of the queue and destroyed

        for (i = 0; i < gridLength; ++i)                        //Initializing the 2D array
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                gridArray[i, j] = new Cell(0, false);
            }
        }


        
        tracker = new Rivermaker(0, 0, 0);                      //This means that both the rivermakers are on the bottom most and left most cell of the grid
        anchor = new Rivermaker(0, 0, 0);

        CreateRiver();
    }



    //--Determining which cells in the grid are supposed to contain which tiles
    void CreateRiver()
    {
        anchor = tracker;                                                       //recording the starting position of the tracker to draw the river from after the tracker has done probing
        tracker.mode = 0;

        while (tracker.mode != 4)
        {
            if (tracker.mode == 0)                                              //When the Rivermaker is on the left most grid
            {
                riverOffset = betterRandom(minLandLength, maxLandLength);       //calculate a pseudo random integer for the distance between two rivers

                if (tracker.gridi + riverOffset > gridLength - 15)
                {
                    tracker.mode = 4;                                           //Getting out of the loop condition, when there is no enough space on the top of the grid to start making a river
                    continue;                                                   //skip to the end of the loop
                }
                else
                {
                    tracker.gridi = tracker.gridi + riverOffset;                //Updating the location of the rivermaker

                    gridArray[tracker.gridi, tracker.gridj].tileType = 1;       //make the current cell of the grid to hold a border water tile
                    gridArray[tracker.gridi, tracker.gridj].border = true;

                    tracker.mode = 1;                                           //Change the tracker mode to move right and construct the border till the left
                    continue;                                                   //Skip to the end of the loop
                }
            }

            if (tracker.mode == 1)
            {
                for (j = 0; j < gridBreadth - 1; ++j)
                {
                    randomHelper = betterRandom(0, 1000);                       //choosing a random number to decide how the rivermaker will proceed to the right

                    if (randomHelper <= 600)                                    //The rivermaker will move to the right of the current cell
                    {
                        tracker.gridj++;
                    }
                    else if (randomHelper > 600 && randomHelper <= 800)         //The rivermaker will move to the right and above the current cell
                    {
                        tracker.gridj++;
                        tracker.gridi++;
                    }
                    else if (randomHelper > 800 && randomHelper <= 1000)        //The rivermaker will move to the right and below the current cell
                    {
                        tracker.gridj++;
                        tracker.gridi--;
                    }

                    gridArray[tracker.gridi, tracker.gridj].tileType = 1;        //make the current cell of the grid to hold a border water tile
                    gridArray[tracker.gridi, tracker.gridj].border = true;
                }

                tracker.mode = 2;                                               //Change the tracker mode to move up on the right most part of the grid along the width of the river
                continue;                                                       //Skip to the end of the loop
            }

            if (tracker.mode == 2)
            {
                riverOffset = betterRandom(minRiverWidth, maxRiverWidth);       //calculating a pseudo random integer for the width of the river on the right most grid

                tracker.gridi = tracker.gridi + riverOffset - 1;                //Moving the tracker up along the width of the river

                gridArray[tracker.gridi, tracker.gridj].tileType = 1;           //make the current cell of the grid to hold a border water tile
                gridArray[tracker.gridi, tracker.gridj].border = true;

                for (i = 1; i < riverOffset - 1; ++i)
                {
                    gridArray[tracker.gridi - i, tracker.gridj].tileType = 1;   //assigning non-border water tiles to the tiles between the border tiles
                }

                tracker.mode = 3;                                               //Changing the mode of the tracker to start moving on the left from the next iteration
                continue;                                                       //Skip to the end of the loop
            }

            if (tracker.mode == 3)
            {
                for (j = 0; j < gridBreadth - 1; ++j)
                {
                    withinTheLimits = false;                                        //Setting up the condition for the following while loop

                    while (withinTheLimits == false)                                //To check if the desired cell is appropriate according to the limits of the river width
                    {
                        randomHelper = betterRandom(0, 1000);                       //choosing a random number to decide how the rivermaker should proceed to the left

                        if (randomHelper <= 600)                                    //The left of the current cell will be probed for validity for the river width
                        {
                            tempj = tracker.gridj - 1;
                            tempi = tracker.gridi;
                        }
                        else if (randomHelper > 600 && randomHelper <= 800)         //The cell top left of the current cell will be probed for validity for the river width
                        {
                            tempj = tracker.gridj - 1;
                            tempi = tracker.gridi + 1;
                        }
                        else if (randomHelper > 800 && randomHelper <= 1000)         //The cell bottom left of the current cell will be probed for validity for the river width
                        {
                            tempj = tracker.gridj - 1;
                            tempi = tracker.gridi - 1;
                        }

                        tempFound = false;                                          //Setting up the conditions for the following while loop
                        tempCount = 1;

                        while (tempFound == false)                                  //Calculating at what distance is the lower border of the potential upper border cell
                        {
                            if (gridArray[tempi - tempCount, tempj].border == true)
                            {
                                tempFound = true;
                            }
                            else
                            {
                                gridArray[tempi - tempCount, tempj].tileType = 1;   //assigning non-border water tiles to the tiles between the border tiles
                                print(++tempCount);
                                print(gridArray[tempi - tempCount, tempj].border);
                            }
                        }

                        if (tempCount >= minRiverWidth - 1 && tempCount <= maxRiverWidth - 1)   //Checking if the distance between between the lower border cell and the potential upper border cell lies with the river width limits
                        {
                            withinTheLimits = true;
                        }
                    }

                    tracker.gridi = tempi;                                          //Moving the tracker to the next cell
                    tracker.gridj = tempj;
                    gridArray[tracker.gridi, tracker.gridj].tileType = 1;           //make the current cell of the grid to hold a border water tile
                    gridArray[tracker.gridi, tracker.gridj].border = true;
                }

                tracker.mode = 0;                                               //Changing the mode of the tracker to check to find the position of the next river
            }
        }

        if (anchor.gridi != tracker.gridi)
        {
            for (i = anchor.gridi; i <= tracker.gridi; ++i)                                        //Here i is used for change in z-axis position, j is used for change in x-axis position
            {
                for (j = 0; j < gridBreadth; ++j)
                {
                    if (gridArray[i, j].tileType == 0)
                    {
                        TileQueue.Add(Instantiate(GroundTile, new Vector3(0.5f + j, 0f, 0.5f + i + offset), Quaternion.Euler(new Vector3(90, 0, 0))));
                    }
                    else if (gridArray[i, j].tileType == 1)
                    {
                        TileQueue.Add(Instantiate(WaterTile, new Vector3(0.5f + j, 0f, 0.5f + i + offset), Quaternion.Euler(new Vector3(90, 0, 0))));
                    }
                }
            }
        }
    }


    // Update is called once per frame
    void Update()
    {

        if (pauseFlood == false)
        {
            offset++;                                           //increasing the offset for real world spawning coordinates of the new tiles
            StartCoroutine(Ascend());                           //starting the coroutine for waiting for sometime and then making the flood ascend onto the map
        }
    }





    IEnumerator Ascend()
    {
        pauseFlood = true;                                      //changing the variable for the Update function so that it does not continuously call on the coroutine
        yield return new WaitForSeconds(0.5f);
        transform.position = transform.position + new Vector3(0f, 0f, 1f);

        for (i = 0; i < gridLength - 1; ++i)                    //shifting the entire grid one row downwards
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                gridArray[i, j] = gridArray[i + 1, j];
            }
        }

        for (j = 0; j < gridBreadth; ++j)                       //giving the default new values to the uppermost new row of the grid
        {
            gridArray[gridLength - 1, j].tileType = 0;
            gridArray[gridLength - 1, j].border = false;
        }

        print(--tracker.gridi);                                 //wherever the tracker is, bringing it down by one row

        if (TileQueue.Count != 0)                               //Removing the tiles overlapped by the flood from the array
        {
            for (k = 0; k < gridBreadth; ++k)
            {
                tileObject = TileQueue[0];                      //storing the tile in temporary variable to destroy it
                TileQueue.RemoveAt(0);                          //removing the tile from the front of the list
                Destroy(tileObject);
            }
        }

        //CreateRiver();

        pauseFlood = false;                                     //after the wait, allowing the Update function to call on the coroutine
    }

    #region Better random number generator                      

    private static readonly RNGCryptoServiceProvider _generator = new RNGCryptoServiceProvider();

    public static int betterRandom(int minimumValue, int maximumValue)
    {
        byte[] randomNumber = new byte[1];

        _generator.GetBytes(randomNumber);

        double asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

        // We are using Math.Max, and substracting 0.00000000001,  
        // to ensure "multiplier" will always be between 0.0 and .99999999999 
        // Otherwise, it's possible for it to be "1", which causes problems in our rounding. 
        double multiplier = Math.Max(0, (asciiValueOfRandomCharacter / 255d) - 0.00000000001d);

        // We need to add one to the range, to allow for the rounding done with Math.Floor 
        int range = maximumValue - minimumValue + 1;

        double randomValueInRange = Math.Floor(multiplier * range);

        return (int)(minimumValue + randomValueInRange);
    }
    #endregion
}
