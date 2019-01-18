using Microsoft.Bot.Builder;
using Microsoft.Recognizers.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    public class SetLocaleMiddleware : IMiddleware
    {
        private string locale = string.Empty;

        public SetLocaleMiddleware(string locale)
        {
            // 指定したロケールがサポートされたものであれば設定保持。違う場合は en-us を設定。
            if (Culture.SupportedCultures.Where(x => x.CultureName == locale) != null)
            {
                this.locale = locale;
            }
            else
            {
                this.locale = Culture.English;
            }
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            turnContext.Activity.Locale = locale;
            await next.Invoke(cancellationToken);
        }
    }
}