using System.Collections.Generic;
using UnityEngine;

namespace Pinefall
{
    public sealed class SettlementDetail : MonoBehaviour
    {
        public Renderer[] full, distant;
        public Vector3 center;
        int state = -1;
        static readonly List<SettlementDetail> sites = new List<SettlementDetail>(); static int cursor;
        void Awake() { sites.Add(this); }
        void OnDestroy() { sites.Remove(this); }
        public static void Tick(Camera camera)
        {
            if (!camera || ReserveSession.Arg("--bake-atlas") != "") return;
            for (int budget = 0; budget < 8 && sites.Count > 0; budget++)
            {
                if (cursor >= sites.Count) cursor = 0; var site = sites[cursor++]; if (!site) continue;
                float distance = Vector3.Distance(camera.transform.position, site.center);
                float near = Ranger.Local && Ranger.Local.Aiming ? 850 : Mathf.Lerp(175, 300, RuntimePerformance.Detail);
                int state = distance < near ? 2 : distance < RuntimePerformance.ViewDistance + 120 ? 1 : 0;
                if (state == site.state) continue; site.state = state;
                foreach (var renderer in site.full) if (renderer) renderer.enabled = state == 2;
                foreach (var renderer in site.distant) if (renderer) renderer.enabled = state == 1;
            }
        }
    }
}
