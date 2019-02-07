using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using System.Reflection;

namespace myfirstbot.unittest.Helpers
{
    public static class MyBotExtensions
    {
        public static void ReplaceDialog(this MyBot bot, Dialog dialog)
        {
            var field = typeof(MyBot).GetField("dialogs", BindingFlags.Instance | BindingFlags.NonPublic);
            var dialogSet = field.GetValue(bot) as DialogSet;
            field = typeof(DialogSet).GetField("_dialogs", BindingFlags.Instance | BindingFlags.NonPublic);
            var dialogs = field.GetValue(dialogSet) as Dictionary<string, Dialog>;
            dialogs[dialog.Id] = dialog;
        }
    }
}
