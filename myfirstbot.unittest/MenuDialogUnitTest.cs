using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MenuDialogUnitTest
    {
        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs, StringLocalizer<MenuDialog> localizer) ArrangeTest(string language)
        {
            var accessors = AccessorsFactory.GetAccessors(language);

            // ���\�[�X�𗘗p���邽�� StringLocalizer ���쐬
            var localizer = StringLocalizerFactory.GetStringLocalizer<MenuDialog>();

            // IServiceProvider �̃��b�N
            var serviceProvider = new Mock<IServiceProvider>();

            // MenuDialog �N���X�ŉ������ׂ��T�[�r�X��o�^
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(WeatherDialog))).Returns(new WeatherDialog(accessors, StringLocalizerFactory.GetStringLocalizer<WeatherDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(ScheduleDialog))).Returns(new ScheduleDialog(accessors, serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>(), new ScheduleNotificationStore()));
            serviceProvider.Setup(x => x.GetService(typeof(QnADialog))).Returns(new QnADialog(accessors,null, null, StringLocalizerFactory.GetStringLocalizer<QnADialog>()));

            // �e�X�g�Ώۂ̃_�C�A���O���C���X�^���X��
            var dialogs = new DialogSet(accessors.ConversationDialogState);
            dialogs.Add(new MenuDialog(serviceProvider.Object, localizer));

            // �A�_�v�^�[���쐬���K�v�ȃ~�h���E�F�A��ǉ�
            var adapter = new TestAdapter()
                .Use(new SetLocaleMiddleware(Culture.Japanese))
                .Use(new AutoSaveStateMiddleware(accessors.UserState, accessors.ConversationState));

            // TestFlow �̍쐬
            var testFlow = new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                // �_�C�A���O�ɕK�v�ȃR�[�h�����ǉ�
                var dialogContext = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync(nameof(MenuDialog), null, cancellationToken);
                }
            });

            return (testFlow, adapter, dialogs, localizer);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MenuDialog_ShouldGoToWeatherDialog(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["choicemenu"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkweather"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkschedule"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkqa"]) >= 0);
            })
            .Send(arrange.localizer["checkweather"])
            .AssertReply((activity) =>
            {
            // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
            var turnContext = new TurnContext(arrange.adapter, activity as Activity);
            // �_�C�A���O�R���e�L�X�g���擾
            var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
            // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ WeatherDialog �� ���̉��� MenuDialog �ł��邱�Ƃ��m�F�B
            var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, nameof(WeatherDialog));
            })
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        [ExpectedException(typeof(System.Exception), "OAuthPrompt.GetUserToken(): not supported by the current adapter")]

        public async Task MenuDialog_ShouldGoToScheduleDialog(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["choicemenu"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkweather"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkschedule"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkqa"]) >= 0);
            })
            // �\����m�F�𑗂������_�� OAuthPrompt.GetUserToken(): not supported by the current adapter �G���[���o��
            .Test(arrange.localizer["checkschedule"], "dummy")
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MenuDialog_ShouldGoToQnADialog(string language)
        {
            // ������w�肵�ăe�X�g���쐬
            var arrange = ArrangeTest(language);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(language);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
            .Send("foo")
            .AssertReply((activity) =>
            {
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["choicemenu"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkweather"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkschedule"]) >= 0);
                Assert.IsTrue((activity as Activity).Text.IndexOf(arrange.localizer["checkqa"]) >= 0);
            })
            .Send(arrange.localizer["checkqa"])
            .AssertReply((activity) =>
            {
                // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                // �_�C�A���O�R���e�L�X�g���擾
                var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ QnADialog �ł��邱�Ƃ��m�F�B
                var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, nameof(QnADialog));
            })
            .StartTestAsync();
        }
    }
}