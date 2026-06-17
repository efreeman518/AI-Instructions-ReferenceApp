using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Notifications;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;
using TaskFlow.Uno.Core.Client.Http;
using TaskFlow.Uno.Infrastructure;
using TaskFlow.Uno.Presentation;
using TaskFlow.Uno.Views;
using Uno.Extensions.Http.Kiota;

namespace TaskFlow.Uno;

/// <summary>Configures Uno application startup, dependency injection, and host-specific services.</summary>
public partial class App : Application
{
    /// <summary>Configures app builder behavior for this component.</summary>
    private void ConfigureAppBuilder(IApplicationBuilder builder)
    {
        builder
            .UseToolkitNavigation()
            .Configure(host => host
                .UseAuthentication(auth =>
                    auth.AddCustom(custom =>
                    {
                        custom.Login(async (sp, dispatcher, credentials, cancellationToken) =>
                            await ProcessCredentials(credentials));
                    }, name: "CustomAuth")
                )
                .UseHttp((context, services) =>
                {
                    var gatewayUrl = ResolveGatewayUrl(context.Configuration);
                    services.AddSingleton<MockHttpMessageHandler>();
                    services.AddTransient<BusyDelegatingHandler>();
                    services.AddTransient<ProblemDetailsDelegatingHandler>();
                    services
                        .AddHttpClient<TaskFlowApiClient>(client =>
                            client.BaseAddress = new Uri(gatewayUrl))
                        // Busy is outermost so the indicator stays on during
                        // the inner problem+json parse. ProblemDetails is
                        // innermost so it sees the raw non-2xx response before
                        // anything else translates it.
                        .AddHttpMessageHandler<BusyDelegatingHandler>()
                        .AddHttpMessageHandler<ProblemDetailsDelegatingHandler>()
#if USE_MOCKS
                        .ConfigurePrimaryHttpMessageHandler<MockHttpMessageHandler>()
#endif
                    ;
                })
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    logBuilder.SetMinimumLevel(
                        context.HostingEnvironment.IsDevelopment()
                            ? LogLevel.Information
                            : LogLevel.Warning);
                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder.EmbeddedSource<App>()
                )
                .UseLocalization()
                .UseSerialization(configure: ConfigureSerialization)
                .ConfigureServices((context, services) =>
                {
                    // Captured here because this callback runs on the UI thread
                    // during OnLaunched -> host build; resolving it lazily from
                    // DI could surface a background thread.
                    var uiDispatcher = new DispatcherQueueUiDispatcher(DispatcherQueue.GetForCurrentThread());

                    services
                        // StrongReferenceMessenger: MVUX bindables hold models
                        // weakly in some scenarios, so WeakReferenceMessenger
                        // registrations get GC'd and cross-model refresh
                        // messages (TaskItemsChangedMessage) silently drop.
                        .AddSingleton<IMessenger, StrongReferenceMessenger>()
                        .AddSingleton<IUiDispatcher>(uiDispatcher)
                        .AddSingleton<IBusyTracker, BusyTracker>()
                        .AddSingleton<INotificationService, NotificationService>()
                        .AddSingleton<IFormGuard, FormGuard>()
                        .AddSingleton<ITaskItemApiService, TaskItemApiService>()
                        .AddSingleton<ICategoryApiService, CategoryApiService>()
                        .AddSingleton<ITagApiService, TagApiService>()
                        .AddSingleton<ICommentApiService, CommentApiService>()
                        .AddSingleton<IChecklistItemApiService, ChecklistItemApiService>()
                        .AddSingleton<IAttachmentApiService, AttachmentApiService>()
                        .AddSingleton<IDashboardService, DashboardService>();
                })
                .UseNavigation(
                    ReactiveViewModelMappings.ViewModelMappings,
                    RegisterRoutes,
                    configure: navConfig => navConfig with { AddressBarUpdateEnabled = false }));
    }

    /// <summary>Provides the resolve gateway URL operation for app.</summary>
    private static string ResolveGatewayUrl(IConfiguration configuration)
    {
#if __ANDROID__
        const string platformGatewayKey = "AndroidGatewayBaseUrl";
#elif __IOS__ && !__MACCATALYST__
        const string platformGatewayKey = "IosGatewayBaseUrl";
#else
        const string platformGatewayKey = "GatewayBaseUrl";
#endif
        var gatewayUrl = configuration[platformGatewayKey]
            ?? configuration["GatewayBaseUrl"];

        if (string.IsNullOrWhiteSpace(gatewayUrl))
        {
            throw new InvalidOperationException($"{platformGatewayKey} or GatewayBaseUrl must be configured.");
        }

        return gatewayUrl;
    }

    /// <summary>
    /// Dev/sample login stub: issues a local token for any non-empty username so the UI gets
    /// past the login gate without an interactive IdP flow. This is NOT a security bypass - the
    /// real boundary is the backend, which is config-gated: the API's <c>AuthMode</c> (default
    /// "Scaffold" = dev passthrough; else real Entra JWT) and the Gateway's <c>EntraExternal</c>
    /// section (absent = dev passthrough; present = real Entra validation). In a configured
    /// (production) backend, the token minted here is rejected. A real app replaces this stub with
    /// an interactive MSAL/Entra login. Intentionally left ungated so the normal non-mocked dev
    /// loop (UI + Aspire dev gateway) can log in; do not gate it behind USE_MOCKS.
    /// </summary>
    private async ValueTask<IDictionary<string, string>?> ProcessCredentials(
        IDictionary<string, string> credentials)
    {
        if (!(credentials?.TryGetValue("Username", out var username) ?? false)
            || string.IsNullOrEmpty(username))
        {
            return null;
        }

        return new Dictionary<string, string>
        {
            { TokenCacheExtensions.AccessTokenKey, "SampleToken" },
            { TokenCacheExtensions.RefreshTokenKey, "RefreshToken" },
            { "Expiry", DateTime.Now.AddMinutes(30).ToString("g") }
        };
    }

    /// <summary>Configures serialization behavior for this component.</summary>
    private void ConfigureSerialization(HostBuilderContext context, IServiceCollection services)
    {
        // Register JSON serialization type info as needed
    }

    /// <summary>Registers routes dependencies in the service container.</summary>
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellModel)),
            new ViewMap<MainPage, MainModel>(),
            new ViewMap<DashboardPage, DashboardModel>(),
            new ViewMap<TaskListPage, TaskListModel>(),
            new ViewMap<TaskItemPage, TaskItemPageModel>(Data: new DataMap<TaskItemModel>()),
            new ViewMap<CategoryTreePage, CategoryTreeModel>(),
            new ViewMap<TagManagementPage, TagManagementModel>(),
            new ViewMap<SettingsPage, SettingsModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellModel>(),
                Nested:
                [
                    new RouteMap("Main", View: views.FindByViewModel<MainModel>(), IsDefault: true,
                        Nested:
                        [
                            new RouteMap("Dashboard", View: views.FindByViewModel<DashboardModel>(), IsDefault: true),
                            new RouteMap("TaskList", View: views.FindByViewModel<TaskListModel>()),
                            new RouteMap("Categories", View: views.FindByViewModel<CategoryTreeModel>()),
                            new RouteMap("Tags", View: views.FindByViewModel<TagManagementModel>()),
                            new RouteMap("TaskItem", View: views.FindByViewModel<TaskItemPageModel>()),
                            new RouteMap("Settings", View: views.FindByViewModel<SettingsModel>()),
                        ]
                    ),
                ]
            )
        );
    }
}
