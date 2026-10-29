#include "PFWorldPickup.h"
#include "PFWeaponDefinition.h"
#include "Components/SphereComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Net/UnrealNetwork.h"
APFWorldPickup::APFWorldPickup()
{
    bReplicates = true;
    SetNetCullDistanceSquared(FMath::Square(20000.f));
    SetReplicateMovement(false);
    Interaction = CreateDefaultSubobject<USphereComponent>(TEXT("Interaction"));
    SetRootComponent(Interaction);
    Interaction->InitSphereRadius(38);
    Interaction->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
    Interaction->SetCollisionObjectType(ECC_WorldDynamic);
    Interaction->SetCollisionResponseToAllChannels(ECR_Ignore);
    Interaction->SetCollisionResponseToChannel(ECC_GameTraceChannel1, ECR_Block);
    Body = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Supply model"));
    Body->SetupAttachment(Interaction);
    Body->SetCollisionEnabled(ECollisionEnabled::NoCollision);
    Detail = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Supply detail"));
    Detail->SetupAttachment(Interaction);
    Detail->SetCollisionEnabled(ECollisionEnabled::NoCollision);
}
void APFWorldPickup::BeginPlay()
{
    Super::BeginPlay();
    RefreshVisual();
    if (HasAuthority())
        SetLifeSpan(900);
}
void APFWorldPickup::Initialize(const FPFItemStack &Stack)
{
    Item = Stack;
    RefreshVisual();
    ForceNetUpdate();
}
void APFWorldPickup::RefreshVisual()
{
    UStaticMesh *Cube = LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cube.Cube"));
    Body->SetStaticMesh(Cube);
    Body->SetRelativeScale3D(FVector(.27, .18, .13));
    Body->SetRelativeLocation(FVector(0, 0, 10));
    Body->SetRelativeRotation(FRotator::ZeroRotator);
    Detail->SetStaticMesh(Cube);
    Detail->SetRelativeScale3D(FVector(.07, .185, .135));
    Detail->SetRelativeLocation(FVector(0, 0, 10));
    Detail->SetVisibility(true);
    FLinearColor Color(.18, .25, .23), Stripe(.65, .72, .66);
    if (Item.Kind == EPFItemKind::Weapon)
    {
        auto *Table = LoadObject<UDataTable>(nullptr, TEXT("/Game/Pinefall/Data/DT_Weapons.DT_Weapons"));
        if (auto *Row = Table ? Table->FindRow<FPFWeaponDefinition>(Item.WeaponId, TEXT("Pickup")) : nullptr)
            Body->SetStaticMesh(Row->Model);
        Body->SetRelativeScale3D(FVector(.8));
        Body->SetRelativeRotation(FRotator(0, 0, 85));
        Body->SetRelativeLocation(FVector(0, 0, 12));
        Detail->SetVisibility(false);
        return;
    }
    if (Item.Kind == EPFItemKind::Food)
    {
        Body->SetStaticMesh(LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cylinder.Cylinder")));
        Body->SetRelativeScale3D(FVector(.12, .12, .19));
        Color = FLinearColor(.3, .39, .18);
    }
    if (Item.Kind == EPFItemKind::Medkit)
    {
        Color = FLinearColor(.62, .66, .61);
        Stripe = FLinearColor(.6, .07, .05);
    }
    if (Item.Kind == EPFItemKind::Ammo)
    {
        Color = FLinearColor(.22, .28, .17);
        Stripe = FLinearColor(.55, .4, .16);
        Body->SetRelativeScale3D(FVector(.25, .14, .2));
    }
    if (Item.Kind >= EPFItemKind::ArmorPlate)
    {
        Color = FLinearColor(.07, .19, .27);
        Stripe = FLinearColor(.16, .48, .72);
    }
    const TCHAR *ModelName = Item.Kind == EPFItemKind::Food         ? TEXT("SM_RationTins")
                             : Item.Kind == EPFItemKind::Medkit     ? TEXT("SM_MedicalBag")
                             : Item.Kind == EPFItemKind::Ammo       ? TEXT("SM_AmmoBox")
                             : Item.Kind == EPFItemKind::ServiceKit ? TEXT("SM_ServiceCase")
                                                                    : TEXT("SM_FieldVest");
    FString ModelPath = FString::Printf(TEXT("/Game/Pinefall/Art/Reserve/%s.%s"), ModelName, ModelName);
    if (auto *Model = LoadObject<UStaticMesh>(nullptr, *ModelPath))
    {
        Body->SetStaticMesh(Model);
        Body->SetRelativeScale3D(FVector(1));
        Body->SetRelativeLocation(FVector(0, 0, 2));
        // A small rarity plate anchors supplies without obscuring the actual model.
        Detail->SetStaticMesh(
            LoadObject<UStaticMesh>(nullptr, TEXT("/Engine/BasicShapes/Cylinder.Cylinder")));
        Detail->SetRelativeScale3D(FVector(.48, .48, .012));
        Detail->SetRelativeLocation(FVector(0, 0, 1));
        const FLinearColor Tiers[] = {FLinearColor(.35, .39, .4), FLinearColor(.22, .57, .3),
                                      FLinearColor(.2, .45, .85), FLinearColor(.56, .3, .8),
                                      FLinearColor(.85, .57, .16)};
        auto *Base =
            LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/Pinefall/Materials/M_Supply.M_Supply"));
        if (Base)
        {
            auto *Mark = UMaterialInstanceDynamic::Create(Base, this);
            Mark->SetVectorParameterValue(TEXT("Tint"), Tiers[FMath::Min<int32>(Item.Rarity, 4)]);
            Detail->SetMaterial(0, Mark);
        }
        return;
    }
    auto *Material =
        LoadObject<UMaterialInterface>(nullptr, TEXT("/Game/Pinefall/Materials/M_Supply.M_Supply"));
    if (Material)
    {
        auto *A = UMaterialInstanceDynamic::Create(Material, this);
        A->SetVectorParameterValue(TEXT("Tint"), Color);
        Body->SetMaterial(0, A);
        auto *B = UMaterialInstanceDynamic::Create(Material, this);
        B->SetVectorParameterValue(TEXT("Tint"), Stripe);
        Detail->SetMaterial(0, B);
    }
}
void APFWorldPickup::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(APFWorldPickup, Item);
}
