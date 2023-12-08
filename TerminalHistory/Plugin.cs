using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine.InputSystem;
using static TerminalApi.Events.Events;
using System.Collections.Generic;

namespace TerminalHistory
{
	[BepInPlugin("atomic.terminalhistory", "Terminal History", "1.0.0")]
	[BepInDependency("atomic.terminalapi", MinimumDependencyVersion: "1.3.0")]
	public partial class Plugin : BaseUnityPlugin
	{
		const int SIZE = 20;
		private Terminal Terminal;
		private readonly LinkedList<string> _commands = [];
		private InputAction _upArrow;
		private InputAction _downArrow;
		private int _index = -1;
		private void Awake()
		{
			Logger.LogInfo($"Plugin Terminal History is loaded!");
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
			TerminalStarted += OnTerminalStarted;
			TerminalExited += OnTerminalExited;
			TerminalBeginUsing += OnTerminalBeginUsing;
			TerminalParsedSentence += OnTerminalSubmit;
			TerminalTextChanged += OnTerminalTextChange;
		}

        private void OnTerminalTextChange(object sender, TerminalTextChangedEventArgs e)
        {
            if (e.CurrentInputText == string.Empty && _index != -1 )
			{
				_index = -1;
			}
        }

        private void OnTerminalSubmit(object sender, TerminalParseSentenceEventArgs e)
        {

	        if (_commands.First?.Value == e.SubmittedText)
	        {
		        return;
	        }

	        _commands.AddFirst(e.SubmittedText);
	        
	        if (_commands.Count > SIZE)
	        {
		        _commands.RemoveLast();
	        }
        }

        private void OnTerminalExited(object sender, TerminalEventArgs e)
        {
			_upArrow.performed -= OnUpArrowPerformed;
			_upArrow.Disable();

			_downArrow.performed -= OnDownArrowPerformed;
			_downArrow.Disable();
        }

        private void OnTerminalBeginUsing(object sender, TerminalEventArgs e)
        {
            _upArrow.Enable();
            _upArrow.performed += OnUpArrowPerformed;

            _downArrow.Enable();
            _downArrow.performed += OnDownArrowPerformed;
        }

        private void OnTerminalStarted(object sender, TerminalEventArgs e)
		{
			// _commands.Clear(); // Maybe keep the history across the game session?
			_index = -1;

			_upArrow = new InputAction("UpArrow", 0, "<Keyboard>/uparrow", "Press");
			_downArrow = new InputAction("DownArrow", 0, "<Keyboard>/downarrow", "Press");

			Terminal = TerminalApi.TerminalApi.Terminal;

        }

        private void OnDownArrowPerformed(InputAction.CallbackContext context)
        {
	        if (_commands.Count == 0 || !Terminal.terminalInUse)
	        {
		        return;
	        }

	        switch (--_index)
	        {
		        case < 0:
			        _index = -1;
			        break;
		        case 0:
			        LinkedListNode<string> firstCommand = _commands.First;
			        if (firstCommand == null)
			        {
				        return;
			        }

			        SetTerminalText(firstCommand.Value);
			        break;
		        default:
			        string command = GetValueFromIndex(_index);
			        SetTerminalText(command);
			        break;
	        }
        }

        private void OnUpArrowPerformed(InputAction.CallbackContext context)
        {
	        if (!Terminal.terminalInUse || _commands.Count <= 0)
	        {
		        return;
	        }

	        if (_index >= _commands.Count)
	        {
		        _index = _commands.Count - 1;
		        LinkedListNode<string> lastCommand = _commands.Last!;
		        SetTerminalText(lastCommand.Value);
	        }
	        else
	        {
		        string command = GetValueFromIndex(++_index);
		        SetTerminalText(command);
	        }
        }

		private void SetTerminalText(string text)
		{
			Terminal.TextChanged(TerminalApi.TerminalApi.Terminal.currentText.Substring(0, TerminalApi.TerminalApi.Terminal.currentText.Length - TerminalApi.TerminalApi.Terminal.textAdded) + text);
            Terminal.screenText.text = TerminalApi.TerminalApi.Terminal.currentText;
			Terminal.textAdded = text.Length;
        }
		
		private string GetValueFromIndex(int index)
		{
			if (index <= 0)
			{
				return _commands.First != null ? _commands.First.Value : string.Empty;
			}

			int count = _commands.Count;
			if (index >= count)
			{
				return _commands.Last!.Value;
			}

			int iterator = 0;
			LinkedListNode<string> currentNode = _commands.First!;

			while (iterator++ < index && currentNode.Next != null)
			{
				currentNode = currentNode.Next;
			}

			return currentNode.Value;
		}
    }
}