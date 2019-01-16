using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

public class MyStateAccessors
{
    public MyStateAccessors(
        ConversationState conversationState
        )
    {
        ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
    }

    public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }
    public ConversationState ConversationState { get; }
}