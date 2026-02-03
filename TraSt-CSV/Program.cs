// Copyright © 2025 Triamec Motion AG

namespace TraSt_CSV {
    internal class Program {
        static void Main() {
            Controller? controller = new();               // create a new instance of the Controller class
            controller.Initialize().GetAwaiter().GetResult();       // initialize the controller (connect to motion system, prepare axis group and trajectory streamer)
            controller.StreamCSV().GetAwaiter().GetResult();        // start the streaming of the CSV file
        }
    }
}
