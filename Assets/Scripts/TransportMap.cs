using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public struct TransportPin:INetworkSerializable
    {
        public ulong id;public Vector3 position;public byte type,condition,fuel;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer)where T:IReaderWriter{serializer.SerializeValue(ref id);serializer.SerializeValue(ref position);serializer.SerializeValue(ref type);serializer.SerializeValue(ref condition);serializer.SerializeValue(ref fuel);}
    }
    public sealed partial class WildfrontMatch
    {
        public TransportPin[] TransportPins{get;private set;}=new TransportPin[0];
        public float TransportUpdatedAt{get;private set;}
        float nextTransport;
        void TransportTick()
        {
            if(!IsServer||Time.unscaledTime<nextTransport)return;nextTransport=Time.unscaledTime+1;
            foreach(var group in Ranger.Players.Where(p=>p&&p.IsSpawned&&!p.IsBot.Value).GroupBy(p=>p.Team.Value))
            {
                int team=group.Key;var pins=RoadVehicle.All.Where(car=>car&&car.IsSpawned&&VisibleTransport(car,team)).Select(car=>new TransportPin{id=car.NetworkObjectId,position=car.transform.position,type=(byte)car.Type.Value,condition=(byte)Mathf.Clamp(car.Condition.Value,0,100),fuel=(byte)Mathf.Clamp(Mathf.RoundToInt(car.Fuel.Value),0,100)}).ToArray();
                TransportMapClientRpc(pins,new ClientRpcParams{Send=new ClientRpcSendParams{TargetClientIds=group.Select(p=>p.OwnerClientId).Distinct().ToArray()}});
            }
        }
        public static bool VisibleTransport(RoadVehicle car,int team)
        {
            for(int seat=0;seat<car.Capacity;seat++){ulong id=car.Occupant(seat);if(id==RoadVehicle.Empty)continue;var occupant=Ranger.Players.FirstOrDefault(p=>p&&p.NetworkObjectId==id);if(occupant&&occupant.Team.Value!=team)return false;}return true;
        }
        [ClientRpc]void TransportMapClientRpc(TransportPin[] pins,ClientRpcParams rpc=default){TransportPins=pins;TransportUpdatedAt=Time.unscaledTime;}
    }
}
