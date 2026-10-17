using Microsoft.Extensions.DependencyInjection;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Application.Pipeline.Behaviors;
using Pipeline.Application.UseCases;
using Pipeline.Application.UseCases.Admin;
using Pipeline.Application.UseCases.Bigin;
using Pipeline.Application.UseCases.Gmail;
using Pipeline.Application.UseCases.Payments;
using Pipeline.Application.UseCases.Sheets;
using Pipeline.Contracts.Responses;
using Pipeline.Desktop.Host.Security;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Security;

namespace Pipeline.Desktop.Host.Composition;

public static class ServiceRegistration
{
    public static IServiceCollection AddPipelineHostServices(this IServiceCollection services)
    {
        // Core Singletons
        services.AddSingleton<IClock, SystemClock>();
        var auditWriter = new HashChainAuditWriter();
        services.AddSingleton<IAuditWriter>(auditWriter);
        var jobStore = new SqliteJobStore();
        services.AddSingleton<IJobStore>(jobStore);
        services.AddSingleton<IIdempotencyStore>(jobStore);

        // Security & Session
        services.AddSingleton<SessionManager>();
        services.AddSingleton<IPrincipalAccessor>(sp => sp.GetRequiredService<SessionManager>());
        services.AddSingleton<ICredentialStore, DpapiCredentialStore>();
        services.AddSingleton<ICredentialBroker, CredentialBroker>();
        services.AddSingleton<IResourceAuthorizer, ScopedResourceAuthorizer>();
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        // Pipeline Behaviors
        services.AddSingleton<AuthenticationBehavior>();
        services.AddSingleton<ValidationBehavior>();
        services.AddSingleton<AuthorizationBehavior>();
        services.AddSingleton<RateLimitBehavior>();
        services.AddSingleton<IdempotencyBehavior>();
        services.AddSingleton<AuditBehavior>();
        services.AddSingleton<IUseCaseDispatcher, UseCaseDispatcher>();

        // Handlers
        services.AddTransient<IUseCaseHandler<ReadSheetRangeCommand, SheetRangeResponse>, ReadSheetRangeUseCase>();
        services.AddTransient<IUseCaseHandler<WriteSheetRangeCommand, SheetWriteResponse>, WriteSheetRangeUseCase>();
        services.AddTransient<IUseCaseHandler<SendGmailCommand, GmailMessageResponse>, SendGmailMessageUseCase>();
        services.AddTransient<IUseCaseHandler<CreatePaymentCheckoutCommand, PaymentCheckoutResponse>, CreatePaymentCheckoutUseCase>();
        services.AddTransient<IUseCaseHandler<ApprovePaymentCheckoutCommand, PaymentApprovalResponse>, ApprovePaymentCheckoutUseCase>();
        services.AddTransient<IUseCaseHandler<ManageUserRolesCommand, ManageUserRolesResponse>, ManageUserRolesUseCase>();
        services.AddTransient<IUseCaseHandler<GetBiginModulesCommand, BiginModulesResponseContract>, GetBiginModulesUseCase>();
        services.AddTransient<IUseCaseHandler<GetBiginOrgCommand, BiginOrgResponseContract>, GetBiginOrgUseCase>();
        services.AddTransient<IUseCaseHandler<UploadBiginOrgPhotoCommand, BiginPhotoUploadResponseContract>, UploadBiginOrgPhotoUseCase>();
        services.AddTransient<IUseCaseHandler<GetBiginRelatedRecordsCommand, BiginRelatedRecordsResponseContract>, GetBiginRelatedRecordsUseCase>();

        // IPC Router
        services.AddSingleton<IpcMessageRouter>();

        return services;
    }
}
