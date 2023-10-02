using System;
using MGInput;

namespace MGConsole
{
    class Tabbing
    {
        private ConsoleInput _input;

        public void LoadContent(ConsoleInput input) => _input = input;

        public void OnAction(string name, InputAction action)
        {
			if (name == "ConsoleAction.Tab")
			{
				_input.Append(_input.Console.TabSymbol);
			}
			if (name == "ConsoleAction.RemoveTab")
			{
				RemoveTab();
			}
        }

        public void RemoveTab()
        {
            bool isTab = true;
            int counter = 0;
            string tabSymbol = _input.Console.TabSymbol;
            for (int i = _input.Caret.Index - 1; i >= 0; i--)
            {
                if (counter >= tabSymbol.Length) break;
                if (_input[i] != tabSymbol[tabSymbol.Length - counter++ - 1])
                {
                    isTab = false;
                    break;
                }
            }
            int numToRemove = counter;
            if (isTab)
                _input.Remove(Math.Max(0, _input.Caret.Index - tabSymbol.Length), numToRemove);
        }
    }
}
