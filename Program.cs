using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server {

    class Program {

        //la lista mi serve per mandare i messaggi in broadcast
        //la seconda per evitare che più processi accedano alla lista contemporaneamente
        private readonly object balanceLock = new ();
        private List<TcpClient> listClients = new ();

        //inizializzo il socket e ogni volta che trovo una nuova connessione gli chiedo il nick
        public Program() {
            TcpListener listener = new TcpListener(IPAddress.Any, 5000);
            try{
                listener.Start();
                while (true) {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread t = new Thread(() => GetName(client));
                    t.Start();
                    lock (balanceLock) listClients.Add(client);
                }
            } finally { listener.Stop(); }
        }

        //preso il nick inizio ad ascoltare i suoi messaggi
        private void GetName(TcpClient client) {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            string data = Encoding.UTF8.GetString(buffer, 0, byteCount);
            broadcast(data + " si è connesso!!", client);
            Console.WriteLine(data + " si è connesso!!");
            HandleClients(client, stream);
        }

        //ascolta i messaggi di un client
        private void HandleClients(TcpClient client, NetworkStream stream) {
            while (true) {
                byte[] buffer = new byte[1024];
                int byteCount;
                try {
                    byteCount = stream.Read(buffer, 0, buffer.Length);
                } catch (Exception e) { byteCount = 0; }
                if (byteCount == 0) break;
                string data = Encoding.UTF8.GetString(buffer, 0, byteCount);
                broadcast(data, client);
                Console.WriteLine(data);
            }
            lock (balanceLock) listClients.Remove(client);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
            Console.ReadKey();
        }

        //manda un messaggio a tutti i client tranne il mandante
        private void broadcast(string data, TcpClient client) {
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            lock (balanceLock) listClients.Where(tcp => !tcp.Equals(client)).Select(tcp => tcp.GetStream())
                    .ToList().ForEach(s => s.Write(buffer, 0, buffer.Length));
        }

        //avvia il server
        public static void Main(string[] args) {
            new Program();
        }

    }

}
