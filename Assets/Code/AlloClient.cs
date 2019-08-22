using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class AlloIdentity
{
    public string display_name { get; set; }
}

class AlloClient
{
    private unsafe _AlloClient* client;
    public Dictionary<string, AlloEntity> entities = new Dictionary<string, AlloEntity>();
    public delegate void EntityAdded(AlloEntity entity);
    public EntityAdded added = null;
    public delegate void EntityRemoved(AlloEntity entity);
    public EntityRemoved removed = null;
    public delegate void Interaction(string type, AlloEntity from, AlloEntity to, LitJson.JsonData command);
    public Interaction interaction = null;

    public delegate void ResponseCallback(string body);
    private Dictionary<string, ResponseCallback> responseCallbacks = new Dictionary<string, ResponseCallback>();

    private _AlloClient.InteractionCallbackFun interactionCallback;
    private GCHandle interactionCallbackHandle;

    public AlloClient(string url, AlloIdentity identity, LitJson.JsonData avatarDesc)
    {
        unsafe
        {
            IntPtr urlPtr = Marshal.StringToHGlobalAnsi(MenuParameters.urlToOpen);
            IntPtr identPtr = Marshal.StringToHGlobalAnsi(LitJson.JsonMapper.ToJson(identity));
            IntPtr avatarPtr = Marshal.StringToHGlobalAnsi(LitJson.JsonMapper.ToJson(avatarDesc));

            client = _AlloClient.allo_connect(urlPtr, identPtr, avatarPtr);
            Marshal.FreeHGlobal(urlPtr);
            Marshal.FreeHGlobal(identPtr);
            Marshal.FreeHGlobal(avatarPtr);
            if (client == null)
            {
                throw new Exception("Failed to connect to " + url);
            }

            interactionCallback = new _AlloClient.InteractionCallbackFun(this._interaction);
            interactionCallbackHandle = GCHandle.Alloc(interactionCallback);
            IntPtr icp = Marshal.GetFunctionPointerForDelegate(interactionCallback);
            client->interaction_callback = icp;
        }
    }
    ~AlloClient()
    {
        interactionCallbackHandle.Free();
    }

    public void SetIntent(AlloIntent intent)
    {
        unsafe
        {
            _AlloClient.SetIntentFun setIntent = (_AlloClient.SetIntentFun)Marshal.GetDelegateForFunctionPointer(client->set_intent, typeof(_AlloClient.SetIntentFun));
            setIntent(client, intent);

        }
    }

    void _Interact(string interactionType, string senderEntityId, string receiverEntityId, string requestId, string body)
    {
        unsafe
        {
            _AlloClient.InteractFun interact = (_AlloClient.InteractFun)Marshal.GetDelegateForFunctionPointer(client->interact, typeof(_AlloClient.InteractFun));

            IntPtr interactionTypePtr = Marshal.StringToHGlobalAnsi(interactionType);
            IntPtr senderEntityIdPtr = Marshal.StringToHGlobalAnsi(senderEntityId);
            IntPtr receiverEntityIdPtr = Marshal.StringToHGlobalAnsi(receiverEntityId);
            IntPtr requestIdPtr = Marshal.StringToHGlobalAnsi(requestId);
            IntPtr bodyPtr = Marshal.StringToHGlobalAnsi(body);
            interact(client, interactionTypePtr, senderEntityIdPtr, receiverEntityIdPtr, requestIdPtr, bodyPtr);
            Marshal.FreeHGlobal(interactionTypePtr);
            Marshal.FreeHGlobal(senderEntityIdPtr);
            Marshal.FreeHGlobal(receiverEntityIdPtr);
            Marshal.FreeHGlobal(requestIdPtr);
            Marshal.FreeHGlobal(bodyPtr);
        }
    }
    public void InteractOneway(string senderEntityId, string receiverEntityId, string body)
    {
        _Interact("oneway", senderEntityId, receiverEntityId, "", body);
    }
    public void InteractRequest(string senderEntityId, string receiverEntityId, string body, ResponseCallback callback)
    {
        string requestId = System.Guid.NewGuid().ToString();
        responseCallbacks[requestId] = callback;
        _Interact("request", senderEntityId, receiverEntityId, requestId, body);
    }

    public void Poll()
    {
        HashSet<string> newEntityIds = new HashSet<string>();
        HashSet<string> lostEntityIds = new HashSet<string>();
        unsafe
        {
            // 1. Run network to send and get world changes
            _AlloClient.PollFun poll = (_AlloClient.PollFun)Marshal.GetDelegateForFunctionPointer(client->poll, typeof(_AlloClient.PollFun));
            poll(client);

            // 2. Parse through all the C entities and create C# equivalents
            HashSet<string> incomingEntityIds = new HashSet<string>();

            _AlloEntity* entry = client->state.entityHead;
            while(entry != null)
            {
                string entityId = Marshal.PtrToStringAnsi(entry->id);
                AlloEntity entity;
                bool exists = entities.TryGetValue(entityId, out entity);
                if (!exists)
                {
                    entity = new AlloEntity();
                    entity.id = entityId;
                    entities[entityId] = entity;
                    newEntityIds.Add(entityId);
                }
                incomingEntityIds.Add(entityId);
                string componentsJson = _AlloClient.cJSON_Print(entry->components);

                entity.components = LitJson.JsonMapper.ToObject(componentsJson);
                entry = entry->le_next;
            }
            HashSet<String> existingEntityIds = new HashSet<string>(entities.Keys);
            lostEntityIds = new HashSet<string>(existingEntityIds);
            lostEntityIds.ExceptWith(incomingEntityIds);
        }
        if(added != null)
        {
            foreach (string addedId in newEntityIds)
            {
                added(entities[addedId]);
            }
        }
        foreach(string removedId in lostEntityIds)
        {
            if(removed != null)
            {
                removed(entities[removedId]);
            }
            entities.Remove(removedId);
        }
    }
    public void Disconnect(int reason)
    {
        unsafe
        {
            _AlloClient.DisconnectFun disconnect = (_AlloClient.DisconnectFun)Marshal.GetDelegateForFunctionPointer(client->disconnect, typeof(_AlloClient.DisconnectFun));
            disconnect(client, reason);
        }
    }

    unsafe private void _interaction(_AlloClient* _client, IntPtr _type, IntPtr _senderEntityId, IntPtr _receiverEntityId, IntPtr _requestId, IntPtr _body)
    {
        string type = Marshal.PtrToStringAnsi(_type);
        string from = Marshal.PtrToStringAnsi(_senderEntityId);
        string to = Marshal.PtrToStringAnsi(_receiverEntityId);
        string cmd = Marshal.PtrToStringAnsi(_body);
        string requestId = Marshal.PtrToStringAnsi(_requestId);

        Debug.Log("Incoming " + type + " interaction alloclient: " + from + " > " + to + ": " + cmd + ";");
        LitJson.JsonData data = LitJson.JsonMapper.ToObject(cmd);
        AlloEntity fromEntity = null;
        if (!string.IsNullOrEmpty(from))
            entities.TryGetValue(from, out fromEntity);
        AlloEntity toEntity = null;
        if (!string.IsNullOrEmpty(to))
            entities.TryGetValue(to, out toEntity);

        ResponseCallback callback = null; 
        if (type == "response" && !string.IsNullOrEmpty(requestId))
        {
            responseCallbacks.TryGetValue(requestId, out callback);
        }
        if (callback != null)
        {
            callback(cmd);
            responseCallbacks.Remove(requestId);
        }
        else if (interaction != null)
        {
            interaction(type, fromEntity, toEntity, data);
        }
    }
}

class AlloEntity
{
    public string id;
    public LitJson.JsonData components;
    public AlloComponent.Transform Transform {
        get {
            if(!components.ContainsKey("transform")) {
                return null;
            }
            LitJson.JsonData transformRep = components["transform"];
            AlloComponent.Transform transform = new AlloComponent.Transform();
            LitJson.JsonData posRep = transformRep["position"];
            LitJson.JsonData rotRep = transformRep["rotation"];
            transform.position = JsonVec3(posRep);
            transform.rotation = JsonVec3(rotRep);
            return transform;
        }
    }

    public static float JsonFlt(LitJson.JsonData data)
    {
        if (data.IsInt)
        {
            return (float)(int)data;
        }
        else if (data.IsDouble)
        {
            return (float)(double)data;
        }
        return 0;
    }

    public static Vector2 JsonVec2(LitJson.JsonData vector)
    {
        return new Vector2(JsonFlt(vector[0]), JsonFlt(vector[1]));
    }

    public static Vector3 JsonVec3(LitJson.JsonData vector)
    {
        return new Vector3(JsonFlt(vector[0]), JsonFlt(vector[1]), JsonFlt(vector[2]));
    }
};

namespace AlloComponent {
    class Transform {
        public Vector3 position;
        public Vector3 rotation;
    }



}








[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
struct _AlloState
{
    public Int64 revision;
    public unsafe _AlloEntity* entityHead;
};

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct AlloIntent
{
    public double zmovement;
    public double xmovement;
    public double yaw;
    public double pitch;
};

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
struct AlloVector
{
    public double x, y, z;
};

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
struct _AlloEntity
{
    public IntPtr id; // to string
    public IntPtr components; // cJSON*
    public unsafe _AlloEntity* le_next;
};

[StructLayout(LayoutKind.Sequential)]
struct _AlloClient
{
    [DllImport("liballonet")]
    public unsafe static extern bool allo_initialize(bool redirect_stdout);

    [DllImport("liballonet")]
    public unsafe static extern _AlloClient* allo_connect(IntPtr urlString, IntPtr identity, IntPtr avatarDesc);

    [DllImport("liballonet")]
    public unsafe static extern string cJSON_Print(IntPtr cjson);

    public IntPtr set_intent;
    public IntPtr interact;
    public IntPtr disconnect;

    public IntPtr poll;

    public IntPtr state_callback;
    public IntPtr interaction_callback;

    // internal
    public _AlloState state;
    public IntPtr _internal;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void SetIntentFun(_AlloClient* client, AlloIntent intent);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void InteractFun(_AlloClient* client, IntPtr interactionType, IntPtr senderEntityId, IntPtr receiverEntityId, IntPtr requestId, IntPtr body);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void DisconnectFun(_AlloClient* client, int reason);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void PollFun(_AlloClient* client);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void StateCallbackFun(_AlloClient* client, ref _AlloState state);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void InteractionCallbackFun(_AlloClient* client, IntPtr type, IntPtr senderEntityId, IntPtr receiverEntityId, IntPtr requestId, IntPtr body);


};
