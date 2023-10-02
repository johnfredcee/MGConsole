﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MGInput;
using MGUtilities;

namespace MGConsole
{
    partial class ConsoleInput : IConsoleInput
    {
        public event EventHandler InputChanged;
        public event EventHandler PrefixChanged;
        private readonly SpriteFontStringBuilder _inputBuffer = new SpriteFontStringBuilder();
        private bool _dirty = true;
        private string _inputPrefix;
        private int _numPosToMoveWhenOutOfScreen;
        public int VisibleStartIndex { get; private set; }
        public int VisibleLength { get; private set; }
        private bool _loaded;
        public Console Console { get; private set; }
        public IInputManager InputManager { get { return Console.InputManager; } }
        public InputState Input { get { return Console.InputManager.InputState; } }
        public Caret Caret { get; } = new Caret();
        public InputHistory InputHistory { get; } = new InputHistory();
        public Autocompletion Autocompletion { get; } = new Autocompletion();
        public CopyPasting CopyPasting { get; } = new CopyPasting();
        public Movement Movement { get; } = new Movement();
        public Tabbing Tabbing { get; } = new Tabbing();
        public Deletion Deletion { get; } = new Deletion();
        public CommandExecution CommandExecution { get; } = new CommandExecution();
        public CaseSensitivity CaseSenitivity { get; } = new CaseSensitivity();
        public Selection Selection { get; } = new Selection();

        public Dictionary<Keys, InputSymbol> SymbolMappings
        {
            get { return Console.InputManager.SymbolMappings; }
        }

        public string LastAutocompleteEntry
        {
            get { return Autocompletion.LastAutocompleteEntry; }
            set { Autocompletion.LastAutocompleteEntry = value; }
        }

        public int CaretIndex
        {
            get { return Caret.Index; }
            set { Caret.Index = value; }
        }

        public string InputPrefix
        {
            get { return _inputPrefix; }
            set
            {
                value = value ?? "";
                _inputPrefix = value;
                if (_loaded)
                    CalculateInputPrefixWidth();
                PrefixChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Color InputPrefixColor { get; set; }

        public int Length => _inputBuffer.Length;

        public int NumPositionsToMoveWhenOutOfScreen
        {
            get { return _numPosToMoveWhenOutOfScreen; }
            set { _numPosToMoveWhenOutOfScreen = Math.Max(value, 1); }
        }

        public Vector2 InputPrefixSize { get; private set; }

        public void LoadContent(Console console)
        {
			Console = console;
            Console.FontChanged += (s, e) =>
            {
                CalculateInputPrefixWidth();
                _inputBuffer.Font = console.Font;
                SetDirty();
            };
            _inputBuffer.Font = console.Font;
            Console.WindowAreaChanged += (s, e) => SetDirty();
            Caret.Moved += (s, e) => SetDirty();

            CalculateInputPrefixWidth();

            Caret.LoadContent(this);
            InputHistory.LoadContent(this);
            Autocompletion.LoadContent(this);
            CopyPasting.LoadContent(this);
            Movement.LoadContent(this);
            Tabbing.LoadContent(this);
            Deletion.LoadContent(this);
            CommandExecution.LoadContent(this);
            CaseSenitivity.LoadContent(this);
            Selection.LoadContent(this);

            _loaded = true;
        }

        public void Append(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            _inputBuffer.Insert(Caret.Index, value);
            Caret.MoveBy(value.Length);
            SetDirty();
            InputChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Remove(int startIndex, int length)
        {
            if (length <= 0) return;

            Caret.Index = startIndex;
            _inputBuffer.Remove(startIndex, length);
            SetDirty();
            InputChanged?.Invoke(this, EventArgs.Empty);
        }

        public string Value
        {
            get { return _inputBuffer.ToString(); } // Does not allocate if value is cached.
            set
            {
                if (Value != value)
                {
                    Clear();
                    if (value != null)
                        _inputBuffer.Append(value);
                    Caret.Index = _inputBuffer.Length;
                    SetDirty();
                    InputChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Vector2 MeasureSubstring(int startIndex, int length) => _inputBuffer.MeasureSubstring(startIndex, length);
        public string Substring(int startIndex, int length) => _inputBuffer.Substring(startIndex, length);
        public string Substring(int startIndex) => _inputBuffer.Substring(startIndex);

        public void Clear()
        {
            if (_inputBuffer.Length == 0) return;

            _inputBuffer.Clear();
            Caret.MoveBy(int.MinValue);
            SetDirty();
            InputChanged?.Invoke(this, EventArgs.Empty);
        }

        public char this[int i]
        {
            get { return _inputBuffer[i]; }
            set
            {
                if (_inputBuffer[i] != value)
                {
                    _inputBuffer[i] = value;
                    SetDirty();
                    InputChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            ProcessInput();

            Caret.Update(deltaSeconds);
            if (_dirty)
            {
                CalculateStartAndEndIndices();
                _dirty = false;
            }
        }
        public void ProcessInput()
        {
            InputState input = Console.InputManager.InputState;
            if (InputManager.ActionsPressedThisUpdate.Count > 0)
            {
                foreach (var name in InputManager.ActionsPressedThisUpdate)
                {
                    ProcessAction(name, InputManager.GetAction(name));
                }
            }
            
            foreach(var symbol in InputManager.SymbolsPressedThisUpdate)
				ProcessSymbol(symbol);
        }
        public void ProcessAction(string name, InputAction action)
		{
            InputHistory.OnAction(name, action);
            Autocompletion.OnAction(name, action);
            CopyPasting.OnAction(name, action);
            Movement.OnAction(name, action);
            Tabbing.OnAction(name, action);
            Deletion.OnAction(name, action);
            CommandExecution.OnAction(name, action);
        }
        public void ProcessSymbol(InputSymbol symbol)
        {
            if (Selection.HasSelection)
                Remove(Selection.SelectionStart, Selection.SelectionLength);
            Append(CaseSenitivity.ProcessSymbol(symbol));
            InputHistory.OnSymbol(symbol);
        }
        public void Draw()
        {
            Selection.Draw();

            // Draw input prefix.
            var inputPosition = new Vector2(Console.Padding, Console.WindowArea.Y + Console.WindowArea.Height - Console.Padding - Console.FontSize.Y);
            Console.SpriteBatch.DrawString(
                Console.Font,
                InputPrefix,
                inputPosition,
                InputPrefixColor);
            // Draw input buffer.
            inputPosition.X += InputPrefixSize.X;
            if (_inputBuffer.Length > 0)
                Console.SpriteBatch.DrawString(Console.Font, _inputBuffer.Substring(VisibleStartIndex, VisibleLength), inputPosition, Console.FontColor);

            Caret.Draw();
        }

        public void SetDefaults(ConsoleSettings settings)
        {
            InputPrefix = settings.InputPrefix;
            InputPrefixColor = settings.InputPrefixColor;
            NumPositionsToMoveWhenOutOfScreen = settings.NumPositionsToMoveWhenOutOfScreen;
            Selection.Color = settings.SelectionColor;

            Caret.SetSettings(settings);
        }

        public override string ToString() => Value;

        private void CalculateInputPrefixWidth()
        {
            InputPrefixSize = Console.Font.MeasureString(InputPrefix) + new Vector2(Console.Font.Spacing, 0);
        }

        private void CalculateStartAndEndIndices()
        {
            float windowWidth = Console.WindowArea.Width - Console.Padding * 2 - InputPrefixSize.X;

            if (CaretIndex > Length - 1)
                windowWidth -= Caret.Width;

            while (CaretIndex <= VisibleStartIndex && VisibleStartIndex > 0)
                VisibleStartIndex = Math.Max(VisibleStartIndex - NumPositionsToMoveWhenOutOfScreen, 0);

            float widthProgress = 0f;
            VisibleLength = 0;
            int indexer = VisibleStartIndex;
            int targetIndex = CaretIndex;
            while (indexer < Length)
            {
                char c = this[indexer++];

                float charWidth;
                if (!Console.CharWidthMap.TryGetValue(c, out charWidth))
                {
                    charWidth = Console.Font.MeasureString(c.ToString()).X + Console.Font.Spacing;
                    Console.CharWidthMap.Add(c, charWidth);
                }

                widthProgress += charWidth;

                if (widthProgress > windowWidth)
                {
                    if (targetIndex >= VisibleStartIndex && targetIndex - VisibleStartIndex < VisibleLength || indexer - 1 == VisibleStartIndex)
                        break;

                    if (targetIndex >= VisibleStartIndex)
                    {
                        VisibleStartIndex += NumPositionsToMoveWhenOutOfScreen;
                        VisibleStartIndex = Math.Min(VisibleStartIndex, Length - 1);
                    }
                    CalculateStartAndEndIndices();
                    break;
                }

                VisibleLength++;
            }
        }

        private void SetDirty() => _dirty = true;
    }
}
