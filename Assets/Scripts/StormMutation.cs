using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class Wildlife
    {
        public NetworkVariable<bool> Boss = new NetworkVariable<bool>(false);
        public int EffectiveMaxHealth { get { return maxHealth * (Boss.Value ? 3 : 1); } }
        int mutationCycle = -1; bool sizeApplied; Vector3 originalScale;
        public bool MutateBoss()
        {
            if (!IsServer || Dead || Aquatic || Boss.Value) return false;
            Boss.Value = true; Health.Value += maxHealth * 2; ApplyBossSize(); return true;
        }
        void ApplyBossSize()
        {
            if (Boss.Value == sizeApplied) return;
            if (Boss.Value) { originalScale = transform.localScale; transform.localScale = originalScale * 1.7f; }
            else transform.localScale = originalScale;
            sizeApplied = Boss.Value;
        }
        void MutationTick()
        {
            ApplyBossSize();
            if (!IsServer || Dead || Boss.Value || Aquatic) return;
            var match = WildfrontMatch.Current;
            if (!match || !match.Closing) return;
            int cycle = match.Round.Value * 16 + match.Stage.Value;
            if (mutationCycle == cycle) return;
            mutationCycle = cycle;
            // A bounded roll near active players makes bosses discoverable without multiplying the population.
            bool near = Ranger.Players.Any(p => p && p.Fighting && (p.transform.position - transform.position).sqrMagnitude < 600 * 600);
            if (near && rng.NextDouble() < .12 && ReserveSession.Instance.Animals.Count(a => a && !a.Dead && a.Boss.Value) < 4)
                if (MutateBoss() && WildfrontMatch.Current) WildfrontMatch.Current.MutationAlertClientRpc();
        }
    }
}
