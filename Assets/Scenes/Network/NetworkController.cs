using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

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
        if(MenuParameters.urlToOpen == null)
        {
            SceneManager.LoadScene("Scenes/Menu/Menu");
            return;
        }

        if (!_AlloClient.allo_initialize(false)) {
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
        Debug.Log("New entity: " + entity.id);
        GameObject obj = Instantiate(baseEntityGO);
        entityGOs[entity.id] = obj;
    }

    void EntityRemoved(AlloEntity entity)
    {
        Debug.Log("Lost entity: " + entity.id);
        GameObject obj = entityGOs[entity.id];
        Destroy(obj);
        entityGOs.Remove(entity.id);
    }

    void Interaction(string type, AlloEntity from, AlloEntity to, LitJson.JsonData cmd)
    {
        if(cmd.Count >= 3 && cmd[0].ToString() == "announce") {
            myAvatarEntityId = cmd[1].ToString();
            string placeName = cmd[2].ToString();
            Debug.Log("Successfully connected to " + placeName);
            VisorSettings.GlobalSettings().addPlace(new PlaceDescriptor(MenuParameters.urlToOpen, placeName));
        }
    }

    void UpdateAvatar()
    {
        GameObject avatarEntity = null;
        Vector3 offset = new Vector3(0, 1, 0);
        if(myAvatarEntityId != null && (avatarEntity = GOFromEntityId(myAvatarEntityId)) != null) {
            mainCamera.transform.position = avatarEntity.transform.position + offset;
            mainCamera.transform.rotation = avatarEntity.transform.rotation;
        }
    }

    void OnApplicationQuit()
    {
        client.Disconnect(0);
    }

    // todo: on disconnection, go to menu scene




    public GameObject GOFromEntityId(string id)
    {
        GameObject e = null;
        entityGOs.TryGetValue(myAvatarEntityId, out e);
        return e;
    }

    public string EntityIdFromGO(GameObject go)
    {
        return entityGOs.FirstOrDefault(x => x.Value == go).Key;
    }

    public void SendPointing(string pointedAtEntity, Vector3 finger, Vector3 hit)
    {
        // avoid accidentally pointing at myself
        if (pointedAtEntity == myAvatarEntityId)
        {
            return;
        }

        client.InteractOneway(myAvatarEntityId, pointedAtEntity,
            "[" +
                "\"point\", "+
                String.Format("[{0}, {1}, {2}]", finger.x, finger.y, finger.z)+", "+
                String.Format("[{0}, {1}, {2}]", hit.x, hit.y, hit.z) + 
            "]"
        );
    }



}
