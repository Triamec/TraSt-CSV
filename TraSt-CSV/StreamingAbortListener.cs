// Copyright © 2025 Triamec Motion AG

namespace TraSt_CSV {
    internal class StreamingAbortListener {
        Thread? _inputThread;
        bool _abortedByUserInput;
        bool _abortedByError;
        bool _listenToExit;
        public required Controller Controller { get; init; }
        public bool Exit { get; private set; }
        public bool ListenToExit {
            get => _listenToExit;
            set {
                _listenToExit = value;
                if (value == true) {
                    Console.WriteLine("\nPress 'q' or 'ESC' to quit application.");
                }
            }
        }
        public bool IsAborted => _abortedByUserInput || _abortedByError;

        public void AbortByError(Exception ex) {
            _abortedByError = true;
            Controller.Dispose();
            Console.WriteLine($"\nAborted streaming due to error: {ex.Message}");
        }

        public void Start() {
            _inputThread = new Thread(ListenForAbortKey) {
                IsBackground = true
            };
            _inputThread.Start();

            Console.WriteLine("\nPress 'q' or 'ESC' to abort streaming.");
        }

        private void ListenForAbortKey() {
            while (true) {
                var key = Console.ReadKey(intercept: true);
                if ((key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape) && Controller.Streamer != null && Controller.Streamer.IsStreaming && ListenToExit == false && _abortedByUserInput == false) {
                    Console.WriteLine("\n\nAborted streaming safely by pressing 'q or 'ESC'.");
                    Controller.Dispose();
                    _abortedByUserInput = true;
                }
                if ((key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape) && ListenToExit) {
                    Controller.Dispose();
                    Exit = true;
                }
                Thread.Sleep(100);
            }
        }
    }
}
