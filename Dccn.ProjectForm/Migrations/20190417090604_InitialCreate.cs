using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dccn.ProjectForm.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER DATABASE CURRENT SET ENABLE_BROKER;", true);
            migrationBuilder.Sql("ALTER DATABASE CURRENT SET TRUSTWORTHY ON;", true);

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Timestamp = table.Column<byte[]>(type: "ROWVERSION", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastEditedOn = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastEditedBy = table.Column<string>(nullable: false),
                    OwnerId = table.Column<string>(maxLength: 10, nullable: false),
                    ProjectId = table.Column<string>(maxLength: 10, nullable: true),
                    Title = table.Column<string>(nullable: false),
                    SupervisorId = table.Column<string>(maxLength: 10, nullable: false),
                    FundingContactName = table.Column<string>(nullable: true),
                    FundingContactEmail = table.Column<string>(nullable: true),
                    FinancialCode = table.Column<string>(nullable: true),
                    EcApproved = table.Column<bool>(nullable: false),
                    EcCode = table.Column<string>(nullable: true),
                    EcReference = table.Column<string>(nullable: true),
                    StartDate = table.Column<DateTime>(type: "DATE", nullable: true),
                    EndDate = table.Column<DateTime>(type: "DATE", nullable: true),
                    CustomQuota = table.Column<bool>(nullable: false),
                    CustomQuotaAmount = table.Column<int>(nullable: true),
                    CustomQuotaMotivation = table.Column<string>(nullable: true),
                    ExternalPreservation = table.Column<bool>(nullable: false),
                    ExternalPreservationLocation = table.Column<string>(nullable: true),
                    ExternalPreservationSupervisor = table.Column<string>(nullable: true),
                    ExternalPreservationReference = table.Column<string>(nullable: true),
                    PrivacyDataTypes = table.Column<string>(nullable: false),
                    PrivacyMotivations = table.Column<string>(nullable: false),
                    PrivacyStorageLocations = table.Column<string>(nullable: false),
                    PrivacyDataAccessors = table.Column<string>(nullable: false),
                    PrivacySecurityMeasures = table.Column<string>(nullable: true),
                    PrivacyDataDisposalTerm = table.Column<string>(nullable: true),
                    PaymentSubjectCount = table.Column<int>(nullable: true),
                    PaymentAverageSubjectCost = table.Column<decimal>(type: "DECIMAL(5, 2)", nullable: true),
                    PaymentMaxTotalCost = table.Column<decimal>(type: "DECIMAL(6, 2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    ProposalId = table.Column<int>(nullable: false),
                    AuthorityRole = table.Column<string>(nullable: false),
                    Status = table.Column<string>(nullable: false, defaultValue: "NotSubmitted"),
                    ValidatedBy = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => new { x.ProposalId, x.AuthorityRole });
                    table.ForeignKey(
                        name: "FK_Approvals_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    ProposalId = table.Column<int>(nullable: false),
                    SectionId = table.Column<string>(nullable: false),
                    Content = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => new { x.ProposalId, x.SectionId });
                    table.ForeignKey(
                        name: "FK_Comments_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Experimenters",
                columns: table => new
                {
                    ProposalId = table.Column<int>(nullable: false),
                    UserId = table.Column<string>(maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experimenters", x => new { x.ProposalId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Experimenters_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Labs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProposalId = table.Column<int>(nullable: false),
                    Modality = table.Column<string>(nullable: true),
                    SubjectCount = table.Column<int>(nullable: true),
                    ExtraSubjectCount = table.Column<int>(nullable: true),
                    SessionCount = table.Column<int>(nullable: true),
                    SessionDuration = table.Column<TimeSpan>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Labs_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorageAccessRules",
                columns: table => new
                {
                    ProposalId = table.Column<int>(nullable: false),
                    UserId = table.Column<string>(maxLength: 10, nullable: false),
                    Role = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageAccessRules", x => new { x.ProposalId, x.UserId });
                    table.ForeignKey(
                        name: "FK_StorageAccessRules_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_AuthorityRole",
                table: "Approvals",
                column: "AuthorityRole");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_ProposalId",
                table: "Labs",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_OwnerId",
                table: "Proposals",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_ProjectId",
                table: "Proposals",
                column: "ProjectId",
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_SupervisorId",
                table: "Proposals",
                column: "SupervisorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Experimenters");

            migrationBuilder.DropTable(
                name: "Labs");

            migrationBuilder.DropTable(
                name: "StorageAccessRules");

            migrationBuilder.DropTable(
                name: "Proposals");
        }
    }
}
