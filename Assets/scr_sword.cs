using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class scr_sword : MonoBehaviour
{
    
    //Testing
    [SerializeField]
    private bool debug = true;

    [SerializeField]
    private Vector3 tpPosition = new Vector3(0, 3, 0);

    [SerializeField]
    private Quaternion tpRotation = new Quaternion(30, 0, -180, 0);


    //Components
    public CapsuleCollider hiltHitbox;
    public CapsuleCollider swordHitbox;
    public CapsuleCollider pointHitbox;


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
    }

    // Update is called once per frame
    void Update()
    {
        DebugCommands();
    }

    void DebugCommands() {
        if (debug) {
            if (Input.GetKeyUp(KeyCode.Tab)) {
                transform.position = tpPosition;
                transform.rotation = tpRotation;
            }
        }
    }

    void OnTriggerEnter(UnityEngine.Collider other)
    {
        Debug.Log("Colliding with " + other.name);


        //Set the sword hitbox to no longer collide
        swordHitbox.enabled = false;
    }

    void OnTriggerExit(UnityEngine.Collider other)
    {
        Debug.Log("Uncolliding with " + other.name);

        swordHitbox.enabled = true;

    }
}
