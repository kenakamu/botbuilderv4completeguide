using Microsoft.Bot.Connector;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;

namespace myfirstbot.unittest.Helpers
{
    public class TestConnectorClient : IConnectorClient
    {
        public TestConnectorClient(TestConnectorClientValidator testConnectorClientValidator)
        {
            Conversations = new TestConnectorClientConversation(testConnectorClientValidator);
        }

        public Uri BaseUri
        {
            get { return new Uri("https://test.com"); }
            set { throw new NotImplementedException(); }
        }

        public JsonSerializerSettings SerializationSettings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public JsonSerializerSettings DeserializationSettings
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ServiceClientCredentials Credentials
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IAttachments Attachments
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IConversations Conversations { get; }

        public void Dispose()
        {
        }
    }
}
