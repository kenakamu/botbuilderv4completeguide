using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using myfirstbot.unittest.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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

        private (TestFlow testFlow, BotAdapter adapter, DialogSet dialogs) ArrangeTest(bool returnUserProfile)
        {
            // �A�_�v�^�[���쐬
            var adapter = new TestAdapter();
            // �X�g���[�W�Ƃ��ă��b�N�̃X�g���[�W�𗘗p
            var mockStorage = new Mock<IStorage>();
            // User1�p�ɕԂ��f�[�^���쐬
            // UserState �̃L�[�� <channelId>/users/<userId>
            var dictionary = new Dictionary<string, object>();
            if (returnUserProfile)
            {
                dictionary.Add("test/users/user1", new Dictionary<string, object>()
                {
                    { "UserProfile", new UserProfile() { Name = name, Age = 0 } }
                });
            }
            // �X�g���[�W�ւ̓ǂݏ�����ݒ�
            mockStorage.Setup(ms => ms.WriteAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
                .Returns((Dictionary<string, object> dic, CancellationToken token) =>
                {
                    foreach (var dicItem in dic)
                    {
                        if (dicItem.Key != "test/users/user1")
                        {
                            if (dictionary.ContainsKey(dicItem.Key))
                            {
                                dictionary[dicItem.Key] = dicItem.Value;
                            }
                            else
                            {
                                dictionary.Add(dicItem.Key, dicItem.Value);
                            }
                        }
                    }

                    return Task.CompletedTask;
                });
            mockStorage.Setup(ms => ms.ReadAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    return Task.FromResult(result: (IDictionary<string, object>)dictionary);
                });

            // ���ꂼ��̃X�e�[�g���쐬
            var conversationState = new ConversationState(mockStorage.Object);
            var userState = new UserState(mockStorage.Object);
            var accessors = new MyStateAccessors(userState, conversationState)
            {
                // DialogState �� ConversationState �̃v���p�e�B�Ƃ��Đݒ�
                ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
                // UserProfile ���쐬
                UserProfile = userState.CreateProperty<UserProfile>("UserProfile")
            };

            // IServiceProvider �̃��b�N
            var serviceProvider = new Mock<IServiceProvider>();

            // MyBot �N���X�ŉ������ׂ��T�[�r�X��o�^
            serviceProvider.Setup(x => x.GetService(typeof(LoginDialog))).Returns(new LoginDialog());
            serviceProvider.Setup(x => x.GetService(typeof(WeatherDialog))).Returns(new WeatherDialog());
            serviceProvider.Setup(x => x.GetService(typeof(ProfileDialog))).Returns(new ProfileDialog(accessors));
            serviceProvider.Setup(x => x.GetService(typeof(SelectLanguageDialog))).Returns(new SelectLanguageDialog(accessors));
            serviceProvider.Setup(x => x.GetService(typeof(WelcomeDialog))).Returns
                (new WelcomeDialog(accessors, null, serviceProvider.Object));
            serviceProvider.Setup(x => x.GetService(typeof(ScheduleDialog))).Returns(new ScheduleDialog(serviceProvider.Object));
            serviceProvider.Setup(x => x.GetService(typeof(MenuDialog))).Returns(new MenuDialog(serviceProvider.Object));
            serviceProvider.Setup(x => x.GetService(typeof(PhotoUpdateDialog))).Returns(new PhotoUpdateDialog(serviceProvider.Object));

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

                    switch(turnContext.Activity.Text)
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
            // �e�X�g�Ώۂ̃N���X���C���X�^���X��
            var bot = new MyBot(accessors, mockRecognizer.Object, serviceProvider.Object);

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
        public async Task MyBot_ShouldGoToWeatherDialog()
        {
            var arrange = ArrangeTest(false);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Send("�V�C���m�F")
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
        public async Task MyBot_ShouldGoToWeatherDialogWithEntityResult()
        {
            var arrange = ArrangeTest(false);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Send("�����̓V�C���m�F")
                .AssertReply((activity) =>
                {
            // �A�_�v�e�B�u�J�[�h���r
            Assert.AreEqual(
                        JObject.Parse((activity as Activity).Attachments[0].Content.ToString()).ToString(),
                        JObject.Parse(File.ReadAllText("./AdaptiveJsons/Weather.json").Replace("{0}", "����")).ToString()
                    );
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToHelpDialog()
        {
            var arrange = ArrangeTest(false);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("�w���v", "�V�C�Ɨ\�肪�m�F�ł��܂��B")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task MyBot_ShouldGoToSelectLanguageDialogWithConversationUpdateWithoutUserProfile()
        {
            var arrange = ArrangeTest(false);

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
        public async Task MyBot_ShouldGoToMenuDialogWithConversationUpdateWithUserProfile()
        {
            var arrange = ArrangeTest(true);

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
                .AssertReply($"�悤���� '{name}' ����I")
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
        public async Task MyBot_ShouldWelcomeAndMenuDialogWithMessage()
        {
            var arrange = ArrangeTest(true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", $"�悤���� '{name}' ����I")
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
        public async Task MyBot_ShouldGoToPhotoUpdateDialog()
        {
            var arrange = ArrangeTest(true);
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
        public async Task MyBot_GlobalCommand_ShouldCancelAllDialog()
        {
            var arrange = ArrangeTest(true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", $"�悤���� '{name}' ����I")
                .AssertReply((activity) =>
                {
            //// Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
            //var turnContext = new TurnContext(arrange.adapter, activity as Activity);
            //// �_�C�A���O�R���e�L�X�g���擾
            //var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
            //// ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
            //var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
            //Assert.AreEqual(dialogInstances[0].Id, "choice");
        })
                .Send("�V�C���m�F")
                .AssertReply((activity) =>
                {
            //    // Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
            //    var turnContext = new TurnContext(arrange.adapter, activity as Activity);
            //    // �_�C�A���O�R���e�L�X�g���擾
            //    var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
            //    // ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ WeatherDialog �� choice �ł��邱�Ƃ��m�F�B
            //    var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WeatherDialog)).First().State["dialogs"] as DialogState).DialogStack;
            //    Assert.AreEqual(dialogInstances[0].Id, "choice");
        })
                .Test("�L�����Z��", "�L�����Z�����܂�")
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
        public async Task MyBot_GlobalCommand_ShouldGoToProfileDialog()
        {
            var arrange = ArrangeTest(true);

            // �e�X�g�̒ǉ��Ǝ��s
            await arrange.testFlow
                .Test("foo", $"�悤���� '{name}' ����I")
                .AssertReply((activity) =>
                {
            //// Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
            //var turnContext = new TurnContext(arrange.adapter, activity as Activity);
            //// �_�C�A���O�R���e�L�X�g���擾
            //var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
            //// ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ MenuDialog �� choice �ł��邱�Ƃ��m�F�B
            //var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(MenuDialog)).First().State["dialogs"] as DialogState).DialogStack;
            //Assert.AreEqual(dialogInstances[0].Id, "choice");
        })
                .Send("�V�C���m�F")
                .AssertReply((activity) =>
                {
            //// Activity �ƃA�_�v�^�[����R���e�L�X�g���쐬
            //var turnContext = new TurnContext(arrange.adapter, activity as Activity);
            //// �_�C�A���O�R���e�L�X�g���擾
            //var dc = arrange.dialogs.CreateContextAsync(turnContext).Result;
            //// ���݂̃_�C�A���O�X�^�b�N�̈�ԏオ WeatherDialog �� choice �ł��邱�Ƃ��m�F�B
            //var dialogInstances = (dc.Stack.Where(x => x.Id == nameof(WeatherDialog)).First().State["dialogs"] as DialogState).DialogStack;
            //Assert.AreEqual(dialogInstances[0].Id, "choice");
        })
                .Send("�v���t�@�C���̕ύX")
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
