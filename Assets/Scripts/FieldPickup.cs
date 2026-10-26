using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed class FieldPickup : NetworkBehaviour
    {
        public NetworkVariable<int> Materials=new NetworkVariable<int>(1);
        public NetworkVariable<int> Healing=new NetworkVariable<int>(25);
        public static readonly System.Collections.Generic.List<FieldPickup> All=new System.Collections.Generic.List<FieldPickup>();
        public Transform marker;
        float spawnTime;
        public override void OnNetworkSpawn(){All.Add(this);spawnTime=Time.time;}
        public override void OnNetworkDespawn(){All.Remove(this);}
        void Update(){if(marker)marker.localPosition=new Vector3(0,.95f+Mathf.Sin(Time.time*2)*.1f,0);if(IsServer&&Time.time-spawnTime>180)NetworkObject.Despawn();}
    }
}
