using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class Ranger
    {
        public struct DamageReceipt { public Vector3 position; public int damage, shield, health, maximum; public bool killed, headshot, enemy; public ulong target; public float at; }
        public readonly List<DamageReceipt> DamageReceipts = new List<DamageReceipt>(16);
        public int ConfirmedHits { get; private set; }
        public void ConfirmDamage(Vector3 point, int damage, int shield, int health, int maximum, bool killed, bool headshot, ulong target, bool enemy)
        {
            if (!IsServer || damage + shield <= 0) return;
            ConfirmDamageClientRpc(point, damage, shield, health, maximum, killed, headshot, target, enemy,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
        }
        [ClientRpc] void ConfirmDamageClientRpc(Vector3 point, int damage, int shield, int health, int maximum, bool killed, bool headshot, ulong target, bool enemy, ClientRpcParams rpc = default)
        {
            if (!LocalHuman) return; if(shield>0)ReserveEffects.Shield(point); hitUntil = Time.time + .32f; ConfirmedHits++;
            if (DamageReceipts.Count >= 16) DamageReceipts.RemoveAt(0);
            DamageReceipts.Add(new DamageReceipt { position = point, damage = damage, shield = shield, health = health, maximum = maximum, killed = killed, headshot = headshot, target = target, enemy = enemy, at = Time.time });
            CombatSounds.Play(killed ? 3 : shield > 0 ? 2 : 1);
        }
        public void AnimalReward(Wildlife animal)
        {
            if (!IsServer || !animal) return; Score.Value++; AnimalLoot.Drop(animal);
            if (WildfrontMatch.Current) { WildfrontMatch.Current.DropReward(animal.transform.position, animal.kind, animal.Enraged.Value); if (animal.Boss.Value) { ExpeditionLoot.Spawn(CaveLayout.LootPosition(animal.transform.position,Vector3.left), 5, 0, 0, 4); ExpeditionLoot.Spawn(CaveLayout.LootPosition(animal.transform.position,Vector3.right), 9, 0, 2, 1); } }
        }
    }
    public static class CombatSounds
    {
        static AudioClip[] clips;
        public static void Play(int kind)
        {
            if (ReserveSession.Arg("--silence") == "1") return;
            if (clips == null)
            {
                clips = new AudioClip[4];
                for (int k = 0; k < clips.Length; k++)
                {
                    int rate = 22050, count = k == 3 ? 5300 : 2200; var samples = new float[count]; var random = new System.Random(101 + k); float low = 0;
                    for (int i = 0; i < count; i++)
                    {
                        float t = i / (float)rate; low = Mathf.Lerp(low, (float)random.NextDouble() * 2 - 1, .22f);
                        float tone = Mathf.Sin(t * Mathf.PI * 2 * (k == 2 ? 510 - 1600 * t : k == 3 ? 260 + 600 * t : 150));
                        samples[i] = (low * .65f + tone * .18f) * Mathf.Exp(-t * (k == 3 ? 18 : 45)) * Mathf.Min(1, t * 1000);
                    }
                    clips[k] = AudioClip.Create("Combat confirmation " + k, count, 1, rate, false); clips[k].SetData(samples, 0);
                }
            }
            var obj = new GameObject("Confirmed impact sound"); var audio = obj.AddComponent<AudioSource>(); audio.spatialBlend = 0; audio.clip = clips[Mathf.Clamp(kind, 0, 3)]; audio.volume = kind == 0 ? .12f : .35f * Soundscape.Level("Weapons"); audio.Play(); Object.Destroy(obj, .5f);
        }
    }
}
