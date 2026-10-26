using Unity.Netcode.Components;
using Unity.Netcode;
namespace Pinefall
{
    public sealed class OwnerTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative(){return false;}
        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
            if (!IsServer || !IsSpawned || !ShotHistory.Current) return;
            var player = GetComponent<Ranger>(); if (!player) return;
            double sentAt = new NetworkTime(NetworkManager.NetworkConfig.TickRate, newState.GetNetworkTick()).Time;
            ShotHistory.Current.Record(player.NetworkObjectId, GetSpaceRelativePosition(true), player.StanceHeight, .33f, sentAt, GetSpaceRelativeRotation(true).eulerAngles.y);
        }
    }
}
