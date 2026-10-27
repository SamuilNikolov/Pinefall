using System.Collections.Generic;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class ReserveUI
    {
        readonly Color glass = new Color(.035f, .065f, .105f, .72f), raisedGlass = new Color(.09f, .145f, .21f, .74f), ice = new Color(.58f, .84f, .98f);
        readonly HashSet<ulong> healthDrawn = new HashSet<ulong>();
        readonly HashSet<int> editDragged = new HashSet<int>();
        int editPaint = -1;
        public bool EditingBuild { get { return panel == "build-edit"; } }
        public void OpenBuildEditor() { panel = "build-edit"; Unlock(); }
        public void CloseBuildEditor() { panel = ""; Lock(); }
        void FieldcraftOverlay()
        {
            if (!P || !P.view || panel != "" || !P.Alive) return;
            if (P.Building)
            {
                Rect(1040, 440, 525, 153, glass); Glyph(1058, 456, 24, "crate", ice);
                Text(1093, 451, 450, 37, BuildPiece.Shapes[P.BuildKind] + " / " + BuildPiece.Materials[P.BuildMaterial], type, 26, ice);
                Text(1058, 496, 480, 27, "10 per piece    " + P.BuildingMaterial(P.BuildMaterial) + " carried", small, 19, paper);
                Text(1058, 533, 480, 44, GameBindings.Label("Fire")+" build   "+GameBindings.Label("Aim")+" material   "+GameBindings.Label("Reload")+" rotate\n"+GameBindings.Label("Build Wall")+"–"+GameBindings.Label("Build Pyramid")+" shape   "+GameBindings.Label("Edit build")+" edit   "+GameBindings.Label("Build")+" return", small, 16, muted);
                if (P.BlueprintError != "") Text(560, 490, 480, 38, P.BlueprintError, Centered(small), 18, danger);
            }
            ResourceStrip(1140, 605, false);
            var aimed = P.AimedBuild(); if (aimed) SmallHealth(aimed.Center + Vector3.up * 1.2f, aimed.Health.Value, aimed.Maximum, BuildPiece.Materials[aimed.Material.Value] + " " + BuildPiece.Shapes[aimed.Kind.Value], aimed.Team.Value == P.Team.Value ? ice : danger);
            if (P.FocusAnimal && !P.FocusAnimal.Dead && (P.FocusAnimal.transform.position - P.transform.position).sqrMagnitude < 150 * 150)
                SmallHealth(P.FocusAnimal.transform.position + Vector3.up * (P.FocusAnimal.Boss.Value ? 3.4f : 2), P.FocusAnimal.Health.Value, P.FocusAnimal.EffectiveMaxHealth, P.FocusAnimal.Boss.Value ? "Storm alpha" : P.FocusAnimal.species, P.FocusAnimal.Boss.Value ? gold : prey);
            if (Physics.Raycast(P.view.transform.position, P.view.transform.forward, out var focused, 7, ~((1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore))
            {
                var node = HarvestResources.Find(focused.collider);
                if (node != null && node.health > 0) SmallHealth(focused.point + Vector3.up * .8f, node.health, node.maximum, node.material == 0 ? "Timber" : node.material == 1 ? "Stone" : "Metal ore", ice);
                var vehicle = focused.collider.GetComponentInParent<RoadVehicle>(); if (vehicle) SmallHealth(focused.point + Vector3.up, vehicle.Condition.Value, 100, "Engine condition", ice);
            }
            healthDrawn.Clear();
            for (int i = P.DamageReceipts.Count - 1; i >= 0; i--)
            {
                var receipt = P.DamageReceipts[i]; float age = Time.time - receipt.at;
                if (age > 4) { P.DamageReceipts.RemoveAt(i); continue; }
                Vector3 screen = P.view.WorldToScreenPoint(receipt.position);
                if (screen.z <= 0) continue;
                Color color = receipt.killed ? gold : receipt.shield > 0 ? ice : receipt.headshot ? gold : paper;
                if (age < .85f)
                {
                    float x = screen.x / Screen.width * 1600, y = (1 - screen.y / Screen.height) * 900 - 35 - age * 42;
                    color.a = Mathf.Clamp01((.85f - age) * 3);
                    Text(x - 60, y, 120, 42, receipt.killed ? "Eliminated" : (receipt.damage + receipt.shield).ToString(), Centered(type), receipt.headshot ? 30 : 25, color);
                    if (receipt.shield > 0) Text(x - 60, y + 31, 120, 21, "armor", Centered(small), 12, ice);
                }
                if (!receipt.enemy || receipt.killed || receipt.health <= 0 || !healthDrawn.Add(receipt.target)) continue;
                if (!S.Network.SpawnManager.SpawnedObjects.TryGetValue(receipt.target, out var obj) || !obj) continue;
                var enemy = obj.GetComponent<Ranger>(); if (!enemy || !enemy.Alive || (enemy.transform.position - P.transform.position).sqrMagnitude > 150 * 150) continue;
                Vector3 top = enemy.transform.position + Vector3.up * (enemy.StanceHeight + .25f);
                if (!Physics.Linecast(P.view.transform.position, top, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore)) SmallHealth(top, receipt.health, receipt.maximum, "", danger);
            }
        }
        void SmallHealth(Vector3 position, int health, int maximum, string label, Color color)
        {
            Vector3 projected = P.view.WorldToScreenPoint(position); if (projected.z <= 0) return;
            float x = projected.x / Screen.width * 1600, y = (1 - projected.y / Screen.height) * 900;
            if (x < 25 || x > 1575 || y < 55 || y > 800) return;
            if (label != "") Text(x - 100, y - 25, 200, 24, label, Centered(small), 14, color);
            Rect(x - 42, y, 84, 5, new Color(0, 0, 0, .65f)); Rect(x - 41, y + 1, 82 * Mathf.Clamp01(health / (float)Mathf.Max(1, maximum)), 3, color);
        }
        void ResourceStrip(float x, float y, bool interactive)
        {
            for (int material = 0; material < 3; material++)
            {
                float left = x + material * 140; Rect(left, y, 130, 38, glass); Glyph(left + 8, y + 9, 20, material == 0 ? "wood" : material == 1 ? "stone" : "metal", material == 0 ? prey : material == 1 ? paper : ice);
                Text(left + 37, y + 5, 89, 28, P.BuildingMaterial(material).ToString(), type, 23, paper);
                if (interactive && GUI.Button(new Rect(left, y, 130, 38), GUIContent.none, GUIStyle.none)) { inventoryKind = 13; inventoryItem = material; }
            }
        }
        void Loadout()
        {
            if (!P) { OldLoadout(); return; }
            if (!P.Owns(inventoryWeapon)) inventoryWeapon = P.Weapon.Value;
            Rect(656, 42, 925, 831, glass); Rect(656, 42, 4, 831, ice);
            Text(686, 63, 460, 45, "Equipment", type, 36, paper); ResourceStrip(1138, 66, true);
            var ev = Event.current;
            if (ev.type == EventType.MouseDrag && dragSlot >= 0) draggingWeapon = true;
            if (ev.type == EventType.MouseUp && dragSlot >= 0)
            {
                if (draggingWeapon) for (int i = 0; i < 3; i++) if (new Rect(686 + i * 290, 127, 278, 176).Contains(ev.mousePosition)) P.SwapSlotsServerRpc(dragSlot, i);
                dragSlot = -1; draggingWeapon = false;
            }
            for (int slot = 0; slot < 3; slot++)
            {
                float x = 686 + slot * 290; int gun = P.SlotWeapon(slot); bool owned = gun >= 0; Color rarity = owned ? WeaponData.RarityColor(P.Rarity(gun)) : muted;
                Rect(x, 127, 278, 176, gun == inventoryWeapon ? raisedGlass : new Color(.07f, .1f, .15f, .6f)); Rect(x, 127, 278, 3, rarity);
                Text(x + 13, 139, 229, 31, owned ? WeaponData.Get(gun).name : "Empty slot", type, 22, paper); Text(x + 246, 141, 28, 26, (slot + 1).ToString(), small, 16, muted);
                if (!owned) continue;
                Text(x+13,165,250,20,WeaponData.Get(gun).ClassLabel,small,13,muted);GunIcon(x + 18, 186, 145, 31, gun, rarity); Text(x + 190, 184, 77, 38, P.MagazineFor(gun).ToString(), number, 30, rarity);
                if (ev.type == EventType.MouseDown && new Rect(x, 127, 278, 100).Contains(ev.mousePosition)) dragSlot = slot;
                if (GUI.Button(new Rect(x, 130, 278, 94), GUIContent.none, GUIStyle.none)) { inventoryWeapon = gun; inventoryKind = 0; inventoryItem = gun; }
                for (int fit = 0; fit < 3; fit++)
                {
                    int id = P.Mod(gun, fit); float y = 227 + fit * 23;
                    if (Button(x + 12, y, 254, 22, id > 0 ? WeaponMods.Names[id] + "  ×" : "+ " + WeaponMods.SlotName(fit), id > 0)) { inventoryWeapon = gun; if (id > 0) P.DetachModServerRpc(gun, fit); }
                }
            }
            Text(686, 323, 400, 31, "Backpack", type, 23, paper);Glyph(1170,327,23,"launchpad",ice);Text(1200,327,180,25,"Launchpads ×"+P.LaunchPads.Value,small,17,ice);if(Button(1385,323,162,30,"Drop pad"))P.DropItemServerRpc(14,0,1);
            int[] kinds = { 1, 1, 1, 1, 1, 5, 2, 3, 4, 6, 7, 12 };
            string[] labels = { ".308", ".223", "12-gauge", "9 mm", "Rockets", "Food", "Medkit", "Repair", "Fuel", "Vest", "Plates", "Survey" };
            for (int i = 0; i < kinds.Length; i++)
            {
                int kind = kinds[i], item = i < 5 ? i : 0; float x = 686 + i % 6 * 145, y = 365 + i / 6 * 91;
                int amount = i < 5 ? P.ReserveFor(i) : kind == 5 ? P.Food.Value : kind == 2 ? P.Medkits.Value : kind == 3 ? P.Craft.Value : kind == 4 ? P.FuelCans.Value : kind == 6 ? P.Armor.Value : kind == 7 ? P.ArmorPlates.Value : P.SurveyCharges.Value;
                Rect(x, y, 133, 80, inventoryKind == kind && inventoryItem == item ? raisedGlass : new Color(.08f, .11f, .16f, .68f));
                Glyph(x + 12, y + 13, 24, kind == 5 ? "food" : kind == 2 ? "health" : kind == 6 || kind == 7 ? "shield" : kind == 12 ? "radio" : kind == 1 ? "ammo" : "crate", kind == 6 || kind == 7 ? ice : paper);
                Text(x + 51, y + 10, 75, 32, amount.ToString(), type, 23, paper); Text(x + 12, y + 49, 112, 25, labels[i], small, 16, muted);
                if (GUI.Button(new Rect(x, y, 133, 80), GUIContent.none, GUIStyle.none)) { inventoryKind = kind; inventoryItem = item; }
            }
            Text(686, 566, 840, 32, "Attachments / select a weapon, then fit a part", type, 21, paper);
            for (int id = 1; id <= 9; id++)
            {
                float x = 686 + (id - 1) % 3 * 290, y = 611 + (id - 1) / 3 * 54; int count = P.ModCount(id);
                Rect(x, y, 278, 45, inventoryKind == 8 && inventoryItem == id ? raisedGlass : new Color(.08f, .11f, .16f, .64f));
                Rect(x, y, 3, 45, count > 0 ? ice : muted); Text(x + 12, y + 9, 250, 31, WeaponMods.Names[id] + "  ×" + count, small, 17, count > 0 ? paper : muted);
                if (GUI.Button(new Rect(x, y, 278, 45), GUIContent.none, GUIStyle.none)) { inventoryKind = 8; inventoryItem = id; }
            }
            string selected = inventoryKind == 8 ? WeaponMods.Names[Mathf.Clamp(inventoryItem, 1, 9)] : inventoryKind == 0 ? WeaponData.Get(inventoryWeapon).name : inventoryKind == 13 ? BuildPiece.Materials[inventoryItem] : inventoryKind == 1 ? WeaponData.AmmoNames[inventoryItem] : inventoryKind == 5 ? "Food" : inventoryKind == 2 ? "Medkit" : inventoryKind == 3 ? "Repair supplies" : inventoryKind == 4 ? "Fuel" : inventoryKind == 6 ? "Armor vest" : inventoryKind == 12 ? "Survey beacon" : "Armor plates";
            Rect(48, 612, 550, 225, glass); Text(72, 632, 500, 40, selected, type, 30, ice);
            string description = inventoryKind == 8 ? WeaponMods.Effects[inventoryItem] + "\nFit to " + WeaponData.Get(inventoryWeapon).name : inventoryKind == 0 ? WeaponData.Get(inventoryWeapon).ClassLabel+" / "+WeaponData.Get(inventoryWeapon).ModeLabel+"\nDamage " + Mathf.RoundToInt(WeaponData.Get(inventoryWeapon).playerDamage * P.DamageFactor(inventoryWeapon)) + "     Range " + WeaponData.Get(inventoryWeapon).range + " m\nReload " + (WeaponData.Get(inventoryWeapon).reload * P.ReloadFactor(inventoryWeapon)).ToString("0.0") + " s" : inventoryKind == 13 ? "Ten materials build one piece.\nWood 300 HP / stone 450 HP / metal 600 HP." : inventoryKind == 5 ? "+35 hunger and +5 health. Restores sprinting." : inventoryKind == 2 ? "+45 health. Share spare medicine with your team." : inventoryKind == 1 ? "Spare ammunition is used when reloading." : inventoryKind == 6 || inventoryKind == 7 ? "Protects against gunshots and animal attacks.\nStorm exposure and hunger bypass armor." : inventoryKind == 12 ? "Reveals enemy snapshots to your team for 12 seconds." : "Select to use, or drop supplies for a teammate.";
            Text(72, 693, 500, 100, description, body, 21, paper);
            string action = inventoryKind == 8 ? "Fit attachment" : inventoryKind == 0 ? "Equip weapon" : inventoryKind == 5 ? "Eat food" : inventoryKind == 2 ? "Use medkit" : inventoryKind == 7 ? "Use armor plate" : inventoryKind == 12 ? "Activate survey" : "Selected";
            if (Button(686, 794, 278, 44, action, true))
            {
                if (inventoryKind == 8) P.AttachModServerRpc(inventoryWeapon, inventoryItem); else if (inventoryKind == 0) P.SelectWeapon(inventoryWeapon); else if (inventoryKind == 5) P.EatServerRpc(); else if (inventoryKind == 2) P.HealServerRpc(); else if (inventoryKind == 7) P.PlateServerRpc(); else if (inventoryKind == 12) P.SurveyServerRpc();
            }
            if (Button(976, 794, 278, 44, inventoryKind == 1 || inventoryKind == 13 ? "Drop up to 30" : "Drop item")) P.DropItemServerRpc(inventoryKind, inventoryKind == 0 ? inventoryWeapon : inventoryItem, inventoryKind == 1 || inventoryKind == 13 ? 30 : 1);
            if (Button(1266, 794, 278, 44, "Return to field")) ClosePanel();
            Text(686, 846, 840, 23, "Drag weapon headers to reorder. Click a fitted part to remove it.", small, 14, muted);
        }
        void BuildEditPanel()
        {
            var piece = P ? P.EditTarget : null; if (!piece) { CloseBuildEditor(); return; }
            Rect(1040, 170, 525, 573, glass); Text(1065, 194, 475, 44, "Edit " + BuildPiece.Shapes[piece.Kind.Value].ToLowerInvariant(), type, 32, ice);
            Text(1065, 253, 475, 61, piece.Kind.Value == 3 ? "Select raised corners. Clear all to restore the pyramid." : "Paint cells to remove them. Keep a connected shape.", small, 19, paper);
            int count = piece.Kind.Value == 0 ? 3 : 2; float size = count == 3 ? 82 : 122; float left = 1160, top = 337;
            if (Event.current.type == EventType.MouseDown) { editDragged.Clear(); editPaint = -1; }
            WorldEditGrid(piece);
            for (int row = 0; row < count; row++) for (int col = 0; col < count; col++)
            {
                int index = piece.Kind.Value == 0 ? (count - 1 - row) * count + col : piece.Kind.Value == 3 && row == 1 ? col == 0 ? 3 : 2 : row * count + col;
                var cell = new Rect(left + col * (size + 6), top + row * (size + 6), size, size); bool active = (P.EditMask & (1 << index)) != 0;
                Rect(cell.x, cell.y, cell.width, cell.height, active ? new Color(.22f, .60f, .85f, .8f) : new Color(.12f, .18f, .24f, .55f));
                Text(cell.x, cell.y + size * .3f, size, 35, active ? piece.Kind.Value == 3 ? "↑" : "Keep" : piece.Kind.Value == 3 ? "Low" : "Cut", Centered(small), 18, paper);
                if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0 && cell.Contains(Event.current.mousePosition) && editDragged.Add(index))
                {
                    if (editPaint < 0) editPaint = active ? 0 : 1;
                    int mask = editPaint == 0 ? P.EditMask & ~(1 << index) : P.EditMask | (1 << index); P.PreviewBuildEdit(mask); Event.current.Use();
                }
            }
            if (Button(1065, 628, 220, 44, "Reset / "+GameBindings.Label("Aim") ) || Event.current.type == EventType.MouseDown && Event.current.button == 1) P.PreviewBuildEdit(BuildPiece.FullMask(piece.Kind.Value));
            if (Button(1300, 628, 240, 44, "Confirm / "+GameBindings.Label("Edit build"), true)) P.ConfirmBuildEdit();
            if (Button(1065, 689, 220, 33, "Cancel / Esc")) { P.CancelBuildEdit(); CloseBuildEditor(); }
            if (piece.Kind.Value >= 2 && Button(1300, 689, 240, 33, "Rotate / "+GameBindings.Label("Reload"))) { P.BuildTurn = (P.BuildTurn + 1) % 4; P.PreviewBuildEdit(P.EditMask); }
        }
        void WorldEditGrid(BuildPiece piece)
        {
            int count = piece.Kind.Value == 0 ? 3 : 2; bool wall = piece.Kind.Value == 0;
            for (int row = 0; row < count; row++) for (int col = 0; col < count; col++)
            {
                float x = -2 + col * 4f / count, a = row * (wall ? 3f : 4f) / count;
                Vector3[] corners = wall ? new[] { new Vector3(x, a, 2), new Vector3(x + 4f / count, a, 2), new Vector3(x + 4f / count, a + 3f / count, 2), new Vector3(x, a + 3f / count, 2) } :
                    new[] { new Vector3(x, 0, a - 2), new Vector3(x + 4f / count, 0, a - 2), new Vector3(x + 4f / count, 0, a - 2 + 4f / count), new Vector3(x, 0, a - 2 + 4f / count) };
                if (piece.Kind.Value == 2) for (int i = 0; i < 4; i++) corners[i].y = (corners[i].z + 2) * .75f;
                for (int edge = 0; edge < 4; edge++)
                {
                    Vector3 from = P.view.WorldToScreenPoint(piece.transform.TransformPoint(corners[edge])), to = P.view.WorldToScreenPoint(piece.transform.TransformPoint(corners[(edge + 1) % 4]));
                    if (from.z > .1f && to.z > .1f) Line(new Vector2(from.x / Screen.width * 1600, (1 - from.y / Screen.height) * 900), new Vector2(to.x / Screen.width * 1600, (1 - to.y / Screen.height) * 900), new Color(.65f, .86f, 1, .8f), 1.5f);
                }
            }
            var ev = Event.current;
            if (ev.button != 0 || ev.mousePosition.x >= 1020 || ev.type != EventType.MouseDown && ev.type != EventType.MouseDrag) return;
            Ray ray = P.view.ScreenPointToRay(new Vector3(ev.mousePosition.x / 1600 * Screen.width, (1 - ev.mousePosition.y / 900) * Screen.height));
            Vector3 normal = wall ? piece.transform.forward : piece.transform.TransformDirection(piece.Kind.Value == 2 ? new Vector3(0, 1, -.75f).normalized : Vector3.up);
            Vector3 planePoint = piece.transform.TransformPoint(wall ? new Vector3(0, 0, 2) : piece.Kind.Value == 2 ? new Vector3(0, 1.5f, 0) : Vector3.zero);
            if (!new Plane(normal, planePoint).Raycast(ray, out float distance) || distance > 12) return;
            Vector3 at = piece.transform.InverseTransformPoint(ray.GetPoint(distance)); float u = (at.x + 2) / 4, v = wall ? at.y / 3 : (at.z + 2) / 4;
            if (u < 0 || u >= 1 || v < 0 || v >= 1) return;
            int column = (int)(u * count), rowIndex = (int)(v * count), index = wall ? rowIndex * 3 + column : piece.Kind.Value == 3 && rowIndex == 1 ? column == 0 ? 3 : 2 : rowIndex * 2 + column;
            if (!editDragged.Add(index)) return; bool active = (P.EditMask & (1 << index)) != 0; if (editPaint < 0) editPaint = active ? 0 : 1;
            P.PreviewBuildEdit(editPaint == 0 ? P.EditMask & ~(1 << index) : P.EditMask | (1 << index)); ev.Use();
        }
        void Pause()
        {
            if (!S.Playing) return; Rect(980, 72, 585, 775, glass); Text(1010, 104, 520, 56, "Field menu", type, 40, paper);
            Text(1010, 177, 520, 62, "The world keeps moving. Stay in cover while changing equipment or settings.", small, 19, muted);
            string[] names = { "Resume", "Equipment", "Map", "Settings", "Key bindings", "How to play", "Appearance" };
            string[] pages = { "", "loadout", "map", "settings", "bindings", "guide", "character" };
            for (int i = 0; i < names.Length; i++) if (Button(1010, 270 + i * 58, 525, 46, names[i], i == 0)) { if (i == 0) ClosePanel(); else Open(pages[i]); }
            if (S.IsHost && Button(1010, 692, 525, 40, "Connection details")) Open("session");
            if (Button(1010, 756, 525, 42, S.IsHost ? "Close reserve and leave" : "Leave reserve")) S.Leave();
        }
        void VoiceSettings()
        {
            Rect(946,40,619,833,glass);Text(976,66,550,55,"Voice & narration",type,36,paper);
            var voice=ProximityVoice.Current;
            if(voice)
            {
                Text(976,139,550,45,voice.Status,small,19,ice);
                Text(976,210,550,30,"Nearby voice volume",small,20,paper);voice.ReceiveVolume=Slider(976,254,550,voice.ReceiveVolume,0,1);
                Text(976,319,550,30,"Open-mic noise gate",small,20,paper);voice.GateThreshold=Slider(976,362,550,voice.GateThreshold,.005f,.06f);
                Text(976,410,550,72,"Lower the gate for a quiet microphone. Higher settings ignore more background noise. Voice reaches every team within 75 m.",small,18,muted);
            }
            var narrator=FieldNarrator.Current;
            if(narrator)
            {
                Text(976,511,550,30,"Narrator volume",small,20,paper);narrator.Volume=Slider(976,554,550,narrator.Volume,0,1);
                if(Button(976,615,550,43,narrator.Captions?"Captions: on":"Captions: off",narrator.Captions))narrator.Captions=!narrator.Captions;
                if(Button(976,675,550,43,narrator.Playful?"Occasional commentary: on":"Occasional commentary: off",narrator.Playful))narrator.Playful=!narrator.Playful;
            }
            if(Button(976,786,550,43,"Back to Settings",true))Open("settings");
        }
        void Settings()
        {
            if (!S.Playing) { LegacySettings(); return; }
            Rect(946, 40, 619, 833, glass); Text(976, 60, 550, 49, "Settings", type, 36, paper);
            if (Button(976, 126, 265, 40, "Sound mix")) Open("audio"); if (Button(1254, 126, 280, 40, "Performance")) Open("performance");
            Text(976, 188, 500, 31, "Mouse sensitivity   " + Sensitivity.ToString("0.0"), small, 19, paper); Sensitivity = Slider(976, 229, 550, Sensitivity, .5f, 5);
            float volume = AudioListener.volume; Text(976, 274, 500, 31, "Master sound   " + Mathf.RoundToInt(volume * 100) + "%", small, 19, paper); volume = Slider(976, 315, 550, volume, 0, 1); AudioListener.volume = volume;
            Text(976, 365, 550, 31, "Landscape detail", small, 19, paper); for (int q = 0; q < 3; q++) if (Button(976 + q * 187, 408, 177, 40, new[] { "Light", "Balanced", "High" }[q], !RuntimePerformance.Automatic && PlayerPrefs.GetInt("quality", 1) == q)) { RuntimePerformance.Automatic = false; ApplyQuality(q); RuntimePerformance.ApplyDetail(); }
            var voice = ProximityVoice.Current;
            if (voice)
            {
                if (Button(976, 480, 550, 44, voice.Enabled ? "Microphone enabled" : "Enable microphone", voice.Enabled)) voice.Enabled = !voice.Enabled;
                if (Button(976, 536, 265, 39, "Push to talk", !voice.OpenMic)) voice.OpenMic = false; if (Button(1254, 536, 272, 39, "Open mic", voice.OpenMic)) voice.OpenMic = true;
                Text(976, 598, 550, 56, "Voice reaches all teams within 75 m.\nTurning it off stops capture.", small, 18, muted);
            }
            if(Button(976,666,550,44,"Voice & narration options"))Open("voice");
            if(voice)Text(976,725,550,35,voice.Status,small,17,muted);
            if (Button(976, 786, 265, 43, "Key bindings")) Open("bindings"); if (Button(1254, 786, 272, 43, "Back")) Back();
            if(ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")==""){PlayerPrefs.SetFloat("sensitivity", Sensitivity); PlayerPrefs.SetFloat("volume", volume);}
        }
    }
}
