// Copyright © 2025 Triamec Motion AG

namespace TraSt_CSV {
    internal class Program {


        static void Main(string[] args) {
            Controller controller = new Controller();       // create a new instance of the Controller class
            controller.Initialize();
            controller.StreamCSV();                         // start the streaming of the CSV file

        }
    }
}
