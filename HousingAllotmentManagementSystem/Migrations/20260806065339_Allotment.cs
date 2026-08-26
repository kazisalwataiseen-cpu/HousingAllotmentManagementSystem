using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousingAllotmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Allotment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "AITStudent");

            migrationBuilder.CreateTable(
                name: "Amenities",
                schema: "AITStudent",
                columns: table => new
                {
                    AmenityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmenityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Amenitie__842AF50BF8FBAB69", x => x.AmenityId);
                });

            migrationBuilder.CreateTable(
                name: "HousingSchemes",
                schema: "AITStudent",
                columns: table => new
                {
                    SchemeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchemeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LaunchDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastApplicationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalUnits = table.Column<int>(type: "int", nullable: true),
                    Brochure = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BannerImage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HousingS__DB7E1A627551CBA3", x => x.SchemeId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "AITStudent",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__8AFACE1A64327652", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                schema: "AITStudent",
                columns: table => new
                {
                    PropertyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchemeId = table.Column<int>(type: "int", nullable: false),
                    UnitNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PlotNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PropertyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    Bathrooms = table.Column<int>(type: "int", nullable: true),
                    CarpetArea = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    BuiltupArea = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    FloorPlanImage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Facing = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BookingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Properti__70C9A73536F11736", x => x.PropertyId);
                    table.ForeignKey(
                        name: "FK__Propertie__Schem__164F3FA9",
                        column: x => x.SchemeId,
                        principalSchema: "AITStudent",
                        principalTable: "HousingSchemes",
                        principalColumn: "SchemeId");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "AITStudent",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DOB = table.Column<DateOnly>(type: "date", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Pincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AadhaarNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PANNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnnualIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProfileImage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Status = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__1788CC4C23EDA423", x => x.UserId);
                    table.ForeignKey(
                        name: "FK__Users__RoleId__0EAE1DE1",
                        column: x => x.RoleId,
                        principalSchema: "AITStudent",
                        principalTable: "Roles",
                        principalColumn: "RoleId");
                });

            migrationBuilder.CreateTable(
                name: "PropertyAmenities",
                schema: "AITStudent",
                columns: table => new
                {
                    PropertyAmenityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    AmenityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Property__0D6BB45E11D852F0", x => x.PropertyAmenityId);
                    table.ForeignKey(
                        name: "FK__PropertyA__Ameni__20CCCE1C",
                        column: x => x.AmenityId,
                        principalSchema: "AITStudent",
                        principalTable: "Amenities",
                        principalColumn: "AmenityId");
                    table.ForeignKey(
                        name: "FK__PropertyA__Prope__1FD8A9E3",
                        column: x => x.PropertyId,
                        principalSchema: "AITStudent",
                        principalTable: "Properties",
                        principalColumn: "PropertyId");
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                schema: "AITStudent",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    ApplicationDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    EmploymentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnnualIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NomineeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NomineeRelation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Applicat__C93A4C99CA7CB274", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_Applications_Properties",
                        column: x => x.PropertyId,
                        principalSchema: "AITStudent",
                        principalTable: "Properties",
                        principalColumn: "PropertyId");
                    table.ForeignKey(
                        name: "FK_Applications_Users",
                        column: x => x.UserId,
                        principalSchema: "AITStudent",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "AITStudent",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecordId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BrowserInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AuditLog__EB5F6CBDAA30C653", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users",
                        column: x => x.UserId,
                        principalSchema: "AITStudent",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "AITStudent",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    ReadDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Sent")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__20CF2E129114B071", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users",
                        column: x => x.UserId,
                        principalSchema: "AITStudent",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "UserDocuments",
                schema: "AITStudent",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AadhaarCard = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PANCard = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IncomeCertificate = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SalarySlip = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PassportPhoto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BankStatement = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    OtherDocument = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    VerifiedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserDocu__1ABEEF0FAE576FBE", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_UserDocuments_Users",
                        column: x => x.UserId,
                        principalSchema: "AITStudent",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Allotments",
                schema: "AITStudent",
                columns: table => new
                {
                    AllotmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    AllotmentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllotmentDate = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())"),
                    BookingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllotmentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Booked"),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Allotmen__E9FEF60FD165CD91", x => x.AllotmentId);
                    table.ForeignKey(
                        name: "FK_Allotments_Applications",
                        column: x => x.ApplicationId,
                        principalSchema: "AITStudent",
                        principalTable: "Applications",
                        principalColumn: "ApplicationId");
                    table.ForeignKey(
                        name: "FK_Allotments_Properties",
                        column: x => x.PropertyId,
                        principalSchema: "AITStudent",
                        principalTable: "Properties",
                        principalColumn: "PropertyId");
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                schema: "AITStudent",
                columns: table => new
                {
                    LoanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllotmentId = table.Column<int>(type: "int", nullable: false),
                    LoanNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoanAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DownPayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LoanTenure = table.Column<int>(type: "int", nullable: false),
                    EMIAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SanctionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LoanStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Loans__4F5AD457605EC373", x => x.LoanId);
                    table.ForeignKey(
                        name: "FK_Loans_Allotments",
                        column: x => x.AllotmentId,
                        principalSchema: "AITStudent",
                        principalTable: "Allotments",
                        principalColumn: "AllotmentId");
                });

            migrationBuilder.CreateTable(
                name: "EMIPlans",
                schema: "AITStudent",
                columns: table => new
                {
                    EMIPlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<int>(type: "int", nullable: false),
                    EMIStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EMIEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalEMIs = table.Column<int>(type: "int", nullable: false),
                    PaidEMIs = table.Column<int>(type: "int", nullable: false),
                    RemainingEMIs = table.Column<int>(type: "int", nullable: false),
                    MonthlyEMI = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NextDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlanStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EMIPlans__201438C967969F60", x => x.EMIPlanId);
                    table.ForeignKey(
                        name: "FK_EMIPlans_Loans",
                        column: x => x.LoanId,
                        principalSchema: "AITStudent",
                        principalTable: "Loans",
                        principalColumn: "LoanId");
                });

            migrationBuilder.CreateTable(
                name: "Installments",
                schema: "AITStudent",
                columns: table => new
                {
                    InstallmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EMIPlanId = table.Column<int>(type: "int", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LateFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Installm__42B42D82FDA40456", x => x.InstallmentId);
                    table.ForeignKey(
                        name: "FK_Installments_EMIPlans",
                        column: x => x.EMIPlanId,
                        principalSchema: "AITStudent",
                        principalTable: "EMIPlans",
                        principalColumn: "EMIPlanId");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "AITStudent",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstallmentId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Success"),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payments__9B556A388E5BF6BA", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Installments",
                        column: x => x.InstallmentId,
                        principalSchema: "AITStudent",
                        principalTable: "Installments",
                        principalColumn: "InstallmentId");
                    table.ForeignKey(
                        name: "FK_Payments_Users",
                        column: x => x.UserId,
                        principalSchema: "AITStudent",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allotments_ApplicationId",
                schema: "AITStudent",
                table: "Allotments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Allotments_PropertyId",
                schema: "AITStudent",
                table: "Allotments",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "UQ__Allotmen__F43E412123E3D83C",
                schema: "AITStudent",
                table: "Allotments",
                column: "AllotmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Amenitie__7B4A459F5B470095",
                schema: "AITStudent",
                table: "Amenities",
                column: "AmenityName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_PropertyId",
                schema: "AITStudent",
                table: "Applications",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_UserId",
                schema: "AITStudent",
                table: "Applications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                schema: "AITStudent",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EMIPlans_LoanId",
                schema: "AITStudent",
                table: "EMIPlans",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_EMIPlanId",
                schema: "AITStudent",
                table: "Installments",
                column: "EMIPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_AllotmentId",
                schema: "AITStudent",
                table: "Loans",
                column: "AllotmentId");

            migrationBuilder.CreateIndex(
                name: "UQ__Loans__EEC26628AAEF7968",
                schema: "AITStudent",
                table: "Loans",
                column: "LoanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                schema: "AITStudent",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InstallmentId",
                schema: "AITStudent",
                table: "Payments",
                column: "InstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                schema: "AITStudent",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ__Payments__55433A6A71835DA9",
                schema: "AITStudent",
                table: "Payments",
                column: "TransactionId",
                unique: true,
                filter: "[TransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ__Payments__C08AFDAB7B50AA5D",
                schema: "AITStudent",
                table: "Payments",
                column: "ReceiptNumber",
                unique: true,
                filter: "[ReceiptNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_SchemeId",
                schema: "AITStudent",
                table: "Properties",
                column: "SchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAmenities_AmenityId",
                schema: "AITStudent",
                table: "PropertyAmenities",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAmenities_PropertyId",
                schema: "AITStudent",
                table: "PropertyAmenities",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_UserId",
                schema: "AITStudent",
                table: "UserDocuments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                schema: "AITStudent",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__6FAE0782435CDC2F",
                schema: "AITStudent",
                table: "Users",
                column: "Mobile",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D10534DE3FB8E8",
                schema: "AITStudent",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "PropertyAmenities",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "UserDocuments",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Installments",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Amenities",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "EMIPlans",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Loans",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Allotments",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Applications",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Properties",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "HousingSchemes",
                schema: "AITStudent");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "AITStudent");
        }
    }
}
