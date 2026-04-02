using ask.Dtos.Reponses;
using ask.Model;
using AutoMapper;

namespace ask.AutoMapper
{
    public class ConfigurationAutoMapper :Profile
    {
        public ConfigurationAutoMapper()
        {
          
         
            CreateMap<t_fonction, FonctionDto>();
            CreateMap<FonctionDto, t_fonction>();

            CreateMap< t_direction, DirectionDto>();
            CreateMap<DirectionDto, t_direction>();


        }
    }
}
