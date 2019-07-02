using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkController : MonoBehaviour
{
    AlloClient client;
    int frameCount;
    public GameObject baseEntityGO;
    public GameObject mainCamera;
    Dictionary<string, GameObject> entityGOs = new Dictionary<string, GameObject>();
    string myAvatarEntityId;
    public AlloIntent intent = new AlloIntent();

    void Start()
    {
        if(!_AlloClient.allo_initialize(false)) {
            throw new Exception("Unable to initialize AlloNet");
        }

        AlloIdentity identity = new AlloIdentity();
        identity.display_name = "Egg";
        LitJson.JsonData avatarDesc = new LitJson.JsonData();
        avatarDesc["geometry"] = new LitJson.JsonData();
        avatarDesc["geometry"]["type"] = new LitJson.JsonData("hardcoded-model");
        avatarDesc["geometry"]["name"] = new LitJson.JsonData("cubegal");

        try
        {
            client = new AlloClient(MenuParameters.urlToOpen, identity, avatarDesc);
        } catch(Exception e) {
            MenuParameters.lastError = e.Message;
            SceneManager.LoadScene("Menu/Menu");
            return;
        }
        client.added = EntityAdded;
        client.removed = EntityRemoved;
        client.interaction = Interaction;
    }

    void Update()
    {
        if(client == null) {
            return;
        }

        if (frameCount++ % 3 == 0) // only send @ 20hz
        {
            // actually, SetIntent shouldn't send it; Poll should
            client.SetIntent(intent);
        }

        client.Poll();
        foreach(AlloEntity entity in client.entities.Values)
        {
            GameObject go = entityGOs[entity.id];
            go.transform.position = entity.Transform.position;
            Quaternion q = new Quaternion();
            Vector3 radRotation = entity.Transform.rotation;
            Vector3 degRotation = radRotation * 180/Mathf.PI;
            q.eulerAngles = degRotation;
            go.transform.rotation = q;
        }

        UpdateAvatar();
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

    void Interaction(AlloEntity from, AlloEntity to, LitJson.JsonData cmd)
    {
        if(cmd.Count == 2 && cmd[0].ToString() == "your_avatar") {
            myAvatarEntityId = cmd[1].ToString();
        }
    }

    void UpdateAvatar()
    {
        GameObject avatarEntity = null;
        Vector3 offset = new Vector3(0, 1, 0);
        if(myAvatarEntityId != null && entityGOs.TryGetValue(myAvatarEntityId, out avatarEntity)) {
            mainCamera.transform.position = avatarEntity.transform.position + offset;
            mainCamera.transform.rotation = avatarEntity.transform.rotation;
        }
    }

    void OnApplicationQuit()
    {
        client.Disconnect(0);
    }

    // todo: on disconnection, go to menu scene

}
