using System.Collections;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class CaptureProbe
    {
        IEnumerator FieldcraftShots()
        {
            var p = Ranger.Local; var ui = ReserveUI.Instance; var world = ConstructionWorld.Current; p.InitializeEquipment(true); p.Wood.Value = 300; p.Stone.Value = 240; p.Metal.Value = 160;
            var launcher = ExpeditionLoot.Spawn(p.transform.position + Vector3.right, 0, 7, 3, 1); p.ScavengeServerRpc(launcher.NetworkObjectId);
            for (int mod = 1; mod <= 9; mod++) p.SetModCountServer(mod, 2);
            p.ArmorTier.Value = 2; p.Armor.Value = 65; p.Food.Value = 4; p.Medkits.Value = 2;
            for (int allyIndex = 0; allyIndex < 2; allyIndex++)
            {
                var friend = Instantiate(Resources.Load<GameObject>("Ranger"), ReserveWorld.OnGround(18 + allyIndex * 3, -169, .1f), Quaternion.identity).GetComponent<Ranger>();
                friend.IsBot.Value = true; friend.Team.Value = p.Team.Value; friend.DisplayName.Value = new FixedString64Bytes(allyIndex == 0 ? "Alder" : "Birch"); friend.GetComponent<NetworkObject>().Spawn(); friend.GetComponent<HunterBot>().enabled = false;
                friend.Health.Value = allyIndex == 0 ? 78 : 46; friend.ArmorTier.Value = 1; friend.Armor.Value = 35;
            }
            var point = new Vector3(12, Mathf.Ceil((ReserveWorld.Height(12, -172) + .03f) / .75f) * .75f, -172);
            p.SmokeMoveTo(point + Vector3.back * 6 + Vector3.up * .15f); p.view.transform.position = p.transform.position + Vector3.up * 1.7f; p.view.transform.LookAt(point + Vector3.up * 1.4f);
            world.TryPlace(p, 1, 0, point, 0); world.TryPlace(p, 0, 0, point, 0); world.TryPlace(p, 2, 2, point + Vector3.left * 4, 0); world.TryPlace(p, 3, 1, point + Vector3.right * 4, 0);
            ui.VerificationPanel("lobby");yield return Shot("front-lobby");
            ui.VerificationPanel("character");yield return Shot("front-character");
            ui.VerificationPanel("audio");yield return Shot("front-audio");ui.VerificationPanel("performance");yield return Shot("front-performance");
            for(int page=0;page<7;page++){ui.VerificationGuide(page);yield return Shot("guide-"+page);}
            bool originalBattle=WildfrontMatch.Current.Battle.Value;WildfrontMatch.Current.Battle.Value=true;SpawnDraft.Current.Begin();yield return Shot("front-draft");WildfrontMatch.Current.Phase.Value=0;WildfrontMatch.Current.Battle.Value=originalBattle;
            ui.VerificationPanel("browser");yield return Shot("front-browser");ui.VerificationPanel("join");yield return Shot("front-join");ui.VerificationPanel(""); yield return Shot("fieldcraft-hud");
            ui.VerificationPanel("loadout"); yield return Shot("fieldcraft-inventory"); ui.VerificationPanel("map"); yield return Shot("fieldcraft-map");
            ui.VerificationPanel("settings"); yield return Shot("fieldcraft-settings"); ui.VerificationPanel("voice");yield return Shot("fieldcraft-voice");ui.VerificationPanel("bindings"); yield return Shot("fieldcraft-controls");
            ui.VerificationPanel(""); var wall = BuildPiece.All.FirstOrDefault(piece => piece.Kind.Value == 0);
            if (wall && p.BeginBuildEdit(wall)) { p.PreviewBuildEdit(511 ^ 16); yield return Shot("fieldcraft-wall-edit"); p.ConfirmBuildEdit(); ui.VerificationPanel(""); yield return Shot("fieldcraft-window"); }
            p.SelectTool(1); yield return Shot("fieldcraft-pickaxe"); p.SelectTool(0); p.SelectWeapon(7); yield return new WaitForSecondsRealtime(.3f); yield return Shot("fieldcraft-rpg");
            p.Crouched.Value = true; p.view.GetComponent<RangerCamera>().ThirdPerson = true; p.view.transform.position = p.transform.position + new Vector3(2, 1.2f, 3); p.view.transform.LookAt(p.transform.position + Vector3.up * .8f); yield return Shot("fieldcraft-crouch");
            p.Crouched.Value = false; p.view.GetComponent<RangerCamera>().ThirdPerson = false;
            var beast = Instantiate(Resources.Load<GameObject>("Boar"), ReserveWorld.OnGround(12, -145), Quaternion.identity).GetComponent<Wildlife>(); beast.GetComponent<NetworkObject>().Spawn(); beast.MutateBoss();
            p.view.transform.position = p.transform.position + Vector3.up * 1.7f; p.view.transform.LookAt(beast.transform.position + Vector3.up * 1.3f); ui.VerificationPanel(""); yield return Shot("fieldcraft-boss");
            WildfrontMatch.Current.Battle.Value=true;WildfrontMatch.Current.Phase.Value=1;WildfrontMatch.Current.ShrinkAt.Value=ReserveSession.Instance.Network.ServerTime.Time+10000;WildfrontMatch.Current.StageEnds.Value=WildfrontMatch.Current.ShrinkAt.Value+100;
            p.Health.Value = 0; var teammate = Ranger.Players.First(q => q != p && q.Alive && q.Team.Value == p.Team.Value); p.SpectateServerRpc(teammate.NetworkObjectId); yield return Shot("fieldcraft-spectate");
            p.Health.Value=100;WildfrontMatch.Current.Battle.Value=false;ui.VerificationPanel("");
            var cave=CaveLayout.Sites[0];var entry=cave.World(0,-cave.length*.5f);entry.y=cave.Floor(-cave.length*.5f);
            p.SmokeMoveTo(entry+Quaternion.Euler(0,cave.yaw,0)*Vector3.back*9+Vector3.up*.3f);p.view.transform.position=p.transform.position+Vector3.up*1.7f;p.view.transform.LookAt(cave.World(0,-cave.length*.35f)+Vector3.up*(entry.y+2));yield return Shot("living-cave-entrance");
            var inside=cave.World(0,0);inside.y=cave.Floor(0)+.3f;p.SmokeMoveTo(inside);p.view.transform.position=inside+Vector3.up*1.7f;var look=cave.World(0,30);look.y=cave.Floor(30)+1.7f;p.view.transform.LookAt(look);yield return Shot("living-cave-interior");
            var chamber=cave.Room(1);p.SmokeMoveTo(chamber+Vector3.up*.2f);p.view.transform.position=chamber+Vector3.up*1.7f;var crystal=chamber+Quaternion.Euler(0,cave.yaw,0)*new Vector3(4.8f,1.2f,4.8f);p.view.transform.LookAt(crystal);yield return Shot("living-crystal-chamber");
            p.SelectTool(1);var woods=ReserveWorld.OnGround(-420,-250,.3f);p.SmokeMoveTo(woods);p.view.transform.position=woods+Vector3.up*1.7f;p.view.transform.rotation=Quaternion.Euler(0,35,0);yield return Shot("living-forest");
            p.SelectTool(0);p.SmokeMoveTo(ReserveWorld.OnGround(0,-180,.1f));yield return new WaitForSecondsRealtime(.5f);
            p.view.transform.position=p.transform.position+p.transform.forward*3.4f+Vector3.up*1.5f;p.view.transform.LookAt(p.transform.position+Vector3.up*1.2f);ui.VerificationEmotes(new Vector2(0,180));yield return Shot("emote-wheel");ui.FinishEmoteWheel(true);yield return new WaitForSecondsRealtime(.6f);yield return Shot("emote-wave");p.CancelEmoteServer();
            Debug.Log("FIELDCRAFT_VISUAL_OK");
        }
    }
}
