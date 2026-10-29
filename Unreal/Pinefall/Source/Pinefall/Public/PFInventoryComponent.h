#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "PFInventoryComponent.generated.h"

UENUM(BlueprintType)
enum class EPFItemKind : uint8
{
    Weapon,
    Ammo,
    Food,
    Medkit,
    ArmorPlate,
    LightVest,
    FieldVest,
    HeavyVest,
    ServiceKit
};
USTRUCT(BlueprintType)
struct FPFItemStack
{
    GENERATED_BODY()
    UPROPERTY(EditAnywhere, BlueprintReadOnly) EPFItemKind Kind = EPFItemKind::Food;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) FName WeaponId = NAME_None;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 Count = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) int32 Magazine = 0;
    UPROPERTY(EditAnywhere, BlueprintReadOnly) uint8 Rarity = 0;
    UPROPERTY() uint32 Revision = 0;
    FString Label() const;
    int32 Capacity() const;
};
UCLASS(ClassGroup = (Pinefall), meta = (BlueprintSpawnableComponent))
class PINEFALL_API UPFInventoryComponent : public UActorComponent
{
    GENERATED_BODY()
  public:
    UPFInventoryComponent();
    UPROPERTY(Replicated, BlueprintReadOnly) TArray<FPFItemStack> Slots;
    void InitializeLoadout(bool Range, int32 Kit);
    int32 Add(const FPFItemStack &Stack);
    bool HasWeapon(FName Id) const;
    int32 Magazine(FName Id) const;
    void StoreMagazine(FName Id, int32 Amount);
    int32 Reserve(FName Id) const;
    int32 TakeAmmo(FName Id, int32 Wanted);
    void DropEverything();
    int32 CountKind(EPFItemKind Kind) const;
    bool ConsumeKind(EPFItemKind Kind);
    UFUNCTION(Server, Reliable) void ServerUseSlot(int32 Index, uint32 Revision);
    UFUNCTION(Server, Reliable) void ServerDropSlot(int32 Index, int32 Count, uint32 Revision);
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const override;

  private:
    bool CanTransact() const;
    uint32 NextRevision = 1;
};
