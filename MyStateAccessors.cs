using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

public class MyStateAccessors
{
    public MyStateAccessors(
        UserState userState,
        ConversationState conversationState)
    {
        UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
    }

    public IStatePropertyAccessor<UserProfile> UserProfile { get; set; }
    public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }
    public IStatePropertyAccessor<IList<Microsoft.Graph.Event>> Events { get; set; }
    public UserState UserState { get; }
    public ConversationState ConversationState { get; }
}