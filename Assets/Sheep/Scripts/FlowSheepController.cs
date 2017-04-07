//#define CAPSULE

using UnityEngine;

public class FlowSheepController : MonoBehaviour
{
    // speedup
    private const float speedup = 30.0f;

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

    // disable fence drive if not on border
    private int minFenceBoundary;
    private int maxFenceBoundary;

    [HideInInspector]
    public float fenceRepulsion2;

    // neighbour interaction
    // beta - cohesion factor
    private const float beta = .8f;
    private const float r_o = 2.0f;

    // speed
    private float desiredV = .0f;
    private float v;
    private const float v_1 = 0.15f;
    private const float v_2 = 1.5f;

    // noise
    private const float eta = 0.13f;

    // cell
    [HideInInspector]
    public Cell currentCell;

    // debug parameters
    //private MeshRenderer meshRenderer;

    // update timers
    //private float stateUpdateInterval = .5f;
    //private float stateTimer;

    void Start()
    {
        // GameManager
        GM = FindObjectOfType<GameManager>();

        // interaction squared
        fenceRepulsion2 = fenceRepulsion * fenceRepulsion;
        minFenceBoundary = Mathf.CeilToInt(fenceRepulsion / GM.binSize);
        maxFenceBoundary = GM.precision - minFenceBoundary;

        // get mesh renderer for debug coloring purposes
        //meshRenderer = GetComponentInChildren<MeshRenderer>();
        //meshRenderer.material.color = new Color(1.0f, 1.0f, .0f);

        // random state
        sheepState = (Enums.SheepState)Random.Range(0, 3);

        // speed
        SetSpeed();
        desiredV = v;

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

        Vector3 newPosition = transform.position + (Time.deltaTime * v * newHeading * speedup);
        newPosition.y = .0f;

        transform.position = newPosition;
        transform.forward = newHeading;

#if !CAPSULE
        // Sheep state animation
        anim.SetBool("IsIdle", sheepState == Enums.SheepState.Idle);
        anim.SetBool("IsRunning", sheepState == Enums.SheepState.Running);
#endif
    }

    void UpdateState()
    {
        // test states
        float random = .0f;

        // first test the transition between idle and walking and viceversa
        if (sheepState == Enums.SheepState.Idle)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < currentCell.p_iw)
                sheepState = Enums.SheepState.Walking;
        }
        else if (sheepState == Enums.SheepState.Walking)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < currentCell.p_wi)
                sheepState = Enums.SheepState.Idle;
        }

        // second test the transition to running
        // which has the same rate regardless if you start from walking or idle
        if (sheepState == Enums.SheepState.Idle || sheepState == Enums.SheepState.Walking)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < currentCell.p_iwr)
                sheepState = Enums.SheepState.Running;
        }
        // while testing the transition to running also test the transition from running to standing
        else if (sheepState == Enums.SheepState.Running)
        {
            random = Random.Range(.0f, 1.0f);
            if (random < currentCell.p_ri)
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
        if (currentCell.x <= minFenceBoundary || currentCell.x >= maxFenceBoundary ||
          currentCell.y <= minFenceBoundary || currentCell.y >= maxFenceBoundary)
        {
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
        }

        if (sheepState == Enums.SheepState.Walking)
        {
            // grazefield
            desiredTheta += currentCell.grazingForce;

            // noise
            float eps = Random.Range(-Mathf.PI, Mathf.PI);
            desiredTheta += new Vector3(Mathf.Cos(eps), .0f, Mathf.Sin(eps)) * eta;
        }
        else
        {
            // cohesion field
            desiredTheta += currentCell.cohesionForce;
        }

        // separation
        foreach (FlowSheepController neighbour in currentCell.sheepList)
        {
            e_ij = neighbour.transform.position - transform.position;
            float dist = Vector3.Magnitude(e_ij);
            if (dist < r_o)
            {
                f_ij = (dist - r_o) / r_o;
                desiredTheta += beta * f_ij * e_ij.normalized;
            }
        }

        desiredTheta.Normalize();
    }
}