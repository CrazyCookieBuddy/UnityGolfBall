using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class SC_Ball : MonoBehaviour
{

    SC_TrajectoryController TC;
    Rigidbody rb;
    public SC_TrajectoryController PManager;
    int stoppingCounter;
    bool isActivePlayer = false;
    public bool isDummy = false;
    public bool hasHit = false;
    public Vector3 v = new Vector3(1, 1, 0);

    public float maxForce = 2000;
    float forceIncrementSize = 250;
    public float force;
    public bool useMaxForce = false;
    float aimRotation = 0;
    public float stepSize = 2;
    public float radius;
    public bool canShoot;
    bool startOfRoll = false;
    float stopBelowVelo = 0.136f;
    float initialShotWaitTime = 0.25f;


    float MoveAxis;

    public bool leftGround;
    public Modes mode = Modes.Short;
    public LayerMask layer;

    public float overlapResize = 1.4f;


    public enum Modes
    {
        Short,
        Long,
        Jump
    }

    private void Awake()
    {
        layer = 1 << 8;
        layer = ~layer;
        if (TC == null)
        {
            TC = GetComponent<SC_TrajectoryController>();
        }
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();

        }
        if (PManager == null)
        {
            GameObject d = GameObject.Find("PredictionManager");
            PManager = d.GetComponent<SC_TrajectoryController>();
        }
    }

    public void Update()
    {
        if (!isActivePlayer)
        {
            PauseBall();
        }
        if (!canShoot && !startOfRoll)
        {
            if (rb.velocity.magnitude > stopBelowVelo)
            {
                //still rolling
            }
            else
            {
                rb.constraints = RigidbodyConstraints.FreezeAll;
                rb.velocity = Vector3.zero;
                canShoot = true;
            }
        }
    }

    public void PauseBall()
    {
        
    }
    public bool isInAir()
    {
        bool b = false;
        //Collider[] hitCollider = Physics.OverlapSphere(rb.position, rb.transform.localScale.x * overlapResize, layer);
        float r = rb.transform.localScale.x / 2f;
        Collider[] hitCollider = Physics.OverlapBox(rb.position, new Vector3(r, overlapResize, r), Quaternion.identity, layer);
        if (hitCollider.Length > 0)
        {
            b = true;
        }
        return !b;

    }
    public void DebugShoot(InputAction.CallbackContext context)
    {

        if (context.phase == InputActionPhase.Canceled)
        {
            if (canShoot)
            {
                startOfRoll = true;
                StartCoroutine(initialShotWait(initialShotWaitTime));
                rb.constraints = RigidbodyConstraints.None;
                rb.AddForce(v);
                PManager.ClearPath();
                canShoot = false;
            }
        }
    }
    public void RotateAim(InputAction.CallbackContext context)
    {
        MoveAxis = context.ReadValue<float>();

        if (context.phase == InputActionPhase.Started)
        {
            if (MoveAxis != 0)
            {

                Rotate(MoveAxis, stepSize);
                setV(force);
            }
        }
        if (context.phase == InputActionPhase.Canceled)
        {
        }
    }
    void clampForce()
    {
        force = Mathf.Clamp(force, 0, maxForce);
    }
    public void ModeSwitch(InputAction.CallbackContext context)
    {

        if (context.phase == InputActionPhase.Started)
        {
            mode = (Modes)((int)(mode + 1) % System.Enum.GetNames(typeof(Modes)).Length);
            setV(force);
            DrawPreview();
        }
    }
    void Rotate(float dir, float deg)
    {
        aimRotation += deg * dir;
        setV(force);
        DrawPreview();

    }
    public void Force(InputAction.CallbackContext context)
    {
        float MoveAxis = context.ReadValue<float>();
        if (context.phase == InputActionPhase.Started)
        {
            force += forceIncrementSize * MoveAxis;
            clampForce();
            setV(force);
            DrawPreview();
        }
    }
    void DrawPreview()
    {
        rb.constraints = RigidbodyConstraints.None;
        float x = Mathf.Cos(aimRotation * Mathf.Deg2Rad) * (useMaxForce == true ? maxForce : force);
        float z = Mathf.Sin(aimRotation * Mathf.Deg2Rad) * (useMaxForce == true ? maxForce : force);
        PManager.Predict(gameObject, gameObject.transform.position, new Vector3(x, mode == Modes.Jump ? (useMaxForce == true ? maxForce : force) : 0, z));
        rb.constraints = RigidbodyConstraints.FreezeAll;

    }
    //void DrawPreview(Vector3 v2)
    //{
    //    rb.constraints = RigidbodyConstraints.None;
    //    PManager.Predict(gameObject, gameObject.transform.position, v2);
    //    rb.constraints = RigidbodyConstraints.FreezeAll;
    //}

    void OnCollisionEnter(Collision collision)
    {
        float d = Vector3.Dot(collision.contacts[0].normal, Vector3.up);

        if (mode != Modes.Jump)
        {
            if (leftGround)
            {
                PManager.dummyCollisions = PManager.maxCollisions;
                return;
            }
        }


        switch (d)
        {
            case 1.0f:
                //floor
                break;
            case 0.0f:
                //wall
                break;
            default:
                //else
                break;
        }
        if (isDummy && !hasHit && leftGround)
        {
            hasHit = true;
            PManager.dummyCollisions++;
            return;
        }
    }

    void setV(float f)
    {
        float x = Mathf.Cos(aimRotation * Mathf.Deg2Rad) * f;
        float z = Mathf.Sin(aimRotation * Mathf.Deg2Rad) * f;
        v = new Vector3(x, mode == Modes.Jump ? f : 0, z);
    }
    IEnumerator initialShotWait(float t)
    {
        startOfRoll = true;
        yield return new WaitForSeconds(t);
        startOfRoll = false;
    }
}
