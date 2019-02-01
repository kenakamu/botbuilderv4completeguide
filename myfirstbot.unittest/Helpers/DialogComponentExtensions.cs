using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using System.Reflection;

namespace myfirstbot.unittest.Helpers
{
    public static class DialogComponentExtensions
    {
        public static void ReplaceDialog(this ComponentDialog componentDialog, Dialog dialog)
        {
            var field = typeof(ComponentDialog).GetField("_dialogs", BindingFlags.Instance | BindingFlags.NonPublic);
            var dialogSet = field.GetValue(componentDialog) as DialogSet;
            field = typeof(DialogSet).GetField("_dialogs", BindingFlags.Instance | BindingFlags.NonPublic);
            var dialogs = field.GetValue(dialogSet) as Dictionary<string, Dialog>;
            dialogs[dialog.Id] = dialog;
        }
    }
}
