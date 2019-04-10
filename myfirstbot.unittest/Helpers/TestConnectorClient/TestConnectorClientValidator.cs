using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;

namespace myfirstbot.unittest.Helpers
{
    public class TestConnectorClientValidator
    {
        private Queue<IActivity> storedActivities = new Queue<IActivity>();

        private IActivity GetNextActivity()
        {
            return storedActivities.Dequeue();
        }

        public void AddActivity(IActivity activity)
        {
            storedActivities.Enqueue(activity);
        }

        public TestConnectorClientValidator AssertReply(Action<IActivity> validateActivity)
        {
            validateActivity(GetNextActivity());
            return this;
        }
    }
}
