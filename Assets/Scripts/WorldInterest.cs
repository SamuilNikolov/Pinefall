using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class WorldInterest:MonoBehaviour
    {
        static readonly List<NetworkObject> actors=new List<NetworkObject>();readonly List<ulong> clients=new List<ulong>();readonly List<Vector3> viewpoints=new List<Vector3>();float next;int cursor;
        public static void Install(NetworkBehaviour behaviour){var obj=behaviour.GetComponent<NetworkObject>();obj.CheckObjectVisibility=client=>Relevant(obj,client);if(NetworkManager.Singleton&&NetworkManager.Singleton.IsServer)actors.Add(obj);}
        static bool Relevant(NetworkObject obj,ulong client){if(client==NetworkManager.ServerClientId)return true;Vector3 position=new Vector3(0,0,-160);foreach(var player in Ranger.Players)if(player&&player.OwnerClientId==client&&!player.IsBot.Value){position=player.ObserverPosition;break;}Vector3 d=obj.transform.position-position;return d.x*d.x+d.z*d.z<1100*1100;}
        void Update()
        {
            var network=ReserveSession.Instance.Network;if(!network.IsServer||network.ShutdownInProgress)return;
            if(Time.unscaledTime>=next){next=Time.unscaledTime+.20f;clients.Clear();viewpoints.Clear();foreach(ulong id in network.ConnectedClientsIds){if(id==NetworkManager.ServerClientId)continue;Vector3 view=new Vector3(0,0,-160);foreach(var player in Ranger.Players)if(player&&player.OwnerClientId==id&&!player.IsBot.Value){view=player.ObserverPosition;break;}clients.Add(id);viewpoints.Add(view);}}
            if(clients.Count==0)return;int changes=0;
            // Spread visibility work across frames instead of serializing a whole region in one burst.
            for(int checkedActors=0;checkedActors<64&&actors.Count>0&&changes<12;checkedActors++)
            {
                if(cursor>=actors.Count)cursor=0;var actor=actors[cursor++];if(!actor||!actor.IsSpawned)continue;Vector3 at=actor.transform.position;
                for(int i=0;i<clients.Count&&changes<12;i++){ulong client=clients[i];if(!network.ConnectedClients.ContainsKey(client))continue;Vector3 delta=at-viewpoints[i];bool wanted=delta.x*delta.x+delta.z*delta.z<1100*1100,visible=actor.IsNetworkVisibleTo(client);if(wanted&&!visible){actor.NetworkShow(client);changes++;}else if(!wanted&&visible){actor.NetworkHide(client);changes++;}}
            }
        }
        public void ResetSession(){actors.Clear();clients.Clear();viewpoints.Clear();cursor=0;next=0;}
        public static void Forget(NetworkObject obj)
        {
            actors.Remove(obj);if(!obj||!obj.IsSpawned||!obj.NetworkManager||!obj.NetworkManager.IsServer||obj.NetworkManager.ShutdownInProgress)return;
            // NGO 1.8 queues NetworkShow until its next tick. Hide cancels that queue before destruction.
            var clients=new List<ulong>();var observers=obj.GetObservers();while(observers.MoveNext())if(observers.Current!=NetworkManager.ServerClientId)clients.Add(observers.Current);
            foreach(ulong client in clients)if(obj.IsNetworkVisibleTo(client))obj.NetworkHide(client);
        }
    }
}
