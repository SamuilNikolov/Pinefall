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
    Detail->SetCollisionEnabled(ECollisionEnabled::NoCollision);Detail->SetCastShadow(false);Detail->SetCullDistance(9000);
    Beacon=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Rarity beacon"));Beacon->SetupAttachment(Interaction);Beacon->SetCollisionEnabled(ECollisionEnabled::NoCollision);Beacon->SetCastShadow(false);Beacon->SetCullDistance(6000);
    Body->SetCullDistance(12000);
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
    static const FLinearColor Tiers[]={FLinearColor(.46,.53,.6),FLinearColor(.2,.8,.35),FLinearColor(.15,.5,1),FLinearColor(.65,.2,1),FLinearColor(1,.57,.08)};
    FLinearColor Rarity=Tiers[FMath::Min<int32>(Item.Rarity,4)];
    Detail->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Plane.Plane")));Detail->SetRelativeScale3D(FVector(.7,.7,1));Detail->SetRelativeLocation({0,0,1});Detail->SetVisibility(true);
    Beacon->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Cylinder.Cylinder")));Beacon->SetRelativeScale3D({.006,.006,.65});Beacon->SetRelativeLocation({0,0,33});
    if(!HaloMaterial)if(auto *Base=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Pinefall/Materials/M_LootHalo.M_LootHalo")))HaloMaterial=UMaterialInstanceDynamic::Create(Base,this);
    if(!BeaconMaterial)if(auto *Base=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Pinefall/Materials/M_LootBeacon.M_LootBeacon")))BeaconMaterial=UMaterialInstanceDynamic::Create(Base,this);
    if(HaloMaterial){HaloMaterial->SetVectorParameterValue(TEXT("Tint"),Rarity);Detail->SetMaterial(0,HaloMaterial);}
    if(BeaconMaterial){BeaconMaterial->SetVectorParameterValue(TEXT("Tint"),Rarity);Beacon->SetMaterial(0,BeaconMaterial);}
    FLinearColor Color(.18, .25, .23), Stripe(.65, .72, .66);
    if (Item.Kind == EPFItemKind::Weapon)
    {
        auto *Table = LoadObject<UDataTable>(nullptr, TEXT("/Game/Pinefall/Data/DT_Weapons.DT_Weapons"));
        if (auto *Row = Table ? Table->FindRow<FPFWeaponDefinition>(Item.WeaponId, TEXT("Pickup")) : nullptr)
            Body->SetStaticMesh(Row->Model);
        Body->SetRelativeScale3D(FVector(.8));
        Body->SetRelativeRotation(FRotator(0, 0, 85));
        Body->SetRelativeLocation(FVector(0, 0, 12));
        return;
    }
    if(Item.Kind>=EPFItemKind::Wood&&Item.Kind<=EPFItemKind::Metal)
    {
        const TCHAR *Names[]={TEXT("SM_WoodBundle"),TEXT("SM_StoneStack"),TEXT("SM_MetalBundle")};const TCHAR *Name=Names[int32(Item.Kind)-int32(EPFItemKind::Wood)];
        if(auto *Model=LoadObject<UStaticMesh>(nullptr,*FString::Printf(TEXT("/Game/Pinefall/Art/Construction/%s.%s"),Name,Name)))
        {Body->SetStaticMesh(Model);Body->SetRelativeScale3D(FVector(1));Body->SetRelativeLocation({0,0,2});return;}
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

void APFWorldPickup::SetFocused(bool Focused)
{
    if(HaloMaterial)HaloMaterial->SetScalarParameterValue(TEXT("Strength"),Focused?1.5f:.75f);
    if(BeaconMaterial)BeaconMaterial->SetScalarParameterValue(TEXT("Strength"),Focused?1.35f:.65f);
    if(Beacon){Beacon->SetRelativeScale3D(FVector(.006,.006,Focused?.95:.65));Beacon->SetRelativeLocation(FVector(0,0,Focused?48:33));}
}
