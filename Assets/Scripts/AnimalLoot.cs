using UnityEngine;

namespace Pinefall
{
    public static class AnimalLoot
    {
        public static bool ExtraRoll(bool enraged, bool boss, float roll)
        { return boss || roll < (enraged ? .80f : .35f); }

        public static void Drop(Wildlife animal)
        {
            if (!animal || !animal.IsServer) return;
            if (!ExtraRoll(animal.Enraged.Value, animal.Boss.Value, Random.value)) return;
            int count = animal.Boss.Value ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                float roll = Random.value;
                int rarity = animal.Boss.Value ? (Random.value < .12f ? 4 : 3) : animal.Enraged.Value ? (Random.value < .25f ? 3 : 2) : (Random.value < .15f ? 2 : 1);
                Vector3 point = CaveLayout.LootPosition(animal.transform.position, new Vector3(1.1f + i * .8f, 0, .5f));
                int weapon = Random.Range(0, WeaponData.All.Length);
                if (roll < .18f) ExpeditionLoot.Spawn(point, 0, weapon, rarity, -1);
                else if (roll < .47f) ExpeditionLoot.Spawn(point, 1, WeaponData.Get(weapon).ammoType, 0, WeaponData.Get(weapon).magazine * 2);
                else if (roll < .67f) ExpeditionLoot.Spawn(point, 8, Random.Range(1, 10), rarity, 1);
                else if (roll < .80f) ExpeditionLoot.Spawn(point, 6, Mathf.Clamp(rarity, 1, 3), rarity, Ranger.ArmorMaximum(Mathf.Clamp(rarity, 1, 3)));
                else if (roll < .90f) ExpeditionLoot.Spawn(point, 7, 0, 1, 2);
                else if (roll < .95f) ExpeditionLoot.Spawn(point, 2, 0, 0, 1);
                else ExpeditionLoot.Spawn(point, 14, 0, 3, 1);
            }
        }
    }
}
