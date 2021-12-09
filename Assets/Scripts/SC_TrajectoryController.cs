using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_TrajectoryController : MonoBehaviour
{
    public List<GameObject> obstacles;
    public int maxIterations;

    Scene currentScene;
    Scene predictionScene;

    PhysicsScene currentPhysicsScene;
    PhysicsScene predictionPhysicsScene;

    List<GameObject> dummyObstacles = new List<GameObject>();

    public LineRenderer lineRenderer;

    public List<Vector3> positions;


    GameObject dummy;
    public int dummyCollisions = 0;
    public int maxCollisions;
    public float[] bounces = new float[3];
    public float distanceTraveled = 0f;
    List<GameObject> balls;
    public GameObject TrajectoryPrefab;
    public float ballSpacing = 0.5f;


    private void Awake()
    {
        positions = new List<Vector3>();
        balls = new List<GameObject>();
        bounces = new float[maxCollisions];
        obstacles.Clear();
        foreach (Collider c in GameObject.FindObjectsOfType<Collider>())
        {
            if (c.gameObject != null)
            {
                if (c.CompareTag("Obstacle"))
                {
                    obstacles.Add(c.gameObject);
                }
            }
        }
        Physics.autoSimulation = false;

        currentScene = SceneManager.GetActiveScene();
        currentPhysicsScene = currentScene.GetPhysicsScene();

        CreateSceneParameters parameters = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        predictionScene = SceneManager.CreateScene("Prediction", parameters);
        predictionPhysicsScene = predictionScene.GetPhysicsScene();

        lineRenderer = GetComponent<LineRenderer>();
    }

    void FixedUpdate()
    {
        if (currentPhysicsScene.IsValid())
        {
            currentPhysicsScene.Simulate(Time.fixedDeltaTime);
        }
    }
    private void Update()
    {
        float width = lineRenderer.startWidth;
        lineRenderer.material.mainTextureScale = new Vector2(1f / width, 1.0f);
    }

    public void copyAllObstacles()
    {
        foreach (GameObject o in obstacles)
        {
            if (o.gameObject.GetComponent<Collider>() != null)
            {
                GameObject fakeT = Instantiate(o.gameObject);
                fakeT.transform.position = o.transform.position;
                fakeT.transform.rotation = o.transform.rotation;
                Renderer fakeR = fakeT.GetComponent<Renderer>();
                if (fakeR)
                {
                    fakeR.enabled = false;
                }
                SceneManager.MoveGameObjectToScene(fakeT, predictionScene);
                dummyObstacles.Add(fakeT);
            }

        }
    }

    void killAllObstacles()
    {
        foreach (var o in dummyObstacles)
        {
            Destroy(o);
        }
        dummyObstacles.Clear();
    }

    public void Predict(GameObject subject, Vector3 currentPosition, Vector3 force)
    {
        ClearPath();
        positions.Clear();

        dummyCollisions = 0;
        if (currentPhysicsScene.IsValid() && predictionPhysicsScene.IsValid())
        {
            if (dummy == null)
            {
                dummy = Instantiate(subject);
                SceneManager.MoveGameObjectToScene(dummy, predictionScene);
            }
            copyAllObstacles();
            dummy.GetComponent<SC_Ball>().isDummy = true;
            dummy.transform.position = currentPosition;
            dummy.GetComponent<Rigidbody>().AddForce(force);
            lineRenderer.positionCount = maxIterations;

            int t_dummyCollision = dummyCollisions;
            float distance = 0f;
            for (int i = 0; i < maxIterations; i++)
            {

                if (dummy.GetComponent<SC_Ball>().leftGround == false)
                {
                    dummy.GetComponent<SC_Ball>().leftGround = dummy.GetComponent<SC_Ball>().isInAir();
                }



                predictionPhysicsScene.Simulate(Time.fixedDeltaTime);

                positions.Add(dummy.transform.position - new Vector3(0, dummy.transform.localScale.y / 1.2f, 0));
                lineRenderer.SetPosition(i, dummy.transform.position - new Vector3(0, dummy.transform.localScale.y / 1.2f, 0));

                if (i != 0)
                {
                    distance += (positions[i - 1] - positions[i]).magnitude;
                }


                if (dummy.GetComponent<SC_Ball>().hasHit)
                {
                    bounces[dummyCollisions - 1] = distance;
                }

                dummy.GetComponent<SC_Ball>().hasHit = false;

                if (dummyCollisions >= maxCollisions)
                {
                    lineRenderer.positionCount = i;
                    break;
                }
            }
            distanceTraveled = distance;
            SetLineColors();
            MakePathBalls();
            Destroy(dummy);
        }
        killAllObstacles();
        SetPathBallMaterial();



    }

    [ContextMenu("Balls")]
    void MakePathBalls()
    {
        float spacing = 0f;
        for (int i = 0; i < positions.Count; i++)
        {
            if (i != 0)
            {
                spacing += (positions[i - 1] - positions[i]).magnitude;
                if (spacing >= ballSpacing)
                {
                    balls.Add(Instantiate(TrajectoryPrefab, positions[i], Quaternion.identity));
                    spacing = 0f;
                }
            }

        }
    }
    void SetPathBallMaterial()
    {
        //add color or transparency
        float f = balls.Count;

        int i = 0;
        foreach (GameObject item in balls)
        {
            i++;
            Color c = item.gameObject.GetComponent<MeshRenderer>().material.color;
            float t =((f+(f/0.3f)) / (float)i)/f;
            Debug.Log(t);
            c.a = t;
            item.gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", c);

        }

    }
    void ClearPathBalls()
    {
        foreach (GameObject b in balls)
        {
            Destroy(b);
        }
        balls.Clear();
    }


    void SetLineColors()
    {

    }
    void OnDestroy()
    {
        killAllObstacles();
    }

    public void ClearPath()
    {
        ClearPathBalls();
        lineRenderer.positionCount = 0;
    }
}
