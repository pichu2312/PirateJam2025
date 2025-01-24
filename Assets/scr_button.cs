using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_button : MonoBehaviour
{
    public GameObject otherObject;
    public enum Action {Open}
    public Action action;
    private bool activated = false;

    void Start() {

    }
    public void Activate() {
        activated = true;  

        GetComponentsInChildren<Transform>()[2].localScale = new Vector3(GetComponentInChildren<Transform>().localScale.x, 0.1f, GetComponentInChildren<Transform>().localScale.z);
        GetComponentInChildren<MeshRenderer>().material.color = Color.red;

        GetComponentInChildren<MeshCollider>().enabled = false;
    }

    public bool IsActive() {
        return activated;
    }
}
