using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


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

    public AlloClient(string url)
    {
        unsafe
        {
            IntPtr urlPtr = Marshal.StringToHGlobalAnsi(MenuParameters.urlToOpen);
            client = _AlloClient.allo_connect(urlPtr);
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
                entity.position = new Vector3((float)entry->position.x, (float)entry->position.y, (float)entry->position.z);
                entity.rotation = new Vector3((float)entry->rotation.x, (float)entry->rotation.y, (float)entry->rotation.z);

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
    public Vector3 position;
    public Vector3 rotation;
};








[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
struct _AlloState
{
    public Int64 revision;
    public unsafe _AlloEntity* entityHead;
};

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
struct AlloIntent
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
    public AlloVector position;
    public AlloVector rotation;
    public unsafe _AlloEntity* le_next;
};

[StructLayout(LayoutKind.Sequential)]
struct _AlloClient
{
    [DllImport("liballonet")]
    public unsafe static extern bool allo_initialize(bool redirect_stdout);

    [DllImport("liballonet")]
    public unsafe static extern _AlloClient* allo_connect(IntPtr urlString);

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
