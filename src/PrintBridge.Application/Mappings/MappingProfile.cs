using AutoMapper;
using PrintBridge.Application.DTOs;
using PrintBridge.Domain.Entities;

namespace PrintBridge.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
     
        CreateMap<Account, AccountDTO>();

        CreateMap<CreateUserDTO, Account>()
            .ForMember(d => d.PasswordHash, opt => opt.Ignore()) // hash in service
            .ForMember(d => d.Role, o => o.MapFrom(s => s.IsAdmin ? Domain.Enums.AccountRole.Admin : Domain.Enums.AccountRole.User));

        CreateMap<UpdateUserDTO, Account>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
   
    }
}