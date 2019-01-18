using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
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
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBot<MyBot>(options =>
            {
                options.Middleware.Add(new MyLoggingMiddleware());
                options.Middleware.Add(new MyMiddleware());

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
