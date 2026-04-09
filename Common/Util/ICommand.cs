using Kodnix.Character;

namespace EggLink.DanhengServer.Util
{
    public static class IConsole
    {
        public const string PrefixContent = "> ";
        private const string PinkColor = "\e[38;2;255;192;203m";
        private const string RedColor = "\e[38;2;255;0;0m";
        private const string ResetColor = "\e[0m";

        // coloured prefix
        public static string Prefix => $"{(IsCommandValid ? ResetColor : RedColor)}{PrefixContent}{ResetColor}";

        public static bool IsCommandValid { get; private set; } = true;
        private const int HistoryMaxCount = 10;

        public static readonly object ConsoleLock = new();

        public static List<char> Input { get; set; } = [];
        private static int CursorIndex { get; set; }
        private static readonly List<string> InputHistory = [];
        private static int HistoryIndex = -1;

        public static event Action<string>? OnConsoleExcuteCommand;

        public static bool ForceDisable { get; set; } = false;

        private static bool? _isConsoleAvailable;
        public static bool IsConsoleAvailable
        {
            get
            {
                if (ForceDisable) return false;
                if (_isConsoleAvailable.HasValue) return _isConsoleAvailable.Value;

                try
                {
                    // Check TERM environment variable
                    var term = Environment.GetEnvironmentVariable("TERM");
                    if (string.IsNullOrEmpty(term) || term == "dumb")
                    {
                        _isConsoleAvailable = false;
                        return false;
                    }

                    _ = Environment.UserInteractive;
                    if (Console.IsInputRedirected || Console.IsOutputRedirected || Console.IsErrorRedirected)
                    {
                        _isConsoleAvailable = false;
                        return false;
                    }

                    // On Linux/Unix, WindowWidth might be 0 or throw if no TTY
                    if (Console.WindowWidth <= 0 || Console.BufferWidth <= 0)
                    {
                        _isConsoleAvailable = false;
                        return false;
                    }

                    // Verify cursor position check removed to prevent blocking
                    // _ = Console.CursorTop;

                    _isConsoleAvailable = true;
                    return true;
                }
                catch
                {
                    _isConsoleAvailable = false;
                    return false;
                }
            }
        }

        public static void InitConsole()
        {
            if (!IsConsoleAvailable) return;
            try { Console.Title = "Danheng Server"; } catch { }
        }

        public static int GetWidth(string str)
            => str.ToCharArray().Sum(EastAsianWidth.GetLength);

        public static void RedrawInput(List<char> input, bool hasPrefix = true)
            => RedrawInput(new string([.. input]), hasPrefix);

        public static void RedrawInput(string input, bool hasPrefix = true)
        {
            if (!IsConsoleAvailable) return;

            lock (ConsoleLock)
            {
                // check validity
                UpdateCommandValidity(input);

                var inputStr = input;
                if (hasPrefix)
                {
                    inputStr = Prefix + input;
                }

                var totalWidth = GetWidth(inputStr);
                var cursorEffectiveIndex = CursorIndex + (hasPrefix ? GetWidth(PrefixContent) : 0);

                // 1. Carriage Return to line start
                Console.Write('\r');
                // 2. Write the full line (with prefix if needed)
                Console.Write(inputStr);

                // 3. Clear the rest of the line safely
                int clearLen = Console.BufferWidth - totalWidth;
                if (clearLen < 0) clearLen = 0; // Prevent negative padding (safety)
                if (clearLen > 0)
                {
                    Console.Write(new string(' ', clearLen));
                }

                // 4. Return to line start and move cursor to correct column
                Console.Write('\r');
                // Ensure cursor column does not exceed buffer bounds
                int moveRight = cursorEffectiveIndex;
                if (moveRight >= Console.BufferWidth)
                {
                    moveRight = Console.BufferWidth - 1; // Clamp to last valid column
                }
                if (moveRight > 0)
                {
                    Console.Write($"\x1b[{moveRight}C");
                }
            }
        }

        // check validity and update
        private static void UpdateCommandValidity(string input)
        {
            IsCommandValid = CheckCommandValid(input);
        }

        #region Handlers

        public static void HandleEnter()
        {
            string cmdToRun = null;
            lock (ConsoleLock)
            {
                var input = new string([.. Input]);
                if (string.IsNullOrWhiteSpace(input)) return;

                // New line
                Console.WriteLine();
                Input = [];
                CursorIndex = 0;
                if (InputHistory.Count >= HistoryMaxCount)
                    InputHistory.RemoveAt(0);
                InputHistory.Add(input);
                HistoryIndex = InputHistory.Count;

                // Handle command
                if (input.StartsWith('/')) input = input[1..].Trim();
                cmdToRun = input;

                // reset
                IsCommandValid = true;
            }

            if (cmdToRun != null)
                OnConsoleExcuteCommand?.Invoke(cmdToRun);
        }

        public static void HandleBackspace()
        {
            lock (ConsoleLock)
            {
                if (CursorIndex <= 0) return;

                // Safety check
                if (CursorIndex > Input.Count) CursorIndex = Input.Count;

                CursorIndex--;
                Input.RemoveAt(CursorIndex);

                // Full redraw is safer than partial differential updates that require cursor reads
                RedrawInput(Input);
            }
        }

        public static void HandleUpArrow()
        {
            lock (ConsoleLock)
            {
                if (InputHistory.Count == 0) return;
                if (HistoryIndex <= 0) return;

                HistoryIndex--;
                var history = InputHistory[HistoryIndex];
                Input = [.. history];
                CursorIndex = Input.Count;

                // update
                UpdateCommandValidity(history);
                RedrawInput(Input);
            }
        }

        public static void HandleDownArrow()
        {
            lock (ConsoleLock)
            {
                if (HistoryIndex >= InputHistory.Count) return;

                HistoryIndex++;
                if (HistoryIndex >= InputHistory.Count)
                {
                    HistoryIndex = InputHistory.Count;
                    Input = [];
                    CursorIndex = 0;
                    IsCommandValid = true;
                }
                else
                {
                    var history = InputHistory[HistoryIndex];
                    Input = [.. history];
                    CursorIndex = Input.Count;
                    // update
                    UpdateCommandValidity(history);
                }
                RedrawInput(Input);
            }
        }

        public static void HandleLeftArrow()
        {
            lock (ConsoleLock)
            {
                if (CursorIndex <= 0) return;
                CursorIndex--;
                RedrawInput(Input);
            }
        }

        public static void HandleRightArrow()
        {
            lock (ConsoleLock)
            {
                if (CursorIndex >= Input.Count) return;
                CursorIndex++;
                RedrawInput(Input);
            }
        }

        public static void HandleInput(ConsoleKeyInfo keyInfo)
        {
            lock (ConsoleLock)
            {
                if (char.IsControl(keyInfo.KeyChar)) return;
                var newWidth = GetWidth(new string([.. Input])) + GetWidth(keyInfo.KeyChar.ToString());
                if (newWidth >= (Console.BufferWidth - GetWidth(PrefixContent))) return;
                HandleInput(keyInfo.KeyChar);
            }
        }

        public static void HandleInput(char keyChar)
        {
            lock (ConsoleLock)
            {
                // Crash fix: Bounds check
                if (CursorIndex < 0) CursorIndex = 0;
                if (CursorIndex > Input.Count) CursorIndex = Input.Count;

                Input.Insert(CursorIndex, keyChar);
                CursorIndex++;

                RedrawInput(Input);
            }
        }

        #endregion

        public static string ListenConsole()
        {
            if (!IsConsoleAvailable) return string.Empty;

            while (true)
            {
                ConsoleKeyInfo keyInfo;
                try { keyInfo = Console.ReadKey(true); }
                catch (InvalidOperationException) { continue; }

                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        HandleEnter();
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace();
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow();
                        break;
                    case ConsoleKey.RightArrow:
                        HandleRightArrow();
                        break;
                    case ConsoleKey.UpArrow:
                        HandleUpArrow();
                        break;
                    case ConsoleKey.DownArrow:
                        HandleDownArrow();
                        break;
                    default:
                        HandleInput(keyInfo);
                        break;
                }
            }
        }

        private static bool CheckCommandValid(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            var invalidChars = new[] { '@', '#', '$', '%', '&', '*' };
            return !invalidChars.Any(c => input.Contains(c));
        }
    }
}
