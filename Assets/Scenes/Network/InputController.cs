using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour {
    public NetworkController network;
    public Camera cam;

	// Use this for initialization
	void Start () {
		
	}

    private Vector3 lastMouse;
    float yaw, pitch;

    void Update () {
        if (MenuParameters.urlToOpen == null)
            return;

        AlloIntent intent = IntentFromMouse();
        network.intent = intent;

        PerformPointingInteraction();
    }

    AlloIntent IntentFromMouse()
    {
        AlloIntent intent = new AlloIntent();
        intent.zmovement = Input.GetKey(KeyCode.W) ? 2 : Input.GetKey(KeyCode.S) ? -2 : 0;
        intent.xmovement = Input.GetKey(KeyCode.D) ? 2 : Input.GetKey(KeyCode.A) ? -2 : 0;
        if (Input.GetMouseButton(0))
        {
            if (lastMouse != Vector3.zero)
            {
                Vector3 delta = lastMouse - Input.mousePosition;
                pitch += delta.y * 0.01f;
                yaw -= delta.x * 0.01f;
            }
            lastMouse = Input.mousePosition;
        }
        else
        {
            lastMouse = Vector3.zero;
        }
        intent.yaw = yaw;
        intent.pitch = pitch;
        return intent;
    }

    void PerformPointingInteraction()
    {
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out hit))
            return;
        if (hit.collider == null)
            return;
        GameObject go = hit.collider.gameObject;
        while ( go.transform.parent)
        {
            go = go.transform.parent.gameObject;
        }
        string entityId = network.EntityIdFromGO(go);
        if (entityId == null)
            return;

        network.SendPointing(entityId, ray.GetPoint(0.0f), hit.point);

    }
}
