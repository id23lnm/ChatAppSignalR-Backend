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

        public async Task JoinChatRoom(string userName, string chatRoom, string role)
        {
            // Add user to main chat group
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoom);

            // Add user to separate announcements group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{chatRoom}_announcements");

            // Store user connection info
            _sharedDb.Connection[Context.ConnectionId] = new UserConnection { UserName = userName, ChatRoom = chatRoom, Role = role };

            await Clients.Group(chatRoom).SendAsync("ReceiveMessage", "admin", $"{userName} ({role}) has joined the chat room {chatRoom}", true);
        }

        public async Task SendMessage(string chatRoom, string userName, string message)
        {
            await Clients.Group(chatRoom).SendAsync("ReceiveMessage", userName, message);
        }

        public async Task SendAnnouncement(string chatRoom, string message)
        {
            if (_sharedDb.Connection.TryGetValue(Context.ConnectionId, out var user))
            {
                // Only allow teachers to send announcements
                if (user.Role != "Teacher")
                {
                    return;
                }

                // Send message to announcement group using a separate event
                await Clients.Group($"{chatRoom}_announcements").SendAsync("ReceiveAnnouncement", user.UserName, message);
            }
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
