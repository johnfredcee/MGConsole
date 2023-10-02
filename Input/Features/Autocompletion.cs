using MGInput;

namespace MGConsole
{
    class Autocompletion
    {
        private ConsoleInput _input;

        public string LastAutocompleteEntry { get; set; }

        public void LoadContent(ConsoleInput console) => _input = console;

        public void OnAction(string name, InputAction action)
        {
			if (name ==  "ConsoleAction.AutocompleteForward")
			{
				_input.Console.Interpreter.Autocomplete(_input, true);
			}
			if (name == "ConsoleAction.AutocompleteBackward")
			{
				_input.Console.Interpreter.Autocomplete(_input, false);
			}
			if ((name=="ConsoleAction.ExecuteCommand")
			|| (name=="ConsoleAction.Paste")
			|| (name=="ConsoleAction.Cut")
			|| (name=="ConsoleAction.Tab")
			|| (name=="ConsoleAction.NewLine"))
			{
				ResetAutocompleteEntry();
			}
			if (name == "ConsoleAction.DeletePreviousChar")
			{
				if (_input.Length > 0 && _input.Caret.Index > 0)
					ResetAutocompleteEntry();

			}
			if (name == "ConsoleAction.DeleteCurrentChar")
			{
				if (_input.Length > _input.Caret.Index)
					ResetAutocompleteEntry();

			}
		}

        public void OnSymbol(InputSymbol symbol)
        {
            ResetAutocompleteEntry();
        }

        private void ResetAutocompleteEntry()
        {
            LastAutocompleteEntry = null;
        }
    }
}
