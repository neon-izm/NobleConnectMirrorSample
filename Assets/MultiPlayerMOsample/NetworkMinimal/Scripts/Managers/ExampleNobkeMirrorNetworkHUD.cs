using NobleConnect.Mirror;
using NobleMirrorSample;
using UnityEngine;


// A GUI for use with NobleNetworkManager
public class ExampleNobkeMirrorNetworkHUD : MonoBehaviour
{
    [SerializeField]
    // The NetworkManager controlled by the HUD
    NobleMirrorLobbyManagerMinimal networkManager;

    // The relay ip and port from the GUI text box
    string hostIP = "";
    string hostPort = "";

    // Used to determine which GUI to display
    bool isHost, isClient;
        
    // Get a reference to the NetworkManager
    public void Start()
    {
        hostIP = PlayerPrefs.GetString("IP", "localhost");

        hostPort= PlayerPrefs.GetString("PORT","60000");
        // Cast from Unity's NetworkManager to a NobleNetworkManager.
        networkManager = (NobleMirrorLobbyManagerMinimal)networkManager;
    }
        
    // Draw the GUI
    private void OnGUI()
    {
        
        if (!isHost && !isClient)
        {
            // Host button
            if (GUI.Button(new Rect(10, 10, 100, 30), "Host"))
            {
                isHost = true;
                isClient = false;

                networkManager.StartHost();
            }

            // Client button
            if (GUI.Button(new Rect(10, 50, 100, 30), "Client"))
            {
                isHost = false;
                isClient = true;
                
            }
        }
        else
        {
            // Host or client GUI
            if (isHost) GUIHost();
            else if (isClient) GUIClient();
        }
    }

    // Draw the host GUI
    void GUIHost()
    {
        // Display host addresss
        if (networkManager.HostEndPoint != null)
        {
            GUI.Label(new Rect(10, 10, 150, 22), "Host IP:");
            GUI.TextField(new Rect(170, 10, 120, 22), networkManager.HostEndPoint.Address.ToString(), "Label");
            GUI.Label(new Rect(10, 37, 150, 22), "Host Port:");
            GUI.TextField(new Rect(170, 37, 60, 22), networkManager.HostEndPoint.Port.ToString(), "Label");
        }

        // Disconnect Button
        if (GUI.Button(new Rect(10, 81, 110, 30), "Disconnect"))
        {
            networkManager.StopHost();
            isHost = false;
        }

        if (!NobleServer.active) isHost = false;
    }

    // Draw the client GUI
    void GUIClient()
    {
        if (!networkManager.isNetworkActive)
        {
            // Text boxes for entering host's address
            GUI.Label(new Rect(10, 10, 150, 22), "Host IP:");
            hostIP = GUI.TextField(new Rect(170, 10, 120, 22), hostIP);
            GUI.Label(new Rect(10, 37, 150, 22), "Host Port:");
            hostPort = GUI.TextField(new Rect(170, 37, 60, 22), hostPort);

            // Connect button
            if (GUI.Button(new Rect(115, 81, 120, 30), "Connect"))
            {
                PlayerPrefs.SetString("IP", hostIP);
                
                // Connecting works just like normal except that you use the 
                // host address from the NobleNetworkManager instead of their normal IP and port
                networkManager.networkAddress = hostIP;
                networkManager.networkPort = ushort.Parse(hostPort);
                networkManager.StartClient();
            }

            // Back button
            if (GUI.Button(new Rect(10, 81, 95, 30), "Back"))
            {
                isClient = false;
            }
        }
        else 
        {
            // Disconnect button
            GUI.Label(new Rect(10, 10, 150, 22), "Connection type: " + networkManager.client.latestConnectionType);
            if (GUI.Button(new Rect(10, 50, 110, 30), "Disconnect"))
            {
                if (networkManager.client.isConnected)
                {
                    // If we are already connected it is best to quit gracefully by sending
                    // a disconnect message to the host.
                    networkManager.client.connection.Disconnect();
                
                    // If the connection is still in progress StopClient will cancel it
                    networkManager.StopClient();
                }
                isClient = false;
            }
        }
    }
}