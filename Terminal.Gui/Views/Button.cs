﻿//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	///   Button is a <see cref="View"/> that provides an item that invokes an <see cref="Action"/> when activated by the user.
	/// </summary>
	/// <remarks>
	/// <para>
	///   Provides a button showing text invokes an <see cref="Action"/> when clicked on with a mouse
	///   or when the user presses SPACE, ENTER, or hotkey. The hotkey is specified by the first uppercase
	///   letter in the button.
	/// </para>
	/// <para>
	///   When the button is configured as the default (<see cref="IsDefault"/>) and the user presses
	///   the ENTER key, if no other <see cref="View"/> processes the <see cref="KeyEvent"/>, the <see cref="Button"/>'s
	///   <see cref="Action"/> will be invoked.
	/// </para>
	/// </remarks>
	public class Button : View {
		ustring text;
		ustring shown_text;
		Rune hot_key;
		int hot_pos = -1;
		bool is_default;

		/// <summary>
		/// Gets or sets whether the <see cref="Button"/> is the default action to activate in a dialog.
		/// </summary>
		/// <value><c>true</c> if is default; otherwise, <c>false</c>.</value>
		public bool IsDefault {
			get => is_default;
			set {
				is_default = value;
				Update ();
			}
		}

		/// <summary>
		///   Clicked <see cref="Action"/>, raised when the button is clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public Action Clicked;

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> based on the given text at position 0,0
		/// </summary>
		/// <remarks>
		///   The size of the <see cref="Button"/> is computed based on the
		///   text length. 
		/// </remarks>
		/// <param name="text">The button's text</param>
		/// <param name="is_default">If set, this makes the button the default button in the current view. <seealso cref="IsDefault"/></param>
		public Button (ustring text, bool is_default = false) : base ()
		{
			CanFocus = true;
			this.IsDefault = is_default;
			Text = text;
			int w = SetWidthHeight (text, is_default);
			Frame = new Rect (0, 0, w, 1);
		}

		int SetWidthHeight (ustring text, bool is_default)
		{
			int w = text.Length + 4 + (is_default ? 2 : 0);
			Width = w;
			Height = 1;
			return w;
		}

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> at the given coordinates, based on the given text
		/// </summary>
		/// <remarks>
		///   The size of the <see cref="Button"/> is computed based on the
		///   text length. 
		/// </remarks>
		/// <param name="x">X position where the button will be shown.</param>
		/// <param name="y">Y position where the button will be shown.</param>
		/// <param name="text">The button's text</param>
		public Button (int x, int y, ustring text) : this (x, y, text, false) { }

		/// <summary>
		///   The text displayed by this <see cref="Button"/>.
		/// </summary>
		public ustring Text {
			get {
				return text;
			}

			set {
				if (text?.Length != value?.Length) {
					SetWidthHeight (value, is_default);
				}
				text = value;
				Update ();
			}
		}

		internal void Update ()
		{
			if (IsDefault)
				shown_text = "[< " + text + " >]";
			else
				shown_text = "[ " + text + " ]";

			hot_pos = -1;
			hot_key = (Rune)0;
			int i = 0;
			foreach (Rune c in shown_text) {
				if (Rune.IsUpper (c)) {
					hot_key = c;
					hot_pos = i;
					break;
				}
				i++;
			}
			SetNeedsDisplay ();
		}

		/// <summary>
		///   Initializes a new instance of <see cref="Button"/> at the given coordinates, based on the given text, and with the specified <see cref="IsDefault"/> value
		/// </summary>
		/// <remarks>
		///   If the value for is_default is true, a special
		///   decoration is used, and the enter key on a
		///   dialog would implicitly activate this button.
		/// </remarks>
		/// <param name="x">X position where the button will be shown.</param>
		/// <param name="y">Y position where the button will be shown.</param>
		/// <param name="text">The button's text</param>
		/// <param name="is_default">If set, this makes the button the default button in the current view, which means that if the user presses return on a view that does not handle return, it will be treated as if he had clicked on the button</param>
		public Button (int x, int y, ustring text, bool is_default)
		    : base (new Rect (x, y, text.Length + 4 + (is_default ? 2 : 0), 1))
		{
			CanFocus = true;

			this.IsDefault = is_default;
			Text = text;
		}

		///<inheritdoc cref="Redraw(Rect)"/>
		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			Move (0, 0);
			Driver.AddStr (shown_text);

			if (hot_pos != -1) {
				Move (hot_pos, 0);
				Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.HotNormal);
				Driver.AddRune (hot_key);
			}
		}

		///<inheritdoc cref="PositionCursor"/>
		public override void PositionCursor ()
		{
			Move (hot_pos == -1 ? 1 : hot_pos, 0);
		}

		bool CheckKey (KeyEvent key)
		{
			if (Char.ToUpper ((char)key.KeyValue) == hot_key) {
				this.SuperView.SetFocus (this);
				Clicked?.Invoke ();
				return true;
			}
			return false;
		}

		///<inheritdoc cref="ProcessHotKey"/>
		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.IsAlt)
				return CheckKey (kb);

			return false;
		}

		///<inheritdoc cref="ProcessColdKey"/>
		public override bool ProcessColdKey (KeyEvent kb)
		{
			if (IsDefault && kb.KeyValue == '\n') {
				if (Clicked != null)
					Clicked ();
				return true;
			}
			return CheckKey (kb);
		}

		///<inheritdoc cref="ProcessKey"/>
		public override bool ProcessKey (KeyEvent kb)
		{
			var c = kb.KeyValue;
			if (c == '\n' || c == ' ' || Rune.ToUpper ((uint)c) == hot_key) {
				if (Clicked != null)
					Clicked ();
				return true;
			}
			return base.ProcessKey (kb);
		}

		///<inheritdoc cref="MouseEvent"/>
		public override bool MouseEvent(MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Clicked) {
				SuperView.SetFocus (this);
				SetNeedsDisplay ();

				if (Clicked != null)
					Clicked ();
				return true;
			}
			return false;
		}
	}
}
