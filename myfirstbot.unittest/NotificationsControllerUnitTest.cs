using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace myfirstbot.unittest
{
    [TestClass]
    public class NotificationsControllerUnitTest
    {
        // �_�~�[�̃f�[�^�쐬
        ScheduleNotification scheduleNotification = new ScheduleNotification
        {
            Title = "Dummy 1",
            NotificationTime = DateTime.Now,
            StartTime = DateTime.Now,
            WebLink = "dummylink",
            ConversationReference = new ConversationReference(
                    Guid.NewGuid().ToString(),
                    user: new ChannelAccount("user1", "User1"),
                    bot: new ChannelAccount("bot", "Bot"),
                    conversation: new ConversationAccount(false, "convo1", "1", "test"),
                    channelId: "test",
                    serviceUrl: "https://test.com"),
        };

        private (NotificationsController notificationsController,
            ScheduleNotificationStore scheduleNotificationStore,
            StringLocalizer<NotificationsController> localizer)
            ArrangeTest(string language, TestConnectorClientValidator testConnectorClientValidator)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            var scheduleNotificationStore = new ScheduleNotificationStore();
            scheduleNotificationStore.Add(scheduleNotification);

            var appId = Guid.NewGuid().ToString();

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new BotFrameworkAdapter(new SimpleCredentialProvider(appId, ""))
                .Use(new TestConnectorClientMiddleware(testConnectorClientValidator));

            // IServiceProvider �̃��b�N
            var serviceProvider = new Mock<IServiceProvider>();
            // �������ׂ��T�[�r�X��o�^
            serviceProvider.Setup(x => x.GetService(typeof(IAdapterIntegration))).Returns(adapter);

            var localizer = StringLocalizerFactory.GetStringLocalizer<NotificationsController>();
            var botConfiguration = new BotConfiguration()
            {
                Services = new List<ConnectedService>()
                {
                    new EndpointService() {
                        Id = "DummyId",
                        Endpoint = "https://test.com",
                        AppId = appId, AppPassword = "" }
                }
            };

            // �R���g���[���[�̍쐬
            NotificationsController notificationsController =
                new NotificationsController(serviceProvider.Object, localizer, botConfiguration, scheduleNotificationStore);

            return (notificationsController, scheduleNotificationStore, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public void NotificationService_ShouldSendNotification(string language)
        {
            var testConnectorClientValidator = new TestConnectorClientValidator();
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language, testConnectorClientValidator);

            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // Post ���\�b�h�̌Ăяo��
            arrange.notificationsController.Post();
            // ���ʂ̌���
            testConnectorClientValidator.AssertReply((activity)=>{
                Assert.AreEqual( 
                    (activity as Activity).Text, 
                    $"{arrange.localizer["notification"]}:{scheduleNotification.StartTime.ToString("HH:mm")} - [{scheduleNotification.Title}]({scheduleNotification.WebLink})");
            });
        }
    }
}