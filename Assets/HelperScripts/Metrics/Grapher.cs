using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

public class Grapher : MonoBehaviour
{
  [Header("Settings")]
  public int resolution;
  public float graphLength;
  public float graphHeight;
  public float pointSize;
  public float refreshRate;

  [Header("Plots")]
  public bool plotStates;
  public bool plotAreas;

  // helper variables
  private float increment;
  private float refreshTimer;

  // system
  private ParticleSystem PS;

  // metrics manager
  private MetricsManager MM;

  // points
  private List<float> areas;
  private List<Vector3> states;
  private List<ParticleSystem.Particle> particlePoints;

  void Start()
  {
    // metrics manager
    MM = GetComponent<MetricsManager>();

    // ParticleSysten
    PS = GetComponent<ParticleSystem>();

    // inits
    areas = new List<float>();
    states = new List<Vector3>();
    particlePoints = new List<ParticleSystem.Particle>();
    increment = graphLength / resolution;

    // timer
    refreshTimer = refreshRate;
  }

  private void Update()
  {
    refreshTimer -= Time.deltaTime;

    ParticleSystem.Particle point;

    // update if refresh
    if (refreshTimer < .0f)
    {
      refreshTimer = refreshRate;

      particlePoints.Clear();

      if (plotAreas)
      {
        areas.Add(MM.currentArea);

        if (areas.Count > resolution)
          areas.RemoveAt(0);

        // create particle points
        for (int i = 0; i < areas.Count; i++)
        {
          point = new ParticleSystem.Particle();
          point.position = new Vector3(i * increment, .0f, areas[i] * graphHeight);
          point.startColor = new Color(.0f, .0f, .0f);
          point.startSize = pointSize;

          particlePoints.Add(point);
        }
      }

      if (plotStates)
      {
        states.Add(MM.currentState);

        if (states.Count > resolution)
          states.RemoveAt(0);

        // create particle points
        for (int i = 0; i < states.Count; i++)
        {
          // running
          point = new ParticleSystem.Particle();
          point.position = new Vector3(i * increment, .0f, states[i].x * graphHeight);
          point.startColor = new Color(1.0f, .0f, .0f);
          point.startSize = pointSize;
          particlePoints.Add(point);

          // walking
          point = new ParticleSystem.Particle();
          point.position = new Vector3(i * increment, .0f, states[i].y * graphHeight);
          point.startColor = new Color(.0f, 1.0f, .0f);
          point.startSize = pointSize;
          particlePoints.Add(point);

          // idle
          point = new ParticleSystem.Particle();
          point.position = new Vector3(i * increment, .0f, states[i].z * graphHeight);
          point.startColor = new Color(.0f, .0f, 1.0f);
          point.startSize = pointSize;
          particlePoints.Add(point);
        }
      }

      PS.SetParticles(particlePoints.ToArray(), particlePoints.Count);
    }
  }
}