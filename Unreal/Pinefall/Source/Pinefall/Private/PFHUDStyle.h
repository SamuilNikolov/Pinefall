#pragma once
#include "CoreMinimal.h"
namespace PFHUDStyle
{
inline FLinearColor TeamColor(int32 PlayerId)
{
    static const FLinearColor Colors[] = {FLinearColor(.3, .65, 1), FLinearColor(.3, .88, .7),
                                          FLinearColor(.95, .53, .76), FLinearColor(1, .77, .34)};
    return Colors[uint32(PlayerId) % 4];
}
} // namespace PFHUDStyle
