﻿using Assets.Scripts.Net.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Net.Server
{
    class ServerPacketSender
    {
        /// <summary>Sends a packet to a client via TCP.</summary>
        /// <param name="_toClient">The client to send the packet the packet to.</param>
        /// <param name="_packet">The packet to send to the client.</param>
        private static void SendTCPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.clients[_toClient].tcp.SendData(_packet);
        }

        /// <summary>Sends a packet to a client via UDP.</summary>
        /// <param name="_toClient">The client to send the packet the packet to.</param>
        /// <param name="_packet">The packet to send to the client.</param>
        private static void SendUDPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.clients[_toClient].udp.SendData(_packet);
        }

        /// <summary>Sends a packet to all clients via TCP.</summary>
        /// <param name="_packet">The packet to send.</param>
        private static void SendTCPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }

        /// <summary>Sends a packet to all clients except one via TCP.</summary>
        /// <param name="_exceptClient">The client to NOT send the data to.</param>
        /// <param name="_packet">The packet to send.</param>
        private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].tcp.SendData(_packet);
                }
            }
        }

        /// <summary>Sends a packet to all clients via UDP.</summary>
        /// <param name="_packet">The packet to send.</param>
        private static void SendUDPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }
        /// <summary>Sends a packet to all clients except one via UDP.</summary>
        /// <param name="_exceptClient">The client to NOT send the data to.</param>
        /// <param name="_packet">The packet to send.</param>
        private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].udp.SendData(_packet);
                }
            }
        }

        #region Packets
        /// <summary>Tells a client to spawn a player.</summary>
        /// <param name="_toClient">The client that should spawn the player.</param>
        /// <param name="_player">The player to spawn.</param>
        public static void SpawnPlayer(int _toClient, Player _player)
        {
            using (Packet _packet = new Packet((int)Opcodes.Opcode.SMSG_SPAWN_PLAYER))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.transform.position);
                _packet.Write(_player.transform.rotation);

                SendTCPData(_toClient, _packet);
            }
        }

        public static void SendWelcome(int _toClient, string msg)
        {
            using (Packet _packet = new Packet((int)Opcodes.Opcode.SMSG_WELCOME))
            {
                _packet.Write(_toClient);
                _packet.Write(msg.Length);
                _packet.Write(msg);

                SendTCPData(_toClient, _packet);
            }
        }

        public static void PlayerPosition(Player plr)
        {
            using (Packet _packet = new Packet((int)Opcodes.Opcode.MSG_PLAYER_MOVEMENT))
            {
                _packet.Write(plr.id);
                _packet.Write(plr.transform.position);

                SendUDPDataToAll(_packet);
            }
        }

        public static void PlayerRotation(Player plr)
        {
            using (Packet _packet = new Packet((int)Opcodes.Opcode.MSG_PLAYER_MOVEMENT))
            {
                _packet.Write(plr.id);
                _packet.Write(plr.transform.rotation);

                SendUDPDataToAll(plr.id, _packet);
            }
        }
        #endregion
    }
}
