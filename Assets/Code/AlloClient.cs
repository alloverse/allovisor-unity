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
    public delegate void Interaction(AlloEntity from, AlloEntity to, LitJson.JsonData command);
    public Interaction interaction = null;

    public AlloClient(string url, AlloIdentity identity, LitJson.JsonData avatarDesc)
    {
        unsafe
        {
            IntPtr urlPtr = Marshal.StringToHGlobalAnsi(MenuParameters.urlToOpen);
            IntPtr identPtr = Marshal.StringToHGlobalAnsi(LitJson.JsonMapper.ToJson(identity));
            IntPtr avatarPtr = Marshal.StringToHGlobalAnsi(LitJson.JsonMapper.ToJson(avatarDesc));

            client = _AlloClient.allo_connect(urlPtr, identPtr, avatarPtr);
            if (client == null)
            {
                Marshal.FreeHGlobal(urlPtr);
                throw new Exception("Failed to connect to " + url);
            }
            client->interaction_callback = Marshal.GetFunctionPointerForDelegate(new _AlloClient.InteractionCallbackFun(this._interaction));
        }
    }
    public void SetIntent(AlloIntent intent)
    {
        unsafe
        {
            _AlloClient.SetIntentFun setIntent = (_AlloClient.SetIntentFun)Marshal.GetDelegateForFunctionPointer(client->set_intent, typeof(_AlloClient.SetIntentFun));
            setIntent(client, intent);

        }
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

    unsafe private void _interaction(_AlloClient *_client, IntPtr _from, IntPtr _to, IntPtr _cmd)
    {
        string from = Marshal.PtrToStringAnsi(_from);
        string to = Marshal.PtrToStringAnsi(_to);
        string cmd = Marshal.PtrToStringAnsi(_cmd);
        LitJson.JsonData data = LitJson.JsonMapper.ToObject(cmd);
        AlloEntity fromEntity = from == null ? null : entities.ContainsKey(from) ? entities[from] : null;
        AlloEntity toEntity = to == null ? null : entities.ContainsKey(to) ? entities[to] : null;
        if (interaction != null)
        {
            interaction(fromEntity, toEntity, data);
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
            transform.position = new Vector3(JsonFlt(posRep[0]), JsonFlt(posRep[1]), JsonFlt(posRep[2]));
            transform.rotation = new Vector3(JsonFlt(rotRep[0]), JsonFlt(rotRep[1]), JsonFlt(rotRep[2]));
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
    public unsafe delegate void InteractFun(_AlloClient* client, string entityId, string cmd);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void DisconnectFun(_AlloClient* client, int reason);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void PollFun(_AlloClient* client);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void StateCallbackFun(_AlloClient* client, ref _AlloState state);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void InteractionCallbackFun(_AlloClient* client, IntPtr senderEntityId, IntPtr receiverEntityId, IntPtr cmd);


};
