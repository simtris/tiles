using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    public Camera mainCamera; 

    public bool displayPollution;
    public GameObject pollutionPrefab;
    public GameObject[] inhabitantsPrefabs; 
    public GameObject tilesGO;
    public float tickLength;

    public TextMeshProUGUI population;
    public TextMeshProUGUI livability;
    public TextMeshProUGUI radiation;
    public TextMeshProUGUI pollution;
    public TextMeshProUGUI food;

    public Image populationCompletion;
    public Image populationDecompletion;

    public Image foodCompletion;
    public Image foodDecompletion;

    public int pollutionMaxDistance;
    public int pollutionSourceStrength;
    public int populationMaxDistance;
    public int maxPopulationPerTile; 

    private int[] pollutionResistancePerState;
    private int[] populationResistancePerState;
    private int[] foodPerState; 

    private Tile[][] tiles;
    private float timeCounter;

    private bool clicking = false;
    private void Start() {
        timeCounter = tickLength;

        tiles = new Tile[tilesGO.transform.childCount][];
        for (int i = 0; i < tiles.Length; i++) {
            GameObject tileRow = tilesGO.transform.GetChild(i).gameObject;
            tiles[i] = tileRow.GetComponentsInChildren<Tile>();
        }

        int tileTypeNb = Tile.GetStateEnumSize();
        pollutionResistancePerState = new int[tileTypeNb - 1];
        pollutionResistancePerState[(int)Tile.State.Desert - 1] = 5;
        pollutionResistancePerState[(int)Tile.State.Forest - 1] = 1;
        pollutionResistancePerState[(int)Tile.State.Meadow - 1] = 0;
        pollutionResistancePerState[(int)Tile.State.Moutain - 1] = 6;
        pollutionResistancePerState[(int)Tile.State.Water - 1] = 3;

        populationResistancePerState = new int[tileTypeNb - 1];
        populationResistancePerState[(int)Tile.State.Desert - 1] = 7;
        populationResistancePerState[(int)Tile.State.Forest - 1] = 4;
        populationResistancePerState[(int)Tile.State.Meadow - 1] = 0;
        populationResistancePerState[(int)Tile.State.Moutain - 1] = 10;
        populationResistancePerState[(int)Tile.State.Water - 1] = 1000;

        foodPerState = new int[tileTypeNb - 1];
        foodPerState[(int)Tile.State.Desert - 1] = 10;
        foodPerState[(int)Tile.State.Forest - 1] = 90;
        foodPerState[(int)Tile.State.Meadow - 1] = 60;
        foodPerState[(int)Tile.State.Moutain - 1] = 30;
        foodPerState[(int)Tile.State.Water - 1] = 70;

        InitializeTiles();

    }

    private void Update() {
        timeCounter -= Time.deltaTime;
        if(timeCounter < 0) {
            timeCounter = tickLength;
            Tick();
        }

        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
            mainCamera.orthographicSize = Mathf.Max(mainCamera.orthographicSize - 0.5f, 2);
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
            mainCamera.orthographicSize = Mathf.Min(mainCamera.orthographicSize + 0.5f, 8);

        if (Input.GetButtonDown("Clic")) 
            clicking = true;
        else if (Input.GetButtonUp("Clic"))
            clicking = false;

        if(clicking) {
            Vector3 position = mainCamera.transform.localPosition;
            position.x -= Input.GetAxis("Mouse X") / 2f;
            position.y -= Input.GetAxis("Mouse Y") / 2f;
            mainCamera.transform.localPosition = position;
        }

    }

    /// <summary>
    /// Function called every tick
    /// </summary>
    private void Tick() {

        for (int i = 0; i < tiles.Length; i++) {
            for (int j = 0; j < tiles[i].Length; j++) {
                Tile t = i - 1 >= 0 ? tiles[i - 1][j] : null;
                Tile b = i + 1 < tiles.Length ? tiles[i + 1][j] : null;
                Tile l = j - 1 >= 0 ? tiles[i][j - 1] : null;
                Tile r = j + 1 < tiles[i].Length ? tiles[i][j + 1] : null;
                Tile tl = i - 1 >= 0 && j - 1 >= 0 ? tiles[i - 1][j - 1] : null;
                Tile tr = i - 1 >= 0 && j + 1 < tiles[i].Length ? tiles[i - 1][j + 1] : null;
                Tile bl = i + 1 < tiles.Length && j - 1 >= 0 ? tiles[i + 1][j - 1] : null;
                Tile br = i + 1 < tiles.Length && j + 1 < tiles[i].Length ? tiles[i + 1][j + 1] : null;
                List<Tile> neighbourTiles = new List<Tile>() { tl, t, tr, r, br, b, bl, l };
                tiles[i][j].UpdateGame(neighbourTiles, pollutionMaxDistance, pollutionSourceStrength, populationMaxDistance);
            }
        }

        for (int i = 0; i < tiles.Length; i++) {
            for (int j = 0; j < tiles[i].Length; j++) {
                tiles[i][j].ApplyUpdateGame(maxPopulationPerTile);
                UpdateInhabitants(tiles[i][j]);
            }
        }
        
    }

    /// <summary>
    /// Initiliazes the tiles 
    /// </summary>
    private void InitializeTiles() {

        int pollutionCenterX = Random.Range(0, tiles[0].Length);
        int pollutionCenterY = Random.Range(0, tiles.Length);

        int tileTypeNb = Tile.GetStateEnumSize();
        int[] statesTotalNb = new int[tileTypeNb - 1];
        for (int i = 0; i < tileTypeNb - 1; i++)
            statesTotalNb[i] = 0;

        for (int i = 0; i < tiles.Length; i++) {
            for (int j = 0; j < tiles[i].Length; j++) {

                if(j == pollutionCenterX && i == pollutionCenterY) {

                    GameObject pollutionCenter = GameObject.Instantiate(pollutionPrefab, tilesGO.transform.GetChild(i).GetChild(j));
                    Vector3 position = pollutionCenter.transform.localPosition;
                    position.x = 0;
                    position.y = 0;
                    position.z = -1;
                    pollutionCenter.transform.localPosition = position;

                    tiles[i][j].pollution = true;
                    tiles[i][j].pollutionSource = true;
                    tiles[i][j].pollutionDistance = 0;

                }

                // Get adjacent (l, tl, t and tr) tiles states 
                Tile.State[] adjacentTiles = new Tile.State[4] {
                    i - 1 >= 0 ? tiles[i - 1][j].state : Tile.State.Null,
                    j - 1 >= 0 ? tiles[i][j - 1].state : Tile.State.Null,
                    i - 1 >= 0 && j - 1 >= 0 ? tiles[i - 1][j - 1].state : Tile.State.Null,
                    i - 1 >= 0 && j + 1 < tiles[i].Length ? tiles[i - 1][j + 1].state : Tile.State.Null
                 };

                // More chance to get the same state as the adjacent tiles' 
                List<Tile.State> statesProbas = new List<Tile.State>(); 
                foreach(Tile.State adjacentTile in adjacentTiles) {
                    if (adjacentTile != Tile.State.Null) {
                        for (int k = 1; k < 10; k++) 
                            statesProbas.Add(adjacentTile);
                    }
                }
                
                // Add the missing states
                for(int k = 1; k < tileTypeNb; k++) {
                    if (!statesProbas.Contains((Tile.State)k))
                        statesProbas.Add((Tile.State)k);
                }

                // No more than 1/4 of the tiles should have the same state
                Tile.State randomTile = statesProbas[Random.Range(0, statesProbas.Count)];
                while(statesTotalNb[(int)randomTile - 1] >= tiles.Length * tiles[0].Length / 4)
                    randomTile = statesProbas[Random.Range(0, statesProbas.Count)];

                statesTotalNb[(int)randomTile - 1]++;
                tiles[i][j].SetState(randomTile);

                tiles[i][j].pollutionResistance = pollutionResistancePerState[(int)tiles[i][j].state - 1];
                tiles[i][j].populationResistance = populationResistancePerState[(int)tiles[i][j].state - 1];
                tiles[i][j].maxFood = foodPerState[(int)tiles[i][j].state - 1];
                tiles[i][j].food = foodPerState[(int)tiles[i][j].state - 1];
                tiles[i][j].foodFloat = foodPerState[(int)tiles[i][j].state - 1];

            }
        }

    }

    /// <summary>
    /// Update the population, radiation, and pollution field of the UI with the sent values 
    /// </summary>
    public void UpdateUI(int population, bool radiation, bool pollution, int livability, int food, float populationProgression, float foodProgression) {
        this.population.text = population.ToString();
        this.livability.text = livability.ToString();
        this.radiation.text = radiation? "Irradiated" : "Not irradiated";
        this.pollution.text = pollution? "Poluted" : "Not poluted";
        this.food.text = food.ToString();

        // population completion
        Image activeImage = populationProgression > 0 ? populationCompletion : populationDecompletion;
        Image unactiveImage= populationProgression > 0 ? populationDecompletion : populationCompletion;

        Color color = unactiveImage.color;
        color.a = 0;
        unactiveImage.color = color;

        color = activeImage.color;
        color.a = 1;
        activeImage.color = color;

        Vector3 scale = activeImage.GetComponent<RectTransform>().localScale;
        scale.x = populationProgression;
        activeImage.GetComponent<RectTransform>().localScale = scale;

        // food completion 
        activeImage = foodProgression> 0 ? foodCompletion : foodDecompletion;
        unactiveImage = foodProgression > 0 ? foodDecompletion : foodCompletion;

        color = unactiveImage.color;
        color.a = 0;
        unactiveImage.color = color;

        color = activeImage.color;
        color.a = 1;
        activeImage.color = color;

        scale = activeImage.GetComponent<RectTransform>().localScale;
        scale.x = foodProgression;
        activeImage.GetComponent<RectTransform>().localScale = scale;
    }

    /// <summary>
    /// Istantiate an inhabitant marker on the specified tile 
    /// </summary>
    public void InstantiateInhabitants(Tile tile, int populationLevel) { 

        GameObject inhabitants = GameObject.Instantiate(inhabitantsPrefabs[populationLevel], tile.transform);
        Vector3 position = inhabitants.transform.localPosition;
        position.x = 0;
        position.y = 0;
        position.z = -1;
        inhabitants.transform.localPosition = position;

    }

    /// <summary>
    /// Istantiate an inhabitant source on the specified tile 
    /// </summary>
    public void InstantiateInhabitantSource(Tile tile) {

        tile.population = 10;
        tile.populationFloat = tile.population;

        UpdateInhabitants(tile);

    }

    /// <summary>
    /// Remove an inhabitant source on the specified tile 
    /// </summary>
    public void RemoveInhabitants(Tile tile) {
        Destroy(tile.transform.GetChild(0).gameObject);
    }

    /// <summary>
    /// Updates the prefab representing the inhabitant level on the specified tile 
    /// </summary>
    public void UpdateInhabitants(Tile tile) {

        int populationLevel = -1;
        if ( tile.population > 0 && tile.population < Mathf.RoundToInt(maxPopulationPerTile / 5f))
            populationLevel = 0;
        else if (tile.population >= Mathf.RoundToInt(maxPopulationPerTile / 5f) && tile.population < Mathf.RoundToInt(maxPopulationPerTile * 2f / 5f))
            populationLevel = 1;
        else if (tile.population >= Mathf.RoundToInt(maxPopulationPerTile * 2f / 5f) && tile.population < Mathf.RoundToInt(maxPopulationPerTile * 3f / 5f))
            populationLevel = 2;
        else if (tile.population >= Mathf.RoundToInt(maxPopulationPerTile * 3f / 5f) && tile.population < Mathf.RoundToInt(maxPopulationPerTile * 4f / 5f))
            populationLevel = 3;
        else if (tile.population >= Mathf.RoundToInt(maxPopulationPerTile * 4f / 5f) && tile.population <= maxPopulationPerTile)
            populationLevel = 4;

        if (populationLevel != tile.populationLevel && tile.populationLevel != -1) 
            RemoveInhabitants(tile);

        if(populationLevel != tile.populationLevel && populationLevel >= 0)
            InstantiateInhabitants(tile, populationLevel);
        
        tile.populationLevel = populationLevel;

       
    }
       
}
