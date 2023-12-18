using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine.InputSystem;
using TApi = TerminalApi.TerminalApi;
using static TerminalApi.Events.Events;
using System.Collections.Generic;
using LethalCompanyInputUtils.Api;

namespace TerminalHistory
{
	[BepInPlugin("atomic.terminalhistory", "Terminal History", "1.0.4")]
	[BepInDependency("atomic.terminalapi", MinimumDependencyVersion: "1.3.0")]
	[BepInDependency("com.rune580.LethalCompanyInputUtils", MinimumDependencyVersion: "0.4.2")]
	public partial class Plugin : BaseUnityPlugin
	{
		const int SIZE = 20;
		private Terminal Terminal;
		private List<string> _commands = new List<string>();
		private string _commandDraft = ""; //? if the user uses the prev key while already having a command typed, it will be saved here
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
            if (_index != -1 )
			{
				if(e.CurrentInputText != _commands[_index])
				{
					_commandDraft = e.CurrentInputText;
					_index = -1;
				}
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

			_index = -1; //? reset the index when the terminal is exited
			_commandDraft = ""; //? reset the command draft when the terminal is exited
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

			Terminal = TApi.Terminal;
        }

        private void OnDownArrowPerformed(InputAction.CallbackContext context)
        {
			if (_commands.Count > 0 && Terminal.terminalInUse)
			{
				_index--;
				if (_index <= -1)
				{
					if(_index == -1)
					{
                        SetTerminalText(_commandDraft);
                    }
                    _index = -1;
                    
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
				if (_index == -1)
				{
					_commandDraft = TApi.GetTerminalInput();
				}

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
			Terminal.TextChanged(TApi.Terminal.currentText.Substring(0, TApi.Terminal.currentText.Length - TApi.Terminal.textAdded) + text);
            Terminal.screenText.text = TApi.Terminal.currentText;
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
