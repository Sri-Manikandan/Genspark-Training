using AutoMapper;
using BankingAPI.Models;
using BankingAPI.Models.DTOs;

namespace BankingAPI.Mappers{
    public class BankingProfile : Profile{
        public BankingProfile(){
            CreateMap<Account, CreateAccountResponse>();
            CreateMap<Account, GetAccountResponse>():
            CreateMap<RegisterUserRequest, Customer>()
            .ForMember(dest=>dest.Status, opt=>opt.MapFrom(_ => Active))
            .ForMember(dest => dest.Username, opt=>opt.Ignore())
            .ForMember(dest => dest.User, opt=>opt.Ignore())
            .ForMember(dest => dest.Accounts, opt=>opt.Ignore());
        }
    }
}