using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.WSA;




public class scr_sword : MonoBehaviour
{
    
    //Testing
    [SerializeField]
    private bool debug = true;

    [SerializeField]
    private Vector3 tpPosition = new Vector3(0, 3, 0);

    [SerializeField]
    private Quaternion tpRotation = new Quaternion(30, 0, -180, 0);
    public TextMeshProUGUI debugText; 


    //Components
    private CapsuleCollider hiltHitbox;
    private CapsuleCollider swordHitbox;
    private CapsuleCollider pointHitbox;


    private Rigidbody rigidbody;
    private Camera camera;
    private Quaternion baseCamRot;
    private SpriteRenderer arrow;

    //Collision
    [SerializeField, Range(0.005f, 0.1f)]
    public float swordSharpness = 1.0f;
    private float swordTimer = 0f;
    private bool swordEnteringTerrain;

    //Launching
    private bool launchCharging;
    private float launchTimer = 0;
    public float minLaunch = 1f;
    public float maxLaunch = 3f;
    

    //Controls
    private KeyCode pullBack = KeyCode.S;


    // Start is called before the first frame update
    void Start()
    {
        InitialiseComponents();
    }

    
    void InitialiseComponents() {
        CapsuleCollider[] cylinders = GetComponentsInChildren<CapsuleCollider>();

        //Skip our own capsule collider by starting at index 1
        hiltHitbox = cylinders[0];
        swordHitbox = cylinders[1];
        pointHitbox = cylinders[2];

        rigidbody = GetComponent<Rigidbody>();

        camera = GetComponentInChildren<Camera>();
        baseCamRot = camera.transform.rotation;

        arrow = GetComponentInChildren<SpriteRenderer>();

    }

    // Update is called once per frame
    void Update() {
        DebugCommands();

        UserInput();

        ProcessLaunchCharging();
    }
    void FixedUpdate()
    {
        ProcessCollision();
    }

    void LateUpdate() {
        camera.transform.rotation = baseCamRot;
    }

    void DebugCommands() {
        if (debug) {
            if (Input.GetKeyUp(KeyCode.Tab)) {
                transform.SetPositionAndRotation(tpPosition, tpRotation);
            }

            if (Input.GetKeyUp(KeyCode.Return)) {
                swordHitbox.enabled = true;
            }

            debugText.text = "Velocity: " + rigidbody.velocity;

        }
    }

    void UserInput() {
        if (Input.GetKeyDown(pullBack)) {
            arrow.enabled = true;
            launchCharging = true;
            launchTimer = 0;
        }


        if (Input.GetKeyUp(pullBack)) {
            arrow.enabled = false;
            launchCharging = false;

            if (launchTimer > minLaunch) {
                Launch();
            }
        }
    }

    void ProcessLaunchCharging() {
        if (launchCharging) {
            launchTimer += Time.deltaTime;


            //POTENTIALLY INEFFICENT
            if (launchTimer > maxLaunch) {
                launchTimer = maxLaunch;
            }


            float arrowLength = Mathf.Lerp(0.05f, 0.3f, launchTimer/maxLaunch);
            arrow.transform.localScale = new Vector3(arrow.transform.localScale.x, arrowLength,0);
        }
    }

    //If the sword is entering a piece of terrain, increase the timer before its hitbox is renabled
    void ProcessCollision() {
        if (swordEnteringTerrain) {
            swordTimer += Time.deltaTime;

            if (swordTimer > swordSharpness) {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.useGravity = false;

                //wordHitbox.enabled = true;
                swordEnteringTerrain = false;
            }
        }
    }

    void OnTriggerEnter(UnityEngine.Collider other)
    {
        Debug.Log("Colliding with " + other.name);


        //Set the sword hitbox to no longer collide
        swordHitbox.enabled = false;
        swordEnteringTerrain = true;

        swordTimer = 0f;
    }

    void OnTriggerExit(UnityEngine.Collider other)
    {
        Debug.Log("Uncolliding with " + other.name);

        swordHitbox.enabled = true;
        rigidbody.useGravity = true;


    }

    void Launch() {
        rigidbody.AddForce(new Vector3(200 * launchTimer, 100 * launchTimer, 0), ForceMode.Force);
    }
}
