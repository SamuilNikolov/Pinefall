using Unity.Netcode;
using UnityEngine;

namespace Pinefall
{
    public sealed partial class Ranger
    {
        public NetworkVariable<int> Wood = new NetworkVariable<int>(80), Stone = new NetworkVariable<int>(40), Metal = new NetworkVariable<int>(20);
        public NetworkVariable<byte> ToolMode = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<int> ToolSwings = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public int BuildKind, BuildMaterial, BuildTurn, EditMask;
        public BuildPiece EditTarget { get; private set; }
        public bool Building { get { return ToolMode.Value == 2; } }
        public bool PickaxeSelected { get { return ToolMode.Value == 1; } }
        public Vector3 BlueprintPoint { get; private set; }
        public string BlueprintError { get; private set; } = "";
        GameObject blueprint, pickaxeView; Material blueprintSurface; int blueprintStamp = -1;
        Vector3 buildOrigin; float buildYaw; int rotationOffset; bool gridReady;
        public float BlueprintYaw { get { return buildYaw + BuildTurn * 90; } }
        float nextBlueprintCheck, nextPlace, nextSwing, serverSwing, swingAt, serverBuildAt, serverEditAt; bool blueprintValid;
        public static bool FiniteVector(Vector3 point) { return Finite(point); }
        public int BuildingMaterial(int material) { return material == 0 ? Wood.Value : material == 1 ? Stone.Value : Metal.Value; }
        public void AddBuildingMaterial(int material, int amount)
        {
            if (!IsServer || material < 0 || material > 2) return;
            int next = Mathf.Clamp(BuildingMaterial(material) + amount, 0, 999);
            if (material == 0) Wood.Value = next; else if (material == 1) Stone.Value = next; else Metal.Value = next;
        }
        public void ResetBuildingMaterials() { if (IsServer) { Wood.Value = 80; Stone.Value = 40; Metal.Value = 20; } }
        public void SelectTool(byte tool) { if (!IsOwner) return; ToolMode.Value = tool; if (tool == 0) CancelBuildEdit(); }
        void ConstructionTick(bool controls)
        {
            if (!LocalHuman || !view) return;
            if (!Alive || CurrentVehicle) { if (ToolMode.Value != 0) ToolMode.Value = 0; CancelBuildEdit(); }
            if (EditTarget && (!ReserveUI.Instance || !ReserveUI.Instance.EditingBuild)) CancelBuildEdit();
            if (!controls) { if (blueprint && !EditTarget) blueprint.SetActive(false); return; }
            if(GameBindings.Down("Launchpad"))TryDeployLaunchPad();
            if (GameBindings.Down("Pickaxe")) ToolMode.Value = PickaxeSelected ? (byte)0 : (byte)1;
            if (GameBindings.Down("Build")) { if(!Building)gridReady=false; ToolMode.Value = Building ? (byte)0 : (byte)2; }
            if (GameBindings.Down("Repair")) { var repair = AimedBuild(); if (repair) RepairBuildServerRpc(repair.NetworkObjectId); }
            for (int kind = 0; kind < 4; kind++) if (GameBindings.Down("Build " + BuildPiece.Shapes[kind])) { if(!Building)gridReady=false; BuildKind = kind; ToolMode.Value = 2; }
            if (GameBindings.Down("Edit build"))
            {
                var piece = AimedBuild();
                if (ConstructionWorld.CanModify(this, piece)) { EditTarget = piece; piece.SetPreviewHidden(true); EditMask = piece.Mask.Value; BuildTurn = BuildGrid.FacingTurn(piece.transform.eulerAngles.y,piece.GridYaw.Value); ReserveUI.Instance.OpenBuildEditor(); ShowBlueprint(piece.transform.position, piece.Kind.Value, EditMask, BuildTurn, true,piece.GridYaw.Value); }
                else ShowNotice("Aim at a nearby structure owned by your team.", 2);
            }
            if (EditTarget) return;
            if (Building)
            {
                if (!gridReady) { buildYaw = Mathf.Repeat(transform.eulerAngles.y,360); buildOrigin = BuildGrid.RootOrigin(transform.position,CaveLayout.SupportHeight(transform.position));rotationOffset=0;gridReady=true; }
                if (GameBindings.Down("Reload")) rotationOffset = (rotationOffset + 1) % 4;
                if (GameBindings.Down("Aim")) BuildMaterial = (BuildMaterial + 1) % 3;
                BuildPiece anchor = null; RaycastHit under;
                if (Physics.Raycast(transform.position + Vector3.up * .5f, Vector3.down, out under, 4, ~((1 << 8) | (1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore)) anchor=under.collider.GetComponentInParent<BuildPiece>();
                if(!anchor && Physics.Raycast(view.transform.position,view.transform.forward,out var ahead,8,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))anchor=ahead.collider.GetComponentInParent<BuildPiece>();
                float level=buildOrigin.y;
                if(anchor)
                {
                    buildYaw=anchor.GridYaw.Value;buildOrigin=anchor.GridOrigin.Value;
                    level=anchor.transform.position.y;
                    if(transform.position.y-level>1.7f)level+=3;
                }
                else if(under.collider && !(under.collider is TerrainCollider) && HarvestResources.Find(under.collider)==null)
                    level=buildOrigin.y+Mathf.Round((under.point.y-buildOrigin.y)/3)*3;
                else
                {
                    float ground=ReserveWorld.Height(transform.position.x,transform.position.z);
                    // A new grounded run can move to a nearby natural terrace; connected pieces keep their storey.
                    level=buildOrigin.y+Mathf.Round((ground-buildOrigin.y)/3)*3;
                }
                BuildTurn=BuildGrid.FacingTurn(transform.eulerAngles.y,buildYaw,rotationOffset);
                BlueprintPoint=BuildGrid.Target(transform.position,transform.forward,BuildKind,buildOrigin,buildYaw,level);
                if(Time.time>=nextBlueprintCheck){nextBlueprintCheck=Time.time+.06f;BlueprintError=ConstructionWorld.PlacementError(this,BuildKind,BuildMaterial,BlueprintPoint,BuildTurn,buildYaw,buildOrigin);blueprintValid=BlueprintError=="";}
                ShowBlueprint(BlueprintPoint,BuildKind,BuildPiece.FullMask(BuildKind),BuildTurn,blueprintValid,buildYaw);
                if(GameBindings.Held("Fire")&&Time.time>=nextPlace){nextPlace=Time.time+.09f;if(blueprintValid)PlaceBuildServerRpc(BuildKind,BuildMaterial,BlueprintPoint,BuildTurn,buildYaw,buildOrigin);}

            }
            else if (blueprint) blueprint.SetActive(false);
            if (PickaxeSelected && GameBindings.Down("Fire") && Time.time >= nextSwing)
            { nextSwing = Time.time + .48f; swingAt = Time.time; ToolSwings.Value++; CombatSounds.Play(0); SwingServerRpc(view.transform.position, view.transform.forward); }
        }
        void LateUpdate(){if(IsSpawned)LateConstructionVisual();}
        void LateConstructionVisual()
        {
            if (!LocalHuman || !view) return;
            bool tool = PickaxeSelected && !IsEmoting && Alive && CanAct && !CurrentVehicle && (!ReserveUI.Instance||!ReserveUI.Instance.FrontPanelVisible);
            if (tool && !pickaxeView)
            {
                pickaxeView = new GameObject("Field pickaxe"); pickaxeView.transform.SetParent(view.transform, false); CreatePickaxe(pickaxeView.transform, true);
            }
            if (pickaxeView)
            {
                pickaxeView.SetActive(tool && ReserveUI.Instance && !ReserveUI.Instance.EditingBuild);
                float swing = Mathf.Sin(Mathf.Clamp01((Time.time - swingAt) / .4f) * Mathf.PI);
                pickaxeView.transform.localScale = Vector3.one * .82f;
                pickaxeView.transform.localPosition = new Vector3(.34f - swing * .22f, -.42f, .85f);
                pickaxeView.transform.localRotation = Quaternion.Euler(-15 + swing * 65, -12 - swing * 50, -25);
            }
            if (weaponView && ToolMode.Value != 0) weaponView.SetVisible(false);
        }
        public static void CreatePickaxe(Transform root, bool firstPerson)
        {
            ToolPart(root, new Vector3(0, .05f, 0), new Vector3(.045f, .9f, .045f), BuildGeometry.Surface(0));
            ToolPart(root, new Vector3(0, .47f, 0), new Vector3(.54f, .09f, .09f), BuildGeometry.Surface(2));
            ToolPart(root, new Vector3(.25f, .42f, 0), new Vector3(.07f, .18f, .07f), BuildGeometry.Surface(2));
            foreach (var part in root.GetComponentsInChildren<Transform>()) part.gameObject.layer = firstPerson ? 10 : 9;
        }
        static void ToolPart(Transform root, Vector3 p, Vector3 size, Material surface)
        { var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.transform.SetParent(root, false); obj.transform.localPosition = p; obj.transform.localScale = size; obj.GetComponent<Collider>().enabled = false; Destroy(obj.GetComponent<Collider>()); obj.GetComponent<Renderer>().sharedMaterial = surface; }
        public BuildPiece AimedBuild()
        {
            if (!view) return null;
            return Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 10, ~((1 << 9) | (1 << 10)), QueryTriggerInteraction.Ignore) ? hit.collider.GetComponentInParent<BuildPiece>() : null;
        }
        public bool BeginBuildEdit(BuildPiece piece)
        {
            if (!ConstructionWorld.CanModify(this, piece)) return false;
            EditTarget = piece; piece.SetPreviewHidden(true); EditMask = piece.Mask.Value; BuildTurn = BuildGrid.FacingTurn(piece.transform.eulerAngles.y,piece.GridYaw.Value);
            ReserveUI.Instance.OpenBuildEditor(); ShowBlueprint(piece.transform.position, piece.Kind.Value, EditMask, BuildTurn, true,piece.GridYaw.Value); return true;
        }
        void ShowBlueprint(Vector3 point, int kind, int mask, int turn, bool valid, float gridYaw = 0)
        {
            if (!blueprintSurface)
            {
                blueprintSurface = new Material(Shader.Find("Standard")); blueprintSurface.SetFloat("_Mode", 3); blueprintSurface.SetInt("_SrcBlend", 5); blueprintSurface.SetInt("_DstBlend", 10); blueprintSurface.SetInt("_ZWrite", 0); blueprintSurface.EnableKeyword("_ALPHABLEND_ON"); blueprintSurface.renderQueue = 3000;
            }
            blueprintSurface.color = valid ? new Color(.2f, .75f, 1, .28f) : new Color(1, .23f, .18f, .3f);
            int stamp = kind * 1024 + mask;
            if (stamp != blueprintStamp || !blueprint)
            {
                if (blueprint) { blueprint.SetActive(false); Destroy(blueprint); }
                blueprint = new GameObject("Blueprint"); BuildGeometry.Create(blueprint.transform, kind, mask, blueprintSurface, false);
                foreach (var child in blueprint.GetComponentsInChildren<Transform>()) child.gameObject.layer = 10;
                // Disable immediately: Destroy of primitive colliders is end-of-frame.
                foreach (var collider in blueprint.GetComponentsInChildren<Collider>()) collider.enabled = false;
                blueprintStamp = stamp;
            }
            blueprint.SetActive(true); blueprint.transform.SetPositionAndRotation(point, Quaternion.Euler(0, gridYaw + turn * 90, 0));
        }
        public void PreviewBuildEdit(int mask)
        { if (!EditTarget) return; EditMask = mask; ShowBlueprint(EditTarget.transform.position, EditTarget.Kind.Value, mask, BuildTurn, BuildPiece.ValidMask(EditTarget.Kind.Value, mask),EditTarget.GridYaw.Value); }
        public void ConfirmBuildEdit()
        { if (EditTarget) EditBuildServerRpc(EditTarget.NetworkObjectId, EditMask, BuildTurn, false); CancelBuildEdit(); if (ReserveUI.Instance) ReserveUI.Instance.CloseBuildEditor(); }
        public void CancelBuildEdit() { if (EditTarget) EditTarget.SetPreviewHidden(false); EditTarget = null; if (blueprint) blueprint.SetActive(false); }
        public void DisposeConstruction()
        { if (blueprint) Destroy(blueprint); if (blueprintSurface) Destroy(blueprintSurface); if (pickaxeView) Destroy(pickaxeView); }
        [ServerRpc] public void PlaceBuildServerRpc(int kind, int material, Vector3 point, int turn, float gridYaw = 0, Vector3 gridOrigin = default)
        { if (Time.time < serverBuildAt) return; serverBuildAt = Time.time + .075f; if (ConstructionWorld.Current) ConstructionWorld.Current.TryPlace(this, kind, material, point, turn, gridYaw, gridOrigin); }
        [ServerRpc] public void EditBuildServerRpc(ulong id, int mask, int turn, bool reset)
        { if (Time.time < serverEditAt) return; serverEditAt = Time.time + .07f; if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj) && ConstructionWorld.Current) ConstructionWorld.Current.TryEdit(this, obj.GetComponent<BuildPiece>(), mask, turn, reset); }
        [ServerRpc] public void RepairBuildServerRpc(ulong id)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj)) return; var piece = obj.GetComponent<BuildPiece>();
            if (!ConstructionWorld.CanModify(this, piece) || piece.Health.Value >= piece.Maximum || BuildingMaterial(piece.Material.Value) < 2) return;
            AddBuildingMaterial(piece.Material.Value, -2); piece.Health.Value = Mathf.Min(piece.Maximum, piece.Health.Value + 50);
        }
        [ServerRpc] public void DoorServerRpc(ulong id)
        {
            if (!Alive || !CanAct || !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj)) return; var piece = obj.GetComponent<BuildPiece>();
            if (!piece || piece.Kind.Value != 0 || (511 ^ piece.Mask.Value) != 18 || (piece.Center - transform.position).sqrMagnitude > 4.5f * 4.5f) return;
            piece.DoorOpen.Value = !piece.DoorOpen.Value;
        }
        [ServerRpc] public void SwingServerRpc(Vector3 origin, Vector3 direction)
        {
            if (!Alive || !CanAct || CurrentVehicle || !Finite(origin) || !Finite(direction) || direction.sqrMagnitude < .5f || (origin - transform.position - Vector3.up * 1.4f).sqrMagnitude > 9 || Time.time < serverSwing) return;
            serverSwing = Time.time + .45f;
            float closest = 3.8f; RaycastHit selected = default;
            foreach (var hit in Physics.SphereCastAll(origin, .12f, direction.normalized, 3.5f, ~(1 << 10), QueryTriggerInteraction.Ignore))
            { if (hit.collider.GetComponentInParent<Ranger>() || hit.distance >= closest) continue; closest = hit.distance; selected = hit; }
            Ranger struck = null;
            foreach (var other in Players)
            {
                if (!other || other == this || !other.Alive) continue;
                var pose = new ShotHistory.Pose { bottom = other.transform.position, height = other.StanceHeight, radius = .33f, yaw = other.transform.eulerAngles.y };
                if (PlayerHitboxes.Ray(origin, direction.normalized, pose, closest, out float distance, out bool head)) { struck = other; closest = distance; }
            }
            if (struck)
            {
                if (struck.Team.Value == Team.Value || !WildfrontMatch.Current || !WildfrontMatch.Current.Active) return;
                int health = struck.Health.Value, armor = struck.Armor.Value; bool killed = struck.TakeDamageServer(20, (int)NetworkObjectId);
                ConfirmDamage(struck.transform.position + Vector3.up, Mathf.Max(0, health - struck.Health.Value), Mathf.Max(0, armor - struck.Armor.Value), struck.Health.Value, 100, killed, false, struck.NetworkObjectId, true); return;
            }
            if (!selected.collider) return;
            var node = HarvestResources.Find(selected.collider);
            if (node != null && ConstructionWorld.Current)
            {
                int damage = ConstructionWorld.Current.DamageNode(node, 45, this);
                if (damage > 0) ConfirmDamage(selected.point, damage, 0, node.health, node.maximum, node.health == 0, false, ulong.MaxValue, false);
                return;
            }
            var piece = selected.collider.GetComponentInParent<BuildPiece>();
            if (piece) { piece.Damage(60); ConfirmDamage(selected.point, 60, 0, piece.Health.Value, piece.Maximum, piece.Health.Value == 0, false, piece.NetworkObjectId, false); return; }
            var animal = selected.collider.GetComponentInParent<Wildlife>();
            if (animal && !animal.Dead) { bool kill = animal.Hit(30, transform.position); ConfirmDamage(selected.point, 30, 0, animal.Health.Value, animal.EffectiveMaxHealth, kill, false, animal.NetworkObjectId, false); if (kill) AnimalReward(animal); }
        }
    }
}
