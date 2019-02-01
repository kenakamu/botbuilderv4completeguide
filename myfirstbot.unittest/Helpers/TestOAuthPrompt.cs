using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace myfirstbot.unittest.Helpers
{
    public class TestOAuthPrompt : Dialog
    {
        private const string token = "dummyToken";
        // regex to check if code supplied is a 6 digit numerical code (hence, a magic code).
        private readonly Regex _magicCodeRegex = new Regex(@"(\d{6})");

        private OAuthPromptSettings _settings;
        private PromptValidator<TokenResponse> _validator;

        public TestOAuthPrompt(string dialogId, OAuthPromptSettings settings, PromptValidator<TokenResponse> validator = null)
            : base(dialogId)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _validator = validator;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            PromptOptions opt = null;
            if (options != null)
            {
                if (options is PromptOptions)
                {
                    // Ensure prompts have input hint set
                    opt = options as PromptOptions;
                    if (opt.Prompt != null && string.IsNullOrEmpty(opt.Prompt.InputHint))
                    {
                        opt.Prompt.InputHint = InputHints.ExpectingInput;
                    }

                    if (opt.RetryPrompt != null && string.IsNullOrEmpty(opt.RetryPrompt.InputHint))
                    {
                        opt.RetryPrompt.InputHint = InputHints.ExpectingInput;
                    }
                }
                else
                {
                    throw new ArgumentException(nameof(options));
                }
            }

            // Attempt to get the users token
            var output = await GetUserTokenAsync(dc.Context, cancellationToken).ConfigureAwait(false);
            if (output != null)
            {
                // Return token
                return await dc.EndDialogAsync(output, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Prompt user to login
                await SendOAuthCardAsync(dc.Context, opt?.Prompt, cancellationToken).ConfigureAwait(false);
                return Dialog.EndOfTurn;
            }
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            // Recognize token
            var recognized = await RecognizeTokenAsync(dc.Context, cancellationToken).ConfigureAwait(false);
            return await dc.EndDialogAsync(recognized.Value, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a token for a user signed in.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of the conversation with the user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<TokenResponse> GetUserTokenAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new TokenResponse() { Token = token };
        }

        /// <summary>
        /// Sign Out the User.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of the conversation with the user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SignOutUserAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
        }

        private async Task SendOAuthCardAsync(ITurnContext turnContext, IMessageActivity prompt, CancellationToken cancellationToken = default(CancellationToken))
        {
            BotAssert.ContextNotNull(turnContext);

            if (!(turnContext.Adapter is TestAdapter adapter))
            {
                throw new InvalidOperationException("OAuthPrompt.Prompt(): not supported by the current adapter");
            }

            // Ensure prompt initialized
            if (prompt == null)
            {
                prompt = Activity.CreateMessageActivity();
            }

            if (prompt.Attachments == null)
            {
                prompt.Attachments = new List<Attachment>();
            }

            if (!prompt.Attachments.Any(a => a.Content is OAuthCard))
            {
                prompt.Attachments.Add(new Attachment
                {
                    ContentType = OAuthCard.ContentType,
                    Content = new OAuthCard
                    {
                        Text = _settings.Text,
                        ConnectionName = _settings.ConnectionName,
                        Buttons = new[]
                        {
                            new CardAction
                            {
                                Title = _settings.Title,
                                Text = _settings.Text,
                                Type = ActionTypes.Signin,
                            },
                        },
                    },
                });
            }

            // Set input hint
            if (string.IsNullOrEmpty(prompt.InputHint))
            {
                prompt.InputHint = InputHints.ExpectingInput;
            }

            await turnContext.SendActivityAsync(prompt, cancellationToken).ConfigureAwait(false);
        }

        private async Task<PromptRecognizerResult<TokenResponse>> RecognizeTokenAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new PromptRecognizerResult<TokenResponse>();
            if (IsTokenResponseEvent(turnContext))
            {
                var tokenResponseObject = turnContext.Activity.Value as JObject;
                var token = tokenResponseObject?.ToObject<TokenResponse>();
                result.Succeeded = true;
                result.Value = token;
            }

            return result;
        }

        private bool IsTokenResponseEvent(ITurnContext turnContext)
        {
            var activity = turnContext.Activity;
            return activity.Type == ActivityTypes.Event && activity.Name == "tokens/response";
        }

        private bool ChannelSupportsOAuthCard(string channelId)
        {            
            return true;
        }
    }
}
