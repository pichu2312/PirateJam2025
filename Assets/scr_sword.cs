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
using Deform;


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
    public float minVelocity = 1f;
    //private bool swordEnteringTerrain;
    //private bool impaled = false;
    enum Piercing {Stopped, Lying, InAir, EnteringTerrain, Impaled}
    Piercing swordStatus = Piercing.InAir;
    UnityEngine.Collider otherEntity;

    //Launching
    private bool launchCharging;
    private float launchTimer = 0;
    private float launchAmount;
    public float minLaunch = 1f;
    public float maxLaunch = 3f;
    public float launchTimeout = 5f;
    private Quaternion launchDir;
    public Vector2 launchVal;
    public Vector2 launchValFloor;
    public float maxBend = 70f;
    private BendDeformer bend;



    //Air rotation
    public float rotAmount = 10;
    public float floorRotAmount;
    private float usedRot;

    //Special Moves
    enum Special {None, Floating, Zooming}
    Special special = Special.None;
    
    private float specialLength;
    private float specialTimer = 0f;

    public float floatLength = 1.0f;
    public float zoomLength = 0.5f;


    //Activate Button
    private scr_button activatedButton;
    enum CameraMode {None, MovingTo, Stopped, MovingBack}
    private CameraMode cameraMode = CameraMode.None;
    private Transform cameraMovePosAndRot;
    private Transform cameraSavedPosAndRot;

    //ANother method
    private Camera savedCamera;
    private Camera newCamera;

    private float cameraTimer;
    private float cameraTimerLength;
    public float cameraMoveTime = 1f;
    public float cameraWaitTime = 1.5f;


    //Controls
    private KeyCode pullBack = KeyCode.Space;

    private KeyCode left = KeyCode.A;
    private KeyCode right = KeyCode.D;
    private KeyCode up = KeyCode.W;
    private KeyCode down = KeyCode.S;

    private KeyCode special1 = KeyCode.LeftShift;
    private KeyCode special2 = KeyCode.LeftControl;



    //Materials
    public PhysicMaterial wood;
    public PhysicMaterial stone;

    public Material myMetal;



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

        bend = GetComponentInChildren<BendDeformer>();

    }

    // Update is called once per frame
    void Update() {
        DebugCommands();

        //Pause for cutscenes and the like
        if (swordStatus != Piercing.Stopped) {
            UserInput();

            ProcessLaunchCharging();

            SpecialMoves();
        }

        MoveCamera();

        RoutineChecks();
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

            if ((bend.Angle > 0) && (!launchCharging)) {
                bend.Angle -= 20;
            }
            else if (bend.Angle < 0) {
                bend.Angle = 0;
            }
    }


    void DebugCommands() {
        if (debug) {
            if (Input.GetKeyUp(KeyCode.Tab)) {
                transform.SetPositionAndRotation(tpPosition, tpRotation);
                rigidbody.angularVelocity = Vector3.zero;
            }

            if (Input.GetKeyUp(KeyCode.Return)) {
                swordHitbox.enabled = true;
            }

            debugText.text = "Velocity: " + rigidbody.velocity;
            debugText.text += "\nStatus: " + swordStatus;
            debugText.text += "\nSpecial: " + special;

        }
    }

    void UserInput() {
        
        //Begin Charging for a launch
        if (Input.GetKeyDown(pullBack) && ((swordStatus == Piercing.Lying) || (swordStatus == Piercing.Impaled)))  {
            arrowSprite.enabled = true;
            launchCharging = true;
            launchTimer = 0;

            //Set positions to the other side of our spring camera
            launchDir = springCamera.transform.rotation.normalized;
            arrowPivot.transform.eulerAngles = new Vector3(0, springCamera.transform.eulerAngles.y, 0);

            //same with bendy
            bend.transform.eulerAngles = new Vector3(0, springCamera.transform.eulerAngles.y + 90, 0);
        }


        if (Input.GetKeyUp(pullBack)) {
            if (launchCharging) {
                EndLaunchCharge();

                if (launchTimer > minLaunch) {
                    Launch();
                }
            }
        }

        if (Input.GetKey("escape")) {
            UnityEngine.Application.Quit();
        }

        //Special moves
        if (special == Special.None) {
            if (Input.GetKeyDown(special1)) {
                Float();
            }

            if (Input.GetKeyDown(special2)) {
                Zoom();
            }
        }

        //Add rotation
        if (swordStatus == Piercing.InAir) {
            AirRotation();
        }
        else if (swordStatus == Piercing.Lying) {
            GroundRotation();
        }
    }

    void EndLaunchCharge() {
        arrowSprite.enabled = false;
        launchCharging = false;
    }

    void AirRotation() {

            Vector3 torque = Vector3.zero;

            if (Input.GetKey(left)) {
                //rigidbody.AddTorque(-rotAmount  * transform.right);

                /*
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(rotAmount, 0, 0) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
                */
                torque = (arrowPivot.rotation  * Vector3.back).normalized * rotAmount;

                

                //rigidbody.AddTorque(launchDir * Vector3.forward * Time.deltaTime * rotAmount);
            }

            if (Input.GetKey(right)) {
                //rigidbody.AddTorque(rotAmount * transform.right);


                /*
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(-rotAmount, 0, 0) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
                */
                torque = (arrowPivot.rotation  * Vector3.forward).normalized * rotAmount;

                

                //transform.localEulerAngles += launchDir * Vector3.right * Time.deltaTime * rotAmount;
            }

            if (Input.GetKey(down)) {
                //rigidbody.AddTorque(rotAmount * transform.forward);

                /*
                Vector3 target = (arrowPivot.transform.position - rigidbody.position).normalized;
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(target.x * rotAmount, 0, target.z * rotAmount) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
                */

                /*
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, 0, rotAmount) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
                */ 
                torque = (arrowPivot.rotation  * Vector3.right).normalized * rotAmount;
            }

            if (Input.GetKey(up)) {
                //rigidbody.AddTorque(-rotAmount * transform.forward);

                /*
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, 0, -rotAmount) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);

                */
                torque = (arrowPivot.rotation  * Vector3.left).normalized * rotAmount;
            }

            if (Input.GetKey(up) || Input.GetKey(down) || Input.GetKey(left) || Input.GetKey(right)) {
                    rigidbody.MoveRotation(rigidbody.rotation * Quaternion.Euler(torque * Time.deltaTime));

            }
    }

    void GroundRotation() {
            if (Input.GetKey(left)) {
                //rigidbody.AddTorque(new Vector3(floorRotAmount, 0, 0));
                rigidbody.AddTorque(rotAmount * transform.right);

            }

            if (Input.GetKey(right)) {
                //rigidbody.AddTorque(new Vector3(-floorRotAmount, 0, 0));
                rigidbody.AddTorque(-rotAmount * transform.right);

            }

            if (Input.GetKey(down)) {
                //rigidbody.AddTorque(new Vector3(0, 0, floorRotAmount));
                rigidbody.AddTorque(rotAmount * transform.forward);
            }

            if (Input.GetKey(up)) {
                //rigidbody.AddTorque(new Vector3(0, 0, -floorRotAmount));
                 rigidbody.AddTorque(-rotAmount * transform.forward);

            }
    }
    
    void EndSpecial() {
        switch (special) {
            case Special.Floating: ToggleRigidbodyGravity(true); break;
        }

        specialTimer = 0;
        special = Special.None;
    }

    void Float() {
        //If in air, cut all vertical speed for a duration
        if (swordStatus == Piercing.InAir)  {
            rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);
            ToggleRigidbodyGravity(false);

            specialLength = floatLength;
            special = Special.Floating;
        }
    }

    void Zoom() {
        if (swordStatus == Piercing.InAir)  {
            rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);
            ToggleRigidbodyGravity(false);

            specialLength = floatLength;
            special = Special.Floating;
        }   
    }
    void ProcessLaunchCharging() {
        if (launchCharging) {
            launchTimer += Time.deltaTime;

            if (launchTimer > launchTimeout) {
                EndLaunchCharge();
                return;
            }

            launchAmount = launchTimer;
            //POTENTIALLY INEFFICENT
            if (launchAmount > maxLaunch) {
                launchAmount = maxLaunch;
            }
            
            

            //Some visual effects
            float arrowLength = EasingFunction.EaseOutExpo(0.5f, 3f, launchAmount/maxLaunch);
            arrowSprite.transform.localScale = new Vector3(arrow.transform.localScale.x, arrowLength,0);

            camera.fieldOfView = EasingFunction.EaseOutExpo(60, 90, launchAmount/maxLaunch);

            //Bend
            bend.Angle = EasingFunction.EaseOutExpo(0, maxBend, launchAmount/maxLaunch);
        }
    }

    void SpecialMoves() {
        switch (special) {
            case Special.Floating: 
                specialTimer += Time.deltaTime;
                
            break;
        }

        if (specialTimer > specialLength)  {
            EndSpecial();
        }
    }

    void MoveCamera() {
        if (cameraMode != CameraMode.None) {
            cameraTimer += Time.deltaTime;

            if (cameraMode != CameraMode.Stopped) {
                //camera.transform.SetPositionAndRotation(Vector3.Lerp(cameraSavedPosAndRot.position ,cameraMovePosAndRot.position, cameraTimerLength/cameraTimer), Quaternion.Lerp(cameraSavedPosAndRot.rotation ,cameraMovePosAndRot.rotation, cameraTimerLength/cameraTimer));
            }

            if (cameraTimer > cameraTimerLength) {
                switch (cameraMode) {
                    case CameraMode.MovingTo: SetCameraMove(cameraSavedPosAndRot, cameraWaitTime, CameraMode.Stopped); TriggerButtonEffect(activatedButton); break;
                    case CameraMode.Stopped: SetCameraMove(cameraSavedPosAndRot, cameraMoveTime, CameraMode.MovingBack); newCamera.enabled = false; break;
                    case CameraMode.MovingBack: cameraMode = CameraMode.None; swordStatus = Piercing.Impaled; break;

                }

            }
        }

    }

    void RoutineChecks() {
        //Potentially wont sue this so
        if (swordStatus == Piercing.Impaled) {
            myMetal.color = Color.red;
        }
        else {
            myMetal.color = Color.white;
        }

        if (swordStatus == Piercing.Impaled) {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }

    //If the sword is entering a piece of terrain, increase the timer before its hitbox is renabled
    void ProcessCollision() {
        if (swordStatus == Piercing.EnteringTerrain) {
            swordTimer += Time.deltaTime;

            if (swordTimer > swordSharpness) {
                Impale();
            }
        }
    }

    void Impale() {

        ToggleRigidbodyGravity(false);

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        //wordHitbox.enabled = true;
        swordStatus = Piercing.Impaled;

        if (otherEntity.CompareTag("Button")) {
            TriggerButton(otherEntity);
        }
        //OnFloor(true);
    }

    void TriggerButton(Collider buttonObject) {
        scr_button button = buttonObject.GetComponentInParent<scr_button>();
        Camera moveToCamera = buttonObject.transform.parent.GetComponentInChildren<Camera>();

        if (button != null) {
            if (!(button.IsActive())) {
                swordStatus = Piercing.Stopped;
                
                //Save values
                activatedButton = button;
                button.Activate();
                //cameraSavedPosAndRot = camera.transform;
                newCamera = moveToCamera;
                newCamera.enabled = true;
                
                //Begin Moving camera
                SetCameraMove(moveToCamera.transform, cameraMoveTime, CameraMode.MovingTo);
            }
        }

    }

    void SetCameraMove(Transform moveTo, float moveTime, CameraMode mode) {
            cameraTimer = 0;
            cameraTimerLength = moveTime;
            cameraMode = mode;
            cameraMovePosAndRot = moveTo;
    }

    void TriggerButtonEffect(scr_button button) {
        if (button != null) {
            switch (button.action) {
                case scr_button.Action.Open: 
                Destroy(button.otherObject);
                
            break;
            }
        }
    }

    //Only process these collisions when we're not entering terrain
    void OnCollisionEnter() {
        Debug.Log("Colliding");
        //OnFloor(true);
        if (swordStatus == Piercing.InAir)  {
            swordStatus = Piercing.Lying;
        }
    }

    void OnCollisionExit() {
        Debug.Log("UnColliding");
        //OnFloor(false);
        if (swordStatus == Piercing.Lying)  {
            swordStatus = Piercing.InAir;
        }
    }


    void OnTriggerEnter(UnityEngine.Collider other)
    {
        otherEntity = other;
        //bad but works
        if (other.material.name == wood.name + " (Instance)") {
            //Check we're above the needed amount of velocity
            if (rigidbody.velocity.magnitude > minVelocity) {
                Debug.Log("Triggering with " + other.name);

                //canLaunch = true;

                //Set the sword hitbox to no longer collide
                EndSpecial();
                swordHitbox.enabled = false;
                swordStatus = Piercing.EnteringTerrain;

                swordTimer = 0f;
            }

            //OnFloor(true);
        }
    }

    void OnTriggerExit(UnityEngine.Collider other)
    {
        Debug.Log("UnTriggering with " + other.name);

        swordHitbox.enabled = true;
        swordStatus = Piercing.InAir;


        ToggleRigidbodyGravity(true);
        //OnFloor(false);
    }


    void Launch() {
        if (swordStatus == Piercing.Impaled) {
                rigidbody.AddForce(arrowPivot.rotation * new Vector3(0, launchVal.y * launchAmount, launchVal.x * launchAmount), ForceMode.Force);
            
                //Vector3 target = (arrow.transform.position - rigidbody.position).normalized;
                //Quaternion deltaRotation = Quaternion.Euler(new Vector3(rotAmount, target.y,0));
                rigidbody.AddTorque(arrowPivot.rotation  * Vector3.right * launchAmount, ForceMode.Force);
        }
        else {
            rigidbody.AddForce(arrowPivot.rotation * new Vector3(0, launchValFloor.y * launchAmount, launchValFloor.x * launchAmount), ForceMode.Force);
        }
        //rigidbody.AddRelativeTorque(arrowPivot.rotation * new Vector3(0, 0, 100 * launchTimer), ForceMode.Force);
        ToggleRigidbodyGravity(true);
        swordStatus = Piercing.InAir;

        //Add a rotation
        //Quaternion lookRot = Quaternion.FromToRotation(arrow.transform.forward - transform.forward, Vector3.forward);
        //rigidbody.AddTorque(lookRot.eulerAngles.normalized * 100, ForceMode.Force);
    }

    void ToggleRigidbodyGravity(bool val) {
        rigidbody.useGravity = val;
    }
}
