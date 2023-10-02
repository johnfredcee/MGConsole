using Microsoft.Xna.Framework.Input;
using MGInput;

namespace MGConsole
{
    class CaseSensitivity
    {
        private ConsoleInput _input;
        private bool _capsLockToggled;

        public void LoadContent(ConsoleInput input)
        {
            _input = input;
        }

        public string ProcessSymbol(InputSymbol symbol)
        {
            _capsLockToggled = _input.Input.IsKeyToggled(Keys.CapsLock);
            bool capsLockApplies = symbol.Lowercase.Length == 1 && char.IsLetter(symbol.Lowercase[0]) && _capsLockToggled;
		
			InputAction action = _input.InputManager.GetAction("ConsoleAction.UppercaseModifier");
            bool uppercaseModifierApplies = action.AreModifiersAppliedForAction(_input.Input);

            return capsLockApplies ^ uppercaseModifierApplies
                ? symbol.Uppercase
                : symbol.Lowercase;
        }

    }
}
