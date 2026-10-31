#include "PFInventoryLayout.h"
EPFItemKind PFInventoryLayout::Kind(int32 QuickSlot)
{
    static const EPFItemKind Kinds[]={EPFItemKind::Weapon,EPFItemKind::Weapon,EPFItemKind::Food,EPFItemKind::Medkit,EPFItemKind::ArmorPlate,EPFItemKind::ServiceKit};
    return Kinds[FMath::Clamp(QuickSlot,0,QuickCount-1)];
}
int32 PFInventoryLayout::FindQuick(const TArray<FPFItemStack> &Slots,int32 QuickSlot)
{
    if(QuickSlot<0||QuickSlot>=QuickCount)return INDEX_NONE;
    static const FName Weapons[]={TEXT("Ridgeline"),TEXT("ServiceNine")};
    for(int32 I=0;I<Slots.Num();I++)if(Slots[I].Count>0&&Slots[I].Kind==Kind(QuickSlot)&&(QuickSlot>=2||Slots[I].WeaponId==Weapons[QuickSlot]))return I;
    return INDEX_NONE;
}
const FString &PFInventoryLayout::Key(int32 QuickSlot)
{
    static const FString Keys[]={TEXT("2"),TEXT("3"),TEXT("4"),TEXT("5"),TEXT("6"),TEXT("7")};return Keys[FMath::Clamp(QuickSlot,0,QuickCount-1)];
}
const FString &PFInventoryLayout::Caption(int32 QuickSlot)
{
    static const FString Names[]={TEXT("SNIPER"),TEXT("PISTOL"),TEXT("FOOD"),TEXT("MEDKIT"),TEXT("PLATE"),TEXT("REPAIR")};return Names[FMath::Clamp(QuickSlot,0,QuickCount-1)];
}
void PFInventoryLayout::DisplayOrder(const TArray<FPFItemStack> &Slots,TArray<int32,TInlineAllocator<12>> &Out)
{
    Out.Reset();for(int32 I=0;I<Slots.Num();I++)Out.Add(I);
    auto Rank=[](const FPFItemStack &Item)
    {
        if(Item.Count<=0)return 100;
        if(Item.Kind==EPFItemKind::Weapon)return Item.WeaponId==TEXT("Ridgeline")?0:1;
        if(Item.Kind==EPFItemKind::Food)return 2;if(Item.Kind==EPFItemKind::Medkit)return 3;
        if(Item.Kind==EPFItemKind::ArmorPlate)return 4;if(Item.Kind==EPFItemKind::ServiceKit)return 5;
        if(Item.Kind==EPFItemKind::Ammo)return Item.WeaponId==TEXT("Ridgeline")?6:7;
        return 8+int32(EPFItemKind::HeavyVest)-int32(Item.Kind);
    };
    Out.StableSort([&](int32 A,int32 B){return Rank(Slots[A])<Rank(Slots[B]);});
}
