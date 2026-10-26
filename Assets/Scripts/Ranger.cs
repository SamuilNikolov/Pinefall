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
            Health.OnValueChanged+=HealthChanged;Armor.OnValueChanged+=ArmorChanged;
        }
        public override void OnNetworkDespawn()
        {
            Players.Remove(this);Weapon.OnValueChanged-=ChangedWeapon;
            Health.OnValueChanged-=HealthChanged;Armor.OnValueChanged-=ArmorChanged;
            if(Local==this){Local=null;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        }
        void ChangedWeapon(int old,int value){if(IsOwner&&weaponView)weaponView.Equip(value);}
        void ArmorChanged(int old,int value){if(IsOwner&&value<old)DamageFlash=Time.time+.65f;}
        void HealthChanged(int old,int value){var hitbox=GetComponent<CapsuleCollider>();if(hitbox)hitbox.enabled=value>0;if(IsOwner&&value<old)DamageFlash=Time.time+.65f;}
        [ServerRpc] void IdentifyServerRpc(string name,int weapon,int team)
        {
            DisplayName.Value=new FixedString64Bytes(ReserveSession.CleanName(name));Weapon.Value=Mathf.Clamp(weapon,0,2);Ammo.Value=magazines[Weapon.Value];
            Team.Value=WildfrontMatch.Current?WildfrontMatch.Current.AssignTeam(this,team):Mathf.Clamp(team,0,7);
            InitializeEquipment(!WildfrontMatch.Current||!WildfrontMatch.Current.Battle.Value);if(WildfrontMatch.Current)WildfrontMatch.Current.CaptureLateJoin(this);
        }
        void Update()
        {
            if(!IsSpawned)return;
            GunHandlingTick();RescueTick();SurvivalTick();SurveyTick();if(LocalHuman&&ReserveUI.Instance&&ReserveUI.Instance.ControlsActive&&GameBindings.Down("Survey"))SurveyServerRpc();
            if(IsServer&&!Alive&&WildfrontMatch.Current&&!WildfrontMatch.Current.Battle.Value&&Time.time>respawnAfter){ResetRound(ReserveWorld.OnGround(0,-160,.3f));InitializeEquipment(true);}
            if(rig)rig.Pose(Velocity.Value,!Alive);
            if(!LocalHuman&&body)body.gameObject.SetActive(Alive);
            if(IsServer&&Reloading.Value&&Time.time>=reloadEnds)CompleteReload();
            if(!LocalHuman)return;
            bool controls=ReserveUI.Instance&&ReserveUI.Instance.ControlsActive&&CanAct;if(controls&&GameBindings.Down("Flashlight")&&torch)torch.enabled=!torch.enabled;if((!Alive||Downed.Value)&&torch)torch.enabled=false;
            if(Downed.Value){Aiming=false;if(weaponView)weaponView.SetVisible(false);if(Velocity.Value!=0)Velocity.Value=0;if(view){view.transform.position=transform.position+Vector3.up*.65f;view.transform.localRotation=Quaternion.Euler(LookPitch,0,0);}return;}
            if(!Alive)
            {
                Aiming=false;if(weaponView){weaponView.SetVisible(false);weaponView.enabled=false;}
                var friend=Players.Find(p=>p&&p!=this&&p.IsSpawned&&p.Alive&&p.Team.Value==Team.Value);
                if(friend&&view){view.transform.position=Vector3.Lerp(view.transform.position,friend.transform.position-friend.transform.forward*4+Vector3.up*2.5f,Time.deltaTime*3);view.transform.LookAt(friend.transform.position+Vector3.up*1.3f);}
                if(Velocity.Value!=0)Velocity.Value=0;return;
            }
            if(weaponView)weaponView.enabled=true;
            if(ReviveControls(controls))return;
            if(VehicleControls(controls))return;
            if(controller&&!controller.enabled)controller.enabled=true;
            if(!controls){Aiming=SmokeScoped;if(view)view.fieldOfView=Aiming?AimFov:74;if(Velocity.Value!=0)Velocity.Value=0;return;}
            transform.Rotate(0,Input.GetAxisRaw("Mouse X")*ReserveUI.Instance.Sensitivity,0);
            pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*ReserveUI.Instance.Sensitivity,-83,83);
            view.transform.localRotation=Quaternion.Euler(LookPitch,0,0);
            Crouched.Value=GameBindings.Held("Crouch")||false;
            float moveX=GameBindings.Horizontal,moveZ=GameBindings.Vertical;
            bool sprint=GameBindings.Held("Sprint")&&moveZ>0&&!Crouched.Value&&stamina>.02f&&CanSprint&&!GameBindings.Held("Aim")&&!GameBindings.Held("Fire");
            stamina=Mathf.Clamp01(stamina+Time.deltaTime*(sprint?-.16f:.14f));
            float speed=(Crouched.Value?1.9f:sprint?7:4)*(Hunger.Value<10?.7f:1);
            var move=Vector3.ClampMagnitude(transform.right*moveX+transform.forward*moveZ,1)*speed;
            if(controller.isGrounded){vertical=-2;if(GameBindings.Down("Jump")&&!Crouched.Value)vertical=5.8f;}else vertical-=18*Time.deltaTime;
            if(!SwimMove(move,sprint))controller.Move((move+Vector3.up*vertical)*Time.deltaTime);
            var p=transform.position;p.x=Mathf.Clamp(p.x,-ReserveWorld.Edge,ReserveWorld.Edge);p.z=Mathf.Clamp(p.z,-ReserveWorld.Edge,ReserveWorld.Edge);
            if(p.y<ReserveWorld.Height(p.x,p.z)-3)p=ReserveWorld.OnGround(p.x,p.z,.15f);
            transform.position=p;
            float actual=new Vector2(controller.velocity.x,controller.velocity.z).magnitude;
            if(Mathf.Abs(Velocity.Value-actual)>.2f)Velocity.Value=actual;
            float head=Crouched.Value?1.17f:1.7f;
            view.transform.localPosition=Vector3.Lerp(view.transform.localPosition,new Vector3(0,head+Mathf.Sin(step*2)*.018f*(actual>1?1:0),0),Time.deltaTime*12);
            if(actual>.5f&&controller.isGrounded&&!Swimming.Value){float before=Mathf.Floor(step);step+=Time.deltaTime*actual*.36f;if(Mathf.Floor(step)>before){ReserveAudio.Step(transform.position,Crouched.Value?.06f:.16f,ExpeditionLayout.RoadDistance(transform.position.x,transform.position.z)<5,true);FootstepServerRpc();}}
            Aiming=GameBindings.Held("Aim")&&!Reloading.Value;
            view.fieldOfView=Mathf.Lerp(view.fieldOfView,Aiming?AimFov:74,Time.deltaTime*11);
            for(int key=0;key<3;key++)if(GameBindings.Down("Weapon "+(key+1)))SelectWeapon(SlotWeapon(key));
            if(GameBindings.Down("Food"))EatServerRpc();if(GameBindings.Down("Heal"))HealServerRpc();
            if(Input.GetAxis("Mouse ScrollWheel")!=0)SelectWeapon(CycleWeapon(Input.GetAxis("Mouse ScrollWheel")>0?1:-1));
            if(GameBindings.Down("Reload"))ReloadServerRpc();


            if(GameBindings.Down("Plate"))PlateServerRpc();
            var nearVehicle=RoadVehicle.Near(transform.position);
            if(nearVehicle){if(GameBindings.Down("Repair"))ServiceVehicleServerRpc(nearVehicle.NetworkObjectId,0);if(GameBindings.Down("Refuel"))ServiceVehicleServerRpc(nearVehicle.NetworkObjectId,1);if(GameBindings.Down("Vehicle upgrade"))ServiceVehicleServerRpc(nearVehicle.NetworkObjectId,2);}
            if(Input.GetKeyDown(KeyCode.Return)&&WildfrontMatch.Current&&ReserveSession.Instance.IsHost&&WildfrontMatch.Current.Phase.Value!=1)WildfrontMatch.Current.TryStartRound();
            bool trigger=WeaponData.Get(Weapon.Value).automatic?GameBindings.Held("Fire"):GameBindings.Down("Fire");
            if(trigger)Fire();
            FocusAnimal=null;
            RaycastHit hit;
            if(Physics.Raycast(view.transform.position,view.transform.forward,out hit,650,~(1<<9),QueryTriggerInteraction.Collide))FocusAnimal=hit.collider.GetComponentInParent<Wildlife>();
            if(GameBindings.Down("Interact"))
            {
                var supply=SupplyDrop.Near(transform.position);if(supply){supply.OpenServerRpc();return;}var loot=WorldLoot.Near(transform.position);if(loot){ScavengeServerRpc(loot.NetworkObjectId);return;}
                var pickup=NearestPickup();
                if(pickup)CollectServerRpc(pickup.NetworkObjectId);
                else if(nearVehicle)EnterVehicleServerRpc(nearVehicle.NetworkObjectId);
                else if(FocusAnimal&&FocusAnimal.Dead&&Vector3.Distance(transform.position,FocusAnimal.transform.position)<4)HarvestServerRpc(FocusAnimal.NetworkObjectId);
            }
        }
        public void SelectWeapon(int index){if(!IsOwner)return;EquipServerRpc(index);if(ReserveSession.Arg("--smoke")==""&&ReserveSession.Arg("--capture")=="")PlayerPrefs.SetInt("weapon",index);}
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
            weaponView.Kick();ReserveAudio.Gun(view.transform.position,Weapon.Value,.7f);
            Vector3 origin=transform.position+Vector3.up*(Crouched.Value?1.17f:1.65f);
            var hits=Physics.RaycastAll(view.transform.position,view.transform.forward,data.range,~(1<<10),QueryTriggerInteraction.Collide).Where(h=>h.collider.GetComponentInParent<Ranger>()!=this&&(!CurrentVehicle||h.collider.GetComponentInParent<RoadVehicle>()!=CurrentVehicle)).OrderBy(h=>h.distance).ToArray();
            Vector3 point=hits.Length>0?hits[0].point:view.transform.position+view.transform.forward*data.range;
            FireServerRpc(origin,(point-origin).normalized,Weapon.Value);FireKick();
        }
        [ServerRpc] void FireServerRpc(Vector3 origin,Vector3 direction,int weapon)
        {
            if(!Alive||!CanAct||weapon!=Weapon.Value||Reloading.Value||Ammo.Value<=0||Time.time+.03f<nextServerShot)return;
            if(WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value&&WildfrontMatch.Current.Phase.Value==2)return;
            if(!Finite(origin)||!Finite(direction)||Vector3.Distance(origin,transform.position+Vector3.up*1.5f)>3||direction.sqrMagnitude<.5f)return;
            var data=WeaponData.Get(weapon);nextServerShot=Time.time+data.interval;Ammo.Value--;magazines[weapon]=Ammo.Value;
            bool hitAnimal=false,killed=false;Vector3 impact=origin+direction.normalized*data.range;Vector3 normal=-direction.normalized;
            float spread=ShotSpread(data);
            var hitCounts=new Dictionary<Wildlife,int>();
            var playerHits=new Dictionary<Ranger,int>();int damage=Mathf.RoundToInt(data.damage*DamageFactor(weapon)*WeaponData.RarityPower(Rarity(weapon)));
            for(int i=0;i<data.pellets;i++)
            {
                Vector3 dir=(direction.normalized+UnityEngine.Random.insideUnitSphere*spread).normalized;
                RaycastHit hit=default;float nearest=data.range+1;foreach(var candidate in Physics.RaycastAll(origin,dir,data.range,~(1<<10),QueryTriggerInteraction.Collide)){if(candidate.collider.GetComponentInParent<Ranger>()==this||CurrentVehicle&&candidate.collider.GetComponentInParent<RoadVehicle>()==CurrentVehicle||candidate.distance>=nearest)continue;hit=candidate;nearest=candidate.distance;}
                if(hit.collider)
                {
                    impact=hit.point;normal=hit.normal;var a=hit.collider.GetComponentInParent<Wildlife>();
                    if(a&&!a.Dead){hitAnimal=true;if(!hitCounts.ContainsKey(a))hitCounts[a]=0;hitCounts[a]+=damage;}
                    var enemy=hit.collider.GetComponentInParent<Ranger>();
                    if(enemy&&enemy!=this&&enemy.Alive&&enemy.Team.Value!=Team.Value&&WildfrontMatch.Current&&WildfrontMatch.Current.Active)
                    {hitAnimal=true;if(!playerHits.ContainsKey(enemy))playerHits[enemy]=0;float headshot=hit.point.y-enemy.transform.position.y>1.5f?1.4f:1;playerHits[enemy]+=Mathf.RoundToInt(data.playerDamage*DamageFactor(weapon)*WeaponData.RarityPower(Rarity(weapon))*headshot);}
                    var vehicle=hit.collider.GetComponentInParent<RoadVehicle>();if(vehicle)vehicle.Damage(damage);
                    var range=hit.collider.GetComponent<RangeTarget>();if(range)range.Hit();
                }
            }
            foreach(var h in hitCounts)if(h.Key.Hit(h.Value,transform.position)){killed=true;Score.Value++;if(WildfrontMatch.Current)WildfrontMatch.Current.DropReward(h.Key.transform.position,h.Key.kind,h.Key.Enraged.Value);}
            foreach(var h in playerHits)if(h.Key.TakeDamageServer(h.Value,(int)NetworkObjectId))killed=true;
            foreach(var a in ReserveSession.Instance.Animals)if(a&&Vector3.Distance(a.transform.position,origin)<180)a.HearShot(origin);
            ShotClientRpc(origin,impact,normal,weapon,hitAnimal,killed);
        }
        static bool Finite(Vector3 v){return !(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));}
        [ClientRpc]void ShotClientRpc(Vector3 origin,Vector3 impact,Vector3 normal,int weapon,bool hit,bool killed)
        {
            if(!LocalHuman)ReserveAudio.Gun(origin,weapon,.8f);
            ReserveEffects.Impact(impact,normal,hit);
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
            var match=WildfrontMatch.Current;if(!IsServer||!Alive||amount<=0||match&&match.Battle.Value&&!match.Active)return false;amount=ProtectDamage(amount,attacker);if(amount<=0)return false;CancelRevive();
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
            if(!IsServer)return;ResetRescue();OrderCycle.Value=0;orderVisited.Clear();ExitVehicleServer();Health.Value=100;Kills.Value=0;Score.Value=0;Harvests.Value=0;Craft.Value=0;Upgrade0.Value=Upgrade1.Value=Upgrade2.Value=OtherUpgrades.Value=0;Optics.Value=Barrels.Value=Handling.Value=0;ModBag.Value=0;Reloading.Value=false;
            magazines=WeaponData.All.Select(w=>w.magazine).ToArray();if(Weapon.Value<0)Weapon.Value=3;Ammo.Value=magazines[Weapon.Value];
            if(IsBot.Value){controller.enabled=false;transform.position=spawn;controller.enabled=true;}else ResetClientRpc(spawn);
        }
        [ClientRpc]void ResetClientRpc(Vector3 spawn)
        {
            if(!IsOwner)return;SmokeMoveTo(spawn);if(view){view.transform.localPosition=Vector3.up*1.7f;view.transform.localRotation=Quaternion.identity;}pitch=0;if(ReserveUI.Instance)ReserveUI.Instance.BeginPlay();ShowNotice(WildfrontMatch.Current&&WildfrontMatch.Current.Battle.Value?"Wildfront has begun. Reach the next safe zone and outlast the other teams.":"Back at the ranger outpost.",6);
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
            if(!IsOwner)return;controller.enabled=false;transform.position=position;controller.enabled=true;
        }
    }
}
