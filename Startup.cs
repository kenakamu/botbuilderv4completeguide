using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace myfirstbot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var secretKey = Configuration.GetSection("botFileSecret")?.Value;
            var botFilePath = Configuration.GetSection("botFilePath")?.Value;

            // 構成ファイルの読込み
            var botConfig = BotConfiguration.Load(botFilePath ?? @".\BotConfiguration.bot", secretKey);
            // 構成ファイルより LuisService を取得
            var luisService = (LuisService)botConfig.Services.Where(x => x.Type == "luis").First();
            // 構成情報より LuisApplication を作成
            var luisApp = new LuisApplication(luisService.AppId, luisService.AuthoringKey, luisService.GetEndpoint());
            var luisRecognizer = new LuisRecognizer(luisApp);
            services.AddSingleton(sp => luisRecognizer);

            services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot config file could not be loaded. ({botConfig})"));

            services.AddBot<MyBot>(options =>
            {
                options.Middleware.Add(new MyLoggingMiddleware());
                options.Middleware.Add(new MyMiddleware());

                // Endpoint を構成ファイルより取得
                EndpointService endpointService = (EndpointService)botConfig.Services.Where(x => x.Type == "endpoint").First();
                // 認証として　AppId と AppPassword を使うように設定
                options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

                // ストレージとしてインメモリを利用
                IStorage dataStore = new MemoryStorage();
                var userState = new UserState(dataStore);
                var conversationState = new ConversationState(dataStore);
                options.State.Add(userState);
                options.State.Add(conversationState);
            });

            // MyStateAccessors を IoC に登録
            services.AddSingleton(sp =>
            {
                // AddBot で登録した options を取得。
                var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
                if (options == null)
                {
                    throw new InvalidOperationException("BotFrameworkOptions を事前に構成してください。");
                }
                var userState = options.State.OfType<UserState>().FirstOrDefault();
                if (userState == null)
                {
                    throw new InvalidOperationException("UserState を事前に定義してください。");
                }

                var conversationState = options.State.OfType<ConversationState>().FirstOrDefault();
                if (conversationState == null)
                {
                    throw new InvalidOperationException("ConversationState を事前に定義してください。");
                }

                var accessors = new MyStateAccessors(userState, conversationState)
                {
                    // DialogState を作成
                    ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                    // UserProfile を作成
                    UserProfile = userState.CreateProperty<UserProfile>("UserProfile")
                };

                return accessors;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseBotFramework();
        }
    }
}