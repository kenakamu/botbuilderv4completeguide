using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class ScheduleDialogUnitTest
    {
        // �_�~�[�̗\��p�̎���
        DateTime datetime = DateTime.Now;

        private (ScheduleNotificationStore scheduleNotificationStore, TestFlow testFlow, StringLocalizer<ScheduleDialog> localizer) ArrangeTest(string language, bool returnEvents)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // ���\�[�X�𗘗p���邽�� StringLocalizer ���쐬
            var localizer = StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>();

            // Microsoft Graph �n�̃��b�N
            var mockGraphSDK = new Mock<IGraphServiceClient>();
            // �_�~�[�̗\���Ԃ��B
            mockGraphSDK.Setup(x => x.Me.CalendarView.Request(It.IsAny<List<QueryOption>>()).GetAsync())
                .ReturnsAsync(() =>
                {
                    var page = new UserCalendarViewCollectionPage();
                    if (returnEvents)
                    {
                        page.Add(new Event()
                        {
                            Subject = "Dummy 1",
                            Start = new DateTimeTimeZone() { DateTime = datetime.ToString() },
                            End = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(30).ToString() }
                        });
                        page.Add(new Event()
                        {
                            Subject = "Dummy 2",
                            Start = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(60).ToString() },
                            End = new DateTimeTimeZone() { DateTime = datetime.AddMinutes(90).ToString() }
                        });
                    }
                    return page;
                });

            // IServiceProvider �̃��b�N
            var serviceProvider = new Mock<IServiceProvider>();

            // ScheduleDialog �N���X�ŉ������ׂ��T�[�r�X��o�^
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(MSGraphService))).Returns(new MSGraphService(mockGraphSDK.Object));

            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var loginDialog = new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>());
            // OAuthPrompt ���e�X�g�p�̃v�����v�g�ɍ����ւ�
            loginDialog.ReplaceDialog(new TestOAuthPrompt("login", new OAuthPromptSettings()));

            var scheduleNotificationStore = new ScheduleNotificationStore();
            var scheduleDialog = new ScheduleDialog(accessors, serviceProvider.Object, localizer, scheduleNotificationStore);
            // ���O�C���_�C�A���O����L�ł��������̂ɍ����ւ�
            scheduleDialog.ReplaceDialog(loginDialog);
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(scheduleDialog);
            dialogs.Add(loginDialog);

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(accessors.UserState, accessors.ConversationState));

            // TestFlow �̍쐬
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(ScheduleDialog), null, cancellationToken);
                }
                // �_�C�A���O�����������ꍇ�́AComplete ���e�X�g���ɕԂ�
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    await turnContext.SendActivityAsync("complete");
                }
            });

            return (scheduleNotificationStore, testFlow, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task ScheduleDialog_ShouldReturnEventsAndSuccessfullyCreateNotification(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language, true);

            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, $"{datetime.ToString("HH:mm")}-{datetime.AddMinutes(30).ToString("HH:mm")} : Dummy 1");
            })
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, $"{datetime.AddMinutes(60).ToString("HH:mm")}-{datetime.AddMinutes(90).ToString("HH:mm")} : Dummy 2");
            })
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["setnotification"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf($"{datetime.ToString("HH:mm")}-Dummy 1") >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf($"{datetime.AddMinutes(60).ToString("HH:mm")}-Dummy 2") >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["nonotification"]) >= 0);
            })
            .Send($"{datetime.ToString("HH:mm")}-Dummy 1")
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, arrange.localizer["notificationset"]);
                Assert.IsTrue(arrange.scheduleNotificationStore.Count == 1);
            })
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task ScheduleDialog_ShouldReturnEventsAndNotSetNotification(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language, true);

            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, $"{datetime.ToString("HH:mm")}-{datetime.AddMinutes(30).ToString("HH:mm")} : Dummy 1");
            })
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, $"{datetime.AddMinutes(60).ToString("HH:mm")}-{datetime.AddMinutes(90).ToString("HH:mm")} : Dummy 2");
            })
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["setnotification"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf($"{datetime.ToString("HH:mm")}-Dummy 1") >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf($"{datetime.AddMinutes(60).ToString("HH:mm")}-Dummy 2") >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["nonotification"]) >= 0);
            })
            .Send(arrange.localizer["nonotification"])
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, "complete");
                Assert.IsTrue(arrange.scheduleNotificationStore.Count == 0);
            })
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task ScheduleDialog_ShouldReturnNoEventMessage(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language, false);

            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.AreEqual((activity as Activity).Text, arrange.localizer["noevents"]);
            })
            .StartTestAsync();
        }
    }
}