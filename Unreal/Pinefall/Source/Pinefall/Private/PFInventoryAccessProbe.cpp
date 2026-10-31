#include "PFPlayerController.h"
#include "PFCharacter.h"
#include "PFConstructionComponent.h"
#include "PFInventoryComponent.h"
#include "PFInventoryLayout.h"
#include "PFInterface.h"
#include "PFItemButton.h"
#include "GameFramework/PlayerInput.h"
#include "TimerManager.h"
#include "Misc/FileHelper.h"
#include "Misc/Paths.h"
#include "HAL/FileManager.h"
#include "Serialization/JsonSerializer.h"
void APFPlayerController::RunInventoryAccessProbe()
{
    if(!FParse::Param(FCommandLine::Get(),TEXT("PFInventoryProbe"))||!IsLocalController())return;
    auto *P=Cast<APFCharacter>(GetPawn());if(!P||!Interface||GetWorld()->GetTimeSeconds()<3||GetWorld()->GetTimeSeconds()<InventoryProbeAfter)return;
    auto Check=[&](bool Value,const TCHAR *Name){(Value?InventoryProbePassed:InventoryProbeFailed).Add(Name);UE_LOG(LogTemp,Display,TEXT("PF_INVENTORY_CHECK %s %s"),Value?TEXT("PASS"):TEXT("FAIL"),Name);};
    FGeometry Geometry;FModifierKeysState Modifiers;
    if(InventoryProbeStep==0)
    {
        Check(!PlayerInput->DebugExecBindings.ContainsByPredicate([](const FKeyBind &Binding){return Binding.Key==EKeys::F1||Binding.Key==EKeys::F2||Binding.Key==EKeys::F3||Binding.Key==EKeys::F4;}),TEXT("build keys have no development view-mode shortcuts"));
        InventoryPressed();Check(bInventoryOpen&&bShowMouseCursor,TEXT("hold Alt opens inventory with a visible cursor"));
        PlayerInput->FlushPressedKeys();InventoryProbeAfter=GetWorld()->GetTimeSeconds()+.3;InventoryProbeStep++;return;
    }
    if(InventoryProbeStep==1)
    {
        Check(bInventoryOpen,TEXT("inventory survives input-state flush during UI focus changes"));
        Interface->NativeOnKeyUp(Geometry,FKeyEvent(EKeys::LeftAlt,Modifiers,0,false,0,0));
        Check(!bInventoryOpen&&!bShowMouseCursor,TEXT("Alt release reaches the UI root and restores game input"));
        ToggleInventory();InventoryProbeAfter=GetWorld()->GetTimeSeconds()+.3;InventoryProbeStep++;return;
    }
    if(InventoryProbeStep==2)
    {
        Check(bInventoryOpen,TEXT("Tab inventory remains open without holding Alt"));
        Interface->NativeOnKeyDown(Geometry,FKeyEvent(EKeys::Tab,Modifiers,0,false,0,0));Check(!bInventoryOpen,TEXT("Tab closes inventory through focused UI"));
        ToggleInventory();Interface->NativeOnKeyDown(Geometry,FKeyEvent(EKeys::Escape,Modifiers,0,false,0,0));Check(!bInventoryOpen&&!bMenuOpen,TEXT("Escape closes inventory without opening another panel"));
        InventoryPressed();ApplicationActivation(false);Check(!bInventoryOpen&&!bShowMouseCursor,TEXT("application focus loss clears held inventory state"));
        SetMenu(true);InventoryPressed();Check(!bInventoryOpen,TEXT("inventory cannot cover the settings menu"));SetMenu(false);
        InventoryProbeStep++;return;
    }
    if(InventoryProbeStep==3)
    {
        const auto &Slots=P->Inventory->Slots;int32 Rifle=PFInventoryLayout::FindQuick(Slots,0),Pistol=PFInventoryLayout::FindQuick(Slots,1);
        Check(Rifle!=INDEX_NONE&&Pistol!=INDEX_NONE&&Slots[Rifle].WeaponId==TEXT("Ridgeline")&&Slots[Pistol].WeaponId==TEXT("ServiceNine"),TEXT("quickbar weapon positions ignore pickup order"));
        TArray<int32,TInlineAllocator<12>> Order;PFInventoryLayout::DisplayOrder(Slots,Order);
        Check(Order.Num()==12&&Order[0]==Rifle&&Order[1]==Pistol,TEXT("inventory displays rifle then pistol without rearranging authoritative slots"));
        Check(PFInventoryLayout::Key(0)==TEXT("2")&&PFInventoryLayout::Key(1)==TEXT("3")&&PFInventoryLayout::Key(5)==TEXT("7"),TEXT("hotbar shortcuts increase from left to right"));
        int32 Food=PFInventoryLayout::FindQuick(Slots,2),Medkit=PFInventoryLayout::FindQuick(Slots,3);int32 BeforeFood=P->Inventory->CountKind(EPFItemKind::Food),BeforeMed=P->Inventory->CountKind(EPFItemKind::Medkit),BeforeWood=P->Construction->Resources.Wood;
        if(Food!=INDEX_NONE&&Medkit!=INDEX_NONE)
        {
            auto *Button=NewObject<UPFItemButton>(Interface);Button->Setup(this,Food,true);Button->ShowSlot(Food,Slots[Food].Revision);Button->ProcessEvent(Button->FindFunctionChecked(TEXT("Pressed")),nullptr);
            Button->ShowSlot(Medkit,Slots[Medkit].Revision);Button->ProcessEvent(Button->FindFunctionChecked(TEXT("Clicked")),nullptr);
        }
        Check(P->Inventory->CountKind(EPFItemKind::Food)==BeforeFood-1&&P->Inventory->CountKind(EPFItemKind::Medkit)==BeforeMed,TEXT("mouse press keeps its original slot when the display reorders"));
        Check(P->Construction->Resources.Wood==BeforeWood,TEXT("dropping one backpack item leaves building materials unchanged"));
        P->Construction->ServerMode(EPFConstructionMode::Build);
        if(Rifle!=INDEX_NONE)P->Inventory->ServerUseSlot(Rifle,Slots[Rifle].Revision);
        Check(P->Construction->Mode==EPFConstructionMode::Combat&&P->WeaponId==TEXT("Ridgeline"),TEXT("inventory equip exits building and equips the selected gun"));
        FPFItemStack Stack;Stack.Kind=EPFItemKind::Weapon;Stack.WeaponId=TEXT("Ridgeline");Stack.Count=1;
        Check(P->Inventory->CapacityFor(Stack)==0,TEXT("pickup card detects an already-carried weapon"));
        Stack=FPFItemStack();Stack.Kind=EPFItemKind::Food;Stack.Count=80;
        Check(P->Inventory->CapacityFor(Stack)==8,TEXT("pickup capacity respects the ration carrying limit"));
        Stack.Count=-1;Check(P->Inventory->CapacityFor(Stack)==0,TEXT("invalid pickup quantities have zero capacity"));
        InventoryReleased();GetWorldTimerManager().ClearTimer(InventoryProbeTimer);
        TSharedRef<FJsonObject> Result=MakeShared<FJsonObject>();TArray<TSharedPtr<FJsonValue>> Passed,Failed;
        for(const auto &Name:InventoryProbePassed)Passed.Add(MakeShared<FJsonValueString>(Name));for(const auto &Name:InventoryProbeFailed)Failed.Add(MakeShared<FJsonValueString>(Name));
        Result->SetArrayField(TEXT("passed"),Passed);Result->SetArrayField(TEXT("failed"),Failed);Result->SetStringField(TEXT("scope"),TEXT("Real UI key handlers, focus-state flush, hotbar ordering and revision-checked inventory transactions; no physical keyboard automation claim"));
        FString Json,Report;FJsonSerializer::Serialize(Result,TJsonWriterFactory<>::Create(&Json));
        if(FParse::Value(FCommandLine::Get(),TEXT("PFInventoryReport="),Report)){IFileManager::Get().MakeDirectory(*FPaths::GetPath(Report),true);FFileHelper::SaveStringToFile(Json,*Report);}
        InventoryProbeStep++;
    }
}
