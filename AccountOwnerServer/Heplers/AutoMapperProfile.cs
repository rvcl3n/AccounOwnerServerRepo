using Entities.Dtos;
using Entities.Models;
using AutoMapper;

namespace AccountOwnerServer.Heplers
{
    public class AutoMapperProfile: Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Owner, OwnerDto>();
            CreateMap<OwnerDto, Owner>();
        }
    }
}
