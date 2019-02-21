using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour {
    public NetworkController network;

	// Use this for initialization
	void Start () {
		
	}

    private Vector3 lastMouse;
    float yaw, pitch;

    void Update () {
        AlloIntent intent = new AlloIntent();
        intent.zmovement = Input.GetKey(KeyCode.W) ? 2 : Input.GetKey(KeyCode.S) ? -2 : 0;
        intent.xmovement = Input.GetKey(KeyCode.D) ? 2 : Input.GetKey(KeyCode.A) ? -2 : 0;
        if (Input.GetMouseButton(0))
        {
            if(lastMouse != Vector3.zero)
            {
                Vector3 delta = lastMouse - Input.mousePosition;
                yaw += delta.x;
                pitch += delta.y;
            }
            lastMouse = Input.mousePosition;
        }
        else
        {
            lastMouse = Vector3.zero;
        }
        intent.yaw = yaw;
        intent.pitch = pitch;

        network.intent = intent;
    }
}
