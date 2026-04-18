using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Uno.Core.Business.Models;
using TaskFlow.Uno.Core.Business.Services;
using TaskFlow.Uno.Core.Client;
using TaskFlow.Uno.Presentation;
using TaskFlow.Uno.Views;
using Uno.Extensions.Http.Kiota;

namespace TaskFlow.Uno;

public partial class App : Application
{
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
                    var gatewayUrl = context.Configuration["GatewayBaseUrl"] ?? "https://localhost:7120";
                    services.AddSingleton<MockHttpMessageHandler>();
                    services
                        .AddHttpClient<TaskFlowApiClient>(client =>
                            client.BaseAddress = new Uri(gatewayUrl))
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
                    services
                        // StrongReferenceMessenger: MVUX bindables hold models
                        // weakly in some scenarios, so WeakReferenceMessenger
                        // registrations get GC'd and cross-model refresh
                        // messages (TaskItemsChangedMessage) silently drop.
                        .AddSingleton<IMessenger, StrongReferenceMessenger>()
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

    private void ConfigureSerialization(HostBuilderContext context, IServiceCollection services)
    {
        // Register JSON serialization type info as needed
    }

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
