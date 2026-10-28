#include "PFInventoryComponent.h"
#include "PFCharacter.h"
#include "PFMatchState.h"
#include "PFWorldPickup.h"
#include "Engine/World.h"
#include "Net/UnrealNetwork.h"
FString FPFItemStack::Label() const
{
    switch (Kind)
    {
    case EPFItemKind::Weapon:
        return WeaponId == TEXT("Ridgeline") ? TEXT("Ridgeline rifle") : TEXT("Service Nine pistol");
    case EPFItemKind::Ammo:
        return WeaponId == TEXT("Ridgeline") ? TEXT("Rifle ammunition") : TEXT("Pistol ammunition");
    case EPFItemKind::Food:
        return TEXT("Field rations");
    case EPFItemKind::Medkit:
        return TEXT("Medkit");
    case EPFItemKind::ArmorPlate:
        return TEXT("Armor plate");
    case EPFItemKind::LightVest:
        return TEXT("Light vest");
    case EPFItemKind::FieldVest:
        return TEXT("Field vest");
    default:
        return TEXT("Heavy vest");
    }
}
int32 FPFItemStack::Capacity() const
{
    return Kind == EPFItemKind::Weapon || Kind >= EPFItemKind::LightVest ? 1
           : Kind == EPFItemKind::Ammo                                   ? 400
           : Kind == EPFItemKind::Food                                   ? 8
           : Kind == EPFItemKind::Medkit                                 ? 5
                                                                         : 6;
}
UPFInventoryComponent::UPFInventoryComponent()
{
    SetIsReplicatedByDefault(true);
    PrimaryComponentTick.bCanEverTick = false;
}
bool UPFInventoryComponent::CanTransact() const
{
    auto *Character = Cast<APFCharacter>(GetOwner());
    if (!Character || !Character->CanFight())
        return false;
    auto *Match = GetWorld()->GetGameState<APFMatchState>();
    return !Match || !Match->IsBattleRoyale() || Match->Phase == EPFRoundPhase::Active;
}
void UPFInventoryComponent::InitializeLoadout(bool Range, int32 Kit)
{
    if (!GetOwner()->HasAuthority())
        return;
    Slots.Empty();
    Slots.SetNum(12);
    FPFItemStack Stack;
    Stack.Kind = EPFItemKind::Weapon;
    Stack.Count = 1;
    Stack.WeaponId = TEXT("ServiceNine");
    Stack.Magazine = 12;
    Add(Stack);
    if (Range || Kit == 5)
    {
        Stack.WeaponId = TEXT("Ridgeline");
        Stack.Magazine = 5;
        Add(Stack);
    }
    Stack = FPFItemStack();
    Stack.Kind = EPFItemKind::Ammo;
    Stack.WeaponId = TEXT("ServiceNine");
    Stack.Count = Range ? 72 : 12;
    Add(Stack);
    if (Range || Kit == 5)
    {
        Stack.WeaponId = TEXT("Ridgeline");
        Stack.Count = Range ? 30 : 15;
        Add(Stack);
    }
    Stack = FPFItemStack();
    Stack.Kind = EPFItemKind::Food;
    Stack.Count = 1;
    Add(Stack);
    if (Range || Kit == 5)
    {
        Stack.Kind = EPFItemKind::Medkit;
        Stack.Count = 1;
        Add(Stack);
    }
}
int32 UPFInventoryComponent::Add(const FPFItemStack &Stack)
{
    if (!GetOwner()->HasAuthority() || Stack.Count <= 0 || Stack.Rarity > 4)
        return 0;
    if (Stack.Kind == EPFItemKind::Weapon &&
        (HasWeapon(Stack.WeaponId) ||
         (Stack.WeaponId != TEXT("Ridgeline") && Stack.WeaponId != TEXT("ServiceNine"))))
        return 0;
    int32 Allowed = Stack.Count;
    if (Stack.Kind == EPFItemKind::Ammo || Stack.Kind == EPFItemKind::Food ||
        Stack.Kind == EPFItemKind::Medkit || Stack.Kind == EPFItemKind::ArmorPlate)
    {
        int32 Held = 0;
        for (const auto &Slot : Slots)
            if (Slot.Kind == Stack.Kind &&
                (Stack.Kind != EPFItemKind::Ammo || Slot.WeaponId == Stack.WeaponId))
                Held += Slot.Count;
        Allowed = FMath::Min(Allowed, FMath::Max(0, Stack.Capacity() - Held));
    }
    int32 Left = Allowed;
    for (auto &Slot : Slots)
        if (Slot.Count > 0 && Slot.Kind == Stack.Kind && Slot.WeaponId == Stack.WeaponId &&
            Slot.Rarity == Stack.Rarity && Slot.Kind != EPFItemKind::Weapon)
        {
            int32 Amount = FMath::Min(Left, FMath::Max(0, Slot.Capacity() - Slot.Count));
            Slot.Count += Amount;
            Left -= Amount;
            if (!Left)
                break;
        }
    for (auto &Slot : Slots)
        if (Left > 0 && Slot.Count == 0)
        {
            Slot = Stack;
            Slot.Revision = NextRevision++;
            Slot.Count = FMath::Min(Left, Slot.Capacity());
            Left -= Slot.Count;
        }
    if (Left < Allowed)
        GetOwner()->ForceNetUpdate();
    return Allowed - Left;
}
bool UPFInventoryComponent::HasWeapon(FName Id) const
{
    for (const auto &Slot : Slots)
        if (Slot.Count > 0 && Slot.Kind == EPFItemKind::Weapon && Slot.WeaponId == Id)
            return true;
    return false;
}
int32 UPFInventoryComponent::Magazine(FName Id) const
{
    for (const auto &Slot : Slots)
        if (Slot.Count > 0 && Slot.Kind == EPFItemKind::Weapon && Slot.WeaponId == Id)
            return Slot.Magazine;
    return 0;
}
void UPFInventoryComponent::StoreMagazine(FName Id, int32 Amount)
{
    if (GetOwner()->HasAuthority())
        for (auto &Slot : Slots)
            if (Slot.Count > 0 && Slot.Kind == EPFItemKind::Weapon && Slot.WeaponId == Id)
            {
                Slot.Magazine = FMath::Max(0, Amount);
                break;
            }
}
int32 UPFInventoryComponent::Reserve(FName Id) const
{
    int32 Total = 0;
    for (const auto &Slot : Slots)
        if (Slot.Kind == EPFItemKind::Ammo && Slot.WeaponId == Id)
            Total += Slot.Count;
    return Total;
}
int32 UPFInventoryComponent::TakeAmmo(FName Id, int32 Wanted)
{
    if (!GetOwner()->HasAuthority())
        return 0;
    int32 Left = FMath::Max(0, Wanted);
    for (auto &Slot : Slots)
        if (Slot.Kind == EPFItemKind::Ammo && Slot.WeaponId == Id)
        {
            int32 Count = FMath::Min(Left, Slot.Count);
            Slot.Count -= Count;
            Left -= Count;
        }
    return Wanted - Left;
}
void UPFInventoryComponent::ServerUseSlot_Implementation(int32 Index, uint32 Revision)
{
    if (!CanTransact() || !Slots.IsValidIndex(Index) || Slots[Index].Count <= 0 ||
        Slots[Index].Revision != Revision)
        return;
    auto &Item = Slots[Index];
    auto *Character = CastChecked<APFCharacter>(GetOwner());
    bool Consume = false;
    switch (Item.Kind)
    {
    case EPFItemKind::Weapon:
        Character->EquipInventoryWeapon(Item.WeaponId);
        break;
    case EPFItemKind::Food:
        if (Character->Hunger < 100 || Character->Health < 100)
        {
            Character->Hunger = FMath::Min(100.f, Character->Hunger + 35);
            Character->Health = FMath::Min(100.f, Character->Health + 5);
            Consume = true;
        }
        break;
    case EPFItemKind::Medkit:
        if (Character->Health < 100)
        {
            Character->Health = FMath::Min(100.f, Character->Health + 45);
            Consume = true;
        }
        break;
    case EPFItemKind::ArmorPlate:
        if (Character->ArmorTier > 0)
        {
            float Max = Character->ArmorTier == 1 ? 50 : Character->ArmorTier == 2 ? 75 : 100;
            if (Character->Armor < Max)
            {
                Character->Armor = FMath::Min(Max, Character->Armor + 30);
                Consume = true;
            }
        }
        break;
    case EPFItemKind::LightVest:
    case EPFItemKind::FieldVest:
    case EPFItemKind::HeavyVest:
    {
        int32 Tier = int32(Item.Kind) - int32(EPFItemKind::LightVest) + 1;
        float Max = Tier == 1 ? 50 : Tier == 2 ? 75 : 100;
        if (Tier > Character->ArmorTier || (Tier == Character->ArmorTier && Character->Armor < Max))
        {
            Character->ArmorTier = Tier;
            Character->Armor = Max;
            Consume = true;
        }
        break;
    }
    default:
        break;
    }
    if (Consume)
        Item.Count--;
    GetOwner()->ForceNetUpdate();
}
void UPFInventoryComponent::ServerDropSlot_Implementation(int32 Index, int32 Count, uint32 Revision)
{
    if (!CanTransact() || !Slots.IsValidIndex(Index) || Count <= 0 || Slots[Index].Count <= 0 ||
        Slots[Index].Revision != Revision)
        return;
    auto *Character = CastChecked<APFCharacter>(GetOwner());
    Character->StoreMagazine();
    FPFItemStack Stack = Slots[Index];
    Stack.Count = FMath::Min(Count, Stack.Count);
    FVector Position =
        Character->GetActorLocation() + Character->GetActorForwardVector() * 120 - FVector(0, 0, 60);
    FActorSpawnParameters Params;
    Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
    if (auto *Pickup = GetWorld()->SpawnActor<APFWorldPickup>(Position, FRotator::ZeroRotator, Params))
    {
        Pickup->SetOwner(Character);
        Pickup->Initialize(Stack);
        Slots[Index].Count -= Stack.Count;
        Character->EnsureEquippedWeapon();
        GetOwner()->ForceNetUpdate();
    }
}
void UPFInventoryComponent::DropEverything()
{
    if (!GetOwner()->HasAuthority())
        return;
    auto *Character = CastChecked<APFCharacter>(GetOwner());
    Character->StoreMagazine();
    int32 Index = 0;
    for (auto &Slot : Slots)
        if (Slot.Count > 0)
        {
            float Angle = Index++ * 2.39996f;
            FVector Position =
                GetOwner()->GetActorLocation() + FVector(FMath::Cos(Angle) * 70, FMath::Sin(Angle) * 70, -60);
            FActorSpawnParameters Params;
            Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
            if (auto *Pickup =
                    GetWorld()->SpawnActor<APFWorldPickup>(Position, FRotator::ZeroRotator, Params))
            {
                Pickup->Initialize(Slot);
                Slot.Count = 0;
            }
        }
}
void UPFInventoryComponent::GetLifetimeReplicatedProps(TArray<FLifetimeProperty> &OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME_CONDITION(UPFInventoryComponent, Slots, COND_OwnerOnly);
}
