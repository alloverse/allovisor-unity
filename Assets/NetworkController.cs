using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    AlloClient client;
    int frameCount;
    public GameObject baseEntityGO;
    Dictionary<string, GameObject> entityGOs = new Dictionary<string, GameObject>();

    void Start()
    {
        if(!_AlloClient.allo_initialize(true)) {
            throw new Exception("Unable to initialize AlloNet");
        }
        client = new AlloClient();
        client.added = EntityAdded;
        client.removed = EntityRemoved;
    }

    void Update()
    {
        if (frameCount++ % 3 == 0) // only send @ 20hz
        {
            AlloIntent intent = new AlloIntent();
            intent.zmovement = Input.GetKey(KeyCode.W) ? 2 : Input.GetKey(KeyCode.S) ? -2 : 0;
            intent.xmovement = Input.GetKey(KeyCode.D) ? 2 : Input.GetKey(KeyCode.A) ? -2 : 0;
            // actually, SetIntent shouldn't send it; Poll should
            client.SetIntent(intent);
        }

        client.Poll();
        foreach(AlloEntity entity in client.entities.Values)
        {
            GameObject go = entityGOs[entity.id];
            go.transform.position = entity.position;
            Quaternion q = new Quaternion();
            q.eulerAngles = entity.rotation;
            go.transform.rotation = q;
        }
    }

    void EntityAdded(AlloEntity entity)
    {
        GameObject obj = Instantiate(baseEntityGO);
        entityGOs[entity.id] = obj;
    }

    void EntityRemoved(AlloEntity entity)
    {
        GameObject obj = entityGOs[entity.id];
        Destroy(obj);
        entityGOs.Remove(entity.id);
    }

    void OnApplicationQuit()
    {
        client.Disconnect(0);
    }


}
