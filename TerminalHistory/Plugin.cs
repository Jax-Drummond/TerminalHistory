using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine.InputSystem;
using static TerminalApi.Events.Events;
using System.Collections.Generic;

using LethalCompanyInputUtils.Api;

namespace TerminalHistory
{
	[BepInPlugin("atomic.terminalhistory", "Terminal History", "1.0.1")]
	[BepInDependency("atomic.terminalapi", MinimumDependencyVersion: "1.3.0")]
	[BepInDependency("com.rune580.LethalCompanyInputUtils", MinimumDependencyVersion: "0.4.2")]
	public partial class Plugin : BaseUnityPlugin
	{
		const int SIZE = 20;
		private Terminal Terminal;
		private List<string> _commands = new List<string>();
		// private InputAction _upArrow; //! old way
		// private InputAction _downArrow; //! old way
		private Keybinds _keybinds = new Keybinds();
		private int _index = -1;

		private void Awake()
		{
			Logger.LogInfo($"\n\n\nPlugin Terminal History is loaded!\n\n\n");
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
			TerminalStarted += OnTerminalStarted;
			TerminalExited += OnTerminalExited;
			TerminalBeginUsing += OnTerminalBeginUsing;
			TerminalParsedSentence += OnTerminalSubmit;
			TerminalTextChanged += OnTerminalTextChange;
		}

        private void OnTerminalTextChange(object sender, TerminalTextChangedEventArgs e)
        {
            if (e.CurrentInputText == "" && _index != -1 )
			{
				_index = -1;
			}
        }

        private void OnTerminalSubmit(object sender, TerminalParseSentenceEventArgs e)
		{

			if (_commands.Contains(e.SubmittedText))
			{
				_commands.Remove(e.SubmittedText);
			}
			
			_commands.Insert(0, e.SubmittedText);

			if(_commands.Count > SIZE) 
			{
				_commands.RemoveRange(SIZE, _commands.Count - SIZE);
			}

			_index = -1;
        }

        private void OnTerminalExited(object sender, TerminalEventArgs e)
        {
			_keybinds.NextTerminalKey.performed -= OnUpArrowPerformed;
			_keybinds.NextTerminalKey.Disable();

			_keybinds.PrevTerminalKey.performed -= OnDownArrowPerformed;
			_keybinds.PrevTerminalKey.Disable();
        }

        private void OnTerminalBeginUsing(object sender, TerminalEventArgs e)
        {
            _keybinds.NextTerminalKey.Enable();
            _keybinds.NextTerminalKey.performed += OnUpArrowPerformed;

            _keybinds.PrevTerminalKey.Enable();
            _keybinds.PrevTerminalKey.performed += OnDownArrowPerformed;
        }

        private void OnTerminalStarted(object sender, TerminalEventArgs e)
		{
			_commands.Clear();

			_keybinds.NextTerminalKey = new InputAction("UpArrow", 0, "<Keyboard>/uparrow", "Press");
			_keybinds.PrevTerminalKey = new InputAction("DownArrow", 0, "<Keyboard>/downarrow", "Press");

			Terminal = TerminalApi.TerminalApi.Terminal;

        }

        private void OnDownArrowPerformed(InputAction.CallbackContext context)
        {
			if (_commands.Count > 0 && Terminal.terminalInUse)
			{
				_index--;
				if (_index <= -1)
				{
					_index = -1;
                    SetTerminalText("");
                }
				else
				{
					string command = _commands[_index];
                    SetTerminalText(command);
                }
            }
        }

        private void OnUpArrowPerformed(InputAction.CallbackContext context)
        {
            if (Terminal.terminalInUse && _commands.Count > 0)
			{
				_index++;
				if (_index >= _commands.Count)
				{
					_index = _commands.Count - 1;
					string command = _commands[_commands.Count - 1];
                    SetTerminalText(command);
                }
				else
				{
					string command = _commands[_index];
                    SetTerminalText(command);
                }
            }
        }

		private void SetTerminalText(string text)
		{
			Terminal.TextChanged(TerminalApi.TerminalApi.Terminal.currentText.Substring(0, TerminalApi.TerminalApi.Terminal.currentText.Length - TerminalApi.TerminalApi.Terminal.textAdded) + text);
            Terminal.screenText.text = TerminalApi.TerminalApi.Terminal.currentText;
			Terminal.textAdded = text.Length;
        }
    }

	public class Keybinds : LcInputActions
	{
		[InputAction("<Keyboard>/downArrow", Name = "Next Command")]
		public InputAction NextTerminalKey { get; set; }
		[InputAction("<Keyboard>/upArrow", Name = "Previous Command")]
		public InputAction PrevTerminalKey { get; set; }
	}
}
