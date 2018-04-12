using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GameServer
{
    class NetworkedGame
    {
        private GameState gameState;
        private TcpClient tcpClient1, tcpClient2;
        private ISourceBlock<NetworkEvent> eventQueue;

        public NetworkedGame(TcpClient tcpClient, ISourceBlock<NetworkEvent> eventQueue)
        {
            this.gameState = new GameState();
            this.tcpClient1 = tcpClient;
            this.tcpClient2 = null;
            this.eventQueue = eventQueue;
        }

        public async Task Run()
        {
            BroadcastPosition();
            while(true)
            {
                await Tick();
            }
        }

        private async Task Tick()
        {
            var buffer1 = new byte[4096];
            Task<int> taskReadFrom1 = tcpClient1 == null ? null : Task.Factory.StartNew(async () => await tcpClient1.GetStream().ReadAsync(buffer1, 0, buffer1.Length)).Unwrap();
            var buffer2 = new byte[4096];
            Task<int> taskReadFrom2 = tcpClient2 == null ? null : Task.Factory.StartNew(async () => await tcpClient2.GetStream().ReadAsync(buffer2, 0, buffer2.Length)).Unwrap();
            Task<NetworkEvent> taskReadFromQueue = Task.Factory.StartNew(async () => await eventQueue.ReceiveAsync()).Unwrap();
            /*{
                var ev = await eventQueue.ReceiveAsync();
                Console.WriteLine("Received Async !");
                return ev;
            }).Unwrap();*/

            var tasks = new List<Task>(new Task[] { taskReadFrom1, taskReadFrom2, taskReadFromQueue });
            tasks.RemoveAll(t => t == null);
            var task = await Task.WhenAny(tasks);
            
            if (taskReadFrom1 != null && taskReadFrom1.IsCompleted)
            {
                HandleMessageFromClient(tcpClient1, 1, buffer1, taskReadFrom1.Result);
            }
            if (taskReadFrom2 != null && taskReadFrom2.IsCompleted)
            {
                HandleMessageFromClient(tcpClient2, 2, buffer2, taskReadFrom2.Result);
            }
            if (taskReadFromQueue.IsCompleted)
            {
                Console.WriteLine("[Server] Read client from the queue.");
                var ev = (PlayerJoinEvent)taskReadFromQueue.Result;
                if (tcpClient1 == null)
                    tcpClient1 = ev.tcpClient;
                else if (tcpClient2 == null)
                    tcpClient2 = ev.tcpClient;
                else
                    throw new ApplicationException("A third client joined the 2-player game.");
                BroadcastPosition();
            }
        }

        private void HandleMessageFromClient(TcpClient client, int player, byte[] buffer, int byteCount)
        {
            if(player == 1)
            {
                var request = Encoding.UTF8.GetString(buffer, 0, byteCount);
                Console.WriteLine($"[Server] Client {player} wrote: \"{request}\"");

                var tokens = request.ToLower().Split();

                try
                {
                    if (tokens[0] == "moveu")
                    {
                        int x = int.Parse(tokens[1]);
                        int y = int.Parse(tokens[2]);
                        int id = int.Parse(tokens[3]);
                        gameState.Move(x, y);
                        BroadcastPosition();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private void BroadcastPosition()
        {
            gameState.GetPosition(out int nx, out int ny);
            var response = Encoding.UTF8.GetBytes($"unit {nx} {ny} 0\n");
#pragma warning disable 4014
            tcpClient1?.GetStream().WriteAsync(response, 0, response.Length);
            tcpClient2?.GetStream().WriteAsync(response, 0, response.Length);
#pragma warning restore 4014
        }
    }
}
