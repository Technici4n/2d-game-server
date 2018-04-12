using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GameServer
{
    class Program
    {
        object _lock = new Object(); // sync lock 
        List<Task> _connections = new List<Task>(); // pending connections
        BufferBlock<NetworkEvent> eventQueue = null;
        int currentGame = 0;

        // The core server task
        private async Task StartListener()
        {
            var address = Console.ReadLine();
            var tcpListener = new TcpListener(IPAddress.Parse(address), 3232);
            tcpListener.Start();
            while (true)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync();
                var queue = eventQueue;
                Task task;
                Console.WriteLine("[Server] Client has connected.");
                if (eventQueue == null)
                {
                    currentGame++;
                    eventQueue = new BufferBlock<NetworkEvent>();
                    task = StartHandleConnectionAsync(tcpClient, eventQueue);
                }
                else
                {
                    eventQueue.Post(new PlayerJoinEvent(tcpClient));
                    eventQueue = null;
                    task = null;
                }
                Console.WriteLine($"[Server] He was assigned to game {currentGame}.");
                // if already faulted, re-throw any error on the calling context
                if (task != null && task.IsFaulted)
                    task.Wait();
            }
        }

        // Register and handle the connection
        private async Task StartHandleConnectionAsync(TcpClient tcpClient, ISourceBlock<NetworkEvent> eventQueue)
        {
            // start the new connection task
            var connectionTask = Task.Factory.StartNew(async () =>
            {
                await Task.Yield(); // continue asynchronously on another thread
                var game = new NetworkedGame(tcpClient, eventQueue);
                await game.Run();
            }).Unwrap();

            // add it to the list of pending task 
            lock (_lock)
                _connections.Add(connectionTask);

            // catch all errors of HandleConnectionAsync
            try
            {
                await connectionTask;
                // we may be on another thread after "await"
            }
            catch (Exception ex)
            {
                // log the error
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                // remove pending task
                lock (_lock)
                    _connections.Remove(connectionTask);
            }
        }

        // The entry point of the console app
        static void Main(string[] args)
        {
            Console.WriteLine("Hit Ctrl-C to exit.");
            new Program().StartListener().Wait();
        }
    }
}
