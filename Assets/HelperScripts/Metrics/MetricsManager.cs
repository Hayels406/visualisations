using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

public class MetricsManager : MonoBehaviour
{
    // save metrics
    [Header("Settings")]
    public float initiationTime;
    public float refreshRate;
    public bool printStates;
    public bool saveFile;
    public bool fpsOnly;

    // helper variables
    private string fileName;
    private int frame = 0;
    private float refreshTimer;
    private float initiationTimer;
    private float fps;
    private float p_iw, p_wi, p_iwr, p_ri;

    // gamemanager
    private GameManager GM;

    // metrics lists
    [HideInInspector]
    public float currentArea = -1.0f;
    [HideInInspector]
    public Vector3 currentState;

    // Use this for initialization
    void Start()
    {
        // GameManager 
        GM = FindObjectOfType<GameManager>();

        // init timer
        initiationTimer = .0f;

        if (fpsOnly)
        {
            // name
            fileName = "DataAnalysis\\FPS_" + GM.nOfSheep + "_" + GM.sheepBehaviour + ".csv";

            using (StreamWriter sw = File.CreateText(fileName))
            {
                sw.WriteLine("frame,fps");
            }
        }
        else if (saveFile)
        {
            // name
            fileName = "DataAnalysis\\" + GM.nOfSheep + "_" + GM.sheepBehaviour + ".csv";

            using (StreamWriter sw = File.CreateText(fileName))
            {
                sw.WriteLine("frame,running,walking,idle,S,dS,p_iw,p_wi,p_iwr,p_ri,fps");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        initiationTimer += Time.deltaTime;
        if (initiationTimer > initiationTime)
        {
            fps = 1.0f / Time.deltaTime;
            refreshTimer -= Time.deltaTime;

            if (refreshTimer < .0f)
            {
                frame++;
                refreshTimer = refreshRate;

                if (fpsOnly)
                {
                    using (StreamWriter sw = File.AppendText(fileName))
                    {
                        sw.WriteLine(frame + "," + fps);
                    }
                }
                else
                {
                    float newArea = CalculateArea();

                    // for correct dS in frame 1
                    if (currentArea < .0f)
                        currentArea = newArea;

                    CountSheep();

                    if (printStates)
                    {
                        Debug.Log("R: " + currentState.x + "   W: " + currentState.y + "   I: " + currentState.z);
                    }

                    // print
                    // save to file
                    if (saveFile)
                    {
                        using (StreamWriter sw = File.AppendText(fileName))
                        {
                            sw.WriteLine(frame + "," + currentState.x + "," + currentState.y
                              + "," + currentState.z + "," + newArea + "," + (newArea - currentArea) +
                              "," + p_iw + "," + p_wi + "," + p_iwr + "," + p_ri + "," + fps);
                        }
                    }

                    currentArea = newArea;
                }
            }
        }
    }

    private float CalculateArea()
    {
        // get points
        List<Vector3> positions = new List<Vector3>();

        if (GM.sheepBehaviour == Enums.SheepBehaviour.Individual)
        {
            foreach (SheepController sc in GM.sheepList)
            {
                positions.Add(sc.transform.localPosition);
            }
        }
        else if (GM.sheepBehaviour == Enums.SheepBehaviour.Flow)
        {
            foreach (FlowSheepController fsc in GM.flowSheepList)
            {
                positions.Add(fsc.transform.localPosition);
            }
        }

        // convex hull
        List<Vector3> hull = ConvexHull.convexHull(positions);

        // area
        float area = ConvexHull.PolygonArea(hull);

        return area / (GM.fieldSize * GM.fieldSize);
    }

    private void CountSheep()
    {
        float nOfRunning = .0f, nOfWalking = .0f, nOfIdle = .0f;

        p_iw = .0f;
        p_wi = .0f;
        p_iwr = .0f;
        p_ri = .0f;

        if (GM.sheepBehaviour == Enums.SheepBehaviour.Individual)
        {
            foreach (SheepController sc in GM.sheepList)
            {
                if (sc.sheepState == Enums.SheepState.Running)
                    nOfRunning++;
                else if (sc.sheepState == Enums.SheepState.Walking)
                    nOfWalking++;
                else
                    nOfIdle++;

                // probabilities
                p_iw += sc.p_iw;
                p_wi += sc.p_wi;
                p_iwr += sc.p_iwr;
                p_ri += sc.p_ri;
            }

            // normalize
            p_iw /= GM.nOfSheep;
            p_wi /= GM.nOfSheep;
            p_iwr /= GM.nOfSheep;
            p_ri /= GM.nOfSheep;
        }
        else if (GM.sheepBehaviour == Enums.SheepBehaviour.Flow)
        {
            foreach (FlowSheepController fsc in GM.flowSheepList)
            {
                if (fsc.sheepState == Enums.SheepState.Running)
                    nOfRunning++;
                else if (fsc.sheepState == Enums.SheepState.Walking)
                    nOfWalking++;
                else
                    nOfIdle++;
            }

            int n = 0;
            for (int i = 0; i < GM.precision; i++)
            {
                for (int j = 0; j < GM.precision; j++)
                {
                    if (GM.forceField[i, j].sheepList.Count > 0)
                    {
                        n++;

                        // probabilities
                        p_iw += GM.forceField[i, j].p_iw;
                        p_wi += GM.forceField[i, j].p_wi;
                        p_iwr += GM.forceField[i, j].p_iwr;
                        p_ri += GM.forceField[i, j].p_ri;
                    }
                }
            }

            // normalize
            p_iw /= n;
            p_wi /= n;
            p_iwr /= n;
            p_ri /= n;
        }

        currentState = new Vector3(nOfRunning / GM.nOfSheep, nOfWalking / GM.nOfSheep, nOfIdle / GM.nOfSheep);
    }
}
