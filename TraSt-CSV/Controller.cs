// Copyright © 2025 Triamec Motion AG
using Triamec.Tam.Motion;
using Triamec.Tam.Motion.TrajectoryStreaming;

namespace TraSt_CSV {
    internal class Controller : IDisposable {
        const int _maxPointsPerSegment = 500000;                                                              // maximum number of points per segment
        readonly double _streamRate;
        const string _filePath = "TraSt_TestCircle_50kHz_20s.csv";
        private readonly CsvParser _csvParser;
        IMotionSystem? _system;
        IAxisGroup<IAxis>? _axisGroup;
        StreamingAbortListener? _abortListener;

        public Controller() {
            _csvParser = new CsvParser(_filePath);
            _streamRate = _csvParser.SamplingRate ?? 10000;                                                  // default to 1000 if not specified in CSV file, which is 1ms between points
        }

        public void Dispose() {

            if (Streamer != null) {
                Streamer.StopStreaming();
                var cnt = 0;
                while (Streamer.IsStreaming && cnt++ < 20) {
                    Thread.Sleep(100);
                }

                Streamer.Dispose();
                Streamer = null;
            }

            _system?.Dispose();
            _system = null; 
        }
        public ITrajectoryStreamer? Streamer { get; private set; }

        public async Task Initialize() {
            Console.WriteLine("\nConnect to System... ");
            _system = await MotionSystem.Connect();
            var allFoundAxes = _system.Axes;
            if (allFoundAxes.Count < _csvParser.columnsName.Count) {                                       // check if the number of axes found is less than the number of columns in the CSV file
                Console.WriteLine($"\nOnly {allFoundAxes.Count} axes found, but the CSV-File requests to control {_csvParser.columnsName.Count} axes.");
                Console.WriteLine($"The application continues with {allFoundAxes.Count} axes and ignores the remaining content of the CSV-File:");
            }
            string[] axesToBeGrouped = [];
            for (int i = 0; i < _csvParser.columnsName.Count && i < allFoundAxes.Count; i++) {                // iterate through the found axes, but only to the number of columns in the CSV file                    
                Console.WriteLine($"...connection to {allFoundAxes[i].Name}");
                axesToBeGrouped = [.. axesToBeGrouped, allFoundAxes[i].Name];                               // add the name of the axis to the list of axes to be grouped
            }
            _axisGroup = await allFoundAxes.CreateGroup(axesToBeGrouped);

            await _axisGroup.Enable().WaitAsync(TimeSpan.FromSeconds(5));                                   // enable all axis in the axis group, so that they can execute commands. Wait for up to 5 seconds for the axes to be enable
            await _axisGroup.Home().WaitAsync(TimeSpan.FromMinutes(2));                                     // home all axes in the axis group.This is required to ensure that all axes are in a known position before starting the trajectory streaming.
            Streamer = await _axisGroup
                .PrepareTrajectoryStreaming()
                .SetStreamRate((uint)_streamRate)
                .CreateStreamer();

            _abortListener = new StreamingAbortListener() {                                                 // Create a listener for aborted streaming
                Controller = this
            };
            _abortListener.Start();                                                                         // Start listening for abort key ('q' or 'ESC') presses in a separate thread.
        }

        public async Task StreamCSV() {
            if (_axisGroup == null || Streamer == null || _abortListener == null) {
                throw new InvalidOperationException("Controller not initialized. Call Initialize() before StreamCSV().");
            }
            int totalSegments = (int)Math.Ceiling((double)_csvParser.NumberOfRows / _maxPointsPerSegment);
            int currentSegment = 0;
            while (_csvParser.HasMoreData() && !_abortListener.IsAborted) {                                 // main loop: read and stream the CSV file until there is no more data or abort requested
                Console.WriteLine();
                double[,] positions = _csvParser.ReadSegment(_maxPointsPerSegment, _axisGroup.Count);  // read one segment from CSV (up to maxPointsPerSegment points) and store it in a 2D array, where each row is a point and each column is an axis value
                                                                                                       // 
                while (true && !_abortListener.IsAborted) {
                    try {
                        bool ok = Streamer.Move(positions, null, !_csvParser.HasMoreData());
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

            if (!_abortListener.IsAborted && Streamer.IsStreaming) {
                Console.WriteLine("\n\nWaiting for streaming to complete...");
            }

            while (!_abortListener.IsAborted && Streamer.IsStreaming) {                                    // wait until the streaming is completed or aborted
                Console.Write(".");
                Thread.Sleep(1000);
            }

            if (!_abortListener.IsAborted) {
                Console.WriteLine("\n\nStreaming completed successfully.");
            }

            await _axisGroup.Disable().WaitAsync(TimeSpan.FromSeconds(5));                                   // disable the axis group after streaming is completed

            _abortListener.ListenToExit = true;
            while (true) {
                Thread.Sleep(100);
                if (_abortListener.Exit) {
                    break;
                }
            }
        }
    }
}
