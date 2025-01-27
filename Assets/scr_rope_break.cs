using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class scr_rope_break : MonoBehaviour
{
    public GameObject cylinder;
    public Rigidbody breakable;

    void OnTriggerEnter(UnityEngine.Collider other) {
        if (other.CompareTag("Sharp")) {
            Destroy(cylinder);
            
            try {
                breakable.mass = 0.1f;
                breakable.useGravity = true;
            }
            catch {
                
            }
        }
    }
}
