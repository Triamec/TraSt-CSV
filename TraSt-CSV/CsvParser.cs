// Copyright © 2025 Triamec Motion AG

using System.Diagnostics;
using System.Transactions;

namespace TraSt_CSV {
    internal class CsvParser {

        public string? Profile { get; private set; }                                                        // Header-Line in csv file, which contains the profile name
        public double? SamplingRate { get; private set; }                                                   // Header-Line in csv file, which contains the sampling rate in seconds
        public double? Duration { get; private set; }                                                       // Header-Line in csv file, which contains the duration of the profile in seconds
        public int NumberOfRows { get; private set; }                                                       // Header-Line in csv file, which contains the number of rows in the profile
        public List<string> columnsName { get; private set; } = new List<string>();
        private readonly StreamReader _reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvParser"/> class and opens the specified CSV file for reading.
        /// Reads the header of the CSV file to extract the profile name, sampling time, and column names.
        /// </summary>
        /// <param name="filePath"></param>
        public CsvParser(string filePath) {
            _reader = new StreamReader(filePath);
            ParseHeader();
        }


        private void ParseHeader() {
            string? line;

            while ((line = _reader.ReadLine()) != null) {                                                                   

                string trimmedLine = line.Trim();

                if (!trimmedLine.StartsWith("#")) {                                                         // not a header line, so we stop reading the header
                    break;
                }

                if (trimmedLine.StartsWith("# Profile:")) {                                                                        
                    Profile = trimmedLine.Substring("# Profile:".Length).Trim();                            // extract and store the profile value after the prefix
                }

                else if (trimmedLine.StartsWith("# Sampling rate:")) {                                      // extract sampling time string after prefix
                    string samplingStr = trimmedLine.Substring("# Sampling rate:".Length).Trim();
                    if (samplingStr.EndsWith("Hz")) {
                        samplingStr = samplingStr.Substring(0, samplingStr.Length - 2);                     // remove trailing 's' if present (e.g. "5s" => "5")
                    }
                    if (double.TryParse(samplingStr, out double tempSamplingRate)) {
                        SamplingRate = tempSamplingRate;                                                    // parse the sampling rate and store it
                    }
                }
                
                else if (trimmedLine.StartsWith("# Number of rows:")) {
                    string numberOfRowsStr = trimmedLine.Substring("# Number of rows:".Length).Trim();
                    if(int.TryParse(numberOfRowsStr, out int numberOfRows)) {
                        NumberOfRows = numberOfRows;                                                        // parse the number of rows and store it
                    }                                 
                }

                else if (trimmedLine.StartsWith("# Column")) {
                    int colonIndex = trimmedLine.IndexOf(':');                                              // find the index of ":" in "Column 1: XYZ"
                    if (colonIndex != -1 && (colonIndex + 1 < trimmedLine.Length)) {                        // index of ":" is found and there is text after it
                        string columnName = trimmedLine.Substring(colonIndex + 1).Trim();                   // extract and trim the column name
                        columnsName.Add(columnName);                                                        // add it to the List
                    }
                }
            }

            Console.WriteLine("The Header of the file was read and the following columns were found: ");    // informs User
            foreach (var column in columnsName) {
                Console.WriteLine("- " + column);
            }

        }

        /// <summary>
        /// Reads up to maxPointPerSegment lines from the CSV file, each containing values for all axes in the axis group.
        /// Returns a trimmed array if fewer lines are read than maxPointsPerSegment.
        /// </summary>
        /// <param name="maxPointsPerSegment">Maximum number of lines to read from CSV file</param>
        /// <param name="numberColumns">Maximum number of columns to read from CSV file</param>
        /// <returns></returns>
        internal double[,] ReadSegment(int maxPointsPerSegment, int numberColumns) {
            double[,] buffer = new double[maxPointsPerSegment, numberColumns];                              // buffer to store parsed values: row = trajectory points and columns = axes in axis group
            int row = 0;

            while (row < maxPointsPerSegment) {
                string? line = _reader.ReadLine();
                if (line == null) { break; }                                                                // end of file reached

                string[] parts = line.Split(',');                                                           // Split the line by commas to get the individual values

                for (int col = 0; col < numberColumns; col++) {                                             // iterate through the individual values, unless there is more than columns
                    if (double.TryParse(parts[col], System.Globalization.NumberStyles.Float,                // try to parse the value as a double
                        System.Globalization.CultureInfo.InvariantCulture, out double value)) {
                        buffer[row, col] = value;                                                           // store the parsed value in the buffer
                    }
                }
                row++;
            }

            if (row < maxPointsPerSegment) {
                double[,] trimmed = new double[row, numberColumns];                                         // create a smaller array if fewer rows were read than maximalPointsPerSegment
                for (int r = 0; r < row; r++) {
                    for (int c = 0; c < numberColumns; c++) {
                        trimmed[r, c] = buffer[r, c];                                                       // copy the read values to the array with the correct size
                    }
                }
                return trimmed;
            }
            return buffer;
        }

        /// <summary>
        /// Checks if there is more data to read from the CSV file.
        /// </summary>
        /// <returns></returns>
        internal bool HasMoreData() {
            return !_reader.EndOfStream;
        }
    }
}

