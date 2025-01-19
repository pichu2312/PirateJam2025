using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.WSA;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;
using Quaternion = UnityEngine.Quaternion;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;


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
    public SpringArm springCamera;
    private Camera camera;
    private Quaternion baseCamRot;

    //Arrow properties
    public GameObject arrow;
    private SpriteRenderer arrowSprite;
    private Transform arrowPivot;

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
    private Quaternion launchDir;
    public Vector2 launchVal;
    private bool canLaunch = false;

    //Air rotation
    public float rotAmount = 10;
    private bool canRotate = true;


    //Controls
    private KeyCode pullBack = KeyCode.Space;

    private KeyCode left = KeyCode.A;
    private KeyCode right = KeyCode.D;
    private KeyCode up = KeyCode.W;
    private KeyCode down = KeyCode.S;



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

        camera = springCamera.GetComponentInChildren<Camera>();
        //baseCamRot = camera.transform.rotation;

        arrowSprite = arrow.GetComponentInChildren<SpriteRenderer>();
        arrowPivot = arrow.transform.GetChild(0);

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
            arrow.transform.position = hiltHitbox.transform.position;

            if ((camera.fieldOfView > 60) && (!launchCharging)) {
                camera.fieldOfView -= 1;
            }
            else if (camera.fieldOfView < 60) {
                camera.fieldOfView = 60;
            }
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
        //Begin Charging for a launch
        if (Input.GetKeyDown(pullBack) /*&& (canLaunch)*/) {
            arrowSprite.enabled = true;
            launchCharging = true;
            launchTimer = 0;
            canLaunch = false;

            //Set positions to the other side of our spring camera
            launchDir = springCamera.transform.rotation.normalized;
            arrowPivot.transform.eulerAngles = new Vector3(0, springCamera.transform.eulerAngles.y, 0);
        }


        if (Input.GetKeyUp(pullBack)) {
            if (launchCharging) {
                arrowSprite.enabled = false;
                launchCharging = false;

                if (launchTimer > minLaunch) {
                    Launch();
                }
            }
        }

        if (Input.GetKey(KeyCode.Escape)) {
            UnityEngine.Device.Application.Quit();
        }

        //Add rotation
        if (canRotate) {
            float turnH = Input.GetAxis("Horizontal");
            float turnV = Input.GetAxis("Vertical");

            if (Input.GetKey(left)) {
                //rigidbody.AddTorque(-rotAmount  * transform.right);

                
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(rotAmount, 0, 0) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
                

                //rigidbody.AddTorque(launchDir * Vector3.forward * Time.deltaTime * rotAmount);
            }

            if (Input.GetKey(right)) {
                //rigidbody.AddTorque(rotAmount * transform.right);

                
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(-rotAmount, 0, 0) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
                

                transform.localEulerAngles += launchDir * Vector3.right * Time.deltaTime * rotAmount;
            }

            if (Input.GetKey(up)) {
                //rigidbody.AddTorque(rotAmount * transform.forward);

                Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, 0, rotAmount) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);

            }

            if (Input.GetKey(down)) {
                //rigidbody.AddTorque(-rotAmount * transform.forward);
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, 0, -rotAmount) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
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

            //Some visual effects
            float arrowLength = EasingFunction.EaseOutExpo(0.5f, 3f, launchTimer/maxLaunch);
            arrowSprite.transform.localScale = new Vector3(arrow.transform.localScale.x, arrowLength,0);

            camera.fieldOfView = EasingFunction.EaseOutExpo(60, 90, launchTimer/maxLaunch);
        }
    }

    //If the sword is entering a piece of terrain, increase the timer before its hitbox is renabled
    void ProcessCollision() {
        if (swordEnteringTerrain) {
            swordTimer += Time.deltaTime;

            if (swordTimer > swordSharpness) {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                ToggleRigidbodyGravity(false);

                //wordHitbox.enabled = true;
                swordEnteringTerrain = false;
            }
        }
    }

    void OnCollisionEnter() {
        Debug.Log("Colliding");
        OnFloor(true);
    }

    void OnCollisionExit() {
        Debug.Log("UnColliding");
        OnFloor(false);
    }


    void OnTriggerEnter(UnityEngine.Collider other)
    {
        Debug.Log("Triggering with " + other.name);

        canLaunch = true;

        //Set the sword hitbox to no longer collide
        swordHitbox.enabled = false;
        swordEnteringTerrain = true;

        swordTimer = 0f;
        OnFloor(true);

    }

    void OnTriggerExit(UnityEngine.Collider other)
    {
        Debug.Log("UnTriggering with " + other.name);

        canLaunch = false;

        swordHitbox.enabled = true;
        ToggleRigidbodyGravity(true);
        OnFloor(false);
    }

    void OnFloor(bool val) {
        canLaunch = val;
        canRotate = !val;
    }

    void Launch() {
        rigidbody.AddForce(arrowPivot.rotation * new Vector3(0, launchVal.y * launchTimer, launchVal.x * launchTimer), ForceMode.Force);
        //rigidbody.AddRelativeTorque(arrowPivot.rotation * new Vector3(0, 0, 100 * launchTimer), ForceMode.Force);
        ToggleRigidbodyGravity(true);
    }

    void ToggleRigidbodyGravity(bool val) {
        rigidbody.useGravity = val;
    }
}
