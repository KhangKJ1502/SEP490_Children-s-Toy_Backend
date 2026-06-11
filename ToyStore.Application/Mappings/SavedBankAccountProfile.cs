using AutoMapper;
using ToyStore.Application.DTOs.BankAccounts;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

public class SavedBankAccountProfile : Profile
{
    public SavedBankAccountProfile()
    {
        CreateMap<SavedBankAccount, SavedBankAccountDto>();
        CreateMap<CreateSavedBankAccountDto, SavedBankAccount>();
    }
}
