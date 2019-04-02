using CognitiveServices.Translator;
using CognitiveServices.Translator.Translate;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest
{
    [TestClass]
    public class MyBotUnitTest
    {

        // �e�X�g�p�ϐ�
        string name = "Ken";

        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs) ArrangeTest(string language, bool returnUserProfile)
        {
            // ������w�肵�ăA�N�Z�T�[���쐬
            var accessors = AccessorsFactory.GetAccessors(language, returnUserProfile);

            // �A�_�v�^�[���쐬
            var adapter = new TestAdapter()
                .Use(new SetLanguageMiddleware(accessors.UserProfile));
       
            // IServiceProvider �̃��b�N
            var serviceProvider = new Mock<IServiceProvider>();

            // MyBot �N���X�ŉ������ׂ��T�[�r�X��o�^
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog(StringLocalizerFactory.GetStringLocalizer<LoginDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(WeatherDialog))).Returns(new WeatherDialog(accessors, StringLocalizerFactory.GetStringLocalizer<WeatherDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(ProfileDialog))).Returns(new ProfileDialog(accessors, StringLocalizerFactory.GetStringLocalizer<ProfileDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(SelectLanguageDialog))).Returns(new SelectLanguageDialog(accessors));
            serviceProvider.Setup(x => x.GetService(typeof(WelcomeDialog))).Returns
                (new WelcomeDialog(accessors, null, serviceProvider.Object));
            serviceProvider.Setup(x => x.GetService(typeof(ScheduleDialog))).Returns(new ScheduleDialog(serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(MenuDialog))).Returns(new MenuDialog(serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<MenuDialog>()));
            serviceProvider.Setup(x => x.GetService(typeof(PhotoUpdateDialog))).Returns(new PhotoUpdateDialog(serviceProvider.Object, StringLocalizerFactory.GetStringLocalizer<PhotoUpdateDialog>()));
            // �e�_�C�A���O�� StringLocalizer ��ǉ�
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<LoginDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<LoginDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<WeatherDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<WeatherDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<ProfileDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<ProfileDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<SelectLanguageDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<SelectLanguageDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<WelcomeDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<WelcomeDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<ScheduleDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<ScheduleDialog>());
            serviceProvider.Setup(x => x.GetService(typeof(IStringLocalizer<MenuDialog>))).Returns(StringLocalizerFactory.GetStringLocalizer<MenuDialog>());

            // IRecognizer �̃��b�N��
            var mockRecognizer = new Mock<IRecognizer>();
            mockRecognizer.Setup(l => l.RecognizeAsync(It.IsAny<TurnContext>(), It.IsAny<CancellationToken>()))
                .Returns((TurnContext turnContext, CancellationToken cancellationToken) =>
                {
                    // RecognizerResult �̍쐬
                    var recognizerResult = new RecognizerResult()
                    {
                        Intents = new Dictionary<string, IntentScore>(),
                        Entities = new JObject()
                    };

                    switch (turnContext.Activity.Text)
                    {
                        case "�L�����Z��":
                            recognizerResult.Intents.Add("Cancel", new IntentScore() { Score = 1 });
                            break;
                        case "�V�C���m�F":
                            recognizerResult.Intents.Add("Weather", new IntentScore() { Score = 1 });
                            break;
                        case "�����̓V�C���m�F":
                            recognizerResult.Intents.Add("Weather", new IntentScore() { Score = 1 });
                            recognizerResult.Entities.Add("day", JArray.Parse("[['����']]"));
                            break;
                        case "�w���v":
                            recognizerResult.Intents.Add("Help", new IntentScore() { Score = 1 });
                            break;
                        case "�v���t�@�C���̕ύX":
                            recognizerResult.Intents.Add("Profile", new IntentScore() { Score = 1 });
                            break;
                        default:
                            recognizerResult.Intents.Add("None", new IntentScore() { Score = 1 });
                            break;
                    }
                    return Task.FromResult(recognizerResult);
                });
            
            // �|��T�[�r�X�̃��b�N��
            var mockTranslateClient = new Mock<ITranslateClient>();
            mockTranslateClient.Setup(l => l.TranslateAsync(It.IsAny<RequestContent>(), It.IsAny<RequestParameter>()))
                .Returns((RequestContent requestContent, RequestParameter requestParameter) =>
                {
                    var response = new List<ResponseBody>();
                    switch (requestContent.Text)
                    {
                        case "Cancel":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "�L�����Z��" } } });
                            break;
                        case "Check weather":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "�V�C���m�F" } } });
                            break;
                        case "Check today's weather":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "�����̓V�C���m�F" } } });
                            break;
                        case "Help":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "�w���v" } } });
                            break;
                        case "Update profile":
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "�v���t�@�C���̕ύX" } } });
                            break;
                        default:
                            response.Add(new ResponseBody() { Translations = new List<Translations>() { new Translations() { Text = "foo" } } });
                            break;
                    }
                    return Task.FromResult(response as IList<ResponseBody>);
                });

            // MyBot �Ń��\�[�X�𗘗p���邽�� StringLocalizer ���쐬
            var localizer = StringLocalizerFactory.GetStringLocalizer<MyBot>();

            // �e�X�g�Ώۂ̃N���X���C���X�^���X��
            var bot = new MyBot(accessors, mockRecognizer.Object, localizer, serviceProvider.Object, mockTranslateClient.Object);

            // �����ւ���K�v��������̂������ւ�
            var photoUpdateDialog = new DummyDialog(nameof(PhotoUpdateDialog));
            bot.ReplaceDialog(photoUpdateDialog);

            // DialogSet ���쐬�����N���X��� Refactor
            var dialogSet = (DialogSet)typeof(MyBot).GetField("dialogs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bot);
            // TestFlow �̍쐬
            var testFlow = new TestFlow(adapter, bot.OnTurnAsync);
            return (testFlow, adapter, dialogSet);
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldGoToWeatherDialog(string language)
        {
            var arrange = ArrangeTest(language, true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Send(language == "ja-JP" ? "�V�C���m�F" : "Check weather")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ WeatherDialog �� choice �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WeatherDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "date");
                })
                .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldGoToHelpDialog(string language)
        {
            var arrange = ArrangeTest(language, true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test(language == "ja-JP" ? "�w���v":"Help", language == "ja-JP" ? "�V�C�Ɨ\�肪�m�F�ł��܂��B": "You can check weather and your schedule")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToSelectLanguageDialogWithConversationUpdateWithoutUserProfile()
        {
            var arrange = ArrangeTest("ja-JP", false);

            var conversationUpdateActivity = new Activity(ActivityTypes.ConversationUpdate)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                MembersAdded = new List<ChannelAccount>() { new ChannelAccount("TestUser", "Test User") }
            };

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Send(conversationUpdateActivity)
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ SelectLanguageDialog �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WelcomeDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "SelectLanguageDialog");
                })
                .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldGoToMenuDialogWithConversationUpdateWithUserProfile(string language)
        {
            var arrange = ArrangeTest(language, true);

            var conversationUpdateActivity = new Activity(ActivityTypes.ConversationUpdate)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                MembersAdded = new List<ChannelAccount>() { new ChannelAccount("TestUser", "Test User") }
            };

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Send(conversationUpdateActivity)
                .AssertReply(language == "ja-JP" ? $"�悤���� '{name}' ����I": $"Welcome {name}!")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldWelcomeAndMenuDialogWithMessage(string language)
        {
            var arrange = ArrangeTest(language, true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", language == "ja-JP" ? $"�悤���� '{name}' ����I" : $"Welcome {name}!")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_ShouldGoToPhotoUpdateDialog(string language)
        {
            var arrange = ArrangeTest(language, true);
            var attachmentActivity = new Activity(ActivityTypes.Message)
            {
                Id = "test",
                From = new ChannelAccount("TestUser", "Test User"),
                ChannelId = "UnitTest",
                ServiceUrl = "https://example.org",
                Attachments = new List<Microsoft.Bot.Schema.Attachment>()
                {
                    new Microsoft.Bot.Schema.Attachment(
                        "image/pgn",
                        "https://github.com/apple-touch-icon.png"
                    )
                }
            };

            await arrange.testFlow
            .Send(attachmentActivity)
            .AssertReply((activity) =>
            {
                // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                // �_�C�A���O�R���e�L�X�g���擾
                var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
                var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                Assert.AreEqual(dialogInstances[0].Id, "choice");
            })
            .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_GlobalCommand_ShouldCancelAllDialog(string language)
        {
            var arrange = ArrangeTest(language, true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", language == "ja-JP" ? $"�悤���� '{name}' ����I" : $"Welcome {name}!")
                .AssertReply((activity) =>
                {
                })
                .Send(language == "ja-JP" ? "�V�C���m�F" : "Check weather")
                .AssertReply((activity) =>
                {
                })
                .Test(language == "ja-JP" ? "�L�����Z��" : "Cancel", language == "ja-JP" ? "�L�����Z�����܂�" : "Cancel")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "choice");
                })
                .StartTestAsync();
        }

        [TestMethod]
        [DataRow("ja-JP")]
        [DataRow("en-US")]
        public async Task MyBot_GlobalCommand_ShouldGoToProfileDialog(string language)
        {
            var arrange = ArrangeTest(language, true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", language == "ja-JP" ? $"�悤���� '{name}' ����I" : $"Welcome {name}!")
                .AssertReply((activity) =>
                {                   
                })
                .Send(language == "ja-JP" ? "�V�C���m�F" : "Check weather")
                .AssertReply((activity) =>
                {
                })
                .Send(language == "ja-JP" ? "�v���t�@�C���̕ύX" : "Update profile")
                .AssertReply((activity) =>
                {
                    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
                    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
                    // �_�C�A���O�R���e�L�X�g���擾
                    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
                    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ ProfileDialog �� name �ł��邱�Ƃ��m�F�B
                    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(ProfileDialog)).First().State["dialogs"] as DialogState).DialogStack;
                    Assert.AreEqual(dialogInstances[0].Id, "adaptive");
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyMiddleware_ShouldStopProcessingWithAttachment()
        {
            // �A�_�v�^�[���쐬���A���p����~�h���E�F�A��ǉ��B
            var adapter = new TestAdapter()
                .Use(new MyMiddleware());

            // �Y�t�t�@�C���𑗂�
            var activityWithAttachment = new Activity(ActivityTypes.Message)
            {
                Attachments = new List<Attachment>() { new Attachment() }
            };

            // �e�X�g�̎��s
            await new TestFlow(adapter)
            .Send(activityWithAttachment)
            .AssertReply("�e�L�X�g�𑗂��Ă�������")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task MyMiddleware_ShouldProcessingWithoutAttachment()
        {
            var nextMiddlewareCalled = false;

            // �o�^�����~�h���E�F�����ׂČĂ΂ꂽ��ɌĂяo�����R�[���o�b�N
            Task ValidateMiddleware(ITurnContext turnContext, CancellationToken cancellationToken)
            {
                // ����� turnContext �̒��g�����؂���K�v�͂Ȃ����߁A
                // ���̃~�h���E�F�A���Ăяo���ꂽ���Ǝ��̂Ō��؂𐬌��Ƃ���B
                nextMiddlewareCalled = true;
                return Task.CompletedTask;
            }
            // MiddlewareSet �Ƀe�X�g�Ώۂ̃~�h���E�F�A��ǉ��B
            var middlewareSet = new MiddlewareSet();
            middlewareSet.Use(new MyMiddleware());

            // �e�L�X�g���b�Z�[�W�� ITurnContext ���쐬�B
            var activityWithoutAttachment = new Activity(ActivityTypes.Message)
            {
                Text = "foo"
            };
            var ctx = new TurnContext(new TestAdapter(), activityWithoutAttachment);

            // MiddlewareSet �Ƀ��b�Z�[�W�𑗐M�B
            await middlewareSet.ReceiveActivityWithStatusAsync(ctx, ValidateMiddleware, default(CancellationToken));

            Assert.IsTrue(nextMiddlewareCalled);
        }
    }
}
