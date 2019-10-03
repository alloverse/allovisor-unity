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
    public delegate void Disconnected();
    public Disconnected disconnected = null;

    public delegate void ResponseCallback(string body);
    private Dictionary<string, ResponseCallback> responseCallbacks = new Dictionary<string, ResponseCallback>();

    private _AlloClient.InteractionCallbackFun interactionCallback;
    private GCHandle interactionCallbackHandle;
    private _AlloClient.DisconnectedCallbackFun disconnectedCallback;
    private GCHandle disconnectedCallbackHandle;
    private GCHandle thisHandle;


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

            interactionCallback = new _AlloClient.InteractionCallbackFun(AlloClient._interaction);
            interactionCallbackHandle = GCHandle.Alloc(interactionCallback);
            IntPtr icp = Marshal.GetFunctionPointerForDelegate(interactionCallback);
            client->interaction_callback = icp;

            disconnectedCallback = new _AlloClient.DisconnectedCallbackFun(AlloClient._disconnected);
            disconnectedCallbackHandle = GCHandle.Alloc(disconnectedCallback);
            IntPtr icp2 = Marshal.GetFunctionPointerForDelegate(disconnectedCallback);
            client->disconnected_callback = icp2;

            thisHandle = GCHandle.Alloc(this);
            client->_backref = (IntPtr)thisHandle;

        }
    }
    ~AlloClient()
    {
        if (interactionCallbackHandle.IsAllocated)
        {
            interactionCallbackHandle.Free();
            disconnectedCallbackHandle.Free();
        }
    }

    public void SetIntent(AlloIntent intent)
    {
        unsafe
        {
            _AlloClient.alloclient_set_intent(client, intent);
        }
    }

    void _Interact(string interactionType, string senderEntityId, string receiverEntityId, string requestId, string body)
    {
        unsafe
        {
            IntPtr interactionTypePtr = Marshal.StringToHGlobalAnsi(interactionType);
            IntPtr senderEntityIdPtr = Marshal.StringToHGlobalAnsi(senderEntityId);
            IntPtr receiverEntityIdPtr = Marshal.StringToHGlobalAnsi(receiverEntityId);
            IntPtr requestIdPtr = Marshal.StringToHGlobalAnsi(requestId);
            IntPtr bodyPtr = Marshal.StringToHGlobalAnsi(body);
            _AlloClient.alloclient_send_interaction(client, interactionTypePtr, senderEntityIdPtr, receiverEntityIdPtr, requestIdPtr, bodyPtr);
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
            _AlloClient.alloclient_poll(client);

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

        unsafe
        {
            // 3. poll for new interactions
            _AlloInteraction* newinter = null;
            while((newinter = _AlloClient.alloclient_pop_interaction(client)) != null)
            {
                _interaction(client, newinter->type, newinter->senderEntityId, newinter->receiverEntityId, newinter->requestId, newinter->body);
                _AlloClient.allo_interaction_free(newinter);
            }
        }
    }
    public void Disconnect(int reason)
    {
        unsafe
        {
            if (client != null)
            {
                _AlloClient.alloclient_disconnect(client);
                client = null;

                thisHandle.Free();
            }
        }
    }

    [AOT.MonoPInvokeCallback(typeof(_AlloClient.InteractionCallbackFun))]
    static unsafe private void _disconnected(_AlloClient* _client)
    {
        GCHandle backref = (GCHandle)_client->_backref;
        AlloClient self = backref.Target as AlloClient;
        Debug.Log("_disconnected: calling delegate " + self.disconnected.ToString());
        self.disconnected?.Invoke();
        Debug.Log("_disconnected: deallocating");
        self.Disconnect(-1);
    }

    [AOT.MonoPInvokeCallback(typeof(_AlloClient.InteractionCallbackFun))]
    static unsafe private void _interaction(_AlloClient* _client, IntPtr _type, IntPtr _senderEntityId, IntPtr _receiverEntityId, IntPtr _requestId, IntPtr _body)
    {
        string type = Marshal.PtrToStringAnsi(_type);
        string from = Marshal.PtrToStringAnsi(_senderEntityId);
        string to = Marshal.PtrToStringAnsi(_receiverEntityId);
        string cmd = Marshal.PtrToStringAnsi(_body);
        string requestId = Marshal.PtrToStringAnsi(_requestId);

        GCHandle backref = (GCHandle)_client->_backref;
        AlloClient self = backref.Target as AlloClient;

        Debug.Log("Incoming " + type + " interaction alloclient: " + from + " > " + to + ": " + cmd + ";");
        LitJson.JsonData data = LitJson.JsonMapper.ToObject(cmd);
        AlloEntity fromEntity = null;
        if (!string.IsNullOrEmpty(from))
            self.entities.TryGetValue(from, out fromEntity);
        AlloEntity toEntity = null;
        if (!string.IsNullOrEmpty(to))
            self.entities.TryGetValue(to, out toEntity);

        ResponseCallback callback = null; 
        if (type == "response" && !string.IsNullOrEmpty(requestId))
        {
            self.responseCallbacks.TryGetValue(requestId, out callback);
        }

        if (callback != null)
        {
            callback(cmd);
            self.responseCallbacks.Remove(requestId);
        }
        else
        {
            self.interaction?.Invoke(type, fromEntity, toEntity, data);
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
public struct _AlloInteraction
{
    public IntPtr type;
    public IntPtr senderEntityId;
    public IntPtr receiverEntityId;
    public IntPtr requestId;
    public IntPtr body;
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
    public unsafe static extern void alloclient_disconnect(_AlloClient* client);

    [DllImport("liballonet")]
    public unsafe static extern void alloclient_poll(_AlloClient* client);

    [DllImport("liballonet")]
    public unsafe static extern void alloclient_send_interaction(_AlloClient* client, IntPtr type, IntPtr sender, IntPtr receiver, IntPtr rid, IntPtr body);

    [DllImport("liballonet")]
    public unsafe static extern void alloclient_set_intent(_AlloClient* client, AlloIntent intent);

    [DllImport("liballonet")]
    public unsafe static extern _AlloInteraction *alloclient_pop_interaction(_AlloClient* client);

    [DllImport("liballonet")]
    public unsafe static extern void allo_interaction_free(_AlloInteraction *interaction);

    [DllImport("liballonet")]
    public unsafe static extern string cJSON_Print(IntPtr cjson);


    public IntPtr state_callback;
    public IntPtr interaction_callback;
    public IntPtr disconnected_callback;

    // internal
    public _AlloState state;
    public IntPtr _internal;
    public IntPtr _backref;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void StateCallbackFun(_AlloClient* client, ref _AlloState state);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void InteractionCallbackFun(_AlloClient* client, IntPtr type, IntPtr senderEntityId, IntPtr receiverEntityId, IntPtr requestId, IntPtr body);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void DisconnectedCallbackFun(_AlloClient* client);
};
