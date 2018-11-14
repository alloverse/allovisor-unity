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


    void Start()
    {
        if(!AlloClientInt.allo_initialize(true)) {
            throw new Exception("Unable to initialize AlloNet");
        }
        client = new AlloClient();
    }

    void Update()
    {
        if (frameCount++ % 3 == 0) // only send @ 20hz
        {
            AlloIntent intent = new AlloIntent();
            intent.zmovement = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
            intent.xmovement = Input.GetKey(KeyCode.A) ? 1 : Input.GetKey(KeyCode.D) ? -1 : 0;
            // actually, SetIntent shouldn't send it; Poll should
            client.SetIntent(intent);
        }

        client.Poll();
    }

    void OnApplicationQuit()
    {
        client.Disconnect(0);
    }


}
