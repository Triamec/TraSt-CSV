using Triamec.Tam;


namespace TraSt_CSV {
    internal class StreamingAbortListener {
        public required TrajectoryStreaming Streaming { get; init; }
        private Thread? _inputThread;
        public bool AbortedByQ { get; private set; } = false;
        public bool AbortedByError { get; private set; } = false;

        public void AbortByError() {
            AbortedByError = true;
            Streaming.Stop();
            Console.WriteLine("\nAborted streaming due to error...");
        }
        public bool IsAborted => AbortedByQ || AbortedByError;

        public void Start() {
            _inputThread = new Thread(ListenForAbortKey) {
                IsBackground = true
            };
            _inputThread.Start();

            Console.WriteLine("\n\nStreaming can always be aborted by pressing the key \"q\".");
        }

        private void ListenForAbortKey() {
            while (true) {
                var key = Console.ReadKey(intercept: true);
                if (key.KeyChar == 'q') {
                    Console.WriteLine("\n\nAborted streaming safely by pressing 'q'...");
                    Streaming.Stop();
                    AbortedByQ = true;
                    break;
                }
                Thread.Sleep(100);
            }
        }
    }
}
