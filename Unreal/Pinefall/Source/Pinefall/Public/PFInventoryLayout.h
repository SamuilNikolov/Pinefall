#pragma once
#include "CoreMinimal.h"
#include "PFInventoryComponent.h"
namespace PFInventoryLayout
{
    constexpr int32 QuickCount=6;
    PINEFALL_API EPFItemKind Kind(int32 QuickSlot);
    PINEFALL_API int32 FindQuick(const TArray<FPFItemStack> &Slots,int32 QuickSlot);
    PINEFALL_API const FString &Key(int32 QuickSlot);
    PINEFALL_API const FString &Caption(int32 QuickSlot);
    PINEFALL_API void DisplayOrder(const TArray<FPFItemStack> &Slots,TArray<int32,TInlineAllocator<12>> &Out);
}
