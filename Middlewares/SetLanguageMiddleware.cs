using System.Linq;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using CognitiveServices.Translator;
using CognitiveServices.Translator.Translate;

public class SetLanguageMiddleware : IMiddleware
{
    // ユーザープロファイルへのプロパティアクセサー
    private readonly IStatePropertyAccessor<UserProfile> userProfile;
    public SetLanguageMiddleware(IStatePropertyAccessor<UserProfile> userProfile)
    {
        this.userProfile = userProfile;
    }
    public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
    {
        // ユーザーの言語設定によって CurrentCulture を設定
        var profile = await userProfile.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

        if (!string.IsNullOrEmpty(profile.Language))
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(profile.Language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(profile.Language);            
        }

        await next.Invoke(cancellationToken);
    }
}