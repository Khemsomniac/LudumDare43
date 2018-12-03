using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Cryptography;

public class MapMaker : MonoBehaviour
{
    public GameObject tileObject;           //gameobject variable to help instantiate the tile
    List<GameObject> TileQueue;

    //---A single cell in the 2D array defining the grid of the map
    public class Cell
    {
        public int tileType;                //0=Ground, 1=Water, 2=Log, 3=Bridge, 4=House
        public bool border;                 //if true, it means the terrain will change from land to water starting this cell, and the terrain will change from water to land from the next cell.
        public bool isWalkable;             //if true, then the villagers can walk on this tile

        public Villager villagerInCell;     //The villager in this cell (if there should be one)
        public bool showBridgePreview;      //If true, turn on the bridge preview for this water cell
        public int iCoord; // The cell's i coord in the gridArray
        public int jCoord; // The cell's j coord in the gridArray
        public GameObject bridgePreview; // The bridge preview game object

        public Cell(int tile, bool bord, bool walk)
        {
            tileType = tile;
            border = bord;
            isWalkable = walk;
        }

        public Cell()
        {

        }
    }

    public Cell[,] gridArray;               //This stores the interactive values of the tiles which are currently in play
    public int floodOffset = 3;             //This is the z-axis value added to the position of the new instantiated tiles
    public int floodCount = 0;              //This stores the number of times the flood has progressed one cell upwards
    public bool pauseFlood = false;         //variable to control the flood, if true, then the flood will not ascend

    public int gridLength;                  //Length of the active grid or the grid array
    public int gridBreadth;                 //Breadth of the active grid or the grid array

    public Cell[,] chunkArray;              //This stores the grid values of the chunk to be made next
    public int chunkCount = 0;              //This keeps the count of the number of chunks created
    public int initialChunksPossible;       //This is the grid length divided by grid breadth, to check how many initial chunks can be made

    //public int maxLandLength;             //maximum length that one stretch of land can have
    //public int minLandLength;             //minimum length that one stretch of land can have
    public int maxRiverWidth;               //maximum width that one river can have
    public int minRiverWidth;               //minimum width that one river can have
    public int chunkLength;                  //length of one chunk, specidfied by the designer    

    //-----------------The algorithm will FAIL if minRiverWidth is less than or equal to 3 !!!!!!!!--------------------------

    //---Tile prefabs specified by the designer
    public GameObject GroundTile;
    public GameObject WaterTile;
    public GameObject LogTile;
    public GameObject BridgeTile;
    public GameObject HouseTile;

    //---Temporary calculation variables
    private int i, j, k, l;                 //loop increment variables
    private int riverOffset;                //temporary variable for calculating random numbers for river boundaries
    private float randomHelper;             //temporary random variable for calculation purposes
    private int tempi, tempj;               //temporary coordinate variables for calculation purposes
    private bool withinTheLimits;           //temporary variable for helping run the while loops for checking if a certain value is under limits
    private int tempCount;                  //temporary variable for keeping a count for verification of certain conditions
    private bool tempFound;                 //temporary variable for helping run the while loops for checking if a certain value is found or not

    // Test
    public int coroutineCount; // Count how many coroutines is running

    //---This struct will be used to move around in the chunk grid in a pseudo random fashion and mark the tiles that need to be river tiles
    public struct Rivermaker
    {
        public int gridi;                   //These are the current location coordinates of the rivermaker on the chunk array
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
    Rivermaker tracker;                     //This will probe the available empty grid to make rivers
    //Rivermaker anchor;                    //This will store the tracker before rivermaking, for the purpose of assigning tiles from the anchor to the tracker

    //This is the class which will host villagers
    public class House
    {
        public int housei, housej;          //coordinates of the house on grid, correspond with the bottom left cell of the house    
        public int numberOfVillagers;       //pseudo random number of villagers that the house is holding
        public bool isEmpty = false;        //false when it is holding certain number of villagers inside, false when thehy are out of the house
    }
    public int maxVillagersInHouse;
    public int minVillagersInHouse;

    List<House> ListOfHouses;               //This will store all the houses which are created

    // Use this for initialization
    void Start()
    {


        //--Creating the stationary flood and making it a child of the gamecontroller object, to which this script is attached--
        for (i = 0; i < floodOffset - 1; ++i)                                           //instantiating the first some rows of the flood tiles
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                Instantiate(WaterTile, new Vector3(0.5f + j, 0f, 0.5f + i), Quaternion.Euler(new Vector3(90, 0, 0)), gameObject.transform);
            }
        }

        for (j = 0; j < gridBreadth; ++j)                                               //instantiating a row of log above the rows of flood
        {
            Instantiate(LogTile, new Vector3(0.5f + j, 0f, 0.5f + floodOffset - 1), Quaternion.Euler(new Vector3(90, 0, 0)), gameObject.transform);
        }


        //--Creating the grid and a list to store the tile objects so that they can be manipulated later
        gridArray = new Cell[gridLength, gridBreadth];                                  //declaring the length and breadth of the 2 dimensional array

        TileQueue = new List<GameObject>();                                             //the array to store the tiles instantiated, so that they can be shifted out of the queue and destroyed

        for (i = 0; i < gridLength; ++i)                                                //Initializing the 2D array
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                gridArray[i, j] = new Cell(0, false, true);

                // Give the cell its coord in the gridArray
                gridArray[i, j].iCoord = i;
                gridArray[i, j].jCoord = j;
            }
        }

        //--Initializing the chunk grid
        chunkArray = new Cell[chunkLength, gridBreadth];
        chunkCount = 0;

        tracker = new Rivermaker(0, 0, 0);                      //This means that both the rivermakers are on the bottom most and left most cell of the grid
        //anchor = new Rivermaker(0, 0, 0);

        for (i = 0; i < chunkLength; ++i)
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                chunkArray[i, j] = new Cell();
            }
        }

        //---Initializing the rivermakers
        tracker = new Rivermaker(0, 0, 0);                                              //This means that the rivermaker is on the bottom most and left most cell of the grid
        //anchor = new Rivermaker(0, 0, 0);

        //---Initializing the List of Houses
        ListOfHouses = new List<House>();

        //---Calculating the chunks to be made
        initialChunksPossible = gridLength / chunkLength;
        for (k = 0; k < initialChunksPossible; ++k)
        {
            CreateChunk();
            CreateAndPushTiles();
            PushChunkIntoGrid(k);
        }

        //CreateRiver();
    }

    //---This function will create a grid representing a chunk of land with a pseudo randomized river flowing horizontal in the middle of it
    void CreateChunk()
    {
        //---Initializing the chunk array with the default values
        for (i = 0; i < chunkLength; ++i)
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                chunkArray[i, j].tileType = 0;
                chunkArray[i, j].border = false;
                chunkArray[i, j].isWalkable = true;
            }
        }

        //---Initializing the tracker for this chunk
        tracker.gridi = 0;
        tracker.gridj = 0;
        tracker.mode = 0;

        //---Making the river
        //---Tracker mode 0
        riverOffset = betterRandom((chunkLength / 2) - 2, (chunkLength / 2) + 2);       //calculate a pseudo random integer for the position of the river in a chunk

        tracker.gridi = riverOffset - 1;                                                //Updating the location of the rivermaker

        chunkArray[tracker.gridi, tracker.gridj].tileType = 1;                          //make the current cell of the grid to hold a border water tile
        chunkArray[tracker.gridi, tracker.gridj].border = true;
        chunkArray[tracker.gridi, tracker.gridj].isWalkable = false;

        //---Tracker mode 1
        for (j = 0; j < gridBreadth - 1; ++j)
        {
            randomHelper = betterRandom(0, 1000);                                       //choosing a random number to decide how the rivermaker will proceed to the right

            if (randomHelper <= 600)                                                    //The rivermaker will move to the right of the current cell
            {
                tracker.gridj++;
            }
            else if (randomHelper > 600 && randomHelper <= 800)                         //The rivermaker will move to the right and above the current cell
            {
                tracker.gridj++;
                tracker.gridi++;
            }
            else if (randomHelper > 800 && randomHelper <= 1000)                        //The rivermaker will move to the right and below the current cell
            {
                tracker.gridj++;
                tracker.gridi--;
            }

            chunkArray[tracker.gridi, tracker.gridj].tileType = 1;                      //make the current cell of the grid to hold a border water tile
            chunkArray[tracker.gridi, tracker.gridj].border = true;
            chunkArray[tracker.gridi, tracker.gridj].isWalkable = false;
        }

        //---Tracker mode 2
        riverOffset = betterRandom(minRiverWidth, maxRiverWidth);                       //calculating a pseudo random integer for the width of the river on the right most grid

        tracker.gridi = tracker.gridi + riverOffset - 1;                                //Moving the tracker up along the width of the river

        chunkArray[tracker.gridi, tracker.gridj].tileType = 1;                          //make the current cell of the grid to hold a border water tile
        chunkArray[tracker.gridi, tracker.gridj].border = true;
        chunkArray[tracker.gridi, tracker.gridj].isWalkable = false;

        for (i = 1; i < riverOffset - 1; ++i)
        {
            chunkArray[tracker.gridi - i, tracker.gridj].tileType = 1;                  //assigning non-border water tiles to the tiles between the border tiles
            chunkArray[tracker.gridi - i, tracker.gridj].isWalkable = false;
        }

        //---Tracker mode 3
        for (j = 0; j < gridBreadth - 1; ++j)
        {
            withinTheLimits = false;                                                    //Setting up the condition for the following while loop

            while (withinTheLimits == false)                                            //To check if the desired cell is appropriate according to the limits of the river width
            {
                randomHelper = betterRandom(0, 1000);                                   //choosing a random number to decide how the rivermaker should proceed to the left

                if (randomHelper <= 600)                                                //The left of the current cell will be probed for validity for the river width
                {
                    tempj = tracker.gridj - 1;
                    tempi = tracker.gridi;
                }
                else if (randomHelper > 600 && randomHelper <= 800)                     //The cell top left of the current cell will be probed for validity for the river width
                {
                    tempj = tracker.gridj - 1;
                    tempi = tracker.gridi + 1;
                }
                else if (randomHelper > 800 && randomHelper <= 1000)                    //The cell bottom left of the current cell will be probed for validity for the river width
                {
                    tempj = tracker.gridj - 1;
                    tempi = tracker.gridi - 1;
                }

                tempFound = false;                                                      //Setting up the conditions for the following while loop
                tempCount = 1;

                while (tempFound == false)                                              //Calculating at what distance is the lower border of the potential upper border cell
                {
                    if (chunkArray[tempi - tempCount, tempj].border == true)
                    {
                        tempFound = true;
                    }
                    else
                    {
                        chunkArray[tempi - tempCount, tempj].tileType = 1;              //assigning non-border water tiles to the tiles between the border tiles
                        ++tempCount;
                    }
                }

                if (tempCount >= minRiverWidth - 1 && tempCount <= maxRiverWidth - 1)   //Checking if the distance between between the lower border cell and the potential upper border cell lies with the river width limits
                {
                    withinTheLimits = true;
                }
            }

            tracker.gridi = tempi;                                                      //Moving the tracker to the next cell
            tracker.gridj = tempj;
            chunkArray[tracker.gridi, tracker.gridj].tileType = 1;                      //make the current cell of the grid to hold a border water tile
            chunkArray[tracker.gridi, tracker.gridj].border = true;
            chunkArray[tracker.gridi, tracker.gridj].isWalkable = false;
        }
        //---Making the houses

    }

    //---This function will instantiate the tiles at the right proper grid locations on the map and push them into a list to keep a record of them
    void CreateAndPushTiles()
    {
        //---Instantiating the tiles and pushing them into the global tile storage list, note that here i is used for change in z-axis position, j is used for change in x-axis position
        for (i = 0; i < chunkLength; ++i)
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                if (chunkArray[i, j].tileType == 0)                         //Checking the tileType values from the grid and instantiating the appropriate tiles
                {
                    TileQueue.Add(Instantiate(GroundTile, new Vector3(0.5f + j, 0f, 0.5f + i + floodOffset + (chunkCount * chunkLength)), Quaternion.Euler(new Vector3(90, 0, 0))));
                }
                else if (chunkArray[i, j].tileType == 1)
                {
                    TileQueue.Add(Instantiate(WaterTile, new Vector3(0.5f + j, 0f, 0.5f + i + floodOffset + (chunkCount * chunkLength)), Quaternion.Euler(new Vector3(90, 0, 0))));
                }
                else if (chunkArray[i, j].tileType == 2)
                {
                    TileQueue.Add(Instantiate(LogTile, new Vector3(0.5f + j, 0f, 0.5f + i + floodOffset + (chunkCount * chunkLength)), Quaternion.Euler(new Vector3(90, 0, 0))));
                }
                else if (chunkArray[i, j].tileType == 3)
                {
                    TileQueue.Add(Instantiate(BridgeTile, new Vector3(0.5f + j, 0f, 0.5f + i + floodOffset + (chunkCount * chunkLength)), Quaternion.Euler(new Vector3(90, 0, 0))));
                }
                else if (chunkArray[i, j].tileType == 4)
                {
                    TileQueue.Add(Instantiate(HouseTile, new Vector3(0.5f + j, 0f, 0.5f + i + floodOffset + (chunkCount * chunkLength)), Quaternion.Euler(new Vector3(90, 0, 0))));
                }
            }
        }
    }

    //---This function will update the appropriate grid values by using the chunk grid values
    void PushChunkIntoGrid(int chunkPositionInGrid)
    {
        for (i = 0; i < chunkLength; ++i)
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                gridArray[chunkPositionInGrid * chunkLength + i, j] = chunkArray[i, j];             //using a multiplier to position the new chunk grid values into the grid
            }
        }

        ++chunkCount;
    }

    //--Determining which cells in the grid are supposed to contain which tiles
    /*void CreateRiver()
    {
        anchor = tracker;                                                       //recording the starting position of the tracker to draw the river from after the tracker has done probing
        tracker.mode = 0;

        int outerWhileStopper = 0;
        while (tracker.mode != 4 && outerWhileStopper < 1000)
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

                    int whileStopper = 0;
                    while (withinTheLimits == false && whileStopper < 1000)                                //To check if the desired cell is appropriate according to the limits of the river width
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

                        int whileStopperInner = 0;
                        while (tempFound == false && whileStopperInner < 1000)                                  //Calculating at what distance is the lower border of the potential upper border cell
                        {
                            if (gridArray[tempi - tempCount, tempj].border == true)
                            {
                                tempFound = true;
                            }
                            else
                            {
                                gridArray[tempi - tempCount, tempj].tileType = 1;   //assigning non-border water tiles to the tiles between the border tiles
                                ++tempCount;
                                //print("tempCount: " + ++tempCount);
                            }

                            whileStopperInner++;
                        }

                        if (tempCount >= minRiverWidth - 1 && tempCount <= maxRiverWidth - 1)   //Checking if the distance between between the lower border cell and the potential upper border cell lies with the river width limits
                        {
                            withinTheLimits = true;
                        }

                        whileStopper++;
                    }

                    tracker.gridi = tempi;                                          //Moving the tracker to the next cell
                    tracker.gridj = tempj;
                    gridArray[tracker.gridi, tracker.gridj].tileType = 1;           //make the current cell of the grid to hold a border water tile
                    gridArray[tracker.gridi, tracker.gridj].border = true;
                }

                tracker.mode = 0;                                               //Changing the mode of the tracker to check to find the position of the next river
            }

            outerWhileStopper++;
        }

        if (anchor.gridi != tracker.gridi)
        {
            for (i = anchor.gridi; i <= tracker.gridi; ++i)                                        //Here i is used for change in z-axis position, j is used for change in x-axis position
            {
                for (j = 0; j < gridBreadth; ++j)
                {
                    if (gridArray[i, j].tileType == 0)
                    {
                        TileQueue.Add(Instantiate(GroundTile, new Vector3(0.5f + j, 0f, 0.5f + i + floodOffset), Quaternion.Euler(new Vector3(90, 0, 0))));
                    }
                    else if (gridArray[i, j].tileType == 1)
                    {
                        TileQueue.Add(Instantiate(WaterTile, new Vector3(0.5f + j, 0f, 0.5f + i + floodOffset), Quaternion.Euler(new Vector3(90, 0, 0))));
                    }
                }
            }
        }
    }
    */

    // Update is called once per frame
    void Update()
    {
        if (pauseFlood == false)
        {
            //offset++;                                             //increasing the offset for real world spawning coordinates of the new tiles
            StartCoroutine(Ascend());                               //starting the coroutine for waiting for sometime and then making the flood ascend onto the map
        }

        //print("coroutineCount" + coroutineCount);
    }

    //---This is the coroutine for the flood to wait for sometime before moving upwards
    IEnumerator Ascend()
    {
        coroutineCount++;

        pauseFlood = true;                                      //changing the variable for the Update function so that it does not continuously call on the coroutine

        pauseFlood = true;                                                          //changing the variable for the Update function so that it does not continuously call on the coroutine

        yield return new WaitForSeconds(0.5f);

        //---Moving the flood one space upwards
        transform.position = transform.position + new Vector3(0f, 0f, 1f);
        floodCount++;                                                               //Keeping a count of the number of times the flood has moved upwards

        //---Shifting the entire grid one row downwards to accommodate the new uppermost row
        for (i = 0; i < gridLength - 1; ++i)
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                gridArray[i, j] = gridArray[i + 1, j];
            }
        }

        //---Giving the default new values to the uppermost new row of the grid
        for (j = 0; j < gridBreadth; ++j)
        {
            gridArray[gridLength - 1, j].tileType = 0;
            gridArray[gridLength - 1, j].border = false;
        }

        --tracker.gridi;
        //print("Tracker: " + --tracker.gridi);                                 //wherever the tracker is, bringing it down by one row

        if (TileQueue.Count != 0)                               //Removing the tiles overlapped by the flood from the array

            //---Removing the tiles overlapped by the flood from the list of stored tiles
            if (TileQueue.Count != 0)

            {
                for (k = 0; k < gridBreadth; ++k)
                {
                    tileObject = TileQueue[0];                                          //storing the tile in temporary variable to destroy it
                    TileQueue.RemoveAt(0);                                              //removing the tile from the front of the list
                    Destroy(tileObject);
                }
            }

        //---Checking if there is space for a new chunk of grid in the upper part of the array, and making one
        if (floodCount % chunkLength == 0)
        {
            CreateChunk();
            CreateAndPushTiles();
            PushChunkIntoGrid(initialChunksPossible - 1);
        }

        pauseFlood = false;                                     //after the wait, allowing the Update function to call on the coroutine

        coroutineCount--;

        pauseFlood = false;                                                         //after the wait, allowing the Update function to call on the coroutine
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
