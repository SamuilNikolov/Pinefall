#include "PFConstructionComponent.h"
#include "PFAnimalHitQueries.h"
#include "PFInventoryLayout.h"
#include "PFPickaxeDamageType.h"
#include "PFInventoryComponent.h"
#include "PFWorldPickup.h"
#include "PFConstructionSubsystem.h"
#include "PFConstructionWorld.h"
#include "PFBuildPiece.h"
#include "PFBuildGeometry.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "PFWildlife.h"
#include "PFVehicle.h"
#include "Camera/CameraComponent.h"
#include "GameFramework/GameStateBase.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"
#include "Components/StaticMeshComponent.h"
#include "ProceduralMeshComponent.h"
#include "Net/UnrealNetwork.h"
#include "Kismet/GameplayStatics.h"
#include "DrawDebugHelpers.h"
UPFConstructionComponent::UPFConstructionComponent()
{
    SetIsReplicatedByDefault(true);PrimaryComponentTick.bCanEverTick=true;PrimaryComponentTick.TickInterval=.05f;
}
void UPFConstructionComponent::BeginPlay()
{
    Super::BeginPlay();
    ValidPreviewMaterial=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Pinefall/Materials/M_BuildPreview.M_BuildPreview"));
    InvalidPreviewMaterial=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Pinefall/Materials/M_BuildInvalid.M_BuildInvalid"));
    if(GetOwner()->HasAuthority()&&!GetWorld()->URL.HasOption(TEXT("br")))Resources.Wood=Resources.Stone=Resources.Metal=500;
    auto CreateTool=[&](const TCHAR *Name,USceneComponent *Parent,FName Socket)
    {
        auto *Mesh=NewObject<UStaticMeshComponent>(GetOwner(),Name);GetOwner()->AddInstanceComponent(Mesh);Mesh->SetupAttachment(Parent,Socket);
        Mesh->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Game/Pinefall/Art/Construction/SM_FieldPickaxe.SM_FieldPickaxe")));
        Mesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);Mesh->RegisterComponent();return Mesh;
    };
    WorldPickaxe=CreateTool(TEXT("Held harvesting tool"),Player()->WorldWeapon->GetAttachParent(),Player()->WorldWeapon->GetAttachSocketName());
    WorldPickaxe->SetRelativeLocation(Player()->WorldWeapon->GetRelativeLocation());WorldPickaxe->SetOwnerNoSee(true);
    ViewPickaxe=CreateTool(TEXT("First person harvesting tool"),Player()->Camera,NAME_None);ViewPickaxe->SetOnlyOwnerSee(true);ViewPickaxe->SetCastShadow(false);ViewPickaxe->SetRelativeLocation({35,20,-30});
    EditGrid=NewObject<UProceduralMeshComponent>(GetOwner(),TEXT("Local edit cells"));GetOwner()->AddInstanceComponent(EditGrid);EditGrid->SetupAttachment(Player()->GetRootComponent());EditGrid->SetAbsolute(true,true,true);EditGrid->SetCollisionEnabled(ECollisionEnabled::NoCollision);EditGrid->SetCastShadow(false);EditGrid->RegisterComponent();EditGrid->SetVisibility(false);
    RemovedEditMaterial=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Pinefall/Materials/M_BuildEditRemoved.M_BuildEditRemoved"));
    HoverEditMaterial=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Pinefall/Materials/M_BuildEditHover.M_BuildEditHover"));
    OnRep_Mode();
}
APFCharacter *UPFConstructionComponent::Player() const {return Cast<APFCharacter>(GetOwner());}
APFConstructionWorld *UPFConstructionComponent::Manager() const
{
    auto *Registry=GetWorld()->GetSubsystem<UPFConstructionSubsystem>();return Registry?Registry->Manager.Get():nullptr;
}
void UPFConstructionComponent::Bind(UInputComponent *Input)
{
    Input->BindKey(EKeys::Four,IE_Pressed,this,&UPFConstructionComponent::Food);
    Input->BindKey(EKeys::Five,IE_Pressed,this,&UPFConstructionComponent::Medkit);
    Input->BindKey(EKeys::Six,IE_Pressed,this,&UPFConstructionComponent::Plate);
    Input->BindKey(EKeys::Seven,IE_Pressed,this,&UPFConstructionComponent::Service);
    Input->BindKey(EKeys::Q,IE_Pressed,this,&UPFConstructionComponent::ToggleBuild);
    Input->BindKey(EKeys::One,IE_Pressed,this,&UPFConstructionComponent::Pickaxe);
    Input->BindKey(EKeys::F1,IE_Pressed,this,&UPFConstructionComponent::Wall);
    Input->BindKey(EKeys::F2,IE_Pressed,this,&UPFConstructionComponent::Floor);
    Input->BindKey(EKeys::F3,IE_Pressed,this,&UPFConstructionComponent::Ramp);
    Input->BindKey(EKeys::F4,IE_Pressed,this,&UPFConstructionComponent::Roof);
    Input->BindKey(EKeys::F,IE_Pressed,this,&UPFConstructionComponent::Edit);
}
void UPFConstructionComponent::OnRep_Mode()
{
    if(auto *P=Player())
    {
        bool ShowGun=Mode==EPFConstructionMode::Combat;
        P->WorldWeapon->SetVisibility(ShowGun);P->ViewWeapon->SetVisibility(ShowGun&&P->bFirstPerson);
        if(WorldPickaxe)WorldPickaxe->SetVisibility(Mode==EPFConstructionMode::Pickaxe&&P->Health>0&&!P->Vehicle);
        if(ViewPickaxe)ViewPickaxe->SetVisibility(Mode==EPFConstructionMode::Pickaxe&&P->Health>0&&!P->Vehicle&&P->bFirstPerson);
    }
}
void UPFConstructionComponent::ServerMode_Implementation(EPFConstructionMode Value)
{
    if(uint8(Value)<3&&Player()->CanFight()&&!Player()->Vehicle){Mode=Value;if(Mode!=EPFConstructionMode::Build)ServerCancelEdit();OnRep_Mode();Player()->bAiming=false;Player()->StopFire();}
}
void UPFConstructionComponent::Combat()
{
    CancelEdit();bHeld=false;Mode=EPFConstructionMode::Combat;ServerMode(Mode);OnRep_Mode();if(Ghost)Ghost->SetActorHiddenInGame(true);
}
void UPFConstructionComponent::ToggleBuild()
{
    if(!Player()->AcceptsInput()||!Player()->CanFight()||Player()->Vehicle)return;
    if(Mode==EPFConstructionMode::Build){Combat();return;}
    Player()->StopFire();Player()->StopAim();Mode=EPFConstructionMode::Build;ServerMode(Mode);OnRep_Mode();UpdatePreview();
}
void UPFConstructionComponent::Pickaxe()
{
    if(!Player()->AcceptsInput()||!Player()->CanFight()||Player()->Vehicle)return;
    Combat();Mode=EPFConstructionMode::Pickaxe;ServerMode(Mode);OnRep_Mode();Hint=TEXT("PRIMARY FIRE  Harvest or strike");
}
void UPFConstructionComponent::Select(EPFBuildKind Kind)
{
    if(!Player()->AcceptsInput())return;
    if(Mode!=EPFConstructionMode::Build)ToggleBuild();Selected=Kind;CancelEdit();UpdatePreview();if(auto *PC=Cast<APlayerController>(Player()->Controller))bHeld=PC->IsInputKeyDown(EKeys::LeftMouseButton);if(bHeld)PlaceLocal();
}
void UPFConstructionComponent::Wall(){Select(EPFBuildKind::Wall);}void UPFConstructionComponent::Floor(){Select(EPFBuildKind::Floor);}void UPFConstructionComponent::Ramp(){Select(EPFBuildKind::Ramp);}void UPFConstructionComponent::Roof(){Select(EPFBuildKind::Roof);}
bool UPFConstructionComponent::Rotate()
{
    if(Mode!=EPFConstructionMode::Build||IsEditing())return false;Rotation=(Rotation+1)%4;UpdatePreview();return true;
}
bool UPFConstructionComponent::PrimaryPressed()
{
    if(Mode==EPFConstructionMode::Combat&&!IsEditing())return false;
    if(!Player()->AcceptsInput()||!Player()->CanFight())return true;
    bHeld=true;
    if(IsEditing()){bDragging=true;Visited=0;RampFirstCell=-1;}
    else if(Mode==EPFConstructionMode::Build)PlaceLocal();else StrikeLocal();return true;
}
void UPFConstructionComponent::PrimaryReleased()
{
    bHeld=false;if(bDragging){bDragging=false;ConfirmEdit();}
}
bool UPFConstructionComponent::SecondaryPressed()
{
    if(!Player()->AcceptsInput())return true;
    if(IsEditing()){EditMask=PFBuildGrid::FullMask(EditTarget->Build.Request.Kind);RampPattern=0;return true;}
    if(Mode!=EPFConstructionMode::Build)return Mode==EPFConstructionMode::Pickaxe;
    Material=EPFBuildMaterial((uint8(Material)+1)%3);UpdatePreview();return true;
}
bool UPFConstructionComponent::SecondaryReleased(){if(IsEditing()){ConfirmEdit();return true;}return Mode!=EPFConstructionMode::Combat;}
void UPFConstructionComponent::UpdatePreview()
{
    auto *P=Player();auto *World=Manager();if(!P||!World||!P->Controller)return;
    FVector Start=P->Camera->GetComponentLocation(),Direction=P->Camera->GetForwardVector();
    FHitResult Hit;FCollisionQueryParams Query(SCENE_QUERY_STAT(PFBuildAim),false,P);if(Ghost)Query.AddIgnoredActor(Ghost);
    GetWorld()->LineTraceSingleByChannel(Hit,Start,Start+Direction*PFBuildGrid::Reach,ECC_Visibility,Query);
    auto *Surface=Cast<APFBuildPiece>(Hit.GetActor());
    if(Surface&&!Surface->bPrediction)
    {
        PreviewRequest.Origin=Surface->Build.Request.Origin;PreviewRequest.Yaw=Surface->Build.Request.Yaw;PreviewRequest.Frame=Surface->Build.Request.Frame;bReady=true;
    }
    FVector Feet=P->GetActorLocation()-FVector(0,0,P->GetCapsuleComponent()->GetScaledCapsuleHalfHeight());
    if(!bReady)
    {
        PreviewRequest.Origin=Feet;PreviewRequest.Yaw=FRotator::ClampAxis(P->GetControlRotation().Yaw);PreviewRequest.FrameNonce=Nonce;PreviewRequest.Frame=0;bReady=true;
    }
    FRotator Frame(0,PreviewRequest.Yaw,0);FVector Local=Frame.UnrotateVector(Feet-FVector(PreviewRequest.Origin));
    FIntVector FeetCell(int32(FMath::RoundToInt(Local.X/PFBuildGrid::Tile)),int32(FMath::RoundToInt(Local.Y/PFBuildGrid::Tile)),int32(FMath::FloorToInt((Local.Z+35)/PFBuildGrid::Storey)));
    bool Far=FVector2D(Local).SizeSquared()>FMath::Square(PFBuildGrid::Tile*2);
    bool HeightMismatch=FMath::Abs(Local.Z-FeetCell.Z*PFBuildGrid::Storey)>60;
    if(!Surface&&P->GetCharacterMovement()->IsMovingOnGround()&&(Far||HeightMismatch)&&!World->HasNearbyInFrame(PreviewRequest,FeetCell))
    {
        PreviewRequest.Origin=Feet;PreviewRequest.Yaw=FRotator::ClampAxis(P->GetControlRotation().Yaw);PreviewRequest.Frame=0;PreviewRequest.FrameNonce=++Nonce;Frame=FRotator(0,PreviewRequest.Yaw,0);Local=FVector::ZeroVector;Facing=0;
    }
    Facing=PFBuildGrid::Facing(P->GetControlRotation().Yaw,PreviewRequest.Yaw,Facing);
    FVector Target=Local;
    if(Selected!=EPFBuildKind::Wall)
    {
        float Level=FMath::FloorToFloat((Local.Z+35)/PFBuildGrid::Storey)*PFBuildGrid::Storey;
        float Den=Direction.Z;
        float T=FMath::Abs(Den)>.001?(FVector(PreviewRequest.Origin).Z+Level-Start.Z)/Den:-1;
        FVector Point=T>0&&T<1050?Start+Direction*T:Feet+FRotator(0,PreviewRequest.Yaw+Facing*90,0).Vector()*380;
        Target=Frame.UnrotateVector(Point-FVector(PreviewRequest.Origin));
    }
    PreviewRequest.Cell={int32(FMath::RoundToInt(Target.X/PFBuildGrid::Tile)),int32(FMath::RoundToInt(Target.Y/PFBuildGrid::Tile)),int32(FMath::FloorToInt((Local.Z+35)/PFBuildGrid::Storey))};
    PreviewRequest.Kind=Selected;PreviewRequest.Material=Material;PreviewRequest.Turn=(Facing+Rotation)%4;
    if(Selected==EPFBuildKind::Ramp&&P->GetCharacterMovement()->IsMovingOnGround())
    {
        const FVector Foot=PFBuildGrid::Transform(PreviewRequest).InverseTransformPosition(Feet);
        // A grounded player must approach the low edge, not spawn underneath
        // the 1.92 m midpoint of a ramp centered on their own feet.
        if(FMath::Abs(Foot.X)<256&&FMath::Abs(Foot.Y)<256&&(Foot.X+256)*.75f-Foot.Z>P->GetCharacterMovement()->MaxStepHeight)
        {
            static const FIntVector Ahead[]={{1,0,0},{0,1,0},{-1,0,0},{0,-1,0}};
            PreviewRequest.Cell+=Ahead[PreviewRequest.Turn];
        }
    }
    if(Selected==EPFBuildKind::Ramp&&Surface&&Surface->Build.Request.Kind==EPFBuildKind::Ramp&&PreviewRequest.Turn==Surface->Build.Request.Turn)
    {
        static const FIntVector Continue[]={{1,0,1},{0,1,1},{-1,0,1},{0,-1,1}};
        PreviewRequest.Cell=Surface->Build.Request.Cell+Continue[PreviewRequest.Turn];
    }
    World->FitTerrainFoundation(PreviewRequest);
    bCanPlace=World->CanPlace(P,PreviewRequest,Hint)&&Resources.Get(uint8(Material))>=PFBuildGrid::Cost;
    if(Resources.Get(uint8(Material))<PFBuildGrid::Cost)Hint=TEXT("Not enough material");
    if(bCanPlace)Hint=FString::Printf(TEXT("%s %d  |  R rotate  |  RMB material"),PFBuildGrid::MaterialName(Material),Resources.Get(uint8(Material)));
    if(!Ghost)
    {
        Ghost=GetWorld()->SpawnActorDeferred<APFBuildPiece>(APFBuildPiece::StaticClass(),PFBuildGrid::Transform(PreviewRequest),P,nullptr,ESpawnActorCollisionHandlingMethod::AlwaysSpawn);
        FPFBuildState Data;Data.Request=PreviewRequest;Data.Mask=PFBuildGrid::FullMask(Selected);Ghost->Initialize(Data,true,true);Ghost->FinishSpawning(PFBuildGrid::Transform(PreviewRequest));
        Ghost->SetActorEnableCollision(false);
    }
    FPFBuildState Data;Data.Request=PreviewRequest;Data.Mask=PFBuildGrid::FullMask(Selected);Ghost->Build=Data;Ghost->ApplyGeometry();Ghost->SetActorHiddenInGame(false);
    if(auto *Mat=bCanPlace?ValidPreviewMaterial.Get():InvalidPreviewMaterial.Get())if(Ghost->Geometry->GetMaterial(0)!=Mat)Ghost->Geometry->SetMaterial(0,Mat);
}
void UPFConstructionComponent::PlaceLocal()
{
    if(!bCanPlace||!Manager()||Pending.Num()>=32)return;
    FPFBuildRequest R=PreviewRequest;R.RequestId=++Sequence;LastPlace=GetWorld()->GetTimeSeconds();
    for(auto &Entry:Pending)if(Entry.Value&&PFBuildGrid::Key(Entry.Value->Build.Request)==PFBuildGrid::Key(R))return;
    auto *Prediction=GetWorld()->SpawnActorDeferred<APFBuildPiece>(APFBuildPiece::StaticClass(),PFBuildGrid::Transform(R),Player(),nullptr,ESpawnActorCollisionHandlingMethod::AlwaysSpawn);
    FPFBuildState Data;Data.Request=R;Data.Mask=PFBuildGrid::FullMask(R.Kind);if(auto *State=Player()->GetPlayerState<APFPlayerState>())Data.CreatorId=State->GetPlayerId();
    Prediction->Initialize(Data,true);Prediction->FinishSpawning(PFBuildGrid::Transform(R));Prediction->SetLifeSpan(3);Pending.Add(R.RequestId,Prediction);
    if(GetWorld()->GetNetMode()==NM_Client)Manager()->RegisterPiece(Prediction);
    ServerPlace(R);
}
void UPFConstructionComponent::ServerPlace_Implementation(FPFBuildRequest R)
{
    double Now=GetWorld()->GetTimeSeconds();APFBuildPiece *Result=nullptr;FString Error;
    PlaceTokens=FMath::Min(8.f,PlaceTokens+float(Now-PlaceTokenTime)*20);PlaceTokenTime=Now;
    bool RateAllowed=PlaceTokens>=1;if(RateAllowed)PlaceTokens-=1;
    if(Mode!=EPFConstructionMode::Build||!RateAllowed||!PFBuildGrid::Valid(R)||Resources.Get(uint8(R.Material))<PFBuildGrid::Cost)Error=TEXT("Cannot place yet");
    else
    {
        LastServerPlace=Now;
        if(Manager()&&Manager()->Place(Player(),R,Result,Error)){Resources.At(uint8(R.Material))-=PFBuildGrid::Cost;Resources.Revision++;}
        else if(Error.IsEmpty())Error=TEXT("World not ready");
    }
    ClientPlaced(R.RequestId,Result?Result->Build.Request.Frame:0,Error);
}
void UPFConstructionComponent::ClientPlaced_Implementation(uint32 RequestId,int32 Frame,const FString &Error)
{
    if(Frame&&PreviewRequest.Frame==0)
    {
        PreviewRequest.Frame=Frame;
        for(auto &Entry:Pending)if(Entry.Value&&Entry.Value->Build.Request.Frame==0)
        {if(Manager())Manager()->UnregisterPiece(Entry.Value);Entry.Value->Build.Request.Frame=Frame;if(Manager())Manager()->RegisterPiece(Entry.Value);}
    }
    if(!Error.IsEmpty()){if(auto *Prediction=Pending.Find(RequestId))if(*Prediction)(*Prediction)->Destroy();Pending.Remove(RequestId);Hint=Error;}
}
void UPFConstructionComponent::ResolvePrediction(APFBuildPiece *Piece)
{
    if(!Piece)return;auto *State=Player()->GetPlayerState<APFPlayerState>();if(!State||Piece->Build.CreatorId!=State->GetPlayerId())return;
    if(auto *Prediction=Pending.Find(Piece->Build.Request.RequestId)){if(*Prediction)(*Prediction)->Destroy();Pending.Remove(Piece->Build.Request.RequestId);}
}
void UPFConstructionComponent::Edit()
{
    if(!Player()->AcceptsInput())return;if(IsEditing()){ConfirmEdit();return;}
    FHitResult Hit;FCollisionQueryParams Q(SCENE_QUERY_STAT(PFEdit),false,Player());if(Ghost)Q.AddIgnoredActor(Ghost);
    FVector Start=Player()->Camera->GetComponentLocation();GetWorld()->LineTraceSingleByChannel(Hit,Start,Start+Player()->Camera->GetForwardVector()*PFBuildGrid::Reach,ECC_Visibility,Q);
    if(auto *Target=Cast<APFBuildPiece>(Hit.GetActor()))if(Manager()&&Manager()->CanEdit(Player(),Target)){if(Mode!=EPFConstructionMode::Build)ToggleBuild();bHeld=false;ServerBeginEdit(Target);}
}
void UPFConstructionComponent::ServerBeginEdit_Implementation(APFBuildPiece *Target)
{
    auto *State=Player()->GetPlayerState<APFPlayerState>();
    if(!Manager()||!Manager()->CanEdit(Player(),Target)||!State||(Target->Build.EditorId!=-1&&Target->Build.EditorId!=State->GetPlayerId())){ClientEditing(nullptr,0,TEXT("Structure cannot be edited"));return;}
    ServerCancelEdit();Target->FlushNetDormancy();Target->Build.EditorId=State->GetPlayerId();Target->ForceNetUpdate();ServerEditTarget=Target;LeaseUntil=GetWorld()->GetTimeSeconds()+20;ClientEditing(Target,Target->Build.Revision,TEXT(""));
}
void UPFConstructionComponent::ClientEditing_Implementation(APFBuildPiece *Target,uint32 Revision,const FString &Error)
{
    Hint=Error;EditTarget=Target;EditRevision=Revision;SetComponentTickInterval(Target?0.f:.05f);if(Target){EditMask=Target->Build.Mask;RampPattern=Target->Build.RampPattern;Visited=0;HoveredCell=-1;PaintedEdit=0;Target->Geometry->SetVisibility(false);Target->Door->SetVisibility(false);if(Ghost)Ghost->SetActorHiddenInGame(true);PaintEditGrid();}
}
void UPFConstructionComponent::ConfirmEdit()
{
    if(!EditTarget.IsValid())return;
    if(!PFBuildGrid::ValidEdit(EditTarget->Build.Request.Kind,EditMask,RampPattern)){Hint=TEXT("Invalid edit — select a connected shape");return;}
    RestoreEditVisual();ServerEdit(EditTarget.Get(),EditRevision,EditMask,RampPattern);EditTarget=nullptr;bDragging=false;
}
void UPFConstructionComponent::CancelEdit(){RestoreEditVisual();if(EditTarget.IsValid())ServerCancelEdit();EditTarget=nullptr;bDragging=false;HoveredCell=-1;}
void UPFConstructionComponent::ServerCancelEdit_Implementation()
{
    if(ServerEditTarget.IsValid()){ServerEditTarget->FlushNetDormancy();ServerEditTarget->Build.EditorId=-1;ServerEditTarget->ForceNetUpdate();}ServerEditTarget=nullptr;
}
void UPFConstructionComponent::ServerEdit_Implementation(APFBuildPiece *Target,uint32 Revision,uint16 Mask,uint8 Pattern)
{
    if(ServerEditTarget.Get()==Target&&Manager()&&Manager()->CanEdit(Player(),Target)&&Target->Build.Revision==Revision&&GetWorld()->GetTimeSeconds()<=LeaseUntil)
    {Target->SetEdit(Mask,Pattern);Manager()->CheckSupport();}
    ServerCancelEdit();
}
void UPFConstructionComponent::StrikeLocal()
{
    if(GetWorld()->GetTimeSeconds()-LastStrike<.45)return;LastStrike=GetWorld()->GetTimeSeconds();LastSwingTime=GetWorld()->GetGameState()?GetWorld()->GetGameState()->GetServerWorldTimeSeconds():LastStrike;ServerStrike(Player()->GetBaseAimRotation().Vector());
}
void UPFConstructionComponent::ServerStrike_Implementation(FVector_NetQuantizeNormal Direction)
{
    auto *P=Player();double Now=GetWorld()->GetTimeSeconds();if(Mode!=EPFConstructionMode::Pickaxe||!P->CanFight()||P->Vehicle||Now-LastServerStrike<.43||FVector::DotProduct(Direction,P->GetBaseAimRotation().Vector())<.6)return;
    LastServerStrike=Now;LastSwingTime=Now;FHitResult Hit;FCollisionQueryParams Q(SCENE_QUERY_STAT(PFPickaxe),false,P);FVector Start=P->GetPawnViewLocation();
    bool bHit=GetWorld()->SweepSingleByChannel(Hit,Start,Start+Direction*300,FQuat::Identity,ECC_Visibility,FCollisionShape::MakeSphere(22),Q);
    FHitResult AnimalHit;
    if(GetWorld()->GetSubsystem<UPFAnimalHitQueries>()->Trace(Start,Start+Direction*300,AnimalHit,22)&&(!bHit||AnimalHit.Distance<Hit.Distance)){Hit=AnimalHit;bHit=true;}
    if(bHit)
    {
        float Damage=Cast<APFBuildPiece>(Hit.GetActor())?75:Cast<APFWildlife>(Hit.GetActor())?35:20;
        int32 Yield=0;uint8 Kind=0;float Fraction=0;
        float Applied=Manager()?Manager()->DamageHarvest(Hit,50,Yield,Kind,Fraction):0;
        if(Applied>0)
        {
            Resources.At(Kind)=FMath::Min(PFBuildGrid::ResourceCap,Resources.Get(Kind)+Yield);Resources.Revision++;
            ClientStrike(nullptr,Applied,Hit.ImpactPoint,Fraction);
        }
        else if(auto *Target=Hit.GetActor())
        {
            Applied=UGameplayStatics::ApplyPointDamage(Target,Damage,Direction,Hit,P->Controller,P,UPFPickaxeDamageType::StaticClass());
            if(Applied>0)
            {
                if(Cast<APFVehicle>(Target)){Resources.Metal=FMath::Min(PFBuildGrid::ResourceCap,Resources.Metal+2);Resources.Revision++;}
                ClientStrike(Target,Applied,Hit.ImpactPoint,0);
            }
        }
    }
}
void UPFConstructionComponent::ClientStrike_Implementation(AActor *Target,float Amount,FVector_NetQuantize Impact,float Fraction)
{
    if(!Target){HarvestBarUntil=GetWorld()->GetTimeSeconds()+2;HarvestBarFraction=Fraction;HarvestBarPoint=FVector(Impact)+FVector(0,0,30);}
    if(Player()->HitConfirmationSound())UGameplayStatics::PlaySound2D(this,Player()->HitConfirmationSound(),.3f);
    Player()->HitUntil=GetWorld()->GetTimeSeconds()+.16;Player()->ConfirmedTarget=Target;Player()->TargetBarUntil=GetWorld()->GetTimeSeconds()+3;
}
void UPFConstructionComponent::TickComponent(float Delta,ELevelTick Tick,FActorComponentTickFunction *Function)
{
    Super::TickComponent(Delta,Tick,Function);auto *P=Player();if(!P)return;
    if(P->HasAuthority()&&ServerEditTarget.IsValid()&&(GetWorld()->GetTimeSeconds()>LeaseUntil||!Manager()||!Manager()->CanEdit(P,ServerEditTarget.Get())))ServerCancelEdit();
    if(!ProbeWalkDirection.IsNearlyZero()&&P->IsLocallyControlled())P->AddMovementInput(ProbeWalkDirection,1,true);
    double Time=GetWorld()->GetGameState()?GetWorld()->GetGameState()->GetServerWorldTimeSeconds():GetWorld()->GetTimeSeconds();
    float Phase=FMath::Clamp(float((Time-LastSwingTime)/.38),0.f,1.f);float Swing=FMath::Sin(Phase*PI)*75;
    if(WorldPickaxe){WorldPickaxe->SetVisibility(Mode==EPFConstructionMode::Pickaxe&&P->Health>0&&!P->Vehicle);WorldPickaxe->SetOwnerNoSee(P->bFirstPerson);WorldPickaxe->SetRelativeRotation(FRotator(40-Swing,0,-15));}
    if(ViewPickaxe){ViewPickaxe->SetVisibility(Mode==EPFConstructionMode::Pickaxe&&P->Health>0&&!P->Vehicle&&P->bFirstPerson);ViewPickaxe->SetRelativeRotation(FRotator(35-Swing,0,-15));}
    if(!P->IsLocallyControlled())return;
    for(auto It=Pending.CreateIterator();It;++It)if(!IsValid(It.Value()))It.RemoveCurrent();
    if(!P->CanFight()||P->Vehicle||!P->AcceptsInput()){CancelEdit();bHeld=false;bDragging=false;if(Ghost)Ghost->SetActorHiddenInGame(true);return;}
    if(EditTarget.IsValid())
    {
        auto *Target=EditTarget.Get();if(GetWorld()->GetTimeSeconds()>=NextEditHeartbeat){NextEditHeartbeat=GetWorld()->GetTimeSeconds()+1;ServerEditHeartbeat(Target,EditRevision);}
        HoveredCell=PFBuildGeometry::TileAtRay(Target->Build,P->Camera->GetComponentLocation(),P->Camera->GetForwardVector());
        if(bDragging&&HoveredCell>=0&&!(Visited&(1<<HoveredCell)))
        {
            Visited|=1<<HoveredCell;
            if(Target->Build.Request.Kind==EPFBuildKind::Ramp)
            {
                if(RampFirstCell<0)RampFirstCell=HoveredCell;
                int32 FirstColumn=RampFirstCell%3,Column=HoveredCell%3;
                if(FirstColumn==Column)RampPattern=Column==0?1:Column==2?2:0;
                else if(RampFirstCell/3==0&&HoveredCell/3==0&&(Visited&448))RampPattern=FirstColumn==0?5:6;
                else RampPattern=Column==0?3:4;
                EditMask=15;
            }
            else EditMask^=1<<HoveredCell;
        }
        PaintEditGrid();
        Hint=TEXT("EDIT  |  Drag cells • release to confirm • right click reset • F confirm");return;
    }
    if(Mode==EPFConstructionMode::Build){UpdatePreview();if(bHeld&&GetWorld()->GetTimeSeconds()-LastPlace>=.05)PlaceLocal();}
    else if(bHeld&&Mode==EPFConstructionMode::Pickaxe)StrikeLocal();
}
void UPFConstructionComponent::EndPlay(EEndPlayReason::Type Reason)
{
    if(GetOwner()->HasAuthority())ServerCancelEdit();if(Ghost)Ghost->Destroy();for(auto &Entry:Pending)if(IsValid(Entry.Value))Entry.Value->Destroy();Pending.Empty();Super::EndPlay(Reason);
}
void UPFConstructionComponent::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);DOREPLIFETIME(UPFConstructionComponent,Mode);DOREPLIFETIME(UPFConstructionComponent,LastSwingTime);DOREPLIFETIME_CONDITION(UPFConstructionComponent,Resources,COND_OwnerOnly);
}

void UPFConstructionComponent::ServerDropMaterial_Implementation(uint8 Kind,int32 Count,uint32 Revision)
{
    if(!Player()->CanFight()||Player()->Vehicle||Kind>=3||Count<=0||Count>PFBuildGrid::ResourceCap||Revision!=Resources.Revision)return;
    Count=FMath::Min(Count,Resources.Get(Kind));if(Count<=0)return;
    FVector Point=Player()->GetActorLocation()+Player()->GetActorForwardVector()*100-FVector(0,0,60);
    FActorSpawnParameters Params;Params.SpawnCollisionHandlingOverride=ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
    if(auto *Drop=GetWorld()->SpawnActor<APFWorldPickup>(Point,FRotator::ZeroRotator,Params))
    {FPFItemStack Stack;Stack.Kind=EPFItemKind(int32(EPFItemKind::Wood)+Kind);Stack.Count=Count;Drop->Initialize(Stack);Resources.At(Kind)-=Count;Resources.Revision++;}
}

void UPFConstructionComponent::RestoreEditVisual()
{
    SetComponentTickInterval(.05f);
    if(EditTarget.IsValid()){EditTarget->Geometry->SetVisibility(true);EditTarget->ApplyGeometry();}
    if(EditGrid)EditGrid->SetVisibility(false);PaintedEdit=0;
}
void UPFConstructionComponent::PaintEditGrid()
{
    if(!EditTarget.IsValid()||!EditGrid)return;auto Kind=EditTarget->Build.Request.Kind;
    uint32 Hash=uint32(EditMask)|uint32(HoveredCell+1)<<10|uint32(Kind)<<16;
    EditGrid->SetWorldTransform(EditTarget->GetActorTransform());EditGrid->SetVisibility(true);
    if(Hash==PaintedEdit)return;PaintedEdit=Hash;
    FPFBuildMesh Sections[3];int32 N=Kind==EPFBuildKind::Wall||Kind==EPFBuildKind::Ramp?3:2;
    for(int32 I=0;I<N*N;I++)
    {
        auto &Mesh=Sections[I==HoveredCell?2:(EditMask&(1<<I))?0:1];
        if(Kind==EPFBuildKind::Wall)
            Mesh.Box({264,-256+(I%3+.5f)*512/3,(I/3+.5f)*384/3},{3,512.f/3-5,384.f/3-5});
        else
        {
            float X0=-256+(I/N)*512.f/N+3,X1=X0+512.f/N-6,Y0=-256+(I%N)*512.f/N+3,Y1=Y0+512.f/N-6;
            auto Height=[Kind](float X){return Kind==EPFBuildKind::Ramp?(X+256)*.75f+5:5.f;};
            Mesh.Quad({X0,Y0,Height(X0)},{X1,Y0,Height(X1)},{X1,Y1,Height(X1)},{X0,Y1,Height(X0)});
        }
    }
    EditGrid->ClearAllMeshSections();TArray<FLinearColor> Colors;TArray<FProcMeshTangent> Tangents;
    for(int32 I=0;I<3;I++)
    {
        auto &Mesh=Sections[I];if(Mesh.Vertices.IsEmpty())continue;
        EditGrid->CreateMeshSection_LinearColor(I,Mesh.Vertices,Mesh.Triangles,Mesh.Normals,Mesh.UV,Colors,Tangents,false);
        EditGrid->SetMaterial(I,I==2&&HoverEditMaterial?HoverEditMaterial.Get():I==1&&RemovedEditMaterial?RemovedEditMaterial.Get():ValidPreviewMaterial.Get());
    }
}

void UPFConstructionComponent::Food(){UseQuick(2);}void UPFConstructionComponent::Medkit(){UseQuick(3);}
void UPFConstructionComponent::Plate(){UseQuick(4);}void UPFConstructionComponent::Service(){UseQuick(5);}
void UPFConstructionComponent::UseQuick(int32 Slot)
{
    if(!Player()->AcceptsInput())return;int32 Index=PFInventoryLayout::FindQuick(Player()->Inventory->Slots,Slot);
    if(Index!=INDEX_NONE){Combat();Player()->Inventory->ServerUseSlot(Index,Player()->Inventory->Slots[Index].Revision);}
}

void UPFConstructionComponent::ServerEditHeartbeat_Implementation(APFBuildPiece *Target,uint32 Revision)
{
    if(Mode==EPFConstructionMode::Build&&Target&&ServerEditTarget.Get()==Target&&Target->Build.Revision==Revision&&Manager()&&Manager()->CanEdit(Player(),Target))LeaseUntil=GetWorld()->GetTimeSeconds()+5;
}
