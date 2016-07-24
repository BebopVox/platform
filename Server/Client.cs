﻿using System;
using GTANetworkShared;
using Lidgren.Network;

namespace GTANetworkServer
{
    public class Client
    {
        public NetConnection NetConnection { get; private set; }
        public DeltaCompressor DeltaCompressor { get; set; }
        public string SocialClubName { get; set; }
        public string Name { get; set; }
        public float Latency { get; set; }
        public ParseableVersion RemoteScriptVersion { get; set; }
        public int GameVersion { get; set; }

        public NetHandle CurrentVehicle { get; set; }
        public Vector3 Position { get; internal set; }
        public Vector3 Rotation { get; internal set; }
        public int Health { get; internal set; }
        public int Armor { get; internal set; }
        public bool IsInVehicle { get; internal set; }

        public DateTime LastUpdate { get; internal set; }

        public NetHandle CharacterHandle { get; set; }

        public PedProperties Properties
        {
            get { return Program.ServerInstance.NetEntityHandler.ToDict()[CharacterHandle.Value] as PedProperties; }
        }

        public Client(NetConnection nc)
        {
            NetConnection = nc;
            DeltaCompressor = new DeltaCompressor(this);
            CharacterHandle = new NetHandle(Program.ServerInstance.NetEntityHandler.GeneratePedHandle());
        }
    }
}