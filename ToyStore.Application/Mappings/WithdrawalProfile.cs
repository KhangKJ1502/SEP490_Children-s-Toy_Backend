using AutoMapper;
using ToyStore.Application.DTOs.Withdrawals;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Mappings;

/// <summary>
/// Cấu hình ánh xạ dữ liệu cho Withdrawal module.
/// </summary>
public class WithdrawalProfile : Profile
{
    public WithdrawalProfile()
    {
        CreateMap<WithdrawalRequest, WithdrawalDto>();

        CreateMap<WithdrawalRequest, AdminWithdrawalListDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Account.Email))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Account.PhoneNumber));

        CreateMap<WithdrawalRequest, AdminWithdrawalDetailDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Account.AccountName))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Account.Email))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Account.PhoneNumber))
            .ForMember(dest => dest.StatusHistory, opt => opt.MapFrom(src => src.WithdrawalStatusHistories));

        CreateMap<WithdrawalStatusHistory, WithdrawalHistoryStepDto>();
    }
}
