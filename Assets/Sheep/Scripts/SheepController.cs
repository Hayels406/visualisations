//#define CAPSULE

using UnityEngine;
using System.Collections.Generic;

public class SheepController : MonoBehaviour
{
    // speedup
    private const float speedup = 1.0f;

    // id
    [HideInInspector]
    public int id;

    // state
    [HideInInspector]
    public Enums.SheepState sheepState;

#if !CAPSULE
    // Sheeps Animator Controller
    public Animator anim;

    // Fur parts
    public Renderer[] sheepCottonParts;
#endif

    // GameManager
    private GameManager GM;

    // heading nad postion
    private Vector3 desiredTheta;

    // fence interacion
    private const float fenceWeight = .2f;
    private const float fenceRepulsion = 5.0f;
    [HideInInspector]
    public float fenceRepulsion2;

    // neighbour interaction
    private const float r_o = 2.0f;
    private const float r_e = 2.0f;
    [HideInInspector]
    public float r_o2;

    // speed
    private float desiredV = .0f;
    private float v;
    private const float v_1 = 0.15f;
    private const float v_2 = 1.5f;

    // noise
    private const float eta = 0.13f;

    // beta - cohesion factor
    private const float beta = .8f;

    // alpha, delta, transition parameters
    private const float alpha = 15.0f;
    private const float delta = 10.0f;
    private const float tau_iw = 24.0f; // original 35.0f
    private const float tau_wi = 8.0f;
    private float tau_iwr;
    private float tau_ri;
    private const float d_R = 31.6f;
    private const float d_S = 6.3f;
    private int n_idle = 0, n_walking = 0, m_idle = 0, m_running = 0;
    float l_i = .0f;

    // probabilities
    [HideInInspector]
    public float p_iw, p_wi, p_iwr, p_ri;

    // neighbour list
    [HideInInspector]
    public List<SheepController> metricNeighbours = new List<SheepController>();
    [HideInInspector]
    public List<SheepController> voronoiNeighbours = new List<SheepController>();

    // debug parameters
    //private MeshRenderer meshRenderer;

    // update timers
    //private float stateUpdateInterval = .0f;
    //private float stateTimer;

    void Start()
    {
        // GameManager
        GM = FindObjectOfType<GameManager>();

        // interaction squared
        fenceRepulsion2 = fenceRepulsion * fenceRepulsion;
        r_o2 = r_o * r_o;

        // get mesh renderer for debug coloring purposes
        //meshRenderer = GetComponentInChildren<MeshRenderer>();
        //meshRenderer.material.color = new Color(1.0f, 1.0f, .0f);

        // random state
        sheepState = (Enums.SheepState)Random.Range(0, 3);

        // speed
        SetSpeed();
        desiredV = v;

        // transition parameters
        tau_iwr = GM.nOfSheep;
        tau_ri = GM.nOfSheep;

        // random heading
        float theta = Random.Range(-Mathf.PI, Mathf.PI);
        transform.forward = new Vector3(Mathf.Cos(theta), .0f, Mathf.Sin(theta)).normalized;
        desiredTheta = transform.forward;

        // timer
        //stateTimer = Random.Range(.0f, stateUpdateInterval);

#if !CAPSULE
        Color cottonColor = Color.white;

        // Assign random collor to fur
        if (Random.value < .05f)
        {
            float blackShade = Random.Range(0.2f, 0.3f);
            cottonColor = new Color(blackShade, blackShade, blackShade, 1.0f);
        }
        else
        {
            float grayShade = Random.Range(0.7f, .9f);
            cottonColor = new Color(grayShade, grayShade, grayShade, 1.0f);
        }

        foreach (Renderer fur in sheepCottonParts)
        {
            if (fur.materials.Length < 2) fur.material.color = cottonColor;
            else fur.materials[1].color = cottonColor;
        }
#endif
    }

    void SetSpeed()
    {
        // debug coloring and speed
        switch (sheepState)
        {
            case Enums.SheepState.Idle:
                desiredV = .0f;
                //meshRenderer.material.color = new Color(1.0f, 1.0f, .0f);
                break;
            case Enums.SheepState.Walking:
                desiredV = v_1;
                //meshRenderer.material.color = new Color(.0f, .0f, 1.0f);
                break;
            case Enums.SheepState.Running:
                desiredV = v_2;
                //meshRenderer.material.color = new Color(1.0f, .0f, .0f);
                break;
        }
    }

    void Update()
    {
        //stateTimer -= Time.deltaTime;
        // state update
        //if (stateTimer < 0)
        //{
        UpdateState();
        //  stateTimer = stateUpdateInterval;
        //}

        // drives update
        // only change speed and heading if not idle
        if (sheepState == Enums.SheepState.Walking || sheepState == Enums.SheepState.Running)
            DrivesUpdate();

        Vector3 newHeading = desiredTheta;
        newHeading.y = .0f;

        float deltaV = desiredV - v;
        v += Mathf.Abs(deltaV) * Mathf.Sign(deltaV);

        Vector3 newPosition = transform.position + (Time.deltaTime * v * newHeading * GM.SpeedUp);
        newPosition.y = .0f;

        transform.position = newPosition;
        if (newHeading != Vector3.zero)
            transform.forward = newHeading;

#if !CAPSULE
        // Sheep state animation
        anim.SetBool("IsIdle", sheepState == Enums.SheepState.Idle);
        anim.SetBool("IsRunning", sheepState == Enums.SheepState.Running);
#endif
    }

    void NeighboursUpdate()
    {
        n_idle = 0;
        n_walking = 0;
        m_idle = 0;
        m_running = 0;

        l_i = .0f;

        foreach (SheepController neighbour in metricNeighbours)
        {
            // state counter
            switch (neighbour.sheepState)
            {
                case Enums.SheepState.Idle:
                    n_idle++;
                    break;
                case Enums.SheepState.Walking:
                    n_walking++;
                    break;
            }
        }

        foreach (SheepController neighbour in voronoiNeighbours)
        {
            // state count
            switch (neighbour.sheepState)
            {
                case Enums.SheepState.Idle:
                    m_idle++;
                    break;
                case Enums.SheepState.Running:
                    m_running++;
                    break;
            }

            // mean distance to topologic calculate
            l_i += (transform.position - neighbour.transform.position).magnitude;
        }
        // divide with number of topologic
        if (voronoiNeighbours.Count > 0)
            l_i /= voronoiNeighbours.Count;
        else
            l_i = .0f;
    }

    void UpdateState()
    {
        // refresh numbers of neighbours
        NeighboursUpdate();

        // probabilities
        p_iw = (1 + alpha * n_walking) / tau_iw;
        p_iw = 1 - Mathf.Exp(-p_iw);

        p_wi = (1 + alpha * n_idle) / tau_wi;
        p_wi = 1 - Mathf.Exp(-p_wi);

        p_iwr = .0f;
        p_ri = .0f;

        if (l_i > .0f)
        {
            p_iwr = (1 / tau_iwr) * Mathf.Pow((l_i / d_R) * (1 + alpha * m_running), delta);
            p_iwr = 1 - Mathf.Exp(-p_iwr);
            p_ri = (1 / tau_ri) * Mathf.Pow((d_S / l_i) * (1 + alpha * m_idle), delta);
            p_ri = 1 - Mathf.Exp(-p_ri);
        }

        // test states
        float random = .0f;

        // first test the transition between idle and walking and viceversa
        if (sheepState == Enums.SheepState.Idle)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < p_iw)
                sheepState = Enums.SheepState.Walking;
        }
        else if (sheepState == Enums.SheepState.Walking)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < p_wi)
                sheepState = Enums.SheepState.Idle;
        }

        // second test the transition to running
        // which has the same rate regardless if you start from walking or idle
        if (sheepState == Enums.SheepState.Idle || sheepState == Enums.SheepState.Walking)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < p_iwr)
                sheepState = Enums.SheepState.Running;
        }
        // while testing the transition to running also test the transition from running to standing
        else if (sheepState == Enums.SheepState.Running)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < p_ri)
                sheepState = Enums.SheepState.Idle;
        }

        SetSpeed();
    }

    void DrivesUpdate()
    {
        desiredTheta = transform.forward;

        // declarations
        Vector3 e_ij, s_f;
        float f_ij, dot;

        Vector3 closestPoint;

        // fences repulsion
        foreach (Collider fenceCollider in GM.fenceColliders)
        {
            // get dist
            closestPoint = fenceCollider.ClosestPointOnBounds(transform.position);
            if ((transform.position - closestPoint).sqrMagnitude < fenceRepulsion2)
            {
                e_ij = closestPoint - transform.position;

                // parallel
                dot = Vector3.Dot(e_ij.normalized, transform.forward);
                s_f = transform.forward - (dot * e_ij);

                // repulsion
                f_ij = fenceRepulsion / Vector3.Magnitude(e_ij);

                desiredTheta += fenceWeight * ((f_ij * -e_ij.normalized) + s_f);
            }
        }

        if (sheepState == Enums.SheepState.Walking)
        {
            foreach (SheepController neighbour in metricNeighbours)
            {
                desiredTheta += neighbour.transform.forward;

                e_ij = neighbour.transform.position - transform.position;
                f_ij = (Vector3.Magnitude(e_ij) - r_o) / r_o;
                desiredTheta += beta * f_ij * e_ij.normalized;
            }

            // noise
            float eps = Random.Range(-Mathf.PI, Mathf.PI);
            desiredTheta += new Vector3(Mathf.Cos(eps), .0f, Mathf.Sin(eps)) * eta;
        }
        else
        {
            foreach (SheepController neighbour in voronoiNeighbours)
            {
                if (neighbour.sheepState == Enums.SheepState.Running)
                    desiredTheta += neighbour.transform.forward;

                e_ij = neighbour.transform.position - transform.position;
                f_ij = Mathf.Min(1.0f, ((Vector3.Magnitude(e_ij) - r_e) / r_e));
                desiredTheta += beta * f_ij * e_ij.normalized;
            }
        }

        desiredTheta.Normalize();
    }
}