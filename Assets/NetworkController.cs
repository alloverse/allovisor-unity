using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;

class AlloClient
{
    private unsafe AlloClientInt *client;
    public AlloClient()
    {
        unsafe
        {
            client = AlloClientInt.allo_connect();
        }
    }
    public void SetIntent(AlloIntent intent)
    {
        unsafe
        {
            AlloClientInt.SetIntentFun setIntent = (AlloClientInt.SetIntentFun)Marshal.GetDelegateForFunctionPointer(client->set_intent, typeof(AlloClientInt.SetIntentFun));
            setIntent(client, intent);
        }
    }
    public void Poll()
    {
        unsafe
        {
            AlloClientInt.PollFun poll = (AlloClientInt.PollFun)Marshal.GetDelegateForFunctionPointer(client->poll, typeof(AlloClientInt.PollFun));
            poll(client);
        }
    }
    public void Disconnect(int reason)
    {
        unsafe
        {
            AlloClientInt.DisconnectFun disconnect = (AlloClientInt.DisconnectFun)Marshal.GetDelegateForFunctionPointer(client->disconnect, typeof(AlloClientInt.DisconnectFun));
            disconnect(client, reason);
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
struct AlloClientInt
{
    [DllImport("liballonet")]
    public unsafe static extern bool allo_initialize(bool redirect_stdout);

    [DllImport("liballonet")]
    public unsafe static extern AlloClientInt* allo_connect();

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
    public unsafe delegate void SetIntentFun(AlloClientInt* client, AlloIntent intent);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void InteractFun(AlloClientInt* client, string entityId, string cmd);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void DisconnectFun(AlloClientInt* client, int reason);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void PollFun(AlloClientInt* client);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void StateCallbackFun(AlloClientInt* client, ref AlloState state);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void InteractionCallbackFun(AlloClientInt* client, string senderEntityId, string receiverEntityId, string cmd);
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
