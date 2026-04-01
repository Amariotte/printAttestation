using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InteroperabiliteProject.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_alias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idCreationAlias = table.Column<string>(type: "text", nullable: false),
                    nomClient = table.Column<string>(type: "text", nullable: false),
                    identificationNationaleClient = table.Column<string>(type: "text", nullable: true),
                    participant = table.Column<string>(type: "text", nullable: true),
                    categorie = table.Column<string>(type: "text", nullable: true),
                    adresse = table.Column<string>(type: "text", nullable: true),
                    paysResidence = table.Column<string>(type: "text", nullable: true),
                    genre = table.Column<string>(type: "text", nullable: true),
                    nationalite = table.Column<string>(type: "text", nullable: true),
                    telephone = table.Column<string>(type: "text", nullable: true),
                    dateNaissance = table.Column<string>(type: "text", nullable: true),
                    villeNaissance = table.Column<string>(type: "text", nullable: true),
                    PaysNaissance = table.Column<string>(type: "text", nullable: true),
                    typeCompte = table.Column<string>(type: "text", nullable: true),
                    dateOuvertureCompte = table.Column<string>(type: "text", nullable: true),
                    denominationSociale = table.Column<string>(type: "text", nullable: true),
                    raisonSociale = table.Column<string>(type: "text", nullable: true),
                    identificationFiscale = table.Column<string>(type: "text", nullable: true),
                    identificationRccm = table.Column<string>(type: "text", nullable: true),
                    numeroPasseport = table.Column<string>(type: "text", nullable: true),
                    shid = table.Column<string>(type: "text", nullable: true),
                    typeAlias = table.Column<string>(type: "text", nullable: true),
                    iban = table.Column<string>(type: "text", nullable: true),
                    valeurAlias = table.Column<string>(type: "text", nullable: true),
                    other = table.Column<string>(type: "text", nullable: true),
                    ville = table.Column<string>(type: "text", nullable: true),
                    codePostale = table.Column<string>(type: "text", nullable: true),
                    categorieEntreprise = table.Column<string>(type: "text", nullable: true),
                    codeActivite = table.Column<string>(type: "text", nullable: true),
                    photo = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    nomMere = table.Column<string>(type: "text", nullable: true),
                    codeQr = table.Column<string>(type: "text", nullable: true),
                    ibanOrOther = table.Column<string>(type: "text", nullable: true),
                    dateCreationAlias = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    dateModificationAlias = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateSuppressionAlias = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateSuppressionAliasMbno = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    aliasMbnoOld = table.Column<string>(type: "text", nullable: true),
                    preConfirmation = table.Column<bool>(type: "boolean", nullable: true),
                    statut = table.Column<string>(type: "text", nullable: true),
                    r_client_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_alias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_annulation_transfert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sensFlux = table.Column<int>(type: "integer", nullable: false),
                    codeMembreParticipantPaye = table.Column<string>(type: "text", nullable: true),
                    codeMembreParticipantPayeur = table.Column<string>(type: "text", nullable: true),
                    endToEndId = table.Column<string>(type: "text", nullable: true),
                    raison = table.Column<string>(type: "text", nullable: true),
                    msgId = table.Column<string>(type: "text", nullable: true),
                    statut = table.Column<int>(type: "integer", nullable: false),
                    raisonRejetDemande = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_annulation_transfert", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom = table.Column<string>(type: "text", nullable: true),
                    icon = table.Column<string>(type: "text", nullable: true),
                    niveau = table.Column<int>(type: "integer", nullable: true),
                    r_client_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_client",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: true),
                    nom = table.Column<string>(type: "text", nullable: true),
                    photo = table.Column<string>(type: "text", nullable: true),
                    security_user_id = table.Column<string>(type: "text", nullable: true),
                    security_username = table.Column<string>(type: "text", nullable: true),
                    numerocompte_register = table.Column<string>(type: "text", nullable: true),
                    telephone = table.Column<string>(type: "text", nullable: true),
                    prenom = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_client", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_code_erreur",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: true),
                    other_code = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    description_client = table.Column<string>(type: "text", nullable: true),
                    tag = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_code_erreur", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_compte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codeAgence = table.Column<string>(type: "text", nullable: true),
                    nomAgence = table.Column<string>(type: "text", nullable: true),
                    numeroCompte = table.Column<string>(type: "text", nullable: true),
                    typeCompte = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: true),
                    cleRib = table.Column<string>(type: "text", nullable: true),
                    ibanOrOther = table.Column<string>(type: "text", nullable: true),
                    intituleCompte = table.Column<string>(type: "text", nullable: true),
                    racineCompte = table.Column<string>(type: "text", nullable: true),
                    titulaireCompte = table.Column<string>(type: "text", nullable: true),
                    codeDeviseCompte = table.Column<string>(type: "text", nullable: true),
                    deviseCompte = table.Column<string>(type: "text", nullable: true),
                    sensCompte = table.Column<string>(type: "text", nullable: true),
                    taxeCompte = table.Column<bool>(type: "boolean", nullable: true),
                    instanceFermetureCompte = table.Column<bool>(type: "boolean", nullable: true),
                    FermetureCompte = table.Column<bool>(type: "boolean", nullable: true),
                    dateOuverture = table.Column<string>(type: "text", nullable: true),
                    dateFermeture = table.Column<string>(type: "text", nullable: true),
                    dateInstanceFermetureCompte = table.Column<string>(type: "text", nullable: true),
                    r_client_id = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_compte", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_contact_client",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom = table.Column<string>(type: "text", nullable: false),
                    alias = table.Column<string>(type: "text", nullable: true),
                    confiance = table.Column<bool>(type: "boolean", nullable: true),
                    r_client_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_contact_client", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_creation_alias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idCreationAlias = table.Column<string>(type: "text", nullable: true),
                    nomClient = table.Column<string>(type: "text", nullable: true),
                    categorieClient = table.Column<string>(type: "text", nullable: true),
                    paysResidenceClient = table.Column<string>(type: "text", nullable: true),
                    telephoneClient = table.Column<string>(type: "text", nullable: true),
                    adresseClient = table.Column<string>(type: "text", nullable: true),
                    participant = table.Column<string>(type: "text", nullable: true),
                    other = table.Column<string>(type: "text", nullable: true),
                    typeCompte = table.Column<string>(type: "text", nullable: true),
                    dateOuvertureCompte = table.Column<string>(type: "text", nullable: true),
                    typeAlias = table.Column<string>(type: "text", nullable: true),
                    valeurAlias = table.Column<string>(type: "text", nullable: true),
                    nationaliteClient = table.Column<string>(type: "text", nullable: true),
                    genreClient = table.Column<string>(type: "text", nullable: true),
                    identificationRccm = table.Column<string>(type: "text", nullable: true),
                    identificationNationaleClient = table.Column<string>(type: "text", nullable: true),
                    dateNaissanceClient = table.Column<string>(type: "text", nullable: true),
                    paysNaissanceClient = table.Column<string>(type: "text", nullable: true),
                    codePostaleClient = table.Column<string>(type: "text", nullable: true),
                    villeNaissanceClient = table.Column<string>(type: "text", nullable: true),
                    iban = table.Column<string>(type: "text", nullable: true),
                    numeroPasseport = table.Column<string>(type: "text", nullable: true),
                    villeClient = table.Column<string>(type: "text", nullable: true),
                    raisonSociale = table.Column<string>(type: "text", nullable: true),
                    emailClient = table.Column<string>(type: "text", nullable: true),
                    denominationSociale = table.Column<string>(type: "text", nullable: true),
                    identificationFiscale = table.Column<string>(type: "text", nullable: true),
                    nomMere = table.Column<string>(type: "text", nullable: true),
                    categorieEntreprise = table.Column<string>(type: "text", nullable: true),
                    codeActivite = table.Column<string>(type: "text", nullable: true),
                    photoClient = table.Column<string>(type: "text", nullable: true),
                    preConfirmation = table.Column<bool>(type: "boolean", nullable: true),
                    bConfirme = table.Column<bool>(type: "boolean", nullable: true),
                    data_client_bq = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    r_client_id = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_creation_alias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_cron_tache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nom = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: true),
                    frequence = table.Column<string>(type: "text", nullable: false),
                    commande = table.Column<string>(type: "text", nullable: false),
                    statut = table.Column<int>(type: "integer", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_cron_tache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_cron_tache_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message = table.Column<string>(type: "text", nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    statut = table.Column<int>(type: "integer", nullable: false),
                    r_cron_id = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_cron_tache_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_demande",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_requete_Type = table.Column<int>(type: "integer", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    r_request = table.Column<string>(type: "text", nullable: true),
                    r_response = table.Column<string>(type: "text", nullable: true),
                    controleur = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: true),
                    statut = table.Column<int>(type: "integer", nullable: true),
                    reference = table.Column<string>(type: "text", nullable: true),
                    dateheureReponse = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateheureRequete = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_demande", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_histo_email",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sender_email = table.Column<string>(type: "text", nullable: true),
                    sender_name = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    subject = table.Column<string>(type: "text", nullable: true),
                    recipients = table.Column<string>(type: "text", nullable: true),
                    statut = table.Column<int>(type: "integer", nullable: false),
                    raison_echec = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_histo_email", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_histo_sms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sender = table.Column<string>(type: "text", nullable: true),
                    text = table.Column<string>(type: "text", nullable: true),
                    recipient = table.Column<string>(type: "text", nullable: true),
                    statut = table.Column<int>(type: "integer", nullable: false),
                    raison_echec = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_histo_sms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_message",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    msgid = table.Column<string>(type: "text", nullable: true),
                    msgiddemande = table.Column<string>(type: "text", nullable: true),
                    endToEndId = table.Column<string>(type: "text", nullable: true),
                    idrevendication = table.Column<string>(type: "text", nullable: true),
                    idcreationalias = table.Column<string>(type: "text", nullable: true),
                    alias = table.Column<string>(type: "text", nullable: true),
                    body_message = table.Column<string>(type: "text", nullable: true),
                    reponse_message = table.Column<string>(type: "text", nullable: true),
                    type_message = table.Column<int>(type: "integer", nullable: true),
                    date_reponse = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    date_message = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    sens = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_message", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_modele",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "text", nullable: true),
                    subject = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    plateforme = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_modele", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "text", nullable: true),
                    idObject = table.Column<string>(type: "text", nullable: true),
                    compte = table.Column<string>(type: "text", nullable: false),
                    dateAction = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    dateLecture = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    estCliquable = table.Column<bool>(type: "boolean", nullable: true),
                    details = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_notification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_operation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_EndtoEndCode = table.Column<string>(type: "text", nullable: false),
                    r_msgid = table.Column<string>(type: "text", nullable: false),
                    r_transactionCode = table.Column<string>(type: "text", nullable: false),
                    r_typeTransaction = table.Column<int>(type: "integer", nullable: false),
                    ContenueEnvoye = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ContenueRetour = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_operation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_operation_masse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    instructionId = table.Column<string>(type: "text", nullable: true),
                    payeurAlias = table.Column<string>(type: "text", nullable: true),
                    dateInstruction = table.Column<string>(type: "text", nullable: true),
                    r_client_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_operation_masse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_otp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codeOtp = table.Column<string>(type: "text", nullable: true),
                    challengeId = table.Column<string>(type: "text", nullable: true),
                    idOperationParent = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    dureeValidite = table.Column<int>(type: "integer", nullable: false),
                    r_client_id_fk = table.Column<int>(type: "integer", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_otp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_parametre_systeme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cle = table.Column<string>(type: "text", nullable: false),
                    valeur = table.Column<string>(type: "text", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_parametre_systeme", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_participant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codeMembreParticipant = table.Column<string>(type: "text", nullable: true),
                    statut = table.Column<string>(type: "text", nullable: true),
                    codeBanque = table.Column<string>(type: "text", nullable: true),
                    nomOfficiel = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_participant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_reference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    valeurRefBanq = table.Column<string>(type: "text", nullable: true),
                    valeurRefAIF = table.Column<string>(type: "text", nullable: true),
                    typeReference = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_reference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_register",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numerocompte = table.Column<string>(type: "text", nullable: true),
                    nom = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    telephone = table.Column<string>(type: "text", nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    motif_rejet = table.Column<string>(type: "text", nullable: true),
                    statut = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_register", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_retour_fonds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    msgId = table.Column<string>(type: "text", nullable: false),
                    endToEndId = table.Column<string>(type: "text", nullable: false),
                    dateHeureIrrevocabilite = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    montantRetourne = table.Column<string>(type: "text", nullable: false),
                    raisonRetour = table.Column<string>(type: "text", nullable: true),
                    sensFlux = table.Column<int>(type: "integer", nullable: false),
                    statut = table.Column<int>(type: "integer", nullable: false),
                    etape = table.Column<int>(type: "integer", nullable: false),
                    numEvenementReserv = table.Column<string>(type: "text", nullable: true),
                    codeOperationReserv = table.Column<string>(type: "text", nullable: true),
                    codeAgenceReserv = table.Column<string>(type: "text", nullable: true),
                    IdOperationSib = table.Column<string>(type: "text", nullable: true),
                    codeRejet = table.Column<string>(type: "text", nullable: true),
                    motifRejet = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_retour_fonds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_revendication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    alias = table.Column<string>(type: "text", nullable: true),
                    compte = table.Column<string>(type: "text", nullable: true),
                    idRevendicationPi = table.Column<string>(type: "text", nullable: true),
                    pspDetenteur = table.Column<string>(type: "text", nullable: true),
                    pspRevendicateur = table.Column<string>(type: "text", nullable: true),
                    sensFlux = table.Column<int>(type: "integer", nullable: true),
                    statut = table.Column<int>(type: "integer", nullable: true),
                    dateDemande = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateVerrouillage = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateDeVerrouillage = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateCloture = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateAction = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateCreation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateModification = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    raisonRejet = table.Column<string>(type: "text", nullable: true),
                    informationsAdditionnelles = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_revendication", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_route_scope",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_libelle = table.Column<string>(type: "text", nullable: false),
                    r_description = table.Column<string>(type: "text", nullable: false),
                    r_controller = table.Column<string>(type: "text", nullable: false),
                    r_action = table.Column<string>(type: "text", nullable: false),
                    r_route = table.Column<string>(type: "text", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_route_scope", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_scheduled",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    endToEndId = table.Column<string>(type: "text", nullable: true),
                    montant = table.Column<double>(type: "double precision", nullable: false),
                    paysClientPayeur = table.Column<string>(type: "text", nullable: true),
                    typeCompteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    deviseCompteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    ibanClientPayeur = table.Column<string>(type: "text", nullable: true),
                    aliasClientPayeur = table.Column<string>(type: "text", nullable: true),
                    compteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    photoClientPayeur = table.Column<string>(type: "text", nullable: true),
                    photoClientPaye = table.Column<string>(type: "text", nullable: true),
                    nomClientPayeur = table.Column<string>(type: "text", nullable: true),
                    typeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    codeMembreParticipantPayeur = table.Column<string>(type: "text", nullable: true),
                    paysClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeCompteClientPaye = table.Column<string>(type: "text", nullable: true),
                    deviseCompteClientPaye = table.Column<string>(type: "text", nullable: true),
                    otherClientPaye = table.Column<string>(type: "text", nullable: true),
                    nomClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeClientPaye = table.Column<string>(type: "text", nullable: true),
                    numeroIdentificationClientPayeur = table.Column<string>(type: "text", nullable: true),
                    systemeIdentificationClientPayeur = table.Column<string>(type: "text", nullable: true),
                    numeroIdentificationClientPaye = table.Column<string>(type: "text", nullable: true),
                    systemeIdentificationClientPaye = table.Column<string>(type: "text", nullable: true),
                    adresseClientPaye = table.Column<string>(type: "text", nullable: true),
                    adresseClientPayeur = table.Column<string>(type: "text", nullable: true),
                    villeClientPaye = table.Column<string>(type: "text", nullable: true),
                    villeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    aliasClientPaye = table.Column<string>(type: "text", nullable: true),
                    latitudeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    longitudeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    dateNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    villeNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    paysNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    dateNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    villeNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    paysNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    otherClientPayeur = table.Column<string>(type: "text", nullable: true),
                    numeroRCCMClientPayeur = table.Column<string>(type: "text", nullable: true),
                    codeMembreParticipantPaye = table.Column<string>(type: "text", nullable: true),
                    ibanClientPaye = table.Column<string>(type: "text", nullable: true),
                    compteClientPaye = table.Column<string>(type: "text", nullable: true),
                    motif = table.Column<string>(type: "text", nullable: true),
                    numeroRCCMClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeDocumentReference = table.Column<string>(type: "text", nullable: true),
                    numeroDocumentReference = table.Column<string>(type: "text", nullable: true),
                    montantAchat = table.Column<double>(type: "double precision", nullable: true),
                    montantRetrait = table.Column<double>(type: "double precision", nullable: true),
                    fraisRetrait = table.Column<double>(type: "double precision", nullable: true),
                    montantFrais = table.Column<double>(type: "double precision", nullable: true),
                    signatureNumeriqueMandat = table.Column<string>(type: "text", nullable: true),
                    montantRemisePaiementImmediat = table.Column<double>(type: "double precision", nullable: true),
                    autorisationModificationMontant = table.Column<bool>(type: "boolean", nullable: true),
                    tauxRemisePaiementImmediat = table.Column<double>(type: "double precision", nullable: true),
                    latitudeClientPaye = table.Column<string>(type: "text", nullable: true),
                    longitudeClientPaye = table.Column<string>(type: "text", nullable: true),
                    data_paye = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    data_payeur = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    txId = table.Column<string>(type: "text", nullable: true),
                    dateDebut = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateFin = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    lastExecution = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    nextExecution = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    periodicite = table.Column<int>(type: "integer", nullable: true),
                    dateAcceptation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateConfirmation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateAnnulation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    confirmation = table.Column<bool>(type: "boolean", nullable: false),
                    frequence = table.Column<string>(type: "text", nullable: true),
                    canal = table.Column<string>(type: "text", nullable: true),
                    motifRejet = table.Column<string>(type: "text", nullable: true),
                    codeRejet = table.Column<string>(type: "text", nullable: true),
                    attenteConfirmation = table.Column<bool>(type: "boolean", nullable: true),
                    r_categorie_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_client_auteur_id_fk = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    statut = table.Column<int>(type: "integer", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_scheduled", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_trace",
                columns: table => new
                {
                    idrequette = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sensRequete = table.Column<int>(type: "integer", nullable: false),
                    titre = table.Column<string>(type: "text", nullable: false),
                    Requete = table.Column<string>(type: "text", nullable: true),
                    ResponseRequete = table.Column<string>(type: "text", nullable: true),
                    AsyncResponse = table.Column<string>(type: "text", nullable: true),
                    ResponseAsyncResponse = table.Column<string>(type: "text", nullable: true),
                    dateEnvoie = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    datereponse = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cleUnifReqRep = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_trace", x => x.idrequette);
                });

            migrationBuilder.CreateTable(
                name: "t_transfert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    msgId = table.Column<string>(type: "text", nullable: true),
                    endToEndId = table.Column<string>(type: "text", nullable: true),
                    montant = table.Column<double>(type: "double precision", nullable: false),
                    paysClientPayeur = table.Column<string>(type: "text", nullable: true),
                    typeCompteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    deviseCompteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    ibanClientPayeur = table.Column<string>(type: "text", nullable: true),
                    compteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    aliasClientPayeur = table.Column<string>(type: "text", nullable: true),
                    photoClientPayeur = table.Column<string>(type: "text", nullable: true),
                    photoClientPaye = table.Column<string>(type: "text", nullable: true),
                    nomClientPayeur = table.Column<string>(type: "text", nullable: true),
                    typeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    codeMembreParticipantPayeur = table.Column<string>(type: "text", nullable: true),
                    paysClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeCompteClientPaye = table.Column<string>(type: "text", nullable: true),
                    deviseCompteClientPaye = table.Column<string>(type: "text", nullable: true),
                    otherClientPaye = table.Column<string>(type: "text", nullable: true),
                    nomClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeClientPaye = table.Column<string>(type: "text", nullable: true),
                    canalCommunication = table.Column<string>(type: "text", nullable: true),
                    dateHeureAcceptation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateHeureIrrevocabilite = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    numeroIdentificationClientPayeur = table.Column<string>(type: "text", nullable: true),
                    systemeIdentificationClientPayeur = table.Column<string>(type: "text", nullable: true),
                    numeroIdentificationClientPaye = table.Column<string>(type: "text", nullable: true),
                    systemeIdentificationClientPaye = table.Column<string>(type: "text", nullable: true),
                    adresseClientPaye = table.Column<string>(type: "text", nullable: true),
                    adresseClientPayeur = table.Column<string>(type: "text", nullable: true),
                    villeClientPaye = table.Column<string>(type: "text", nullable: true),
                    villeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    aliasClientPaye = table.Column<string>(type: "text", nullable: true),
                    latitudeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    longitudeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    dateNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    villeNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    paysNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    dateNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    villeNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    paysNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    IdOperationSib = table.Column<string>(type: "text", nullable: true),
                    etape = table.Column<int>(type: "integer", nullable: true),
                    statut_general = table.Column<int>(type: "integer", nullable: true),
                    sensFlux = table.Column<int>(type: "integer", nullable: false),
                    numEvenementReserv = table.Column<string>(type: "text", nullable: true),
                    codeOperationReserv = table.Column<string>(type: "text", nullable: true),
                    codeAgenceReserv = table.Column<string>(type: "text", nullable: true),
                    identifiantTransaction = table.Column<string>(type: "text", nullable: true),
                    referenceBulk = table.Column<string>(type: "text", nullable: true),
                    typeTransaction = table.Column<string>(type: "text", nullable: true),
                    otherClientPayeur = table.Column<string>(type: "text", nullable: true),
                    compteClientPaye = table.Column<string>(type: "text", nullable: true),
                    numeroRCCMClientPayeur = table.Column<string>(type: "text", nullable: true),
                    codeMembreParticipantPaye = table.Column<string>(type: "text", nullable: true),
                    ibanClientPaye = table.Column<string>(type: "text", nullable: true),
                    motif = table.Column<string>(type: "text", nullable: true),
                    numeroRCCMClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeDocumentReference = table.Column<string>(type: "text", nullable: true),
                    numeroDocumentReference = table.Column<string>(type: "text", nullable: true),
                    montantAchat = table.Column<double>(type: "double precision", nullable: true),
                    montantRetrait = table.Column<double>(type: "double precision", nullable: true),
                    fraisRetrait = table.Column<double>(type: "double precision", nullable: true),
                    montantFrais = table.Column<double>(type: "double precision", nullable: true),
                    codeRejet = table.Column<string>(type: "text", nullable: true),
                    motifRejet = table.Column<string>(type: "text", nullable: true),
                    signatureNumeriqueMandat = table.Column<string>(type: "text", nullable: true),
                    montantRemisePaiementImmediat = table.Column<double>(type: "double precision", nullable: true),
                    autorisationModificationMontant = table.Column<bool>(type: "boolean", nullable: true),
                    tauxRemisePaiementImmediat = table.Column<double>(type: "double precision", nullable: true),
                    latitudeClientPaye = table.Column<string>(type: "text", nullable: true),
                    longitudeClientPaye = table.Column<string>(type: "text", nullable: true),
                    identifiantMandat = table.Column<string>(type: "text", nullable: true),
                    dateHeureExecution = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateLimiteAction = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateConfirmation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    methodeConfirmation = table.Column<string>(type: "text", nullable: true),
                    retourStatut = table.Column<int>(type: "integer", nullable: true),
                    retourStatutRaison = table.Column<string>(type: "text", nullable: true),
                    retourDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    retourEtape = table.Column<int>(type: "integer", nullable: true),
                    annulationRaison = table.Column<string>(type: "text", nullable: true),
                    annulationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    annulationStatut = table.Column<int>(type: "integer", nullable: true),
                    annulationStatutRaison = table.Column<string>(type: "text", nullable: true),
                    r_opmasse_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_scheduled_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_categorie_payeur_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_categorie_paye_id_fk = table.Column<int>(type: "integer", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_transfert", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_transfert_autorise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    typePaye = table.Column<string>(type: "text", nullable: false),
                    typePayeur = table.Column<string>(type: "text", nullable: true),
                    canals = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_transfert_autorise", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_transfert_dispo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    msgId = table.Column<string>(type: "text", nullable: true),
                    endToEndId = table.Column<string>(type: "text", nullable: true),
                    montant = table.Column<double>(type: "double precision", nullable: false),
                    paysClientPayeur = table.Column<string>(type: "text", nullable: true),
                    typeCompteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    deviseCompteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    ibanClientPayeur = table.Column<string>(type: "text", nullable: true),
                    compteClientPayeur = table.Column<string>(type: "text", nullable: true),
                    aliasClientPayeur = table.Column<string>(type: "text", nullable: true),
                    photoClientPayeur = table.Column<string>(type: "text", nullable: true),
                    photoClientPaye = table.Column<string>(type: "text", nullable: true),
                    nomClientPayeur = table.Column<string>(type: "text", nullable: true),
                    typeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    codeMembreParticipantPayeur = table.Column<string>(type: "text", nullable: true),
                    paysClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeCompteClientPaye = table.Column<string>(type: "text", nullable: true),
                    deviseCompteClientPaye = table.Column<string>(type: "text", nullable: true),
                    otherClientPaye = table.Column<string>(type: "text", nullable: true),
                    nomClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeClientPaye = table.Column<string>(type: "text", nullable: true),
                    canalCommunication = table.Column<string>(type: "text", nullable: true),
                    dateHeureAcceptation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateHeureIrrevocabilite = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    numeroIdentificationClientPayeur = table.Column<string>(type: "text", nullable: true),
                    systemeIdentificationClientPayeur = table.Column<string>(type: "text", nullable: true),
                    numeroIdentificationClientPaye = table.Column<string>(type: "text", nullable: true),
                    systemeIdentificationClientPaye = table.Column<string>(type: "text", nullable: true),
                    adresseClientPaye = table.Column<string>(type: "text", nullable: true),
                    adresseClientPayeur = table.Column<string>(type: "text", nullable: true),
                    villeClientPaye = table.Column<string>(type: "text", nullable: true),
                    villeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    aliasClientPaye = table.Column<string>(type: "text", nullable: true),
                    latitudeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    longitudeClientPayeur = table.Column<string>(type: "text", nullable: true),
                    dateNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    villeNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    paysNaissanceClientPayeur = table.Column<string>(type: "text", nullable: true),
                    dateNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    villeNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    paysNaissanceClientPaye = table.Column<string>(type: "text", nullable: true),
                    IdOperationSib = table.Column<string>(type: "text", nullable: true),
                    etape = table.Column<int>(type: "integer", nullable: true),
                    statut_general = table.Column<int>(type: "integer", nullable: true),
                    sensFlux = table.Column<int>(type: "integer", nullable: true),
                    identifiantTransaction = table.Column<string>(type: "text", nullable: true),
                    referenceBulk = table.Column<string>(type: "text", nullable: true),
                    typeTransaction = table.Column<string>(type: "text", nullable: true),
                    otherClientPayeur = table.Column<string>(type: "text", nullable: true),
                    compteClientPaye = table.Column<string>(type: "text", nullable: true),
                    numeroRCCMClientPayeur = table.Column<string>(type: "text", nullable: true),
                    codeMembreParticipantPaye = table.Column<string>(type: "text", nullable: true),
                    ibanClientPaye = table.Column<string>(type: "text", nullable: true),
                    motif = table.Column<string>(type: "text", nullable: true),
                    numeroRCCMClientPaye = table.Column<string>(type: "text", nullable: true),
                    typeDocumentReference = table.Column<string>(type: "text", nullable: true),
                    numeroDocumentReference = table.Column<string>(type: "text", nullable: true),
                    montantAchat = table.Column<double>(type: "double precision", nullable: true),
                    montantRetrait = table.Column<double>(type: "double precision", nullable: true),
                    fraisRetrait = table.Column<double>(type: "double precision", nullable: true),
                    codeRejet = table.Column<string>(type: "text", nullable: true),
                    motifRejet = table.Column<string>(type: "text", nullable: true),
                    signatureNumeriqueMandat = table.Column<string>(type: "text", nullable: true),
                    montantRemisePaiementImmediat = table.Column<double>(type: "double precision", nullable: true),
                    autorisationModificationMontant = table.Column<bool>(type: "boolean", nullable: true),
                    tauxRemisePaiementImmediat = table.Column<double>(type: "double precision", nullable: true),
                    latitudeClientPaye = table.Column<string>(type: "text", nullable: true),
                    longitudeClientPaye = table.Column<string>(type: "text", nullable: true),
                    identifiantMandat = table.Column<string>(type: "text", nullable: true),
                    dateHeureExecution = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateLimiteAction = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    dateConfirmation = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    methodeConfirmation = table.Column<string>(type: "text", nullable: true),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_transfert_dispo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_transfert_plafond",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    typePaye = table.Column<string>(type: "text", nullable: false),
                    typePayeur = table.Column<string>(type: "text", nullable: false),
                    montant = table.Column<double>(type: "double precision", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_transfert_plafond", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_webhook",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_data = table.Column<string>(type: "text", nullable: false),
                    callbackUrl = table.Column<string>(type: "text", nullable: false),
                    alias = table.Column<string>(type: "text", nullable: true),
                    events = table.Column<string[]>(type: "text[]", nullable: false),
                    secret = table.Column<string>(type: "text", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_webhook", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_demande_ligne",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_description = table.Column<string>(type: "text", nullable: false),
                    r_uri = table.Column<string>(type: "text", nullable: false),
                    r_requete = table.Column<string>(type: "text", nullable: false),
                    r_reponse = table.Column<string>(type: "text", nullable: true),
                    r_dateheure_req = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    r_dateheure_rep = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_sens_req = table.Column<int>(type: "integer", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    r_demande_FK = table.Column<int>(type: "integer", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_demande_ligne", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_demande_ligne_t_demande_r_demande_FK",
                        column: x => x.r_demande_FK,
                        principalTable: "t_demande",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "t_scoped",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_libelle = table.Column<string>(type: "text", nullable: false),
                    r_description = table.Column<string>(type: "text", nullable: false),
                    r_route_scope_fk = table.Column<int>(type: "integer", nullable: false),
                    r_t_route_scopeId = table.Column<int>(type: "integer", nullable: false),
                    r_createdby = table.Column<int>(type: "integer", nullable: true),
                    r_createdon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updatedon = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    r_updated_by = table.Column<int>(type: "integer", nullable: true),
                    r_isactive = table.Column<bool>(type: "boolean", nullable: true),
                    is_delete = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_scoped", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_scoped_t_route_scope_r_t_route_scopeId",
                        column: x => x.r_t_route_scopeId,
                        principalTable: "t_route_scope",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_demande_ligne_r_demande_FK",
                table: "t_demande_ligne",
                column: "r_demande_FK");

            migrationBuilder.CreateIndex(
                name: "IX_t_scoped_r_t_route_scopeId",
                table: "t_scoped",
                column: "r_t_route_scopeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_alias");

            migrationBuilder.DropTable(
                name: "t_annulation_transfert");

            migrationBuilder.DropTable(
                name: "t_categories");

            migrationBuilder.DropTable(
                name: "t_client");

            migrationBuilder.DropTable(
                name: "t_code_erreur");

            migrationBuilder.DropTable(
                name: "t_compte");

            migrationBuilder.DropTable(
                name: "t_contact_client");

            migrationBuilder.DropTable(
                name: "t_creation_alias");

            migrationBuilder.DropTable(
                name: "t_cron_tache");

            migrationBuilder.DropTable(
                name: "t_cron_tache_log");

            migrationBuilder.DropTable(
                name: "t_data");

            migrationBuilder.DropTable(
                name: "t_demande_ligne");

            migrationBuilder.DropTable(
                name: "t_histo_email");

            migrationBuilder.DropTable(
                name: "t_histo_sms");

            migrationBuilder.DropTable(
                name: "t_message");

            migrationBuilder.DropTable(
                name: "t_modele");

            migrationBuilder.DropTable(
                name: "t_notification");

            migrationBuilder.DropTable(
                name: "t_operation");

            migrationBuilder.DropTable(
                name: "t_operation_masse");

            migrationBuilder.DropTable(
                name: "t_otp");

            migrationBuilder.DropTable(
                name: "t_parametre_systeme");

            migrationBuilder.DropTable(
                name: "t_participant");

            migrationBuilder.DropTable(
                name: "t_reference");

            migrationBuilder.DropTable(
                name: "t_register");

            migrationBuilder.DropTable(
                name: "t_retour_fonds");

            migrationBuilder.DropTable(
                name: "t_revendication");

            migrationBuilder.DropTable(
                name: "t_scheduled");

            migrationBuilder.DropTable(
                name: "t_scoped");

            migrationBuilder.DropTable(
                name: "t_trace");

            migrationBuilder.DropTable(
                name: "t_transfert");

            migrationBuilder.DropTable(
                name: "t_transfert_autorise");

            migrationBuilder.DropTable(
                name: "t_transfert_dispo");

            migrationBuilder.DropTable(
                name: "t_transfert_plafond");

            migrationBuilder.DropTable(
                name: "t_webhook");

            migrationBuilder.DropTable(
                name: "t_demande");

            migrationBuilder.DropTable(
                name: "t_route_scope");
        }
    }
}
