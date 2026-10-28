#include "PFItemButton.h"
#include "PFCharacter.h"
#include "PFInventoryComponent.h"
#include "GameFramework/PlayerController.h"
void UPFItemButton::Setup(APlayerController *Player, int32 Index, bool Drop)
{
    Controller = Player;
    Slot = Index;
    bDrop = Drop;
    OnPressed.AddDynamic(this, &UPFItemButton::Pressed);
    OnClicked.AddDynamic(this, &UPFItemButton::Clicked);
}
void UPFItemButton::Pressed()
{
    PressedRevision = Revision;
}
void UPFItemButton::Clicked()
{
    auto *PC = Controller.Get();
    auto *Player = PC ? Cast<APFCharacter>(PC->GetPawn()) : nullptr;
    if (!Player)
        return;
    if (bDrop)
    {
        int32 Count = PC->IsInputKeyDown(EKeys::LeftShift) && Player->Inventory->Slots.IsValidIndex(Slot)
                          ? Player->Inventory->Slots[Slot].Count
                          : 1;
        Player->Inventory->ServerDropSlot(Slot, Count, PressedRevision);
    }
    else
        Player->Inventory->ServerUseSlot(Slot, PressedRevision);
}
