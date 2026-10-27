using System.Linq;
using UnityEngine;
namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        public bool SpectatorInput { get { return S && S.Playing && Application.isFocused && (panel == "" || panel == "result"); } }
        void TeamCards(float left, float top)
        {
            if (!P || !WildfrontMatch.Current || WildfrontMatch.Current.TeamSize.Value <= 1) return;
            int row = 0;
            foreach (var friend in Ranger.Players)
            {
                if (!friend || friend == P || friend.Team.Value != P.Team.Value) continue;
                float y = top + row++ * 57; Color color = friend.Downed.Value ? danger : friend.Alive ? ally : muted;
                Rect(left, y, 280, 49, new Color(.035f, .06f, .09f, .60f)); Rect(left, y, 3, 49, color);
                Glyph(left + 11, y + 10, 22, friend.Downed.Value ? "health" : "player", color);
                Text(left + 43, y + 4, 172, 25, friend.DisplayName.Value.ToString(), small, 16, paper);
                Text(left + 210, y + 5, 66, 23, !friend.Alive ? "Out" : friend.Downed.Value ? "Down" : Mathf.RoundToInt(Vector3.Distance(P.ObserverPosition, friend.transform.position)) + " m", small, 13, color);
                Bar(left + 43, y + 33, 210, friend.Alive ? friend.Health.Value / 100f : 0, color);
                if (friend.ArmorTier.Value > 0) { Rect(left + 43, y + 39, 210, 3, new Color(.15f, .19f, .25f)); Rect(left + 43, y + 39, 210 * friend.Armor.Value / Mathf.Max(1f, Ranger.ArmorMaximum(friend.ArmorTier.Value)), 3, ice); }
                if (ProximityVoice.Current && ProximityVoice.Current.Speaking(friend.NetworkObjectId)) Glyph(left + 259, y + 27, 15, "radio", ice);
            }
        }
        void HUD()
        {
            if (RuntimePerformance.HideHud || !P) return;
            bool field = panel == "", map = panel == "map", modal = panel != "" && panel != "pause" && panel != "build-edit";
            if (map) { TeamCards(28, 520); return; }
            if (modal) return;
            var match = WildfrontMatch.Current; bool scoped = P.Alive && P.ScopedView && P.ToolMode.Value == 0;
            if (scoped)
            {
                float size = 760, x = 420, y = 70; Rect(0, 0, x, 900, Color.black); Rect(x + size, 0, x, 900, Color.black); Rect(x, 0, size, y, Color.black); Rect(x, y + size, size, y, Color.black);
                GUI.DrawTexture(new Rect(x, y, size, size), scope); Rect(799, 190, 2, 520, Color.black); Rect(540, 449, 520, 2, Color.black);
                for (int tick = 1; tick < 5; tick++) Rect(791, 450 + tick * 32, 18, 2, Color.black);
            }
            float heading = P.Alive ? P.transform.eulerAngles.y + P.VehicleLook : P.Spectating ? P.Spectating.transform.eulerAngles.y : 0;
            string[] points = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            Text(610, 22, 380, 39, points[Mathf.RoundToInt(Mathf.Repeat(heading, 360) / 45) % 8] + "   " + Mathf.RoundToInt(Mathf.Repeat(heading, 360)).ToString("000"), Centered(type), 25, paper);
            for (int tick = -4; tick <= 4; tick++) Rect(800 + tick * 38, 66, tick == 0 ? 2 : 1, tick % 2 == 0 ? 8 : 4, new Color(1, 1, 1, .42f));
            if (!scoped && panel != "build-edit")
            {
                Rect(1318, 24, 246, 244, glass); DrawTactical(new Rect(1324, 30, 234, 232), new Vector2(P.ObserverPosition.x, P.ObserverPosition.z), 420, false);
                Text(1331, 239, 220, 24, GameBindings.Label("Map") + "  Reserve map", small, 13, paper);
                if (match && match.Battle.Value)
                {
                    int alive = Ranger.Players.Count(p => p && p.Alive), teams = Ranger.Players.Where(p => p && p.Alive).Select(p => p.Team.Value).Distinct().Count();
                    Rect(1318, 280, 246, 57, glass); Glyph(1329, 291, 18, "player", paper); Text(1358, 285, 192, 25, alive + " alive   " + teams + " teams", small, 16, paper);
                    Text(1330, 312, 220, 21, P.Kills.Value + " eliminations", small, 13, muted);
                    string state = match.Phase.Value == 0 ? "Warmup" : match.Phase.Value == 2 ? "Match complete" : match.Closing ? "Closing  " + Clock(match.StageEnds.Value - S.Network.ServerTime.Time) : "Ring " + (match.Stage.Value + 1) + "  " + Clock(match.ShrinkAt.Value - S.Network.ServerTime.Time);
                    Rect(1318, 347, 246, 40, glass); Glyph(1329, 357, 20, "storm", shieldColor); Text(1360, 353, 193, 29, state, small, 18, match.Closing ? shieldColor : paper);
                }
            }
            TeamCards(28, 535);
            foreach (var friend in Ranger.Players)
                if (friend && friend != P && friend.Alive && friend.Team.Value == P.Team.Value && P.view)
                {
                    var location = P.view.WorldToScreenPoint(friend.transform.position + Vector3.up * (friend.StanceHeight + .25f));
                    if (location.z > 0 && location.z < 250) Text(location.x / Screen.width * 1600 - 85, (1 - location.y / Screen.height) * 900, 170, 25, friend.DisplayName.Value.ToString(), Centered(small), 14, ally);
                }
            if (P.Alive) SurvivalPanel();
            if (Time.time < P.ArmorEquipPulse) { float alpha = Mathf.Clamp01((P.ArmorEquipPulse - Time.time) / .65f); Rect(65, 784, 292, 24, new Color(ice.r, ice.g, ice.b, alpha * .22f)); }
            if (field && P.Fighting)
            {
                if (!scoped && !P.Building)
                {
                    float gap = P.Aiming ? 4 : 7 + P.ShotBloom * 6 + P.Velocity.Value * .5f;
                    Rect(800 - gap - 7, 449, 6, 2, paper); Rect(800 + gap, 449, 6, 2, paper); Rect(799, 450 - gap - 7, 2, 6, paper); Rect(799, 450 + gap, 2, 6, paper);
                }
                if (P.HitMarker) { Line(new Vector2(787, 437), new Vector2(795, 445), ice, 2); Line(new Vector2(805, 455), new Vector2(813, 463), ice, 2); Line(new Vector2(787, 463), new Vector2(795, 455), ice, 2); Line(new Vector2(805, 445), new Vector2(813, 437), ice, 2); }
                string hint = P.Interaction(); var piece = P.AimedBuild();
                if (piece) hint = (piece.Team.Value == P.Team.Value ? GameBindings.Label("Edit build")+" Edit   "+GameBindings.Label("Repair")+" Repair" : "Enemy structure") + ((511 ^ piece.Mask.Value) == 18 ? "   "+GameBindings.Label("Interact")+" Door" : "");
                if (hint != "") { Rect(430, 692, 690, 36, glass); Text(440, 699, 670, 28, hint, Centered(small), 16, paper); }
                if (match && match.Active && match.Outside(P.transform.position)) { Rect(585, 102, 430, 49, new Color(.22f, .07f, .34f, .78f)); Text(600, 111, 400, 33, "Storm exposure / reach the safe zone", Centered(type), 20, paper); }
                if (match && match.Active) { Text(30, 40, 285, 26, "Field order", small, 13, gold); Text(30, 75, 285, 66, P.FieldOrder, small, 17, paper); }
                if (P.ActiveNotice != "") { Rect(425, 605, 705, 48, glass); Text(440, 612, 675, 38, P.ActiveNotice, Centered(small), 17, paper); }
                WildlifeSightings();
            }
            var weapon = WeaponData.Get(P.Weapon.Value);
            if (P.Alive && panel != "build-edit")
            {
                Rect(1190,656,374,73,glass);Text(1205,666,220,28,P.ToolMode.Value==1?"Field pickaxe":P.ToolMode.Value==2?"Blueprint":weapon.name,small,21,ice);
                if(P.ToolMode.Value==0){Text(1442,659,110,43,P.Reloading.Value?"…":P.Ammo.Value.ToString("00"),number,35,paper);Text(1205,701,340,23,weapon.ClassLabel+" / "+WeaponData.AmmoNames[weapon.ammoType]+" / "+P.ReserveFor(weapon.ammoType)+" spare",small,14,muted);}
                if(P.Reloading.Value)Bar(1205,725,340,P.ReloadFraction,ice);
                Rect(1030,744,100,54,glass);Glyph(1040,757,23,"launchpad",ice);Text(1070,750,57,22,GameBindings.Label("Launchpad"),small,14,ice);Text(1070,775,57,20,"Pad ×"+P.LaunchPads.Value,small,12,paper);
                for(int shape=0;shape<4;shape++)
                {
                    float x=1140+shape*106;bool selected=P.Building&&P.BuildKind==shape;
                    Rect(x,744,100,54,selected?raisedGlass:glass);if(selected)Rect(x,744,100,3,ice);
                    Glyph(x+9,757,25,new[]{"build-wall","build-floor","build-stairs","build-pyramid"}[shape],selected?ice:paper);
                    Text(x+42,750,55,22,GameBindings.Label("Build "+BuildPiece.Shapes[shape]),small,14,ice);Text(x+42,775,57,20,shape==2?"Stairs":BuildPiece.Shapes[shape],small,12,paper);
                }
                for(int slot=0;slot<6;slot++)
                {
                    float x=904+slot*111;int gun=slot>=1&&slot<=3?P.SlotWeapon(slot-1):-1;bool active=slot==0?P.PickaxeSelected:gun>=0&&gun==P.Weapon.Value&&P.ToolMode.Value==0;
                    Color rarity=slot==0?ice:gun>=0?WeaponData.RarityColor(P.Rarity(gun)):slot==4?prey:healthColor;
                    Rect(x,811,103,68,active?raisedGlass:glass);Rect(x,811,103,3,rarity);if(active){Rect(x,811,2,68,rarity);Rect(x+101,811,2,68,rarity);}
                    if(slot==0)Glyph(x+35,824,32,"pickaxe",ice);else if(gun>=0)GunIcon(x+10,828,83,24,gun,rarity);else if(slot>=4)Glyph(x+39,827,26,slot==4?"food":"health",rarity);
                    Text(x+7,858,94,20,slot==0?GameBindings.Label("Pickaxe")+" Pickaxe":slot<4?GameBindings.Label("Weapon "+slot):slot==4?GameBindings.Label("Food")+" ×"+P.Food.Value:GameBindings.Label("Heal")+" ×"+P.Medkits.Value,small,12,paper);
                }
                Text(410,854,480,28,GameBindings.Label("Inventory")+" hold inventory   "+GameBindings.Label("Emotes")+" emotes   "+GameBindings.Label("Build")+" build   "+GameBindings.Label("Camera")+" camera",small,14,muted);
                FieldcraftOverlay();
            }
            if (P.Downed.Value) Text(500, 537, 600, 58, "Downed  " + Clock(P.BleedEnds.Value - S.Network.ServerTime.Time) + "\nA teammate can revive you", Centered(type), 24, danger);
            if (P.ReviveTarget.Value != ulong.MaxValue) { Text(570, 544, 460, 35, "Reviving / hold " + GameBindings.Label("Interact"), Centered(type), 23, ally); Bar(590, 589, 420, P.ReviveProgress.Value, ally); }
            if (!P.Alive) SpectatorHUD();
            if (P.SurveyWarning) Text(1050, 407, 510, 31, P.SurveyVisible ? "Survey active / enemy pings on map" : "Survey detected / your location is exposed", small, 16, P.SurveyVisible ? ice : danger);
            if (P.CurrentVehicle) { var car = P.CurrentVehicle; Rect(486, 754, 630, 44, glass); Text(497, 763, 608, 31, Mathf.RoundToInt(Mathf.Abs(car.Speed.Value) * 3.6f) + " km/h   Engine " + car.Condition.Value + "%   Fuel " + Mathf.RoundToInt(car.Fuel.Value) + "%   Boost " + Mathf.RoundToInt(car.Charge.Value) + "%", Centered(small), 17, paper); }
            if (Time.time < eventUntil) { int row = 0; foreach (string line in events.Reverse()) { Text(1080, 449 + row++ * 26, 480, 27, line, small, 14, paper); if (row == 3) break; } }
            if (ProximityVoice.Current && ProximityVoice.Current.Transmitting) { Glyph(30, 480, 20, "radio", ice); Text(60, 478, 245, 26, "Speaking nearby", small, 16, ice); }
            if (PlayerPrefs.GetInt("perf.overlay", 1) == 1 && RuntimePerformance.Current) Text(30, 895 - 19, 285, 20, Mathf.RoundToInt(1000 / RuntimePerformance.Current.FrameMs) + " FPS  " + RuntimePerformance.Current.FrameMs.ToString("0.0") + " ms", small, 12, muted);
            if (FieldNarrator.Current && FieldNarrator.Current.Captions && FieldNarrator.Current.Subtitle != "") { Rect(420, 190, 760, 63, glass); Text(435, 198, 730, 47, FieldNarrator.Current.Subtitle, Centered(small), 17, paper); }
        }
        void SpectatorHUD()
        {
            var target = P.Spectating; Rect(455, 745, 690, 106, glass);
            Text(475, 759, 650, 35, target ? "Spectating " + target.DisplayName.Value : "Waiting for a surviving player", Centered(type), 26, ice);
            string restriction = WildfrontMatch.Current && WildfrontMatch.Current.TeamSize.Value > 1 && Ranger.Players.Any(p => p && p != P && p.Alive && p.Team.Value == P.Team.Value) ? "Teammates only" : "All surviving players";
            Text(480, 802, 640, 29, restriction + "   /   "+GameBindings.Label("Next spectator")+" next   "+GameBindings.Label("Previous spectator")+" previous   ← →", Centered(small), 17, muted);
        }
        void Result()
        {
            var match = WildfrontMatch.Current; if (!match) return;
            Rect(970, 110, 595, 626, glass); Text(1000, 146, 535, 86, match.Winner.Value >= 0 ? "Team " + (match.Winner.Value + 1) + " survives" : "The reserve wins", type, 40, paper);
            Text(1000, 265, 535, 40, "Round complete", type, 26, ice);
            if (P) Text(1000, 339, 535, 105, P.Kills.Value + " eliminations\n" + P.Score.Value + " successful hunts", body, 24, paper);
            if (S.IsHost) { if (Button(1000, 493, 535, 52, "Return everyone to lobby", true)) { match.ReturnLobbyServerRpc(); panel = "lobby"; } }
            else Text(1000, 493, 535, 62, "Waiting for the host to start the next round.", small, 21, muted);
            if (Button(1000, 596, 535, 45, "Leave reserve")) S.Leave();
            if (P && !P.Alive) SpectatorHUD();
        }
    }
}
