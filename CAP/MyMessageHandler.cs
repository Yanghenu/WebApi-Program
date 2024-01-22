using DotNetCore.CAP;
using static CAP.ApplicationDbContext;

namespace CAP
{
    public class MyMessageHandler : ICapSubscribe
    {
        private readonly ApplicationDbContext _dbContext;

        public MyMessageHandler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [CapSubscribe("your_topic")] 
        public void HandleMessage(MyMessage message)
        {
            // 处理接收到的消息
            // 将消息保存到数据库
            _dbContext.Messages.Add(new MessageEntity { Content = message.Text });
            //_dbContext.SaveChanges();
        }
    }
}
