#include "PFConstructionWorld.h"
#include "PFConstructionSubsystem.h"
#include "PFBuildPiece.h"
#include "PFCharacter.h"
#include "PFGameMode.h"
#include "PFMatchState.h"
#include "Components/InstancedStaticMeshComponent.h"
#include "TimerManager.h"
#include "Engine/StaticMesh.h"
#include "EngineUtils.h"
APFConstructionWorld::APFConstructionWorld()
{
    bReplicates = true;
    bAlwaysRelevant = true;
    SetNetUpdateFrequency(5);
    PrimaryActorTick.bCanEverTick = false;
}
void APFConstructionWorld::BeginPlay()
{
    Super::BeginPlay();
    GetWorld()->GetSubsystem<UPFConstructionSubsystem>()->Manager = this;
    IndexHarvestables();
    for (TActorIterator<APFBuildPiece> It(GetWorld()); It; ++It)
        RegisterPiece(*It);
}
void APFConstructionWorld::EndPlay(EEndPlayReason::Type Reason)
{
    if (auto *Registry = GetWorld()->GetSubsystem<UPFConstructionSubsystem>();
        Registry && Registry->Manager == this)
        Registry->Manager = nullptr;
    Super::EndPlay(Reason);
}
void APFConstructionWorld::RegisterPiece(APFBuildPiece *Piece)
{
    if (!Piece || (Piece->bPrediction && GetNetMode() != NM_Client))
        return;
    if (auto *Previous = PieceAnchors.Find(Piece))
        for (const auto &Key : *Previous)
            if (auto *Owners = AnchorOwners.Find(Key))
            {
                Owners->Remove(Piece);
                if (Owners->IsEmpty())
                    AnchorOwners.Remove(Key);
            }
    Pieces.Add(PFBuildGrid::Key(Piece->Build.Request), Piece);
    PFBuildGrid::FAnchors Points;
    PFBuildGrid::Anchors(Piece->Build, Points);
    auto &Keys = PieceAnchors.FindOrAdd(Piece);
    Keys.Reset();
    for (const auto &Point : Points)
    {
        FPFBuildKey Key;
        Key.Frame = Piece->Build.Request.Frame;
        Key.X = Point.X;
        Key.Y = Point.Y;
        Key.Z = Point.Z;
        Keys.Add(Key);
        AnchorOwners.FindOrAdd(Key).AddUnique(Piece);
    }
}
void APFConstructionWorld::UnregisterPiece(APFBuildPiece *Piece)
{
    if (auto *Previous = PieceAnchors.Find(Piece))
        for (const auto &Anchor : *Previous)
            if (auto *Owners = AnchorOwners.Find(Anchor))
            {
                Owners->Remove(Piece);
                if (Owners->IsEmpty())
                    AnchorOwners.Remove(Anchor);
            }
    PieceAnchors.Remove(Piece);
    auto Key = PFBuildGrid::Key(Piece->Build.Request);
    if (auto *Current = Pieces.Find(Key); Current && Current->Get() == Piece)
        Pieces.Remove(Key);
    if (HasAuthority() && !bResetting && !GetWorldTimerManager().IsTimerActive(SupportTimer))
        GetWorldTimerManager().SetTimer(SupportTimer, this, &APFConstructionWorld::CheckSupport, .08,
                                        false);
}
APFBuildPiece *APFConstructionWorld::Find(const FPFBuildRequest &Request) const
{
    if (auto *Piece = Pieces.Find(PFBuildGrid::Key(Request)))
        return Piece->Get();
    return nullptr;
}
void APFConstructionWorld::Neighbors(const FPFBuildState &State,
                                     TArray<APFBuildPiece *, TInlineAllocator<24>> &Out) const
{
    PFBuildGrid::FAnchors Points;
    PFBuildGrid::Anchors(State, Points);
    for (const auto &Point : Points)
    {
        FPFBuildKey Key;
        Key.Frame = State.Request.Frame;
        Key.X = Point.X;
        Key.Y = Point.Y;
        Key.Z = Point.Z;
        if (auto *Owners = AnchorOwners.Find(Key))
            for (const auto &Weak : *Owners)
                if (auto *Piece = Weak.Get(); Piece && !Piece->bCollapsing)
                    Out.AddUnique(Piece);
    }
}
bool APFConstructionWorld::IsGrounded(const FPFBuildRequest &R) const
{
    const FTransform Transform = PFBuildGrid::Transform(R);
    const float Half = PFBuildGrid::Tile * .5f;
    // Sample the actual footprint and full vertical wall contact range. A
    // foundation may intersect a slope; it need not hover above one flat point.
    const int32 Rows = R.Kind == EPFBuildKind::Wall ? 1 : 3;
    for (int32 Row = 0; Row < Rows; Row++)
        for (int32 Column = 0; Column < 3; Column++)
        {
            float X = -Half + Row * Half, Y = -Half + Column * Half;
            float Surface = R.Kind == EPFBuildKind::Ramp ? (X + Half) * .75f : 0;
            if (R.Kind == EPFBuildKind::Roof)
                Surface = PFBuildGrid::Storey * (1 - FMath::Max(FMath::Abs(X), FMath::Abs(Y)) / Half);
            FVector P = Transform.TransformPosition(
                R.Kind == EPFBuildKind::Wall ? FVector(Half, Y, 0) : FVector(X, Y, Surface));
            float Up = R.Kind == EPFBuildKind::Wall ? PFBuildGrid::Storey : 64;
            FCollisionQueryParams Query(SCENE_QUERY_STAT(PFBuildFoundation), false);
            for (int32 Attempt = 0; Attempt < 4; Attempt++)
            {
                FHitResult Hit;
                if (!GetWorld()->LineTraceSingleByObjectType(
                        Hit, P + FVector(0, 0, Up), P - FVector(0, 0, 120),
                        FCollisionObjectQueryParams(ECC_WorldStatic), Query))
                    break;
                if (auto *Piece = Cast<APFBuildPiece>(Hit.GetActor()))
                {
                    Query.AddIgnoredActor(Piece);
                    continue;
                }
                auto *Instances = Cast<UInstancedStaticMeshComponent>(Hit.GetComponent());
                if (Instances && Instances->GetStaticMesh() &&
                    Instances->GetStaticMesh()->GetName().Contains(TEXT("TrunkCollision")))
                {
                    Query.AddIgnoredComponent(Instances);
                    continue;
                }
                if (Hit.GetActor() && Hit.ImpactNormal.Z > (R.Kind == EPFBuildKind::Wall ? .15f : .3f))
                    return true;
                break;
            }
        }
    return false;
}
bool APFConstructionWorld::FitTerrainFoundation(FPFBuildRequest &Request) const
{
    // Adjust only a new, unsupported local grid. Existing clusters must keep
    // exactly the same anchors on every peer and cannot be moved by aiming.
    if (Request.Frame != 0 || !PFBuildGrid::Valid(Request) || HasSupport(Request))
        return false;
    const FTransform Transform = PFBuildGrid::Transform(Request);
    const float Half = PFBuildGrid::Tile * .5f;
    float BestDelta = PFBuildGrid::Storey + 1;
    const int32 Rows = Request.Kind == EPFBuildKind::Wall ? 1 : 3;
    for (int32 Row = 0; Row < Rows; Row++)
        for (int32 Column = 0; Column < 3; Column++)
        {
            float X = -Half + Row * Half, Y = -Half + Column * Half;
            float Surface = Request.Kind == EPFBuildKind::Ramp ? (X + Half) * .75f : 0;
            if (Request.Kind == EPFBuildKind::Roof)
                Surface = PFBuildGrid::Storey * (1 - FMath::Max(FMath::Abs(X), FMath::Abs(Y)) / Half);
            const FVector Point = Transform.TransformPosition(
                Request.Kind == EPFBuildKind::Wall ? FVector(Half, Y, 0) : FVector(X, Y, Surface));
            FCollisionQueryParams Query(SCENE_QUERY_STAT(PFBuildTerrainFit), false);
            for (int32 Attempt = 0; Attempt < 4; Attempt++)
            {
                FHitResult Hit;
                if (!GetWorld()->LineTraceSingleByObjectType(
                        Hit, Point + FVector(0, 0, PFBuildGrid::Storey),
                        Point - FVector(0, 0, PFBuildGrid::Storey),
                        FCollisionObjectQueryParams(ECC_WorldStatic), Query))
                    break;
                if (auto *Piece = Cast<APFBuildPiece>(Hit.GetActor()))
                {
                    Query.AddIgnoredActor(Piece);
                    continue;
                }
                auto *Instances = Cast<UInstancedStaticMeshComponent>(Hit.GetComponent());
                if (Instances && Instances->GetStaticMesh() &&
                    Instances->GetStaticMesh()->GetName().Contains(TEXT("TrunkCollision")))
                {
                    Query.AddIgnoredComponent(Instances);
                    continue;
                }
                const float Delta = Hit.ImpactPoint.Z - Point.Z + 2;
                if (Hit.GetActor() &&
                    Hit.ImpactNormal.Z > (Request.Kind == EPFBuildKind::Wall ? .15f : .3f) &&
                    FMath::Abs(Delta) < FMath::Abs(BestDelta))
                    BestDelta = Delta;
                break;
            }
        }
    if (FMath::Abs(BestDelta) > PFBuildGrid::Storey)
        return false;
    FPFBuildRequest Fitted = Request;
    Fitted.Origin = FVector(Request.Origin) + FVector(0, 0, BestDelta);
    if (!IsGrounded(Fitted))
        return false;
    Request = Fitted;
    return true;
}
bool APFConstructionWorld::HasSupport(const FPFBuildRequest &R) const
{
    FPFBuildState State;
    State.Request = R;
    State.Mask = PFBuildGrid::FullMask(R.Kind);
    TArray<APFBuildPiece *, TInlineAllocator<24>> Nearby;
    Neighbors(State, Nearby);
    return !Nearby.IsEmpty() || IsGrounded(R);
}
bool APFConstructionWorld::CanPlace(APFCharacter *User, const FPFBuildRequest &R, FString &Error) const
{
    if (!User || !User->CanFight() || User->Vehicle || !PFBuildGrid::Valid(R))
    {
        Error = TEXT("Cannot build now");
        return false;
    }
    if (Pieces.Num() >= PFBuildGrid::PieceCap)
    {
        Error = TEXT("Structure limit reached");
        return false;
    }
    if (FVector::DistSquared(PFBuildGrid::Transform(R).GetLocation(), User->GetActorLocation()) >
        FMath::Square(PFBuildGrid::Reach))
    {
        Error = TEXT("Out of reach");
        return false;
    }
    if (Find(R))
    {
        Error = TEXT("Already built");
        return false;
    }
    if (!HasSupport(R))
    {
        Error = TEXT("Needs ground or a connected structure");
        return false;
    }
    return true;
}
bool APFConstructionWorld::Place(APFCharacter *User, FPFBuildRequest R, APFBuildPiece *&Result,
                                 FString &Error)
{
    if (!HasAuthority() || !PFBuildGrid::Valid(R))
    {
        Error = TEXT("Invalid build");
        return false;
    }
    auto *State = User ? User->GetPlayerState<APFPlayerState>() : nullptr;
    if (!State)
    {
        Error = TEXT("No player");
        return false;
    }
    if (R.Frame == 0)
    {
        uint64 Key = (uint64(uint32(State->GetPlayerId())) << 32) | R.FrameNonce;
        if (auto *Id = AssignedFrames.Find(Key))
            R.Frame = *Id;
        else
        {
            // Clients choose the grid angle; the server verifies its origin is local terrain.
            if (FVector::DistSquared(FVector(R.Origin), User->GetActorLocation()) >
                FMath::Square(PFBuildGrid::Reach))
            {
                Error = TEXT("Grid out of reach");
                return false;
            }
            if (Frames.Num() >= PFBuildGrid::PieceCap)
            {
                Error = TEXT("Grid limit reached");
                return false;
            }
            R.Frame = NextFrame++;
            AssignedFrames.Add(Key, R.Frame);
            Frames.Add(R.Frame, R);
        }
    }
    auto *Frame = Frames.Find(R.Frame);
    if (!Frame || FVector::DistSquared(FVector(Frame->Origin), FVector(R.Origin)) > 4 ||
        FMath::Abs(FRotator::NormalizeAxis(Frame->Yaw - R.Yaw)) > .1)
    {
        Error = TEXT("Grid changed");
        return false;
    }
    if (!CanPlace(User, R, Error))
        return false;
    FPFBuildState Data;
    Data.Request = R;
    Data.Mask = PFBuildGrid::FullMask(R.Kind);
    Data.CreatorId = State->GetPlayerId();
    Data.Team = State->TeamId;
    Data.StartedAt = GetWorld()->GetTimeSeconds();
    Result = GetWorld()->SpawnActorDeferred<APFBuildPiece>(
        APFBuildPiece::StaticClass(), PFBuildGrid::Transform(R), nullptr, nullptr,
        ESpawnActorCollisionHandlingMethod::AlwaysSpawn);
    if (!Result)
    {
        Error = TEXT("Could not create structure");
        return false;
    }
    Result->Initialize(Data);
    Result->FinishSpawning(PFBuildGrid::Transform(R));
    Result->bGrounded = IsGrounded(R);
    RegisterPiece(Result);
    return true;
}
bool APFConstructionWorld::CanEdit(APFCharacter *User, APFBuildPiece *Piece) const
{
    auto *State = User ? User->GetPlayerState<APFPlayerState>() : nullptr;
    if (!State || !Piece || Piece->bCollapsing || Piece->Health <= 0 || !User->CanFight() ||
        User->Vehicle ||
        FVector::DistSquared(User->GetActorLocation(), Piece->Center()) >
            FMath::Square(PFBuildGrid::Reach))
        return false;
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    return Piece->Build.CreatorId == State->GetPlayerId() ||
           (Match && Match->TeamSize > 1 && State->TeamId == Piece->Build.Team);
}
void APFConstructionWorld::CheckSupport()
{
    if (!HasAuthority() || bResetting)
        return;
    // Bounded by the session cap; only runs after a support is removed, never per actor/frame.
    TSet<APFBuildPiece *> Supported;
    TArray<APFBuildPiece *> Queue;
    Queue.Reserve(Pieces.Num());
    for (auto &Entry : Pieces)
        if (auto *Piece = Entry.Value.Get())
            if (Piece->bGrounded && !Piece->bCollapsing)
            {
                Supported.Add(Piece);
                Queue.Add(Piece);
            }
    for (int32 I = 0; I < Queue.Num(); I++)
    {
        TArray<APFBuildPiece *, TInlineAllocator<24>> Nearby;
        Neighbors(Queue[I]->Build, Nearby);
        for (auto *Next : Nearby)
            if (!Supported.Contains(Next))
            {
                Supported.Add(Next);
                Queue.Add(Next);
            }
    }
    TArray<APFBuildPiece *> Falling;
    for (auto &Entry : Pieces)
        if (auto *Piece = Entry.Value.Get())
            if (!Supported.Contains(Piece))
                Falling.Add(Piece);
    for (auto *Piece : Falling)
        if (!Piece->bCollapsing)
        {
            Piece->bCollapsing = true;
            CollapseQueue.Add(Piece);
        }
    if (!CollapseQueue.IsEmpty() && !GetWorldTimerManager().IsTimerActive(CollapseTimer))
        GetWorldTimerManager().SetTimer(CollapseTimer, this, &APFConstructionWorld::CollapseBatch, .03,
                                        true);
}
void APFConstructionWorld::CollapseBatch()
{
    // A large unsupported tower falls in a wave instead of destroying thousands
    // of collision bodies and network actors in one host frame.
    bResetting = true;
    for (int32 I = 0; I < 16 && !CollapseQueue.IsEmpty(); I++)
        if (auto Piece = CollapseQueue.Pop(EAllowShrinking::No); Piece.IsValid())
            Piece->Destroy();
    bResetting = false;
    if (CollapseQueue.IsEmpty())
        GetWorldTimerManager().ClearTimer(CollapseTimer);
}
void APFConstructionWorld::Reset()
{
    GetWorldTimerManager().ClearTimer(CollapseTimer);
    GetWorldTimerManager().ClearTimer(SupportTimer);
    CollapseQueue.Empty();
    for (auto &Change : Harvest.Items)
        if (Nodes.IsValidIndex(Change.Node))
            Change.Health = Nodes[Change.Node].Maximum;
    for (int32 I = 0; I < Harvest.Items.Num(); I++)
        ApplyHarvest(I);
    Harvest.Items.Empty();
    Harvest.MarkArrayDirty();
    HarvestDeltaLookup.Empty();
    bResetting = true;
    TArray<TWeakObjectPtr<APFBuildPiece>> All;
    Pieces.GenerateValueArray(All);
    for (auto Piece : All)
        if (Piece.IsValid())
            Piece->Destroy();
    Pieces.Empty();
    AnchorOwners.Empty();
    PieceAnchors.Empty();
    Frames.Empty();
    AssignedFrames.Empty();
    NextFrame = 1;
    bResetting = false;
}

bool APFConstructionWorld::HasNearbyInFrame(const FPFBuildRequest &Frame, FIntVector Cell) const
{
    for (int32 Z = -1; Z <= 1; Z++)
        for (int32 Y = -1; Y <= 1; Y++)
            for (int32 X = -1; X <= 1; X++)
                for (uint8 Slot = 0; Slot < 4; Slot++)
                {
                    FPFBuildKey Key;
                    Key.Frame = Frame.Frame;
                    Key.X = Cell.X + X;
                    Key.Y = Cell.Y + Y;
                    Key.Z = Cell.Z + Z;
                    Key.Slot = Slot;
                    if (auto *Piece = Pieces.Find(Key);
                        Piece && Piece->IsValid() && !Piece->Get()->bCollapsing)
                        return true;
                }
    return false;
}
