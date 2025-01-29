using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;
using Quaternion = UnityEngine.Quaternion;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using Deform;
using UnityEngine.UI;


public class scr_sword : MonoBehaviour
{
    //Random
    System.Random rnd = new System.Random();

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
    private float arrowBaseLength;

    //Collision
    [SerializeField, Range(0.005f, 0.1f)]
    public float swordSharpness = 1.0f;
    private float swordTimer = 0f;
    public float minVelocity = 1f;
    //private bool swordEnteringTerrain;
    //private bool impaled = false;
    enum Piercing {Stopped, Lying, InAir, EnteringTerrain, LeavingTerrain, Impaled}
    Piercing swordStatus = Piercing.InAir;
    UnityEngine.Collider otherEntity;
    
    //Hilt
    Rigidbody hiltRigidbody;

    //Launching
    private bool launchCharging;
    private float launchTimer = 0;
    private float launchAmount;
    public float minTimeToCharge = 1f;
    public float maxTimeToCharge = 3f;
    public float chargeTimeout = 5f;
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
    enum Special {None, Floating, Zooming, GroundPound}
    Special special = Special.None;
    
    private float specialLength;
    private float specialTimer = 0f;

    public float floatLength = 1.0f;
    public float zoomLength = 0.5f;

    private float baseMass;
    public float groundPoundVelocity = 50f;
    private Quaternion baseRot;

    //public float groundPoundLength;

    //Activate Button
    private scr_button activatedButton;
    enum CameraMode {None, MovingTo, Stopped, MovingBack}
    private CameraMode cameraMode = CameraMode.None;
    private Transform cameraMovePosAndRot;
    private Transform cameraSavedPosAndRot;
    private GameObject hittingButton;

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

    //Sound effects
    public List<AudioClip> soundEffects = new List<AudioClip>();
    private AudioSource player;

    //Tutorial
    private int tutorialIndex = 0;
    public List<GameObject> tutorialParts = new List<GameObject>();

    //Story
    
    private int storyIndex = 0;
    public List<TextMeshProUGUI> storyParts = new List<TextMeshProUGUI>();

        enum StoryMode {FadeIn, FadeOut, Halt, End}
    private StoryMode storyMode = StoryMode.FadeIn;
    
    public float fadeTime = 1f;
    public float textFadeTime = 1f;

    private float fadeTimer = 0f;

    //public GameObject publicStoryImage;
    public UnityEngine.UI.Image storyImage;
    //public Sprite storyImage;

    public TextMeshProUGUI pressSpaceToContinue;

    //End game
    private bool endGameOnNextFall = false;
    private bool gameEnding = false;
    public GameObject endGame;
    public List<GameObject> creditParts = new List<GameObject>();
    private int creditsIndex = 0;

    public UnityEngine.UI.Image gameEndImage;
    public TextMeshProUGUI gameEndSpace;
    


    //Stat tracks
    private int launches = 0;
    private int fallsOnTheFloor = 0;
    private int groundPounds = 0;
    private int fallsOutOfTheWorld = 0;
    private float timePlayed;



    // Start is called before the first frame update
    void Start()
    {
        InitialiseComponents();
        BeginStory();
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
        arrowBaseLength = arrow.transform.localScale.y;

        baseRot = transform.rotation;

        bend = GetComponentInChildren<BendDeformer>();

        hiltRigidbody = GetComponentInChildren<Rigidbody>();

        player = GetComponent<AudioSource>();

        //storyImage = publicStoryImage.GetComponent<UnityEngine.UI.Image>();
    }

    void BeginStory() {
        rigidbody.useGravity = false;
        swordStatus = Piercing.Stopped;

        rigidbody.AddTorque(0, 180   , 180);
    }

    

    // Update is called once per frame
    void Update() {
        if (gameEnding) {
            GameEnd();
        }
        else {
            DebugCommands();

            //Pause for cutscenes and the like
            if (swordStatus != Piercing.Stopped) {
                UserInput();

                ProcessLaunchCharging();

                SpecialMoves();

                timePlayed += Time.deltaTime;
            }

            MoveCamera();

            RoutineChecks();
            
            PerformStory();
        }
    }
    void FixedUpdate()
    {
        ProcessCollision();
    }

    void LateUpdate() {
            if (arrowSprite.enabled) {
                arrow.transform.position = hiltHitbox.transform.position - hiltHitbox.bounds.size/2;
            }
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

    void GameEnd() {
        if (gameEndImage.color.a != 255) {
            fadeTimer += Time.deltaTime;
            gameEndImage.color = SetAlpha(gameEndImage.color, Mathf.Lerp(0, 255, fadeTimer/fadeTime));

            if (gameEndImage.color.a == 255) {
                gameEndSpace.enabled = true;
                creditParts[0].SetActive(true);

                //Set stats screen quickly too
                creditParts[0].GetComponentsInChildren<TextMeshProUGUI>()[1].SetText($"Stats:\nYou leapt {launches} Times\nYou Groundpounded {groundPounds} times\nYou Fell on the floor {fallsOnTheFloor} times\nAll in {timePlayed} minutes!") ;
            }
        }
        else {
            if (Input.GetKeyDown(pullBack)) {
                if (creditsIndex < creditParts.Count - 1) {
                    creditParts[creditsIndex].SetActive(false);

                    creditsIndex++;
        
                    creditParts[creditsIndex].SetActive(true);
                }

            }
        }
    }


    void DebugCommands() {
                    if (Input.GetKeyUp(KeyCode.R)) {
                transform.SetPositionAndRotation(tpPosition, tpRotation);
                rigidbody.angularVelocity = Vector3.zero;
            }
        if (debug) {


            
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
        if (Input.GetKeyDown(pullBack) && (swordStatus == Piercing.Impaled))  {
            arrowSprite.enabled = true;
            launchCharging = true;
            launchTimer = 0;

            SetLaunchProperties();
            playSound(2);
        }


        if (Input.GetKeyUp(pullBack)) {
            if (launchCharging) {
                EndLaunchCharge();

                if (launchTimer > minTimeToCharge) {
                    Launch();
                }
            }
        }

        //Special moves
        if (special == Special.None) {
            if (Input.GetKeyDown(special1)) {
                GroundPound();
            }

            if (Input.GetKeyDown(special2)) {
                Zoom();
            }
        }

        //Add rotation
        if ((swordStatus == Piercing.InAir) && (special != Special.GroundPound)) {
            AirRotation();
        }
        else if (swordStatus == Piercing.Lying) {
            GroundRotation();
        }
    }

    void SetLaunchProperties() {
        //Set positions to the other side of our spring camera
        launchDir = springCamera.transform.rotation.normalized;
        arrowPivot.transform.eulerAngles = new Vector3(0, springCamera.transform.eulerAngles.y, 0);

        //same with bendy
        bend.transform.eulerAngles = new Vector3(0, springCamera.transform.eulerAngles.y + 90, 0);
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
                //torque = (arrowPivot.rotation  * Vector3.back).normalized * rotAmount;
                torque = new Vector3(rotAmount, 0, 0);
                

                //rigidbody.AddTorque(launchDir * Vector3.forward * Time.deltaTime * rotAmount);
            }

            if (Input.GetKey(right)) {
                //rigidbody.AddTorque(rotAmount * transform.right);


                /*
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(-rotAmount, 0, 0) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);
                */
                //torque = (arrowPivot.rotation  * Vector3.forward).normalized * rotAmount;

                torque = new Vector3(-rotAmount, 0, 0);

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
                //torque = (arrowPivot.rotation  * Vector3.right).normalized * rotAmount;
                 torque = new Vector3(0, 0, rotAmount);
            }

            if (Input.GetKey(up)) {
                //rigidbody.AddTorque(-rotAmount * transform.forward);

                /*
                Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, 0, -rotAmount) * Time.fixedDeltaTime);
                rigidbody.MoveRotation(rigidbody.rotation * deltaRotation);

                */
                //torque = (arrowPivot.rotation  * Vector3.left).normalized * rotAmount;
                torque = new Vector3(0, 0, -rotAmount);

            }

            if (Input.GetKey(up) || Input.GetKey(down) || Input.GetKey(left) || Input.GetKey(right)) {
                    //rigidbody.MoveRotation(rigidbody.rotation * Quaternion.Euler(torque * Time.deltaTime));
                    rigidbody.MoveRotation(rigidbody.rotation * Quaternion.Euler(torque * Time.deltaTime));
                    ProgressTutorial(4);

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

    void GroundPound() {
        if (swordStatus == Piercing.InAir)  {
            //rigidbody.mass = baseMass * groundPoundMassFactor;
            rigidbody.velocity = new Vector3(0, -groundPoundVelocity, 0);
            rigidbody.angularVelocity = Vector3.zero;
            transform.rotation = new Quaternion(0, 0, 180, 1);//baseRot;
            swordHitbox.enabled = false;
            
            //Groundpound has no timelimit so it wont ever reach this value
            specialLength = 420;
            special = Special.GroundPound;

            playSound(3);

            ProgressTutorial(6);
            groundPounds++;
        }   
    }
    void ProcessLaunchCharging() {
        if (launchCharging) {
            launchTimer += Time.deltaTime;

            if (launchTimer > chargeTimeout) {
                EndLaunchCharge();
                return;
            }

            launchAmount = launchTimer;
            //POTENTIALLY INEFFICENT
            if (launchAmount > maxTimeToCharge) {
                launchAmount = maxTimeToCharge;
            }
            
            

            //Some visual effects
            float arrowLength = EasingFunction.EaseOutExpo(0.1f, arrowBaseLength, launchAmount/maxTimeToCharge);
            arrowSprite.transform.localScale = new Vector3(arrow.transform.localScale.x, arrowLength,0);

            camera.fieldOfView = EasingFunction.EaseOutExpo(60, 90, launchAmount/maxTimeToCharge);

            //Bend
            bend.Angle = EasingFunction.EaseOutExpo(0, maxBend, launchAmount/maxTimeToCharge);

            //Set direcftion of arrow
            SetLaunchProperties();
        }
    }

    void SpecialMoves() {
        switch (special) {
            case Special.Floating:
            case Special.Zooming:
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

        //Respawn
        if ((transform.position.y < -30) && (endGameOnNextFall)) {
            gameEnding = true;
            swordStatus = Piercing.Stopped;
            fadeTimer = 0;
            endGame.SetActive(true);
        }
        else if (transform.position.y < -30) {
            transform.position = tpPosition;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.velocity = Vector3.zero;
            fallsOutOfTheWorld++;
        }
    }

    void PerformStory() {
        switch (storyMode) {
            case StoryMode.End: break;
            case StoryMode.FadeIn: 
                fadeTimer += Time.deltaTime;
                var alphaVal1 = Mathf.Lerp(0, 1, fadeTimer/fadeTime);
                var alphaValStoryImage1 = Mathf.Lerp(0, 0.9f, fadeTimer/fadeTime);

                storyImage.color = SetAlpha(storyImage.color, alphaValStoryImage1);
                storyParts[0].color = SetAlpha(storyParts[0].color, alphaVal1);
                pressSpaceToContinue.color = SetAlpha(pressSpaceToContinue.color, alphaVal1);

                if (fadeTimer > fadeTime) {
                    storyMode = StoryMode.Halt;
                    fadeTimer = textFadeTime;
                }
            break;
            case StoryMode.Halt: 
                if (Input.GetKeyDown(pullBack)) {
                    if (storyIndex < storyParts.Count - 1) {
                        if (storyParts[storyIndex].color.a != 1) {
                            storyParts[storyIndex].color = SetAlpha(storyParts[storyIndex].color, 1);
                        }

                        storyIndex++;
                        fadeTimer = 0;
                    }
                    else {
                        storyMode = StoryMode.FadeOut;
                        fadeTimer = 0;
                        break;
                    }

                }

                if (fadeTimer < textFadeTime) {
                    fadeTimer += Time.deltaTime;

                    storyParts[storyIndex].color = SetAlpha(storyParts[storyIndex].color, Mathf.Lerp(0, 1, fadeTimer/textFadeTime));
                }
            
            break;
            case StoryMode.FadeOut: 
                fadeTimer += Time.deltaTime;
                var alphaVal2 = Mathf.Lerp(1, 0, fadeTimer/fadeTime);
                var alphaValStoryImage2 = Mathf.Lerp(0.9f, 0, fadeTimer/fadeTime);


                storyImage.color = SetAlpha(storyImage.color, alphaValStoryImage2);
                
                foreach (TextMeshProUGUI t in storyParts) {
                    t.color = SetAlpha(t.color, alphaVal2);
                }

                pressSpaceToContinue.color = SetAlpha(pressSpaceToContinue.color, alphaVal2);

                if (fadeTimer > fadeTime) {
                    BeginGame();
                }
            break;

        }
    }

    void BeginGame() {
        rigidbody.useGravity = true;
        rigidbody.velocity = new Vector3(0, -50, 0);
        swordStatus = Piercing.InAir;
        springCamera.enabled = true;
        storyMode = StoryMode.End;
    }

    Color SetAlpha(Color image, float alpha) {
        return new Color(image.r, image.g, image.b, alpha);
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
        //Assures this onyl runs once if we're hitting multiple terrains
        if (swordStatus == Piercing.EnteringTerrain) {
            ToggleRigidbodyGravity(false);

            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            //wordHitbox.enabled = true;
            swordStatus = Piercing.Impaled;

            if (hittingButton != null) {
            //if (otherEntity.CompareTag("Button")) {
                TriggerButton(hittingButton);
            }

            playSound(4);

            ProgressTutorial(1);

        }


        //OnFloor(true);
    }

    void TriggerButton(GameObject buttonObject) {
        scr_button button = buttonObject.GetComponentInParent<scr_button>();
        Camera moveToCamera = buttonObject.transform.parent.GetComponentInChildren<Camera>();

        if (button != null) {
            if (!button.IsActive()) {
                swordStatus = Piercing.Stopped;
                
                //Save values
                activatedButton = button;
                activatedButton.Activate();
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
                if (button.rope1 != null) {
                    Destroy(button.rope1);
                    Destroy(button.rope2);

                }

                //Only cause Im going insane
                endGameOnNextFall = true;
                playSound(6);

                
            break;
            }
        }
    }




    void OnTriggerEnter(UnityEngine.Collider other)
    {
        otherEntity = other;
        //bad but works
        if (other.material.name == wood.name + " (Instance)") {
            //Check we're above the needed amount of velocity
            if (/*rigidbody.velocity.magnitude > minVelocity*/true) {
                Debug.Log("Triggering with " + other.name);

                //Hitting Button
                if (otherEntity.CompareTag("Button")) {
                    hittingButton = otherEntity.gameObject;
                 }
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

        hittingButton = null;
        ToggleRigidbodyGravity(true);
        //OnFloor(false);
    }

    //Only process these collisions when we're not entering terrain
    void OnCollisionEnter(Collision collision) {
        Debug.Log("Colliding");

        //OnFloor(true);
        if (swordStatus == Piercing.InAir)  {
            swordStatus = Piercing.Lying;

            //Sound
            if (collision.collider.material.name == wood.name + " (Instance)") {
                playSound(0);
            }
            else if (collision.collider.material.name == stone.name + " (Instance)") {
                playSound(1);
            }

            EndSpecial();

            fallsOnTheFloor++;

            if (fallsOnTheFloor > 5) {
                ProgressTutorial(7);
            }
        }

        ProgressTutorial(0);
    }

    void OnCollisionExit() {
        Debug.Log("UnColliding");
        //OnFloor(false);
        if (swordStatus == Piercing.Lying)  {
            swordStatus = Piercing.InAir;
        }
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
        swordStatus = Piercing.LeavingTerrain;

        //Add a rotation
        //Quaternion lookRot = Quaternion.FromToRotation(arrow.transform.forward - transform.forward, Vector3.forward);
        //rigidbody.AddTorque(lookRot.eulerAngles.normalized * 100, ForceMode.Force);
        launches++;
        //Tutorial
        if (launches == 1) {
            ProgressTutorial(2);
        }
        else if (launches == 3) {
            ProgressTutorial(3);
        }
        else if (launches == 5) {
            ProgressTutorial(5);
        }
        else {
             ProgressTutorial(8);
        }
    }

    void ToggleRigidbodyGravity(bool val) {
        rigidbody.useGravity = val;
    }

    void playSound(int i) {
        AudioClip clip;
        try {
            clip = soundEffects[i];

            if (clip != null) {
                player.clip = clip;
                player.Play();
            }
        }
        catch {}

    }

    void playSound(string s) {

    }

    //ProgressTutorial
    void ProgressTutorial(int index) {
        if ((tutorialIndex == index) && (tutorialIndex <= tutorialParts.Count)) {
            if (index > 0) {tutorialParts[index - 1].SetActive(false); };
            if (index < tutorialParts.Count) {tutorialParts[index].SetActive(true);};
            tutorialIndex++;
            playSound(5);
        }
    }

}
