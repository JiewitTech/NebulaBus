using NebulaBus.Rabbitmq;
using System;

namespace NebulaBus
{
    public class NebulaOptions
    {
        internal RabbitmqOptions RabbitmqOptions { get; }

        public NebulaOptions()
        {
            RabbitmqOptions = new RabbitmqOptions();
        }

        public void UseRabbitmq(Action<RabbitmqOptions> optionsAction)
        {
            optionsAction(RabbitmqOptions);
        }
    }
}