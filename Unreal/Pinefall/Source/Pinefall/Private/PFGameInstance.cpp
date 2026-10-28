#include "PFGameInstance.h"
#include "Kismet/GameplayStatics.h"
#include "GameFramework/PlayerController.h"
#include "SocketSubsystem.h"
#include "IPAddress.h"
#include "Interfaces/IPv4/IPv4Endpoint.h"
void UPFGameInstance::HostRange()
{
    UGameplayStatics::OpenLevel(this, TEXT("/Game/Pinefall/Maps/CedarCrossing"), true, TEXT("listen?play"));
}
bool UPFGameInstance::JoinRange(const FString &Input)
{
    FString Address = Input.TrimStartAndEnd();
    if (Address.Equals(TEXT("localhost"), ESearchCase::IgnoreCase))
        Address = TEXT("127.0.0.1");
    if (!Address.Contains(TEXT(":")))
        Address += TEXT(":7777");
    FIPv4Endpoint Endpoint;
    if (!FIPv4Endpoint::Parse(Address, Endpoint) || Endpoint.Port == 0)
    {
        ConnectionMessage = TEXT("Enter a valid IPv4 address and port, for example 192.168.1.20:7777.");
        return false;
    }
    if (auto *PC = GetFirstLocalPlayerController())
    {
        ConnectionMessage = TEXT("Connecting to ") + Address;
        PC->ClientTravel(Address, TRAVEL_Absolute);
        return true;
    }
    return false;
}
void UPFGameInstance::HostBattleRoyale(int32 TeamSize)
{
    TeamSize = TeamSize == 2 || TeamSize == 4 ? TeamSize : 1;
    UGameplayStatics::OpenLevel(this, TEXT("/Game/Pinefall/Maps/CedarCrossing"), true,
                                FString::Printf(TEXT("listen?br?play?team=%d"), TeamSize));
}
FString UPFGameInstance::LocalAddress() const
{
    if (!CachedLanAddress.IsEmpty())
        return CachedLanAddress;
    auto *Sockets = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM);
    if (!Sockets)
        return TEXT("Address unavailable");
    TArray<TSharedPtr<FInternetAddr>> Addresses;
    Sockets->GetLocalAdapterAddresses(Addresses);
    TArray<FString> Candidates;
    for (const auto &Address : Addresses)
        if (Address.IsValid())
        {
            FString Value = Address->ToString(false);
            if (!Value.StartsWith(TEXT("127.")) && !Value.Contains(TEXT(":")))
                Candidates.AddUnique(Value + TEXT(":7777"));
        }
    if (!Candidates.IsEmpty())
    {
        CachedLanAddress = FString::Join(Candidates, TEXT("  /  "));
        return CachedLanAddress;
    }
    CachedLanAddress = TEXT("127.0.0.1:7777");
    return CachedLanAddress;
}
