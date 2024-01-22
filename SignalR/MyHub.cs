using Microsoft.AspNetCore.SignalR;

namespace SignalR
{
    public class MyHub:Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"建立新的SignalR连接:{Context.ConnectionId}");
            RecordUser();
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"关闭SignalR连接:{Context.ConnectionId}|Exception:{exception?.Message}");
            RecordUser();
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        /// <summary>
        /// 记录
        /// </summary>
        private void RecordUser()
        {
            Console.WriteLine($"用户的ConnectionId：" + Context.ConnectionId + ";Name:" + Context.User.Identity.Name);
        }
    }
}