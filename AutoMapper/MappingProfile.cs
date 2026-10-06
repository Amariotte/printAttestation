using AutoMapper;
using print_attestation.Dtos.Response;
using print_attestation.Dtos.Response.auth;
using print_attestation.Model;
using print_attestation.Tools;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class MappingProfile : Profile
{
    public MappingProfile()
    {


        CreateMap<t_site, SiteResponseDto>()
         .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
         .ForMember(d => d.typeLibelle, o => o.MapFrom(s => Tools.EquivalenceTypeSite(s.r_type)))

         .ForMember(d => d.type, o => o.MapFrom(s => s.r_type))
         .ForMember(d => d.nom, o => o.MapFrom(s => s.r_nom))
         .ForMember(d => d.code, o => o.MapFrom(s => s.r_code));

        CreateMap<t_motif_annulation, MotifAnnulationResponseDto>()
            .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
            .ForMember(d => d.libelle, o => o.MapFrom(s => s.r_libelle));


        CreateMap<t_demande_annulation_fichier, demandeAnnulationFichierResponseDto>()
         .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
         .ForMember(d => d.nomFichierSave, o => o.MapFrom(s => s.r_nom_fichier_save))
         .ForMember(d => d.nomFichier, o => o.MapFrom(s => s.r_nom_fichier))
         .ForMember(d => d.typeId, o => o.MapFrom(s => s.r_type))
         .ForMember(d => d.typeLibelle, o => o.MapFrom(s => s.r_type.ToString()));


    CreateMap<t_job_details, jobDetailReponseDto>()
            .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
            .ForMember(d => d.success, o => o.MapFrom(s => s.r_success))
            .ForMember(d => d.numAttestation, o => o.MapFrom(s => s.r_attestation))
            .ForMember(d => d.raisonEchec, o => o.MapFrom(s => s.r_desc_error));




        CreateMap<t_job, jobReponseDto>()
            .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
            .ForMember(d => d.jobId, o => o.MapFrom(s => s.r_job_id))
            .ForMember(d => d.userId, o => o.MapFrom(s => s.r_user_id_fk))
            .ForMember(d => d.completedAt, o => o.MapFrom(s => s.r_completed_at))
            .ForMember(d => d.fileName, o => o.MapFrom(s => s.r_file_name))
            .ForMember(d => d.createdAt, o => o.MapFrom(s => s.r_created_at))
            .ForMember(d => d.type, o => o.MapFrom(s => s.r_type != null ? s.r_type.ToString() : null))
            .ForMember(d => d.nbTotal, o => o.MapFrom(s => s.r_total))
            .ForMember(d => d.nbSuccess, o => o.MapFrom(s => s.r_success))
            .ForMember(d => d.nbErrors, o => o.MapFrom(s => s.r_errors))
            .ForMember(d => d.status, o => o.MapFrom(s => s.r_status))
            .ForMember(d => d.user, o => o.MapFrom(s => s.r_user))
            .ForMember(d => d.details, o => o.MapFrom(s => s.r_job_details));



        CreateMap<t_demande_annulation, demandeAnnulationResponseDto>()
            .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
            .ForMember(d => d.userId, o => o.MapFrom(s => s.r_user_id_fk))
            .ForMember(d => d.status, o => o.MapFrom(s => s.r_status))
            .ForMember(d => d.numAttestation, o => o.MapFrom(s => s.r_num_attestation))
            .ForMember(d => d.numImmatriculation, o => o.MapFrom(s => s.r_num_immatriculation))
            .ForMember(d => d.createdAt, o => o.MapFrom(s => s.r_created_at))
            .ForMember(d => d.dateTraitement, o => o.MapFrom(s => s.r_date_traitement))
            .ForMember(d => d.motifId, o => o.MapFrom(s => s.r_motif_annulation_id_fk))
            .ForMember(d => d.motif, o => o.MapFrom(s => s.r_motif_annulation))
            .ForMember(d => d.numPolice, o => o.MapFrom(s => s.r_num_police))
            .ForMember(d => d.motifRejet, o => o.MapFrom(s => s.r_motif_rejet))
            .ForMember(d => d.fichiers, o => o.MapFrom(s => s.r_fichiers))
            .ForMember(d => d.user, o => o.MapFrom(s => s.r_user))
            .ForMember(d => d.site, o => o.MapFrom(s => s.r_site));



        CreateMap<t_user, UserResponseDto>()
            .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
            .ForMember(d => d.nom, o => o.MapFrom(s => s.r_nom))
            .ForMember(d => d.prenom, o => o.MapFrom(s => s.r_prenom))
            .ForMember(d => d.email, o => o.MapFrom(s => s.r_email))
            .ForMember(d => d.telephone, o => o.MapFrom(s => s.r_telephone))
            .ForMember(d => d.password_change_required, o => o.MapFrom(s => s.r_password_change_required))
            .ForMember(d => d.siteId, o => o.MapFrom(s => s.r_site_id_fk))
            .ForMember(d => d.actif, o => o.MapFrom(s => s.r_statut == STATUT_USER.ACTIVE))
            .ForMember(d => d.site, o => o.MapFrom(s => s.r_site))
            .ForMember(d => d.roleId, o => o.MapFrom(s => (int?)s.r_type))
            .ForMember(d => d.role, o => o.MapFrom(s => s.r_type != null ? Tools.EquivalenceTypeUtilisateur(s.r_type)
            : null));


        CreateMap<t_trace_connexion, logAccesDto>()
            .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
            .ForMember(d => d.userId, o => o.MapFrom(s => s.r_user_id))
            .ForMember(d => d.date, o => o.MapFrom(s => s.r_created_at))
            .ForMember(d => d.userEmail, o => o.MapFrom(s => s.r_email))
            .ForMember(d => d.typeEvenement, o => o.MapFrom(s => s.r_type_evenement))
            .ForMember(d => d.detailJson, o => o.MapFrom(s => s.r_details_json))
            .ForMember(d => d.ip, o => o.MapFrom(s => s.r_ip_address))
            .ForMember(d => d.userAgent, o => o.MapFrom(s => s.r_user_agent))
            .ForMember(d => d.success, o => o.MapFrom(s => s.r_succes))
            .ForMember(d => d.raisonEchec, o => o.MapFrom(s => s.r_raison_echec))
            .ForMember(d => d.user, o => o.MapFrom(s => s.r_user));

        CreateMap<t_trace_action, logDto>()
            .ForMember(d => d.id, o => o.MapFrom(s => s.r_id))
            .ForMember(d => d.userId, o => o.MapFrom(s => s.r_user_id))
            .ForMember(d => d.description, o => o.MapFrom(s => s.r_description))
            .ForMember(d => d.date, o => o.MapFrom(s => s.r_created_at))
            .ForMember(d => d.userEmail, o => o.MapFrom(s => s.r_user_email))
            .ForMember(d => d.typeAction, o => o.MapFrom(s => s.r_type_action))
            .ForMember(d => d.detailJson, o => o.MapFrom(s => s.r_details_json))
            .ForMember(d => d.ip, o => o.MapFrom(s => s.r_ip_address))
            .ForMember(d => d.userAgent, o => o.MapFrom(s => s.r_user_agent))
            .ForMember(d => d.httpMethod, o => o.MapFrom(s => s.r_http_method))
            .ForMember(d => d.endpoint, o => o.MapFrom(s => s.r_endpoint))
            .ForMember(d => d.statusCode, o => o.MapFrom(s => s.r_status_code))
            .ForMember(d => d.durationMs, o => o.MapFrom(s => s.r_duration_ms))
            .ForMember(d => d.user, o => o.MapFrom(s => s.r_user));
    }
}
