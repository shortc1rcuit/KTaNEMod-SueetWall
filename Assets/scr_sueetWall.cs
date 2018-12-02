﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using KMBombInfoHelper;
using UnityEngine;

using Random = UnityEngine.Random;
using Math = UnityEngine.Mathf;

public class scr_sueetWall : MonoBehaviour {
	public KMAudio BombAudio;
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMSelectable[] ModuleButtons;
	public KMSelectable ModuleSelect;
	public Texture2D[] SuitTexs = new Texture2D [0];

	delegate bool checkCond (int x, int y);
	bool[] correctButtons = new bool [20];
	List<int> pressedButtons = new List<int> ();

	bool moduleSolved;

	static int moduleIdCounter = 1;
	int moduleId;

	void Start () {
		moduleId = moduleIdCounter++;

		int[] chosenSuits = new int [20];
		int[] chosenNums = new int [20];
		int[] chosenColors = new int [20];

		for (int i = 0; i < ModuleButtons.Length; i++) {
			chosenSuits [i] = Random.Range (0, SuitTexs.Length);
			ModuleButtons [i].transform.GetChild (0).GetComponent<Renderer> ().material.SetTexture ("_MainTex", SuitTexs [chosenSuits [i]]);
			var suitText = ModuleButtons [i].transform.GetChild (1).GetComponent<TextMesh> ();
			chosenNums [i] = Random.Range (1, 101);
			suitText.text = chosenNums [i].ToString ();
			chosenColors [i] = Random.Range (0, 2);
			suitText.color = new [] { Color.black, Color.red } [chosenColors [i]];

			int j = i;

			ModuleButtons [i].OnInteract += delegate () {
				OnButtonPress (j);

				return false;
			};
		}

		int[] sideDir = { -6, -5, -4, -1, 1, 4, 5, 6 };
		int[,] checkSides = {
			{ 0, 2, 5, 7 },
			{ 0, 1, 2, 6 },
			{ 1, 5, 6, 7 },
			{ 1, 3, 4, 6 }
		};

		var initTime = (int)BombInfo.GetTime () / 60;
		checkCond[,] getConds = {
			{ ((x, y) => chosenNums [x] > chosenNums [y]), ((x, y) => chosenNums [x] < initTime), ((x, y) => chosenSuits [x] == chosenSuits [y]), ((x, y) => chosenSuits [x] % 2 == chosenSuits [y] % 2) },
			{ ((x, y) => chosenNums [x] < chosenNums [y]), ((x, y) => chosenNums [x] > initTime), ((x, y) => chosenSuits [x] != chosenSuits [y]), ((x, y) => chosenColors [x] == chosenColors [y]) }
		};

		for (int i = 0; i < ModuleButtons.Length; i++) {
			var checkSuits = 0;

			for (int j = 0; j < 4; j++) {
				var checkButton = GetWrapedTable (i, sideDir [checkSides [chosenSuits [i], j]], ModuleButtons.Length, 4);
				checkSuits += (getConds [chosenColors [i], chosenSuits [i]] (checkButton, i)) ? 1 : 0;
			}

			correctButtons [i] = (checkSuits == 4);

			if (correctButtons [i]) {
				Debug.LogFormat (@"[Sueet Wall #{0}] Button #{1} is correct.", moduleId, i + 1);
			}
		}

		if (correctButtons.Count (x => x == true) == 0) {
			Debug.LogFormat (@"[Sueet Wall #{0}] No buttons are correct, you can press any button.", moduleId);
		}
	}

	int GetWrapedTable (int setButton, int tempDir, int tableSize, int tableRows) {
		var tempSquare = (Math.Abs (tempDir) == 4) ? -1 * (int)Math.Sign (tempDir) : (Math.Abs (tempDir) % 5) * (int)Math.Sign (tempDir);

		if ((setButton % tableRows) + tempSquare == -1 || (setButton % tableRows) + tempSquare == tableRows) {
			tempSquare = (tableRows - 1) * ((((setButton % tableRows) + tempSquare) == tableRows) ? -1 : 1);
		}

		setButton += tempSquare;

		if (Math.Abs (tempDir) != 1 && Math.Abs (tempDir) != 0) {
			tempSquare = tableRows * (int)Math.Sign (tempDir);
			setButton += tempSquare;
		}

		if (setButton < 0) {
			setButton = tableSize - Math.Abs (setButton);
		}

		setButton %= tableSize;

		return setButton;
	}

	void OnButtonPress (int buttonPressed) {
		BombAudio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.ButtonPress, transform);
		ModuleSelect.AddInteractionPunch ();

		if (moduleSolved || pressedButtons.Contains (buttonPressed)) {
			return;
		}

		var matColor = ModuleButtons [buttonPressed].transform.GetComponent<Renderer> ();

		if (correctButtons.Count (x => x == true) == 0) {
			correctButtons [buttonPressed] = true;
		}

		if (correctButtons [buttonPressed]) {
			matColor.material.color = new Color32 (181, 255, 181, 255);
			pressedButtons.Add (buttonPressed);
			Debug.LogFormat (@"[Sueet Wall #{0}] Button #{1} correctly pressed!", moduleId, buttonPressed + 1);

			if (pressedButtons.Count == correctButtons.Count (x => x == true)) {
				BombModule.HandlePass ();

				for (int i = 0; i < ModuleButtons.Length; i++) {
					ModuleButtons [i].transform.GetComponent<Renderer> ().material.color = (correctButtons [i]) ? new Color32 (181, 255, 181, 255) : new Color32 (128, 191, 255, 255);
				}

				moduleSolved = true;
				Debug.LogFormat (@"[Sueet Wall #{0}] Module solved!", moduleId);
			}
		} else {
			matColor.material.color = new Color32 (128, 191, 255, 255);
			Debug.LogFormat (@"[Sueet Wall #{0}] Button #{1} is incorrect.", moduleId, buttonPressed + 1);
			BombModule.HandleStrike ();
		}
	}

	#pragma warning disable 414
		private readonly string TwitchHelpMessage = @"!{0} press 1 2 3... (buttons to press [within the range 1-20])";
	#pragma warning restore 414

	KMSelectable[] ProcessTwitchCommand (string command) {
		command = command.ToLowerInvariant ().Trim ();

		if (Regex.IsMatch (command, @"^press +[0-9^, |&]+$")) {
			command = command.Substring (6).Trim ();

			var presses = command.Split (new [] { ',', ' ', '|', '&' }, System.StringSplitOptions.RemoveEmptyEntries);
			var pressList = new List<KMSelectable> ();

			for (int i = 0; i < presses.Length; i++) {
				if (Regex.IsMatch (presses [i], @"^[0-9]{1,2}$")) {
					pressList.Add (ModuleButtons [Math.Clamp (Math.Max (0, int.Parse (presses [i].ToString ()) - 1), 0, ModuleButtons.Length - 1)]);
				}
			}

			return pressList.ToArray ();
		}

		return null;
	}
}