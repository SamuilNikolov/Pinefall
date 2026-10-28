using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Pinefall
{
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class Ranger : NetworkBehaviour
    {
        void Awake(){NetworkSeed.Initialize(this);}
        public static readonly List<Ranger> Players=new List<Ranger>();
        public static Ranger Local;
        public NetworkVariable<FixedString64Bytes> DisplayName=new NetworkVariable<FixedString64Bytes>(new FixedString64Bytes("Ranger"));
        public NetworkVariable<int> Weapon=new NetworkVariable<int>(0);
        public NetworkVariable<int> Ammo=new NetworkVariable<int>(5);
        public NetworkVariable<bool> Reloading=new NetworkVariable<bool>(false);
        public NetworkVariable<int> Score=new NetworkVariable<int>(0);
        public NetworkVariable<int> Harvests=new NetworkVariable<int>(0);
        public NetworkVariable<int> Team=new NetworkVariable<int>(-1);
        public NetworkVariable<int> Health=new NetworkVariable<int>(100);
        public NetworkVariable<int> Kills=new NetworkVariable<int>(0);
        public NetworkVariable<int> Craft=new NetworkVariable<int>(0);
        public NetworkVariable<int> Upgrade0=new NetworkVariable<int>(0),Upgrade1=new NetworkVariable<int>(0),Upgrade2=new NetworkVariable<int>(0);
        public bool Alive {get{return Health.Value>0;}}
        public int UpgradeLevel(int index){return index<0?0:index==0?Upgrade0.Value:index==1?Upgrade1.Value:index==2?Upgrade2.Value:(OtherUpgrades.Value>>((index-3)*2))&3;}
        public NetworkVariable<float> Velocity=new NetworkVariable<float>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> Crouched=new NetworkVariable<bool>(false,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
        public Transform body;
        public VisualRig rig;
        public Camera view;
        public WeaponView weaponView;
        CharacterController controller;
        Light torch;
        float pitch,vertical,step,nextLocalShot,nextServerShot,reloadEnds,noticeUntil,hitUntil,stamina=1,nextStep,respawnAfter;
        public float DamageFlash {get;private set;}
        int[] magazines=WeaponData.All.Select(w=>w.magazine).ToArray();
        public bool Aiming {get;private set;}
        public bool SmokeScoped;
        public string Notice {get;private set;}
        public float Stamina {get{return stamina;}}
        public bool HitMarker {get{return Time.time<hitUntil;}}
        public Wildlife FocusAnimal {get;private set;}
        public override void OnNetworkSpawn()
        {
            Players.Add(this);controller=GetComponent<CharacterController>();
            controller.enabled=IsBot.Value?IsServer:IsOwner;rig=body?body.GetComponentInChildren<VisualRig>():null;
            gameObject.AddComponent<AvatarStyle>();
            if(IsBot.Value&&IsServer){InitializeEquipment(false);gameObject.AddComponent<HunterBot>();}
            if(LocalHuman)
            {
                Local=this;
                ReserveSession.Instance.EnterLocalPlayer();
                if(body)body.gameObject.SetActive(false);
                var c=new GameObject("Ranger view");c.tag="MainCamera";c.transform.SetParent(transform,false);c.transform.localPosition=new Vector3(0,1.7f,0);
                view=c.AddComponent<Camera>();view.nearClipPlane=.07f;view.farClipPlane=1900;view.fieldOfView=74;view.allowHDR=true;
                c.AddComponent<AudioListener>();torch=c.AddComponent<Light>();torch.type=LightType.Spot;torch.range=70;torch.spotAngle=55;torch.intensity=3;torch.color=new Color(1,.94f,.80f);torch.enabled=false;weaponView=c.AddComponent<WeaponView>();weaponView.owner=this;
                c.AddComponent<RangerCamera>().owner=this;
                IdentifyServerRpc(ReserveSession.Instance.PlayerName,ReserveSession.Instance.SelectedWeapon,ReserveSession.Instance.RequestedTeam);
                AppearanceServerRpc(ReserveSession.Instance.ModelChoice,ReserveSession.Instance.ColorChoice,ReserveSession.Instance.GearChoice);
                if(ReserveUI.Instance)ReserveUI.Instance.BeginPlay();
                ShowNotice("Welcome to Pinefall. Hunt for food, scavenge the cabins, and keep your team supplied.",7);
            }
            if(ProximityVoice.Current)ProximityVoice.Current.Prepare(this);
            Weapon.OnValueChanged+=ChangedWeapon;
            Health.OnValueChanged+=HealthChanged;Armor.OnValueChanged+=ArmorChanged;DamageRevision.OnValueChanged+=DamageRevisionChanged;
        }
        public override void OnNetworkDespawn()
        {
            DisposeConstruction();Players.Remove(this);Weapon.OnValueChanged-=ChangedWeapon;
            Health.OnValueChanged-=HealthChanged;Armor.OnValueChanged-=ArmorChanged;DamageRevision.OnValueChanged-=DamageRevisionChanged;
            if(Local==this){Local=null;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        }
        void ChangedWeapon(int old,int value){if(IsOwner&&weaponView)weaponView.Equip(value);}
        void ArmorChanged(int old,int value){if(LocalHuman&&value>old)ArmorEquipPulse=Time.time+.65f;}
        void HealthChanged(int old,int value){var hitbox=GetComponent<CapsuleCollider>();if(hitbox)hitbox.enabled=value>0;}
        [ServerRpc] void IdentifyServerRpc(string name,int weapon,int team)
        {
            DisplayName.Value=new FixedString64Bytes(ReserveSession.CleanName(name));Weapon.Value=Mathf.Clamp(weapon,0,2);Ammo.Value=magazines[Weapon.Value];
            Team.Value=WildfrontMatch.Current?WildfrontMatch.Current.AssignTeam(this,team):Mathf.Clamp(team,0,7);
            InitializeEquipment(!WildfrontMatch.Current||!WildfrontMatch.Current.Battle.Value);if(WildfrontMatch.Current)WildfrontMatch.Current.CaptureLateJoin(this);
        }
        void Update()
        {
            if(!IsSpawned)return;
            EmoteTick();SpectatorTick();StanceTick();if(LocalHuman)ConstructionTick(ReserveUI.Instance&&ReserveUI.Instance.ControlsActive&&CanAct);GunHandlingTick();RescueTick();SurvivalTick();SurveyTick();if(LocalHuman&&ReserveUI.Instance&&ReserveUI.Instance.ControlsActive&&GameBindings.Down("Survey"))SurveyServerRpc();
            if(IsServer&&!Alive&&WildfrontMatch.Current&&!WildfrontMatch.Current.Battle.Value&&Time.time>respawnAfter){ResetRound(ReserveWorld.OnGround(0,-160,.3f));InitializeEquipment(true);}
            if(rig)rig.Pose(Velocity.Value,!Alive);
            if(!LocalHuman&&body)body.gameObject.SetActive(Alive);
            if(IsServer&&Reloading.Value&&Time.time>=reloadEnds)CompleteReload();
            if(!LocalHuman)return;
            // Explicit capture sessions own the camera and actor pose. Normal
            // input/head-bob must not invalidate a scripted rendered viewpoint.
            if(CaptureProbe.Running)return;
            bool controls=ReserveUI.Instance&&ReserveUI.Instance.ControlsActive&&CanAct;if(controls&&GameBindings.Down("Flashlight")&&torch)torch.enabled=!torch.enabled;if((!Alive||Downed.Value)&&torch)torch.enabled=false;
            if(Downed.Value){MovementWithoutInput();Aiming=false;if(weaponView)weaponView.SetVisible(false);if(Velocity.Value!=0)Velocity.Value=0;if(view){view.transform.position=transform.position+Vector3.up*.65f;view.transform.localRotation=Quaternion.Euler(LookPitch,0,0);}return;}
            if(!Alive)
            {
                Aiming=false;if(weaponView){weaponView.SetVisible(false);weaponView.enabled=false;}
                SpectatorCameraTick();
                if(Velocity.Value!=0)Velocity.Value=0;return;
            }
            if(weaponView)weaponView.enabled=true;
            if(ReviveControls(controls))return;
            if(VehicleControls(controls)){ResetHopMomentum();return;}
            if(controller&&!controller.enabled)controller.enabled=true;
            if(!controls){MovementWithoutInput();Aiming=SmokeScoped;if(view)view.fieldOfView=Aiming?AimFov:74;return;}
            transform.Rotate(0,Input.GetAxisRaw("Mouse X")*ReserveUI.Instance.Sensitivity,0);
            pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*ReserveUI.Instance.Sensitivity,-83,83);
            view.transform.localRotation=Quaternion.Euler(LookPitch,0,0);
            // Crouch/slide collision and visible stance are synchronized independently of input.
            float moveX=GameBindings.Horizontal,moveZ=GameBindings.Vertical;
            bool sprint=GameBindings.Held("Sprint")&&moveZ>0&&!GameBindings.Held("Crouch")&&stamina>.02f&&CanSprint&&!GameBindings.Held("Aim")&&!GameBindings.Held("Fire");
            stamina=Mathf.Clamp01(stamina+Time.deltaTime*(sprint&&!Sliding.Value?-.065f:.23f));
            var move=MovementInput(moveX,moveZ,sprint);
            if(!LaunchMotion(ref move))move=ResolveJumpMovement(move,GameBindings.Held("Jump"),GameBindings.Down("Jump"),Time.deltaTime);
            slideJumpRequested=false;if(!SwimMove(move,sprint))controller.Move((move+Vector3.up*vertical)*Time.deltaTime);
            var p=transform.position;p.x=Mathf.Clamp(p.x,-ReserveWorld.Edge,ReserveWorld.Edge);p.z=Mathf.Clamp(p.z,-ReserveWorld.Edge,ReserveWorld.Edge);
            if(p.y<ReserveWorld.Height(p.x,p.z)-3&&!CaveLayout.Underground(p))p=ReserveWorld.OnGround(p.x,p.z,.15f);
            transform.position=p;
            float actual=new Vector2(controller.velocity.x,controller.velocity.z).magnitude;
            if(Mathf.Abs(Velocity.Value-actual)>.2f)Velocity.Value=actual;
            float head=Sliding.Value?.78f:Crouched.Value?1.17f:1.7f;
            view.transform.localPosition=Vector3.Lerp(view.transform.localPosition,new Vector3(0,head+Mathf.Sin(step*2)*.018f*(actual>1?1:0),0),Time.deltaTime*12);
            if(actual>.5f&&controller.isGrounded&&!Swimming.Value){float before=Mathf.Floor(step);step+=Time.deltaTime*actual*.36f;if(Mathf.Floor(step)>before){ReserveAudio.Step(transform.position,Crouched.Value?.06f:.16f,ExpeditionLayout.RoadDistance(transform.position.x,transform.position.z)<5,true);FootstepServerRpc();}}
            Aiming=ToolMode.Value==0&&GameBindings.Held("Aim")&&!Reloading.Value;
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,Aiming?AimFov:74,Time.deltaTime*11);
            for(int key=0;key<3;key++)if(GameBindings.Down("Weapon "+(key+1)))SelectWeapon(SlotWeapon(key));
            if(GameBindings.Down("Food"))EatServerRpc();if(GameBindings.Down("Heal"))HealServerRpc();
            if(ToolMode.Value==0&&Input.GetAxis("Mouse ScrollWheel")!=0)SelectWeapon(CycleWeapon(Input.GetAxis("Mouse ScrollWheel")>0?1:-1));
            if(ToolMode.Value==0&&GameBindings.Down("Reload"))ReloadServerRpc();


            if(GameBindings.Down("Plate"))PlateServerRpc();
            var nearVehicle=RoadVehicle.Near(transform.position);
            if(nearVehicle){if(GameBindings.Down("Repair"))ServiceVehicleServerRpc(nearVehicle.NetworkObjectId,0);if(GameBindings.Down("Refuel"))ServiceVehicleServerRpc(nearVehicle.NetworkObjectId,1);if(GameBindings.Down("Vehicle upgrade"))ServiceVehicleServerRpc(nearVehicle.NetworkObjectId,2);}
            if(Input.GetKeyDown(KeyCode.Return)&&WildfrontMatch.Current&&ReserveSession.Instance.IsHost&&WildfrontMatch.Current.Phase.Value!=1)WildfrontMatch.Current.TryStartRound();
            bool trigger=WeaponData.Get(Weapon.Value).automatic?GameBindings.Held("Fire"):GameBindings.Down("Fire");
            if(trigger&&ToolMode.Value==0)Fire();
            FocusAnimal=null;
            RaycastHit hit;
            if(Physics.Raycast(view.transform.position,view.transform.forward,out hit,650,~(1<<9),QueryTriggerInteraction.Collide))FocusAnimal=hit.collider.GetComponentInParent<Wildlife>();
            if(GameBindings.Down("Interact"))
            {
                var door=AimedBuild();if(door&&(511^door.Mask.Value)==18){DoorServerRpc(door.NetworkObjectId);return;}var supply=SupplyDrop.Near(transform.position);if(supply){supply.OpenServerRpc();return;}var loot=WorldLoot.Near(transform.position);if(loot){ScavengeServerRpc(loot.NetworkObjectId);return;}
                var pickup=NearestPickup();
                if(pickup)CollectServerRpc(pickup.NetworkObjectId);
                else if(nearVehicle)EnterVehicleServerRpc(nearVehicle.NetworkObjectId);
                else if(FocusAnimal&&FocusAnimal.Dead&&Vector3.Distance(transform.position,FocusAnimal.transform.position)<4)HarvestServerRpc(FocusAnimal.NetworkObjectId);
            }
        }
        public void SelectWeapon(int index){if(!IsOwner)return;SelectTool(0);EquipServerRpc(index);if(ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")=="")PlayerPrefs.SetInt("weapon",index);}
        [ServerRpc]void EquipServerRpc(int index)
        {
            if(!Alive||!CanAct||!Owns(index)||index==Weapon.Value)return;
            if(Weapon.Value>=0)magazines[Weapon.Value]=Ammo.Value;Reloading.Value=false;Weapon.Value=index;Ammo.Value=magazines[index];nextServerShot=Time.time+.2f;
        }
        [ServerRpc]public void ReloadServerRpc()
        {
            if(!Alive||!CanAct||Reloading.Value||Ammo.Value==WeaponData.Get(Weapon.Value).magazine||ReserveFor(WeaponData.Get(Weapon.Value).ammoType)<=0)return;
            Reloading.Value=true;ReloadStarted.Value=NetworkManager.ServerTime.Time;reloadEnds=Time.time+WeaponData.Get(Weapon.Value).reload*ReloadFactor(Weapon.Value);
        }
        public void Fire()
        {
            if(!view||!Alive||!CanAct||Weapon.Value<0||Time.time<nextLocalShot||Reloading.Value)return;
            var data=WeaponData.Get(Weapon.Value);
            if(Ammo.Value==0){ShowNotice("Empty magazine. Press R to reload.",2);nextLocalShot=Time.time+.3f;return;}
            nextLocalShot=Time.time+data.interval;
            if(weaponView)weaponView.Kick();ReserveAudio.Gun(view.transform.position,Weapon.Value,.7f);
            Vector3 origin=transform.position+Vector3.up*(Sliding.Value?.78f:Crouched.Value?1.17f:1.65f);
            var hits=Physics.RaycastAll(view.transform.position,view.transform.forward,data.range,~(1<<10),QueryTriggerInteraction.Collide).Where(h=>(!h.collider.isTrigger||h.collider.GetComponentInParent<Ranger>()||h.collider.GetComponentInParent<Wildlife>())&&h.collider.GetComponentInParent<Ranger>()!=this&&(!CurrentVehicle||h.collider.GetComponentInParent<RoadVehicle>()!=CurrentVehicle)).OrderBy(h=>h.distance).ToArray();
            Vector3 point=hits.Length>0?hits[0].point:view.transform.position+view.transform.forward*data.range;
            double stamp=NetworkManager.ServerTime.Time;FireServerRpc(origin,(point-origin).normalized,Weapon.Value,stamp,++shotSequence,Aiming,Mathf.Clamp(.1f-Time.unscaledDeltaTime,.03f,.1f));FireKick();
        }
        static bool Finite(Vector3 v){return !(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));}
        [ClientRpc]void ShotClientRpc(Vector3 origin,Vector3 impact,Vector3 normal,int weapon,bool hit,bool killed)
        {
            if(!LocalHuman)ReserveAudio.Gun(origin,weapon,.8f);
            ReserveEffects.Impact(impact,normal,hit);ReserveEffects.Trace(origin,impact,weapon);
            if(IsOwner&&hit)hitUntil=Time.time+.3f;
        }
        [ServerRpc]void HarvestServerRpc(ulong id)
        {
            NetworkObject obj;if(!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id,out obj))return;
            var a=obj.GetComponent<Wildlife>();if(!a||Vector3.Distance(transform.position,a.transform.position)>4.5f)return;
            if(a.Harvest()){Harvests.Value++;HarvestClientRpc(id);}
        }
        [ClientRpc]void HarvestClientRpc(ulong id){if(IsOwner)ShowNotice("Harvest recorded in your field journal.",4);}
        public void ShowNotice(string text,float seconds){Notice=text;noticeUntil=Time.time+seconds;}
        public string ActiveNotice {get{return Time.time<noticeUntil?Notice:"";}}
        public FieldPickup NearestPickup()
        {FieldPickup best=null;float distance=4;foreach(var drop in FieldPickup.All){float d=Vector3.Distance(transform.position,drop.transform.position);if(d<distance){distance=d;best=drop;}}return best;}
        [ServerRpc]public void CollectServerRpc(ulong id)
        {
            NetworkObject obj;if(!Alive||!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id,out obj))return;
            var drop=obj.GetComponent<FieldPickup>();if(!drop||Vector3.Distance(transform.position,drop.transform.position)>4.5f)return;
            Health.Value=Mathf.Min(100,Health.Value+drop.Healing.Value);Craft.Value+=drop.Materials.Value;drop.NetworkObject.Despawn();RewardClientRpc();
        }
        [ClientRpc]void RewardClientRpc(){if(IsOwner)ShowNotice("Harvest reward collected: healing and fieldcraft materials. Press U to upgrade.",5);}
        [ServerRpc]public void UpgradeServerRpc(int index){if(IsServer)FeedbackClientRpc("Find attachments, then fit them in the inventory.");}
        public bool TakeDamageServer(int amount,int attacker)
        {
            var match=WildfrontMatch.Current;if(!IsServer||!Alive||amount<=0||match&&match.Battle.Value&&!match.Active)return false;int beforeArmor=Armor.Value;amount=ProtectDamage(amount,attacker);if(amount<=0){if(Armor.Value<beforeArmor){CancelEmoteServer();DamageRevision.Value++;}return false;}DamageRevision.Value++;CancelEmoteServer();CancelRevive();
            if(Downed.Value){if(attacker>=0)lastDowner=attacker;Health.Value=Mathf.Max(1,Health.Value-amount);if(Health.Value<=1)EliminateServer(lastDowner);return !Alive;}
            if(amount<Health.Value){Health.Value-=amount;return false;}
            bool rescue=match&&match.Active&&match.TeamSize.Value>1&&knockCount<2&&Players.Any(p=>p&&p!=this&&p.Fighting&&p.Team.Value==Team.Value);
            if(rescue){knockCount++;lastDowner=attacker;Health.Value=35;Downed.Value=true;BleedEnds.Value=NetworkManager.ServerTime.Time+30;ExitVehicleServer();FeedbackClientRpc("Downed. A teammate can hold E nearby for three seconds to revive you.");return false;}
            EliminateServer(attacker);return true;
        }
        [ServerRpc(Delivery=RpcDelivery.Unreliable)]void FootstepServerRpc()
        {if(!Alive||Time.time<nextStep)return;nextStep=Time.time+.16f;FootstepClientRpc(transform.position,Crouched.Value?.12f:.36f,ExpeditionLayout.RoadDistance(transform.position.x,transform.position.z)<5);}
        [ClientRpc(Delivery=RpcDelivery.Unreliable)]void FootstepClientRpc(Vector3 position,float volume,bool gravel)
        {if(!IsOwner)ReserveAudio.Step(position,volume,gravel);}
        public void ResetRound(Vector3 spawn)
        {
            if(!IsServer)return;CancelEmoteServer();ResetHopMomentum();if(ShotHistory.Current)ShotHistory.Current.Teleport(NetworkObjectId,spawn,1.8f,NetworkManager.ServerTime.Time,transform.eulerAngles.y);ResetCombatCadence();ResetRescue();OrderCycle.Value=0;orderVisited.Clear();ExitVehicleServer();Health.Value=100;Kills.Value=0;Score.Value=0;Harvests.Value=0;Craft.Value=0;Upgrade0.Value=Upgrade1.Value=Upgrade2.Value=OtherUpgrades.Value=0;Optics.Value=Barrels.Value=Handling.Value=0;ModBag.Value=0;Reloading.Value=false;
            magazines=WeaponData.All.Select(w=>w.magazine).ToArray();if(Weapon.Value<0)Weapon.Value=3;Ammo.Value=magazines[Weapon.Value];
            if(IsBot.Value){controller.enabled=false;transform.position=spawn;controller.enabled=true;}else ResetClientRpc(spawn);
        }
        [ClientRpc]void ResetClientRpc(Vector3 spawn)
        {
            if(!IsOwner)return;Sliding.Value=false;Crouched.Value=false;SelectTool(0);SmokeMoveTo(spawn);if(view){view.transform.localPosition=Vector3.up*1.7f;view.transform.localRotation=Quaternion.identity;}pitch=0;if(ReserveUI.Instance)ReserveUI.Instance.BeginPlay();ShowNotice(WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value?"Wildfront has begun. Reach the next safe zone and outlast the other teams.":"Back at the ranger outpost.",6);
        }
        public void SmokeAimAtPlayer(Ranger player)
        {
            if(!view)return;var dir=player.transform.position+Vector3.up*1.05f-view.transform.position;transform.rotation=Quaternion.Euler(0,Quaternion.LookRotation(dir).eulerAngles.y,0);view.transform.rotation=Quaternion.LookRotation(dir);
        }
        public void SmokeAimAt(Wildlife animal)
        {
            if(!IsOwner||!view)return;var dir=animal.transform.position+Vector3.up*.8f-view.transform.position;
            transform.rotation=Quaternion.Euler(0,Quaternion.LookRotation(dir).eulerAngles.y,0);pitch=Quaternion.LookRotation(dir).eulerAngles.x;if(pitch>180)pitch-=360;view.transform.localRotation=Quaternion.Euler(LookPitch,0,0);
        }
        public void SmokeMoveTo(Vector3 position)
        {
            if(!IsOwner)return;ResetHopMomentum();if(IsServer&&ShotHistory.Current&&(position-transform.position).sqrMagnitude>25)ShotHistory.Current.Teleport(NetworkObjectId,position,StanceHeight,NetworkManager.ServerTime.Time,transform.eulerAngles.y);controller.enabled=false;transform.position=position;controller.enabled=true;
        }
    }
}
