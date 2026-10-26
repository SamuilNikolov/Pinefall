using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<int> DamageRevision = new NetworkVariable<int>();
        public float ArmorEquipPulse { get; private set; }
        void DamageRevisionChanged(int previous, int next) { if (LocalHuman) DamageFlash = Time.time + .65f; }
        [ClientRpc] void VestEquippedClientRpc()
        { if (LocalHuman) ArmorEquipPulse = Time.time + .65f; }
    }
}
