using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest.Helpers
{
    public class TestConnectorClientMiddleware : IMiddleware
    {
        private TestConnectorClientValidator testConnectorClientValidator;
        public TestConnectorClientMiddleware(TestConnectorClientValidator testConnectorClientValidator)
        {
            this.testConnectorClientValidator = testConnectorClientValidator;
        }
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.ServiceUrl.Contains("https://test.com"))
            {
                if (turnContext.TurnState["Microsoft.Bot.Connector.IConnectorClient"] is ConnectorClient)
                {
                    turnContext.TurnState.Remove("Microsoft.Bot.Connector.IConnectorClient");
                    turnContext.TurnState.Add("Microsoft.Bot.Connector.IConnectorClient",
                        new TestConnectorClient(testConnectorClientValidator)
                    );
                }
            }

            await next.Invoke(cancellationToken);
        }
    }
}
