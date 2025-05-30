// Copyright © 2025 Triamec Motion AG
using Triamec.Tam;
using Triamec.TriaLink.Adapter;

namespace TraSt_CSV {
    internal class Program {
        static string _filePath = "TrajectoryStream_Test1.csv";
        
        static void Main(string[] args) {

            CsvParser _csvParser = new CsvParser();

            _csvParser.ParseHeader(_filePath);

            Console.WriteLine("The Header of the file was read and the following columns were found: ");
            foreach( var column in _csvParser.columnsName) {
                Console.WriteLine("- " +column);
            }

            Console.WriteLine($"Connecting with {_csvParser.columnsName.Count} axes to match the number of columns.");

        }


    }
}
