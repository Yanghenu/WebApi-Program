using DotNetCore.CAP;
using static CAP.ApplicationDbContext;

namespace CAP
{
    public class Publisher 
    {
        private readonly ICapPublisher _capPublisher;
        public Publisher(ICapPublisher capPublisher)
        {
            _capPublisher = capPublisher;
        }

        public void PublishMessage(string text)
        {
            var message = new MyMessage { Text = text };
            _capPublisher.Publish("your_topic", message); 
        }
    }
}
