// Copyright © 2025 Triamec Motion AG
using SQLitePCL;
using Triamec.Tam;
using Triamec.TriaLink.Adapter;

namespace TraSt_CSV {
    internal class Controller {
        const int _maxPointsPerSegment = 500000;                                                              // maximum number of points per segment
        double _streamRate;
        const string _filePath = "TraSt_TestCircle_50kHz_20s.csv";
        readonly CsvParser _csvParser;
        readonly TrajectoryStreamingAxisGroups _axisGroup;
        TrajectoryStreaming _streaming;
        StreamingAbortListener _abortListener;


        public Controller() {
            _csvParser = new CsvParser(_filePath);
            _axisGroup = new TrajectoryStreamingAxisGroups("MyAxisGroup");
            _streamRate = _csvParser.SamplingRate ?? 10000;                                                  // default to 1000 if not specified in CSV file, which is 1ms between points
        }

        public void Initialize() {
            Console.WriteLine("\nSet up TamSystem... ");
            TamTopology topology = new TamTopology();                                                       // create the root object representing the topology of the TAM hardware
            TamSystem system = topology.AddLocalSystem(DataLinkLayers.Network);                             // adds the local TAM on this PC to the topology, but looks only for the network layer
            system.Identify();                                                                              // identify the system, which will connect to the TAM hardware and discover all axes

            TamAxis[]? allFoundAxes = topology.AsDepthFirst<TamAxis>().ToArray();                           // create a list with all axes found in the topology

            if (allFoundAxes.Length < _csvParser.columnsName.Count) {                                       // check if the number of axes found is less than the number of columns in the CSV file
                Console.WriteLine($"\nOnly {allFoundAxes.Length} axes found, but the CSV-File requests to control {_csvParser.columnsName.Count} axes.");
                Console.WriteLine($"The application continues with {allFoundAxes.Length} axes and ignores the remaining content of the CSV-File:");
            }
                                      
            for (int i = 0; i < _csvParser.columnsName.Count && i < allFoundAxes.Length; i++) {             // iterate through the found axes, but only to the number of columns in the CSV file                    
                allFoundAxes[i].Drive.ResetFault();
                _axisGroup.AddAxis(new TraStAxis(allFoundAxes[i]));                                         // create a new TraStAxis instance based on the found axis and add it to the axis group
                Console.WriteLine($"...connection to {allFoundAxes[i].Name} of station {allFoundAxes[i].Drive.Station.Name} and add to axisGroup.");
            }


            //TODO: Is OverrideControlSystem really required? And if yes, maybe possible inside axisGroup?
            foreach (TraStAxis axis in _axisGroup.Axes) {                                                   // tell all axis in axis group, that we're going to take control. Otherwise, the axis might reject our commands
                axis.SetOverrideControlSystem(true);
            }
            _axisGroup.Enable().WaitForSuccess(TimeSpan.FromSeconds(5));                                    // enable all axis in the axis group, so that they can execute commands. Wait for up to 5 seconds for the axes to be enable
            _axisGroup.Home();                                                                              // home all axes in the axis group.This is required to ensure that all axes are in a known position before starting the trajectory streaming.
            
            _streaming = new TrajectoryStreaming {
                AxisGroup = _axisGroup,                                                                     // define which group of axes should execute the streamed motion commands
                StreamRate = (uint)_streamRate,                                                             // interval between streamed points in microseconds
                Override = 100f,                                                                            // speed override in percent, 100% means the speed is not overridden
            };
            _abortListener = new StreamingAbortListener() {                                                 // Create a listener for aborted streaming
                Streaming = _streaming
            };
            _abortListener.Start();                                                                         // Start listening for abort key ('q' or 'ESC') presses in a separate thread.
        }

        public void StreamCSV() {
            int totalSegments = (int)Math.Ceiling((double)_csvParser.NumberOfRows / _maxPointsPerSegment);
            int currentSegment = 0;
            while (_csvParser.HasMoreData() && !_abortListener.IsAborted) {                                 // main loop: read and stream the CSV file until there is no more data or abort requested
                Console.WriteLine();
                double[,] positions = _csvParser.ReadSegment(_maxPointsPerSegment, _axisGroup.Axes.Count);  // read one segment from CSV (up to maxPointsPerSegment points) and store it in a 2D array, where each row is a point and each column is an axis value
                                                                                                            // 
                while (true && !_abortListener.IsAborted) {
                    try {
                        bool ok = _streaming.Move(positions, null, !_csvParser.HasMoreData());
                        if (ok) {
                            currentSegment++;
                            Console.Write($"\rSegment {currentSegment}/{totalSegments} added in the streaming buffer.");
                            break;                                                                          // sending to streaming-buffer succeeded, proceed to next segment
                        } else {
                            Console.Write("\rStreaming buffer full, waiting...");
                            Thread.Sleep(10);                                                               // wait and retry if buffer is full
                        }
                    } catch (Exception ex) {                                                                // if move throws error, mark as aborted due to error and exit
                        _abortListener.AbortByError(ex);
                        break;
                    }
                }
            }

            if (_streaming.IsStreaming && !_abortListener.IsAborted) {
                Console.WriteLine("\n\nWaiting for streaming to complete...");
            }

            while (_streaming.IsStreaming && !_abortListener.IsAborted) {                                    // wait until the streaming is completed or aborted
                Console.Write(".");
                Thread.Sleep(1000);
            }

            if (!_abortListener.IsAborted) {
                Console.WriteLine("\n\nStreaming completed successfully.");
            }

            _axisGroup.Disable().WaitForSuccess(TimeSpan.FromSeconds(5));                                   // disable the axis group after streaming is completed

            _abortListener.ListenToExit = true;
            while (true) {
                Thread.Sleep(100);
                if(_abortListener.Exit) {
                    break;
                }
            }
        }
    }
}
