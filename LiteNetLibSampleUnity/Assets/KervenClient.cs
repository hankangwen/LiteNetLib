using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using UnityEngine;

public class KervenClient : MonoBehaviour, INetEventListener
{
    private NetManager _netClient;
    private const string IpAddress = "localhost";// 8.138.136.204
    private const int Port = 2050;
    private const string ConnectKey = "sample_app";

    [SerializeField] private GameObject _clientBall;
    [SerializeField] private GameObject _clientBallInterpolated;

    private float _newBallPosX;
    private float _oldBallPosX;
    private float _lerpTime;

    private void Start()
    {
        _netClient = new NetManager(this);
        _netClient.UnconnectedMessagesEnabled = true;
        _netClient.UpdateTime = 15;
        _netClient.Start();
        _netClient.Connect(IpAddress, Port, ConnectKey);
    }

    private void Update()
    {
        _netClient.PollEvents();

        var peer = _netClient.FirstPeer;
        if (peer != null && peer.ConnectionState == ConnectionState.Connected)
        {
            //Fixed delta set to 0.05
            var pos = _clientBallInterpolated.transform.position;
            pos.x = Mathf.Lerp(_oldBallPosX, _newBallPosX, _lerpTime);
            _clientBallInterpolated.transform.position = pos;

            //Basic lerp
            _lerpTime += Time.deltaTime / Time.fixedDeltaTime;
        }
        else
        {
            _netClient.SendBroadcast(new byte[] {1}, 80);
        }
    }

    private void OnDestroy()
    {
        if (_netClient != null)
            _netClient.Stop();
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        Debug.Log("[CLIENT] We connected to " + peer);
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        Debug.Log("[CLIENT] We received error " + socketErrorCode);
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader dataReader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (deliveryMethod == DeliveryMethod.ReliableOrdered)
        {
            Debug.Log($"We got: {dataReader.GetString(100)}");
            dataReader.Recycle();
            return;
        }

        _newBallPosX = dataReader.GetFloat();

        var pos = _clientBall.transform.position;

        _oldBallPosX = pos.x;
        pos.x = _newBallPosX;

        _clientBall.transform.position = pos;

        _lerpTime = 0f;

        dataReader.Recycle();
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.BasicMessage && _netClient.ConnectedPeersCount == 0 && reader.GetInt() == 1)
        {
            Debug.Log("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
            _netClient.Connect(remoteEndPoint, "sample_app");
        }
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {

    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {

    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("[CLIENT] We disconnected because " + disconnectInfo.Reason);
    }
}
