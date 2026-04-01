using ask.Dtos.RequestToSendDto;
using AutoMapper;
using InteroperabiliteProject.DtoAppBusiness;
using InteroperabiliteProject.DtoAppMobile.Alias;
using InteroperabiliteProject.Dtos;
using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.AutoMapper
{
    public class ConfigurationAutoMapper :Profile
    {
        public ConfigurationAutoMapper()
        {
            //*****************************DTO gestion BCEAO********************************
            CreateMap<RequeteDemandeDeRechercheAlias, RequeteDemandeDeRechercheAliasClient>();
            CreateMap<RequeteDemandeDeRechercheAliasClient, RequeteDemandeDeRechercheAlias>();
            //*****************************DTO gestion BCEAO********************************

         
            CreateMap<t_categorie, ContactDto>();
            CreateMap<ContactDto, t_categorie>();

            CreateMap< t_client, ClientDto_AIF>();
            CreateMap<ClientDto_AIF, t_client>();

            CreateMap< CompteDto_AIF, t_compte>();
            CreateMap<t_compte, CompteDto_AIF>();

            CreateMap<t_creation_alias, CreationAlias>();
            CreateMap<CreationAlias, t_creation_alias>();


            CreateMap<t_transfert, TransfertDto>();
            CreateMap<TransfertDto, t_transfert > ();


            CreateMap<t_transfert, DemandeDePaiementDTO>();
            CreateMap<DemandeDePaiementDTO, t_transfert>();


            CreateMap<CreerWebHookDto, t_webhook>();
            CreateMap<t_webhook, CreerWebHookDto> ();


            CreateMap<DtoRetCreationWebhooks, t_webhook>();
            CreateMap<t_webhook, DtoRetCreationWebhooks> ();


            
            //*****************************DTO gestion Controlleurs********************************

        }
    }
}
