using UnityEngine;
using System.Collections.Generic;
using csDelaunay;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
  // game settings
  [Header("Game Settings")]
  public Enums.SheepBehaviour sheepBehaviour;
  public int nOfSheep;
  public float simulationTime;
  private float simulationTimer;

  // sheep prefab
  [Header("Sheep")]
  public GameObject sheepPrefab;

  // random size
  private const float minSheepSize = .7f;

  [Header("Flow Settings")]
  public int precision;
  // size of 1 cell in flow grid in Unity units
  [HideInInspector]
  public float binSize;
  [HideInInspector]
  public float fieldSize = 100.0f;

  // list of sheep
  [HideInInspector]
  public List<SheepController> sheepList = new List<SheepController>();
  [HideInInspector]
  public List<FlowSheepController> flowSheepList = new List<FlowSheepController>();

  // fences
  [Header("Fence")]
  public GameObject fence;
  [HideInInspector]
  public Collider[] fenceColliders;

  // flows
  [HideInInspector]
  public Cell[,] forceField;

  // game settings
  // hardcoded spawn boundaries
  private const float minSpawnX = 20.0f;
  private const float maxSpawnX = 90.0f;
  private const float minSpawnZ = 20.0f;
  private const float maxSpawnZ = 80.0f;

  void Start()
  {
    // init timer
    simulationTimer = .0f;

    // spawn
    SpawnSheep();

    // fences colliders
    fenceColliders = fence.GetComponentsInChildren<Collider>();

    if (sheepBehaviour == Enums.SheepBehaviour.Flow)
    {
      binSize = fieldSize / precision;

      // init the field
      forceField = new Cell[precision, precision];
      Vector3 coordinates;
      for (int i = 0; i < precision; i++)
      {
        for (int j = 0; j < precision; j++)
        {
          coordinates = new Vector3((binSize / 2) + (i * binSize), .0f, (binSize / 2) + (j * binSize));
          forceField[i, j] = new Cell(coordinates, i, j, nOfSheep);
        }
      }

      // set neighbours
      for (int i = 0; i < precision; i++)
      {
        for (int j = 0; j < precision; j++)
        {
          SetNeighbours(1, i, j);
        }
      }

      UpdateFlows();
    }
  }

  void SetNeighbours(int neighbourHoodSize, int x, int y)
  {
    Cell currentCell = forceField[x, y];

    // boundaries
    int xMin = Mathf.Max(0, x - neighbourHoodSize);
    int xMax = Mathf.Min(x + neighbourHoodSize, precision - 1);
    int yMin = Mathf.Max(0, y - neighbourHoodSize);
    int yMax = Mathf.Min(y + neighbourHoodSize, precision - 1);


    for (int i = xMin; i < xMax; i++)
    {
      for (int j = yMin; j < yMax; j++)
      {
        if (i != x && j != y)
          currentCell.neighbourCells.Add(forceField[i, j]);
      }
    }
  }

  void SpawnSheep()
  {
    // cleanup
    int i = 0;
    sheepList.Clear();
    GameObject[] sheep = GameObject.FindGameObjectsWithTag("Sheep");
    for (i = 0; i < sheep.Length; i++)
      Destroy(sheep[i]);

    // spawn
    Vector3 position;
    GameObject newSheep;

    i = 0;
    while (i < nOfSheep)
    {
      position = new Vector3(Random.Range(minSpawnX, maxSpawnX), .0f, Random.Range(minSpawnZ, maxSpawnZ));

      float randomFloat = Random.Range(minSheepSize, 1.0f);

      newSheep = (GameObject)Instantiate(sheepPrefab, position, Quaternion.identity);
      newSheep.transform.localScale = new Vector3(randomFloat, randomFloat, randomFloat);
      SheepController sc = newSheep.GetComponent<SheepController>();
      FlowSheepController fsc = newSheep.GetComponent<FlowSheepController>();

      if (sheepBehaviour == Enums.SheepBehaviour.Individual)
      {
        fsc.enabled = false;
        sc.id = i;
        sheepList.Add(sc);
      }
      else
      {
        sc.enabled = false;
        fsc.id = i;
        flowSheepList.Add(fsc);
      }
      i++;
    }
  }

  void UpdateFlows()
  {
    Vector3 centroid = new Vector3();

    // clean list
    for (int i = 0; i < precision; i++)
    {
      for (int j = 0; j < precision; j++)
      {
        forceField[i, j].sheepList.Clear();
      }
    }

    // get centroid
    int x = 0, y = 0;
    foreach (FlowSheepController fsc in flowSheepList)
    {
      centroid += fsc.transform.localPosition;

      // add sheep to field
      x = Mathf.FloorToInt(fsc.transform.position.x / binSize);
      y = Mathf.FloorToInt(fsc.transform.position.z / binSize);
      if (x > precision - 1)
        Debug.Log("ERROR!");
      if (y > precision - 1)
        Debug.Log("ERROR!");

      forceField[x, y].sheepList.Add(fsc);
      fsc.currentCell = forceField[x, y];
    }
    centroid /= flowSheepList.Count;

    centroid += new Vector3(Random.Range(-20.0f, 20.0f), .0f, Random.Range(-20.0f, 20.0f));

    for (int i = 0; i < precision; i++)
    {
      for (int j = 0; j < precision; j++)
      {
        forceField[i, j].UpdateField(centroid);
      }
    }
  }

  void Update()
  {
    simulationTimer += Time.deltaTime;
    if (simulationTimer > simulationTime)
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    // neighours update
    if (sheepBehaviour == Enums.SheepBehaviour.Individual)
      UpdateNeighbours();
    else
      UpdateFlows();
  }
  
  private void UpdateNeighbours()
  {
    List<Vector2f> points = new List<Vector2f>();

    // prepare for Fortunes algorithm and clear neighbours
    foreach (SheepController sheep in sheepList)
    {
      sheep.metricNeighbours.Clear();
      sheep.voronoiNeighbours.Clear();

      points.Add(new Vector2f(sheep.transform.position.x, sheep.transform.position.z, sheep.id));
    }

    // get metric neighbours
    SheepController firstSheep, secondSheep;
    for (int i = 0; i < sheepList.Count; i++)
    {
      firstSheep = sheepList[i];

      for (int j = i + 1; j < sheepList.Count; j++)
      {
        secondSheep = sheepList[j];

        // dist?
        if ((firstSheep.transform.position - secondSheep.transform.position).sqrMagnitude < firstSheep.r_o2)
        {
          firstSheep.metricNeighbours.Add(secondSheep);
          secondSheep.metricNeighbours.Add(firstSheep);
        }
      }
    }

    // voronoi neighbours
    Rectf bounds = new Rectf(0.0f, 0.0f, fieldSize, fieldSize);
    Voronoi voronoi = new Voronoi(points, bounds);

    foreach (Vector2f pt in points)
    {
      SheepController sheep = sheepList[pt.id];
      foreach (Vector2f neighbourPt in voronoi.NeighborSitesForSite(pt))
      {
        SheepController neighbour = sheepList[neighbourPt.id];
        sheep.voronoiNeighbours.Add(neighbour);
      }
    }
  }
}