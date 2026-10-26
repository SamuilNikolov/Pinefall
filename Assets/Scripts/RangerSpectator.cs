using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<ulong> SpectatingId = new NetworkVariable<ulong>(ulong.MaxValue);
        float nextSpectatorCheck;
        public Ranger Spectating
        {
            get { return SpectatingId.Value == ulong.MaxValue ? null : Players.FirstOrDefault(p => p && p.IsSpawned && p.NetworkObjectId == SpectatingId.Value && p.Alive); }
        }
        public static bool CanSpectate(Ranger viewer, Ranger target)
        {
            if (!viewer || viewer.Alive || !target || target == viewer || !target.IsSpawned || !target.Alive) return false;
            var match = WildfrontMatch.Current;
            bool teamMode = match && match.TeamSize.Value > 1;
            bool survivingTeam = teamMode && Players.Any(p => p && p != viewer && p.IsSpawned && p.Alive && p.Team.Value == viewer.Team.Value);
            return !survivingTeam || target.Team.Value == viewer.Team.Value;
        }
        public List<Ranger> SpectatorChoices() { return Players.Where(p => CanSpectate(this, p)).OrderBy(p => p.NetworkObjectId).ToList(); }
        public Vector3 ObserverPosition { get { var target = !Alive ? Spectating : null; return target ? target.transform.position : transform.position; } }
        void SpectatorTick()
        {
            if (!IsServer || Time.time < nextSpectatorCheck) return; nextSpectatorCheck = Time.time + .2f;
            if (Alive) { if (SpectatingId.Value != ulong.MaxValue) SpectatingId.Value = ulong.MaxValue; return; }
            if (!CanSpectate(this, Spectating))
            { var target = Players.Where(p => CanSpectate(this, p)).OrderBy(p => p.NetworkObjectId).FirstOrDefault(); SpectatingId.Value = target ? target.NetworkObjectId : ulong.MaxValue; }
        }
        [ServerRpc] public void SpectateServerRpc(ulong id)
        { var target = Players.FirstOrDefault(p => p && p.NetworkObjectId == id); if (CanSpectate(this, target)) SpectatingId.Value = id; }
        public void CycleSpectator(int direction)
        {
            if (!LocalHuman || Alive) return; var choices = SpectatorChoices(); if (choices.Count == 0) return;
            int index = choices.FindIndex(p => p.NetworkObjectId == SpectatingId.Value);
            index = (index + direction + choices.Count) % choices.Count; SpectateServerRpc(choices[index].NetworkObjectId);
        }
        void SpectatorCameraTick()
        {
            if (!LocalHuman || Alive || !view) return;
            if (ReserveUI.Instance && ReserveUI.Instance.SpectatorInput)
            { if (GameBindings.Down("Next spectator") || Input.GetKeyDown(KeyCode.RightArrow)) CycleSpectator(1); if (GameBindings.Down("Previous spectator") || Input.GetKeyDown(KeyCode.LeftArrow)) CycleSpectator(-1); }
            var target = Spectating; if (!target) return;
            Vector3 focus = target.transform.position + Vector3.up * (target.StanceHeight * .8f), offset = -target.transform.forward * 4.5f + Vector3.up * 1.8f;
            float distance = offset.magnitude;
            foreach (var hit in Physics.SphereCastAll(focus, .18f, offset.normalized, distance, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore))
                if (!target.CurrentVehicle || hit.collider.GetComponentInParent<RoadVehicle>() != target.CurrentVehicle) distance = Mathf.Min(distance, Mathf.Max(.25f, hit.distance - .15f));
            Vector3 destination = focus + offset.normalized * distance;
            if ((view.transform.position - destination).sqrMagnitude > 50 * 50) view.transform.position = destination;
            else view.transform.position = Vector3.Lerp(view.transform.position, destination, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 9));
            view.transform.LookAt(focus); view.fieldOfView = 78;
        }
    }
}
