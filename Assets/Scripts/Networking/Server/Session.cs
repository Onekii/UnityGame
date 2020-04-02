﻿using Assets.Scripts.Networking.Client;
using Assets.Scripts.Networking.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityGame.Scripts.Network.Shared;

namespace Assets.Scripts.Networking.Server
{
    public partial class Session
    {
        class TCPImplementation
        {
            private TcpClient _client;
            private NetworkStream _stream;
            private byte[] _buffer;
            private int _clientId;

            Session _ownerSession;

            internal TCPImplementation(TcpClient client, int clientid, Session owner)
            {
                _client = client;
                _stream = _client.GetStream();
                _buffer = new byte[4096];
                _clientId = clientid;

                _ownerSession = owner;
            }

            public void Connect()
            {
                _client.ReceiveBufferSize = bufferSize;
                _client.SendBufferSize = bufferSize;

                _stream = _client.GetStream();

                _buffer = new byte[bufferSize];
                _stream.BeginRead(_buffer, 0, bufferSize, OnReceivedData, null);
            }

            public void SendDirectMessage(ByteBuffer buff)
            {
                try
                {
                    if (_client != null)
                    {
                        byte[] buffer = buff.AsByteArray();

                        _stream.BeginWrite(buffer, 0, buffer.Length, null, null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send data to player {_clientId}. Error {ex}");
                }
            }

            private void OnReceivedData(IAsyncResult result)
            {
                try
                {
                    int dataLength = _stream.EndRead(result);
                    if (dataLength <= 0)
                    {
                        SessionManager.Instance().KickSession(_ownerSession);
                        return;
                    }

                    byte[] incomingData = new byte[dataLength];
                    Array.Copy(_buffer, incomingData, dataLength);

                    ByteBuffer buff = new ByteBuffer(incomingData);

                    _ownerSession.AddPacketToQueue(buff);

                    _stream.BeginRead(_buffer, 0, bufferSize, OnReceivedData, null);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to receive data due to an exception with message {ex.Message} at {ex.StackTrace}");
                }
            }

            internal void Kick()
            {
                _client.Close();
                _stream.Close();
            }
        }

        internal void AddPacketToQueue(ByteBuffer buffer)
        {
            _receiveQueue.Enqueue(buffer);
        }

        internal void InstantiatePlayer(Vector3 spawnpoint)
        {
            GameObject newGo = GameObject.Instantiate(SessionManager.Instance().PlayerPrefab, spawnpoint, Quaternion.identity);
            _player = newGo.GetComponent<ServerPlayerController>();
            _player.Id = _id;
            _player.Name = "Player";
        }

        internal void UpdatePlayerLocation(Vector3 newLocation)
        {
            _player.transform.position = newLocation;
        }

        internal void UpdatePlayerRotation(Vector3 newRotation)
        {
            _player.transform.rotation = Quaternion.Euler(newRotation);
        }

        internal void SendUDPData(ByteBuffer buffer)
        {
            _udp.SendData(buffer);
            //_udpSendQueue.Enqueue(buffer);
        }

        class UDPImplementation
        {
            private IPEndPoint _endPoint;
            internal IPEndPoint EndPoint { get => _endPoint; }

            private int _clientId;

            private bool _connected;

            internal UDPImplementation(int clientid)
            {
                _endPoint = null;
                _clientId = clientid;
                _connected = false;
            }

            internal void Connect(IPEndPoint ip)
            {
                _endPoint = ip;
                _connected = true;
            }

            internal void SendData(ByteBuffer packet)
            {
                try
                {
                    if (_endPoint is null)
                        return;

                    UdpManager.Instance().SendUDPData(_endPoint, packet);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                    Debug.Log(ex.StackTrace);
                }
            }

            internal bool IsConnected() => _connected;

            internal void Disconnect()
            {
                _endPoint = null;
            }
        }

        internal bool IsUDPConnected() => _udp.IsConnected();

        internal void ConnectTcp()
        {
            _tcp.Connect();
        }

        internal void ConnectUDP(IPEndPoint clientEndpoint)
        {
            _udp.Connect(clientEndpoint);
        }

        internal bool UDPEndpointMatches(IPEndPoint clientEndpoint) => _udp.EndPoint.ToString() == clientEndpoint.ToString();

        private int _id;
        private const int bufferSize = 4096;
        private const int MAX_PACKETS_IN_SINGLE_UPDATE = 50;

        //private Queue<ByteBuffer> _sendQueue;
        //private Queue<ByteBuffer> _udpSendQueue;
        private Queue<ByteBuffer> _receiveQueue;

        private ServerPlayerController _player;

        private TCPImplementation _tcp;
        private UDPImplementation _udp;

        private Assets.Scripts.Logs.Logger _logger;

        public Session(int id, TcpClient client)
        {
            _id = id;

            _tcp = new TCPImplementation(client, id, this);
            _udp = new UDPImplementation(id);

            UdpManager.Instance();

            //_sendQueue = new Queue<ByteBuffer>();
           // _udpSendQueue = new Queue<ByteBuffer>();
            _receiveQueue = new Queue<ByteBuffer>();
        }

        public void SendDirectMessage(ByteBuffer buff)
        {
            //_sendQueue.Enqueue(buff);
            _tcp.SendDirectMessage(buff);
        }

        internal void Update(double diff)
        {
            int processedPackets = 0;

            while (_receiveQueue.Count > 0 && processedPackets < MAX_PACKETS_IN_SINGLE_UPDATE)
            {
                ByteBuffer buff = _receiveQueue.Dequeue();
                HandlePacket(buff);

                processedPackets++;
            }
        }

        internal void UpdateSendQueue(double diff)
        {
            //while (_sendQueue.Count > 0)
            //{
            //    ByteBuffer buff = _sendQueue.Dequeue();
            //    _tcp.SendDirectMessage(buff);
            //}
            
            //while (_udpSendQueue.Count > 0)
            //{
            //    ByteBuffer buff = _udpSendQueue.Dequeue();
            //    _udp.SendData(buff);
            //}
        }

        internal ServerPlayerController GetPlayer() => _player;

        internal void HandlePacket(ByteBuffer packet)
        {
            try
            {
                int opcode = packet.ReadInt();

                if (ServerHandler.OpcodeTable.TryGetValue((Opcode)opcode, out ServerHandler.OpcodeHandler handler))
                {
                    handler.Invoke(packet, this);
                }
                else
                {
                    Debug.LogError("Recieved unknown packet, disconnecting client");
                    SessionManager.Instance().KickSession(this);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.Log("Packet dump: " + packet.AsByteArray());
            }
        }

        internal void Kick()
        {
            _tcp.Kick();
            _udp.Disconnect();
        }

        internal int GetId()
        {
            return _id;
        }

        internal void SendAuth()
        {
            ByteBuffer buff = new ByteBuffer(Opcode.SMSG_AUTH);
            buff.Write(_id);
            SendDirectMessage(buff);
        }
    }
}
