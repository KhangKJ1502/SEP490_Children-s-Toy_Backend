using AutoMapper;
using ToyStore.Application.DTOs.Wallets;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class WalletProfile : Profile
{
    public WalletProfile()
    {
        CreateMap<Wallet, WalletDto>();
    }
}
