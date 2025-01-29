    using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class scr_breaking_control : MonoBehaviour
{
    public float destroyTime = 2f;
    public int destroyObjects = 5;
    public GameObject remains;

    void OnCollisionEnter(Collision other) {
        GetComponent<AudioSource>().Play();
        InitiateDestruction(other.gameObject);
    }

    void OnTriggerEnter(UnityEngine.Collider other) {
        InitiateDestruction(other.gameObject);
    }

    //Done here so it doesn't matter if its the sword or the point hitting the object
    void InitiateDestruction(GameObject other) {
        if (other.CompareTag("Sharp")) {
            for (int i = 0; i < destroyObjects; i++) {
                var created = Instantiate(remains, transform.position, transform.rotation);
                created.GetComponent<scr_remains>().Initiate(GetComponent<MeshRenderer>().material, transform.localScale.x/destroyObjects, destroyTime);

                //if its the first one, play a sound effectt
                if (i == 0) {
                    created.GetComponents<AudioSource>()[0].Play();

                }
            }


            Destroy(gameObject);
        }
    }

}
