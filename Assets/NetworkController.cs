using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
struct AlloClient
{
    [DllImport("liballonet")]
    public unsafe static extern bool allo_initialize(bool redirect_stdout);

    [DllImport("liballonet")]
    public unsafe static extern AlloClient *allo_connect();

    public IntPtr set_intent;
    public IntPtr interact;
    public IntPtr disconnect;

    public IntPtr poll;

    public IntPtr state_callback;
    public IntPtr interaction_callback;

    // internal
    public AlloState state;
    public IntPtr _internal;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void SetIntentFun(AlloClient *client, AlloIntent intent);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void InteractFun(AlloClient *client, string entityId, string cmd);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void DisconnectFun(AlloClient *client);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void PollFun(AlloClient *client);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void StateCallbackFun(AlloClient *client, ref AlloState state);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void InteractionCallbackFun(AlloClient *client, string senderEntityId, string receiverEntityId, string cmd);
}

[StructLayout(LayoutKind.Sequential)]
struct AlloState
{
    public Int64 revision;
    public unsafe AlloEntity* entityHead;
};

[StructLayout(LayoutKind.Sequential)]
struct AlloIntent
{
    public double zmovement;
    public double xmovement;
    public double yaw;
    public double pitch;
};

[StructLayout(LayoutKind.Sequential)]
struct AlloVector
{
    public double x, y, z;
};

[StructLayout(LayoutKind.Sequential)]
struct AlloEntity
{
    public IntPtr id; // to string
    public AlloVector position;
    public AlloVector rotation;
    public unsafe AlloEntity* le_next;
}



public class NetworkController : MonoBehaviour
{
    unsafe AlloClient *client;


    void Start()
    {
        if(!AlloClient.allo_initialize(true)) {
            throw new Exception("Unable to initialize AlloNet");
        }

        AlloIntent intent = new AlloIntent
        {
            zmovement = 1
        };

        unsafe
        {
            client = AlloClient.allo_connect();
            AlloClient.SetIntentFun setIntent = (AlloClient.SetIntentFun)Marshal.GetDelegateForFunctionPointer(client->set_intent, typeof(AlloClient.SetIntentFun));
            setIntent(client, intent);
        }
    }

    void Update()
    {
        unsafe
        {
            AlloClient.PollFun poll = (AlloClient.PollFun)Marshal.GetDelegateForFunctionPointer(client->poll, typeof(AlloClient.PollFun));
            poll(client);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {

        }
    }

    void OnApplicationQuit()
    {

    }


}
