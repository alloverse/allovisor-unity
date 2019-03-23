using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateOminously : MonoBehaviour
{

    int frameCount;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (frameCount++ % 3 == 0) // only send @ 20hz
        { 
            transform.RotateAround(new Vector3(transform.localPosition.x, transform.localPosition.y), 0.05f);
        }
    }
}
