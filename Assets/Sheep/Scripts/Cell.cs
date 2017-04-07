using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    public int x;
    public int y;

    public Vector3 coordinates;
    public List<FlowSheepController> sheepList;
    public List<Cell> neighbourCells;

    public Vector3 grazingForce;
    public Vector3 cohesionForce;

    // TODO REWORKD THIS - probabilities
    private const float alpha = 15.0f;
    private const float delta = 4.0f;
    private const float tau_iw = 24.0f; // original 35.0f
    private const float tau_wi = 8.0f;
    private float tau_iwr;
    public float p_iw = .0f, p_wi = .0f, p_iwr = .0f, p_ri = .0f;

    // noise
    private const float eta = 0.13f;

    public Cell(Vector3 _coordinates, int _x, int _y, int _N)
    {
        x = _x;
        y = _y;

        coordinates = _coordinates;

        sheepList = new List<FlowSheepController>();
        neighbourCells = new List<Cell>();

        grazingForce = new Vector3();
        cohesionForce = new Vector3();

        tau_iwr = _N;
    }

    public void UpdateField(Vector3 centroid)
    {
        Vector3 offset = centroid - coordinates;
        UpdateProbabilities(offset.magnitude);

        Vector3 newGrazingForce = new Vector3();
        Vector3 newCohesionForce = new Vector3();

        // allign with sheep inside this cell
        foreach (FlowSheepController neighbour in sheepList)
        {
            newGrazingForce += neighbour.transform.forward;
        }

        // noise
        float eps = Random.Range(-Mathf.PI, Mathf.PI);
        newGrazingForce += new Vector3(Mathf.Cos(eps), .0f, Mathf.Sin(eps)) * eta;

        // cohesion towards centroid + towards local groups
        // TODO LOCAL COHESION
        newCohesionForce += offset;

        // set
        grazingForce = newGrazingForce.normalized;
        cohesionForce = newCohesionForce.normalized;
    }

    public void UpdateProbabilities(float d_c)
    {
        int n_running = 0, n_walking = 0, n_idle = 0;

        foreach (FlowSheepController fsc in sheepList)
        {
            switch (fsc.sheepState)
            {
                case Enums.SheepState.Idle:
                    n_idle++;
                    break;
                case Enums.SheepState.Walking:
                    n_walking++;
                    break;
                case Enums.SheepState.Running:
                    n_running++;
                    break;
            }
        }

        // probabilities
        p_iw = (1 + alpha * n_walking) / tau_iw;
        p_iw = 1 - Mathf.Exp(-p_iw);
        p_wi = (1 + alpha * n_idle) / tau_wi;
        p_wi = 1 - Mathf.Exp(-p_wi);

        p_iwr = (1 + alpha * n_running + d_c) / tau_iwr;
        p_iwr = 1 - Mathf.Exp(-p_iwr);

        /*
        p_ri = (1 / tau_ri) * Mathf.Pow((d_S / d_c) * (1 + alpha * n_idle), delta);
        p_ri = 1 - Mathf.Exp(-p_ri);
        */
    }
}
