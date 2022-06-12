using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour {

    public enum State {
        Null,
        Forest, 
        Meadow, 
        Water, 
        Desert, 
        Moutain
    }

    public GameManager gameManager; 

    public State state; 

    public bool mouseOver = false;

    private SpriteRenderer spriteRenderer;

    public bool pollutionSource = false;
    public bool pollution = false;
    public float pollutionResistance = 0;
    public int pollutionDistance = -1;
    private bool pollutionBuffer = false;
    private int pollutionDistanceBuffer = -1;

    public int population = 0; 
    public float populationFloat = 0f; 
    public float populationResistance = 0;
    public int populationLevel = -1; 
    private float populationBuffer = 0;

    public int livability = 100;

    public bool radiation = false;

    public int maxFood;
    public int food;
    public float foodFloat;
    public float foodBuffer;

    private void OnMouseEnter() {
        mouseOver = true;
    }

    private void OnMouseExit() {
        mouseOver = false; 
    }

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        food = maxFood;
    }

    private void Update() {

        if (gameManager.displayPollution && pollution) spriteRenderer.color = Color.red;
        else SetState(state);

        Color color = spriteRenderer.color;
        if (mouseOver) {
            color.a = 0.75f;
            gameManager.UpdateUI(population, radiation, pollution, livability, food, populationFloat - population, foodFloat - food);
        }
        else color.a = 1f;
        spriteRenderer.color = color;

        if(mouseOver && Input.GetMouseButtonDown(1)) {
            OnClickTile();
        }

    }

    /// <summary>
    /// Associates a state to a color
    /// </summary>
    /// <param name="state">A state</param>
    /// <returns>A color/returns>
    public Color StateToColor(State state) {

        return state switch {
            State.Water => new Color(60 / 255f, 46 / 255f, 229 / 255f),
            State.Desert => new Color(212 / 255f, 167 / 255f, 49 / 255f),
            State.Forest => new Color(10 / 255f, 70 / 255f, 13 / 255f),
            State.Meadow => new Color(72 / 255f, 180 / 255f, 78 / 255f),
            State.Moutain => new Color(110 / 255f, 110 / 255f, 110 / 255f),
            _ => Color.black,
        };

    }

    /// <summary>
    /// Associates a color to a state
    /// </summary>
    /// <param name="color">A color</param>
    /// <returns>A state</returns>
    public State ColorToState(Color color) {

        Vector3 colorVector = new Vector3(color.r, color.g, color.b);
        if (colorVector.Equals(new Vector3(60 / 255f, 46 / 255f, 229 / 255f)))
            return State.Water;
        else if (colorVector.Equals(new Vector3(212 / 255f, 167 / 255f, 49 / 255f)))
            return State.Desert;
        else if (colorVector.Equals(new Vector3(10 / 255f, 70 / 255f, 13 / 255f)))
            return State.Forest;
        else if (colorVector.Equals(new Vector3(72 / 255f, 180 / 255f, 78 / 255f)))
            return State.Meadow;
        else if (colorVector.Equals(new Vector3(110 / 255f, 110 / 255f, 110 / 255f)))
            return State.Moutain;
        else 
            return State.Null;

    }

    /// <summary>
    /// Returns the number of element in the state enum 
    /// </summary>
    public static int GetStateEnumSize() {

        string[] names = State.GetNames(typeof(State));
        return names.Length;

    }

    /// <summary>
    /// Set the state value 
    /// </summary>
    /// <param name="state">New state value</param>
    public void SetState(State state) {
        this.state = state;
        spriteRenderer.color = StateToColor(state);
    }

    /// <summary>
    /// Update the tile following the Life mode (Game of Life)
    /// </summary>
    /// <param name="tiles">8 Neighbour tiles</param>
    public void UpdateGame(List<Tile> tiles, int pollutionMaxDistance, int pollutionSourceStrength, int populationMaxDistance) {

        pollutionBuffer = false;
        pollutionDistanceBuffer = -1;
        int pollutionCounter = 0;
        int minPollutionDistance = int.MaxValue;
        
        int populationCounter = 0;
        int populationTileCounter = 0;

        // Neighbour tiles 
        foreach (Tile tile in tiles) {
            if (tile) {

                if(tile.pollution) {
                    if (!tile.pollutionSource) pollutionCounter++;
                    else pollutionCounter += pollutionSourceStrength;

                    if (tile.pollutionDistance < minPollutionDistance)
                        minPollutionDistance = tile.pollutionDistance;
                }

                if(tile.population > 0) {
                    populationCounter += tile.population / 5;
                    populationTileCounter++;
                    if (tile.population > 50) populationTileCounter += 1;
                    if (tile.population == 100) populationTileCounter += 1;
                } 
                
            }
        }

        // Pollution 
        if (!pollutionSource && pollutionCounter > pollutionResistance && minPollutionDistance + 1 <= pollutionMaxDistance) {
            pollutionBuffer = true;
            pollutionDistanceBuffer = minPollutionDistance + 1;
        } 

        // Livability 
        livability = 100;
        if (pollutionSource) livability -= 45;
        else if (pollution) livability -= 15;

        livability = Mathf.Max(livability - pollutionCounter * (gameManager.pollutionMaxDistance + 1 - pollutionDistance), 0);
        livability -= Mathf.RoundToInt((1 - ((float)food / maxFood)) * 70);

        if (livability < 0) livability = 0;

        // Population 
        populationCounter = populationTileCounter > populationResistance ? populationCounter + population : population;
        populationBuffer = populationCounter * ((livability - 50) * 2 / (10000f * (populationResistance + 1)));

        // Food
        foodBuffer = ((40 - population)  - (pollution ? 50 : 0)) / 100f;

    }

    /// <summary>
    /// Apply the computer update for the Game mode 
    /// </summary>
    public void ApplyUpdateGame(int maxPopulationPerTile) {

        // Pollution
        pollution = pollutionSource ? true : pollutionBuffer;
        pollutionDistance = pollutionSource ? 0 : pollutionDistanceBuffer;

        // Population
        populationFloat = Mathf.Clamp(populationFloat + populationBuffer, 0, maxPopulationPerTile);
        if (populationFloat >= population)
            population = Mathf.FloorToInt(populationFloat);
        else
            population = Mathf.CeilToInt(populationFloat);

        // Food
        foodFloat = Mathf.Clamp(foodFloat + foodBuffer, 0, maxFood);
        if (foodFloat >= food)
            food = Mathf.FloorToInt(foodFloat);
        else
            food = Mathf.CeilToInt(foodFloat);
    }

    /// <summary>
    /// On Click function 
    /// </summary>
    public void OnClickTile() {
        if(state != State.Water)
        gameManager.InstantiateInhabitantSource(this);
    }


}
