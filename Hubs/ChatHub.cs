using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ChatApp.DataService;
using ChatApp.Models;

namespace ChatApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly SharedDb _sharedDb;

        public ChatHub(SharedDb sharedDb)
        {
            _sharedDb = sharedDb;
        }

        public async Task JoinChatRoom(string userName, string chatRoom)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoom);
            _sharedDb.Connection[Context.ConnectionId] = new UserConnection { UserName = userName, ChatRoom = chatRoom };

            await Clients.Group(chatRoom).SendAsync("ReceiveMessage", "admin", $"{userName} has joined the chat room {chatRoom}");
        }

        public async Task SendMessage(string chatRoom, string userName, string message)
        {
            await Clients.Group(chatRoom).SendAsync("ReceiveMessage", userName, message);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_sharedDb.Connection.TryGetValue(Context.ConnectionId, out var user))
            {
                await Clients.Group(user.ChatRoom).SendAsync("ReceiveMessage", "admin", $"{user.UserName} left");

                _sharedDb.Connection.TryRemove(Context.ConnectionId, out _);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
