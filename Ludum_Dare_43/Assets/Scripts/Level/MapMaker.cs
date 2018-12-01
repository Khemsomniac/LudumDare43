using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Cryptography;

public class MapMaker : MonoBehaviour {

    public GameObject tileObject;           //gameobject variable to help instantiate the tile
    List<GameObject> TileQueue;

    public class Cell                       //A single cell in the 2D array defining the grid of the map
    {
        public int tileType;                //0=Ground, 1=Water, 2=Log, 3=Bridge, 4=House
        public int areaType;                //0=Land, 1=River, 2=Flood
        public bool border = false;                 //if true, it means the terrain will change from land to water starting this cell, and the terrain will change from water to land from the next cell.

        public Cell(int tile, int area, bool bord)         //constructor
        {
            tileType = tile;
            areaType = area;
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

    public GameObject GroundTile;           //Tile prefabs to be specified by the designer
    public GameObject WaterTile;
    public GameObject LogTile;
    public GameObject BridgeTile;
    public GameObject HouseTile;

    private int i, j, k;                    //loop increment variables
    private int riverOffset;                //temporary variable for calculating random numbers for river boundaries

    public bool pauseFlood = false;                 //variable to control the flood, if true, then the flood will not ascend

    
    
    
    // Use this for initialization
    void Start () {


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



        gridArray = new Cell[gridLength, gridBreadth];          //declaring the length and breadth of the 2 dimensional array

        TileQueue = new List<GameObject>();                     //the array to store the tiles instantiated, so that they can be shifted out of the queue and destroyed

        for (i = 0; i < gridLength; ++i)                        //Initializing the 2D array
        {
            for (j = 0; j < gridBreadth; ++j)
            {
                gridArray[i, j] = new Cell(0, false);
            }
        }


        riverOffset = 



        //for (i = 0; i < gridLength; ++i)                    //Here i is used for change in z-axis position, j is used for change in x-axis position
        //{
        //    for (j = 0; j < gridBreadth; ++j)
        //    {
        //        if (i % 2 == 0)
        //        {
        //            gridArray[i, j] = new Cell(0, 0);
        //            TileQueue.Add(Instantiate(GroundTile, new Vector3(0.5f + j, 0f, 0.5f + i + offset), Quaternion.Euler(new Vector3(90, 0, 0))));
        //        }
        //        else
        //        {
        //            gridArray[i, j] = new Cell(1, 0);
        //            TileQueue.Add(Instantiate(WaterTile, new Vector3(0.5f + j, 0f, 0.5f + i + offset), Quaternion.Euler(new Vector3(90, 0, 0))));
        //        }
        //    }
        //}
    }





    // Update is called once per frame
    void Update () {

        //if (pauseFlood == false)
        //{
        //    offset++;                                           //increasing the offset for real world spawning coordinates of the new tiles
        //    StartCoroutine(Ascend());                           //starting the coroutine for waiting for sometime and then making the flood ascend onto the map

        //    for(i = 0; i < gridLength - 1; ++i)
        //    {
        //        for (j = 0; j < gridBreadth; ++j)
        //        {
        //            gridArray[i, j] = gridArray[i + 1, j];
        //        }
        //    }

        //    for (j = 0; j< gridBreadth; ++j)
        //    {
        //        gridArray[gridLength - 1, j] = new Cell(0, 0);
        //        TileQueue.Add(Instantiate(GroundTile, new Vector3(0.5f + j, 0f, 0.5f + gridLength - 1 + offset), Quaternion.Euler(new Vector3(90, 0, 0))));
        //    }
        //}
    }





    IEnumerator Ascend()
    {
        pauseFlood = true;                                      //changing the variable for the Update function so that it does not continuously call on the coroutine
        yield return new WaitForSeconds(0.5f);
        transform.position = transform.position + new Vector3(0f, 0f, 1f);

        if (TileQueue.Count != 0)                               //Removing the tiles overlapped by the flood from the array
        {
            for (i = 0; i < gridBreadth; ++i)
            {
                tileObject = TileQueue[0];                      //storing the tile in temporary variable to destroy it
                TileQueue.RemoveAt(0);                          //removing the tile from the front of the list
                Destroy(tileObject);
            }
        }


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
