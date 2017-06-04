using System;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;

namespace SimpleWebServer {
    internal class Program {
        private static void Main(string[] args) {
            var uri =
                new Uri("http://localhost:1884");

            using (var host = new NancyHost(uri)) {
                host.Start();
                Console.WriteLine($"{DateTime.Now}    Simple web server running on {uri}");
                Console.WriteLine("Press [Enter] to close the host.");
                Console.ReadLine();
            }
        }
    }

    public class BrokerUrl {
        public string Url { get; set; }
    }

    public class Module : NancyModule {
        private static BrokerUrl brokerUrl = new BrokerUrl {
            Url = "tcp://iot.eclipse.org:1883"
        };

        public Module() {
            Get["/"] = _ => "Hello world!";
            Get["/broker_url"] = parameters => brokerUrl;
            Post["/broker_url"] = parameters => {
                var model = this.Bind<BrokerUrl>();
                brokerUrl = model;
                Console.WriteLine($"{DateTime.Now}    MQTT broker URL set to {brokerUrl.Url}");
                return model;
            };
        }
    }
}
