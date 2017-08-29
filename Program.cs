using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace heartbeat
{
    class Program
    {
        static void Main(string[] args)
        {
            // file must be supplied
            var file = File.ReadAllLines(args[0]);

            // line 1 - ip address to check
            var ip = file[0].Trim();

            // line 2 - prior status (1/0 of on/off)
            var status = int.Parse(file[1].Trim());

            // line 3 - pushover apptoken
            var appToken = file[2].Trim();

            // line 4 - pushover usertoken
            var userToken = file[3].Trim();

            // nickname of computer to check
            var name = "Compy";

            Console.WriteLine($"Checking {name} heartbeat...");

            if (Up(ip).Result)
            {
                Console.WriteLine("Online");

                // notify and save if status changed
                if(status == 0)
                {
                    Notify($"{name} is online.", appToken, userToken).Wait();
                    status = 1;
                    Save(args[0], ip, status, appToken, userToken).Wait();
                }
                else
                {
                    Console.WriteLine("No change.");
                }
            }
            else // offline
            {
                Console.WriteLine("Offline");

                // Notify and save if status changed
                if(status == 1)
                {
                    Notify($"{name} is offline.", appToken, userToken).Wait();
                    status = 0;
                    Save(args[0], ip, status, appToken, userToken).Wait();
                }
                else
                {
                    Console.WriteLine("No change.");
                }
            }
        }

        // check ping
        // true if online, false if offline
        static async Task<bool> Up(string host)
        {
            Ping p = new Ping();
            var reply = await p.SendPingAsync(host);
            return reply.Status == IPStatus.Success;
        }

        // notify
        static async Task Notify(string message, string appToken, string userToken)
        {
            Console.WriteLine("Notifying of status change..");
            message += Environment.NewLine + DateTime.Now;

            Console.WriteLine(message);
            await PushOver(message, appToken, userToken);
        }

        // notify via pushover
        static async Task PushOver(string message, string appToken, string userToken)
        {
            Console.WriteLine("Responding via pushover..");
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.pushover.net");
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", appToken),
                    new KeyValuePair<string, string>("user", userToken),
                    new KeyValuePair<string, string>("message", message)
                });
                var result = await client.PostAsync("/1/message.json", content);
            }
        }

        // save status of machine
        static async Task Save(string fileName, string ip, int status, string appToken, string userToken)
        {
            Console.WriteLine("Saving..");
            
            var data = new[] { ip, status.ToString(), appToken, userToken };
            await File.WriteAllLinesAsync(fileName, data);
        }
    }
}
