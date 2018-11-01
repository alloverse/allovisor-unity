using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;


public class NetworkController : MonoBehaviour {
    #region private members     
    private TcpClient socket;
    private Thread consumerThread;
    #endregion

    void Start()
    {
        ConnectToTcpServer();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendMessage();
        }
    }
    /// <summary>   
    /// Setup socket connection.    
    /// </summary>  
    private void ConnectToTcpServer()
    {
        consumerThread = new Thread(new ThreadStart(ListenForData));
        consumerThread.IsBackground = true;
        consumerThread.Start();
    }

    /// <summary>   
    /// Runs in background clientReceiveThread; Listens for incomming data.     
    /// </summary>     
    private void ListenForData()
    {
        try
        {
            socket = new TcpClient("localhost", 16016);
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                // Get a stream object for reading              
                using (NetworkStream stream = socket.GetStream())
                {
                    int length;
                    // Read incomming stream into byte arrary.                  
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incommingData = new byte[length];
                        Array.Copy(bytes, 0, incommingData, 0, length);
                        // Convert byte array to string message.                        
                        string serverMessage = Encoding.ASCII.GetString(incommingData);
                        Debug.Log("server message received as: " + serverMessage);
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    private void SendMessage()
    {
        if (socket == null)
        {
            throw new Exception("Not connected, can't send");
        }

        // Get a stream object for writing.             
        NetworkStream stream = socket.GetStream();
        if (!stream.CanWrite)
        {
            throw new Exception("No write stream");
        }

        string clientMessage = "This is a message from one of your clients.";
        // Convert string message to byte array.                 
        byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
        // Write byte array to socketConnection stream.                 
        stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
        Debug.Log("Client sent her message - should be received by server");
    }
}
