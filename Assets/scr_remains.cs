using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_remains : MonoBehaviour
{
    float destroyTime = 1;
    float destroyTimer;

    // Update is called once per frame
    void Update()
    {
        destroyTimer += Time.deltaTime;

        if (destroyTimer > destroyTime) {
            Destroy(gameObject);
        }

    }

    public void Initiate(Material material, float scale, float destroyTime) {
        GetComponent<MeshRenderer>().material = material;

        transform.localScale = new Vector3(scale, scale, scale);

        this.destroyTime = destroyTime;
    }

    public void OnCollisionEnter() {
//GetComponents<AudioSource>()[1].Play();
    }
}
