﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

using MapView.Properties;

using XCom;


namespace MapView.Forms.MainView
{
	internal sealed class ToolstripFactory
	{
		#region Fields
		private readonly List<ToolStripButton> _editors = new List<ToolStripButton>(); // all edit-buttons except the pasters
		private readonly List<ToolStripButton> _pasters = new List<ToolStripButton>();
		private readonly List<ToolStripButton> _downrs  = new List<ToolStripButton>();
		private readonly List<ToolStripButton> _uppers  = new List<ToolStripButton>();

		// The instantiations of toolstrip-objects that are classvars are for
		// MainView, while the toolstrip-objects for TopView and
		// TopRouteView(Top) are instantiated in the classfuncts.

		private readonly ToolStripTextBox   _tstbSearch     = new ToolStripTextBox();
		private readonly ToolStripButton    _tsbSearchClear = new ToolStripButton();

		private readonly ToolStripSeparator _tss0           = new ToolStripSeparator();

		private readonly ToolStripButton    _tsbScale       = new ToolStripButton();
		private readonly ToolStripButton    _tsbScaleOut    = new ToolStripButton();
		private readonly ToolStripButton    _tsbScaleIn     = new ToolStripButton();

		private readonly ToolStripSeparator _tss1           = new ToolStripSeparator();

		private readonly ToolStripButton    _tsbDown        = new ToolStripButton();
		private readonly ToolStripButton    _tsbUp          = new ToolStripButton();

		private readonly ToolStripSeparator _tss2           = new ToolStripSeparator();

		private readonly ToolStripButton    _tsbCut         = new ToolStripButton();
		private readonly ToolStripButton    _tsbCopy        = new ToolStripButton();
		private readonly ToolStripButton    _tsbPaste       = new ToolStripButton();
		private readonly ToolStripButton    _tsbDelete      = new ToolStripButton();

		private readonly ToolStripSeparator _tss3           = new ToolStripSeparator();

		private readonly ToolStripButton    _tsbFill        = new ToolStripButton();

		private readonly ToolStripSeparator _tss4           = new ToolStripSeparator();
		#endregion Fields


		#region Methods
		/// <summary>
		/// Adds a textfield for search to the specified toolstrip.
		/// </summary>
		/// <param name="ts"></param>
		/// <remarks>Appears only in MainView.</remarks>
		internal void CreateSearchTools(ToolStrip ts)
		{
			ts.Items.Add(_tstbSearch);
			ts.Items.Add(_tsbSearchClear);

			// Search textfield
			_tstbSearch.Name             = "tstbSearch";
			_tstbSearch.Text             = "search";
			_tstbSearch.KeyPress        += OnSearchKeyPress;

			// SearchClear btn
			_tsbSearchClear.Name         = "tsbSearchClear";
			_tsbSearchClear.ToolTipText  = "clear tree hilight";
			_tsbSearchClear.DisplayStyle = ToolStripItemDisplayStyle.Image;
			_tsbSearchClear.Image        = Resources.delete;
			_tsbSearchClear.Click       += OnClearHighlightClick;
		}

		/// <summary>
		/// Focuses the Search textbox.
		/// </summary>
		internal void FocusSearch()
		{
			_tstbSearch.Focus();
		}

		/// <summary>
		/// Gets the Search textbox's current text.
		/// </summary>
		/// <returns></returns>
		internal string GetSearchText()
		{
			return _tstbSearch.Text;
		}


		/// <summary>
		/// Adds buttons for zooming the map-scale to the specified toolstrip.
		/// </summary>
		/// <param name="ts"></param>
		/// <remarks>Appears only in MainView.</remarks>
		internal void CreateScalerTools(ToolStrip ts)
		{
			ts.Items.Add(_tss0);
			ts.Items.Add(_tsbScale);
			ts.Items.Add(_tsbScaleOut);
			ts.Items.Add(_tsbScaleIn);

			// AutoZoom btn
			_tsbScale.Name            = "tsbScale";
			_tsbScale.ToolTipText     = "autoscale";
			_tsbScale.DisplayStyle    = ToolStripItemDisplayStyle.Image;
			_tsbScale.Image           = Resources.scale;
			_tsbScale.Click          += MainViewF.that.OnAutoScaleClick;
			_tsbScale.Checked         = true;
			_tsbScale.Enabled         = false;

			// ZoomOut btn
			_tsbScaleOut.Name         = "tsbScaleOut";
			_tsbScaleOut.ToolTipText  = "scale Out";
			_tsbScaleOut.DisplayStyle = ToolStripItemDisplayStyle.Image;
			_tsbScaleOut.Image        = Resources.scaleOut;
			_tsbScaleOut.Click       += MainViewF.that.OnScaleOutClick;
			_tsbScaleOut.Enabled      = false;

			// ZoomIn btn
			_tsbScaleIn.Name          = "tsbScaleIn";
			_tsbScaleIn.ToolTipText   = "scale In";
			_tsbScaleIn.DisplayStyle  = ToolStripItemDisplayStyle.Image;
			_tsbScaleIn.Image         = Resources.scaleIn;
			_tsbScaleIn.Click        += MainViewF.that.OnScaleInClick;
			_tsbScaleIn.Enabled       = false;
		}

		/// <summary>
		/// Disables the auto-scale button when user switches to glide.
		/// </summary>
		internal void DisableScaleChecked()
		{
			_tsbScale.Checked = false;
		}

		/// <summary>
		/// Toggles the auto-scale button checked/unchecked.
		/// </summary>
		/// <returns>true if checked</returns>
		internal bool ToggleScaleChecked()
		{
			return (_tsbScale.Checked = !_tsbScale.Checked);
		}

		/// <summary>
		/// Enables MainView's auto-scale button.
		/// </summary>
		internal void EnableScaleAutoButton()
		{
			_tsbScale.Enabled = true;
		}

		/// <summary>
		/// Dis/enables the ScaleOut button.
		/// </summary>
		/// <param name="enabled"></param>
		internal void SetScaleOutButtonEnabled(bool enabled)
		{
			_tsbScaleOut.Enabled = enabled;
		}

		/// <summary>
		/// Dis/enables the ScaleIn button.
		/// </summary>
		/// <param name="enabled"></param>
		internal void SetScaleInButtonEnabled(bool enabled)
		{
			_tsbScaleIn.Enabled = enabled;
		}


		/// <summary>
		/// Adds buttons for Up,Down,Cut,Copy,Paste,Delete and Fill to the
		/// specified toolstrip in MainView as well as TopView and
		/// TopRouteView(Top).
		/// </summary>
		/// <param name="ts">a toolstrip to put the buttons in</param>
		/// <param name="tertiary">false for MainView's toolstrip, true for
		/// TopView's and TopRouteView's toolstrips</param>
		internal void CreateEditorTools(
				ToolStrip ts,
				bool tertiary = false)
		{
			ToolStripSeparator tss1;

			ToolStripButton tsbDown;	// NOTE: Down/Up are not really editor-objects ...
			ToolStripButton tsbUp;		// but they appear in TopView and TopRouteView(Top)
										// as well as MainView, with the editor-objects.
			ToolStripSeparator tss2;

			ToolStripButton tsbCut;
			ToolStripButton tsbCopy;
			ToolStripButton tsbPaste;
			ToolStripButton tsbDelete;

			ToolStripSeparator tss3;

			ToolStripButton tsbFill;

			ToolStripSeparator tss4;

			if (tertiary)
			{
				tss1      = new ToolStripSeparator();
				tsbDown   = new ToolStripButton();
				tsbUp     = new ToolStripButton();
				tss2      = new ToolStripSeparator();
				tsbCut    = new ToolStripButton();
				tsbCopy   = new ToolStripButton();
				tsbPaste  = new ToolStripButton();
				tsbDelete = new ToolStripButton();
				tss3      = new ToolStripSeparator();
				tsbFill   = new ToolStripButton();
				tss4      = new ToolStripSeparator();
			}
			else
			{
				tss1      = _tss1;
				tsbDown   = _tsbDown;
				tsbUp     = _tsbUp;
				tss2      = _tss2;
				tsbCut    = _tsbCut;
				tsbCopy   = _tsbCopy;
				tsbPaste  = _tsbPaste;
				tsbDelete = _tsbDelete;
				tss3      = _tss3;
				tsbFill   = _tsbFill;
				tss4      = _tss4;
			}

			var its = new ToolStripItem[]
			{
				tss1, // NOTE: c#/.NET cant figure out how to use 1 separator 4 times.
				tsbDown,
				tsbUp,
				tss2,
				tsbCut,
				tsbCopy,
				tsbPaste,
				tsbDelete,
				tss3,
				tsbFill,
				tss4
			};
			ts.Items.AddRange(its);

			// LevelDown btn
			tsbDown.Name          = "tsbDown";
			tsbDown.ToolTipText   = "level down";
			tsbDown.DisplayStyle  = ToolStripItemDisplayStyle.Image;
			tsbDown.Image         = Resources.down;
			tsbDown.Click        += OnDownClick;
			tsbDown.Enabled       = false;

			_downrs.Add(tsbDown);

			// LevelUp btn
			tsbUp.Name             = "tsbUp";
			tsbUp.ToolTipText      = "level up";
			tsbUp.DisplayStyle     = ToolStripItemDisplayStyle.Image;
			tsbUp.Image            = Resources.up;
			tsbUp.Click           += OnUpClick;
			tsbUp.Enabled          = false;

			_uppers.Add(tsbUp);

			// Cut btn
			tsbCut.Name            = "tsbCut";
			tsbCut.ToolTipText     = "cut";
			tsbCut.DisplayStyle    = ToolStripItemDisplayStyle.Image;
			tsbCut.Image           = Resources.cut;
			tsbCut.Click          += MainViewOverlay.that.OnCut;
			tsbCut.Enabled         = false;
//			tsbCut.Click          += (sender, e) => // -> example of ... lambda usage
//									{
//										EnablePasteButton();
//										MainViewUnderlay.OnCut(sender, e);
//									};

			_editors.Add(tsbCut);

			// Copy btn
			tsbCopy.Name           = "tsbCopy";
			tsbCopy.ToolTipText    = "copy";
			tsbCopy.DisplayStyle   = ToolStripItemDisplayStyle.Image;
			tsbCopy.Image          = Resources.copy;
			tsbCopy.Click         += MainViewOverlay.that.OnCopy;
			tsbCopy.Enabled        = false;
//			tsbCopy.Click         += (sender, e) => // -> example of ... lambda usage
//									{
//										EnablePasteButton();
//										MainViewUnderlay.OnCopy(sender, e);
//									};

			_editors.Add(tsbCopy);

			// Paste btn
			tsbPaste.Name          = "tsbPaste";
			tsbPaste.ToolTipText   = "paste";
			tsbPaste.DisplayStyle  = ToolStripItemDisplayStyle.Image;
			tsbPaste.Image         = Resources.paste;
			tsbPaste.Click        += MainViewOverlay.that.OnPaste;
			tsbPaste.Enabled       = false;

			_pasters.Add(tsbPaste);

			// Delete btn
			tsbDelete.Name         = "tsbDelete";
			tsbDelete.ToolTipText  = "delete";
			tsbDelete.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbDelete.Image        = Resources.delete;
			tsbDelete.Click       += MainViewOverlay.that.OnDelete;
			tsbDelete.Enabled      = false;

			_editors.Add(tsbDelete);

			// Fill btn
			tsbFill.Name           = "tsbFill";
			tsbFill.ToolTipText    = "fill";
			tsbFill.DisplayStyle   = ToolStripItemDisplayStyle.Image;
			tsbFill.Image          = Resources.fill;
			tsbFill.Click         += MainViewOverlay.that.OnFill;
			tsbFill.Enabled        = false;

			_editors.Add(tsbFill);
		}


		/// <summary>
		/// Dis/enables all level up/down buttons on the toolstrips.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="levels"></param>
		internal void EnableLevelers(int level, int levels)
		{
			bool enabled = (level != levels - 1);
			foreach (var tsb in _downrs)
				tsb.Enabled = enabled;

			enabled = (level != 0);
			foreach (var tsb in _uppers)
				tsb.Enabled = enabled;
		}

		/// <summary>
		/// Dis/enables all edit buttons on the toolstrips.
		/// </summary>
		/// <param name="enabled"></param>
		internal void EnableEditors(bool enabled)
		{
			foreach (var tsb in _editors)
				tsb.Enabled = enabled;
		}

		/// <summary>
		/// Dis/enables all paste buttons on the toolstrips.
		/// </summary>
		/// <param name="enabled"></param>
		internal void EnablePasters(bool enabled = true)
		{
			foreach (var tsb in _pasters)
				tsb.Enabled = enabled;
		}
		#endregion Methods


		#region Events (level)
		/// <summary>
		/// Handles a level-down click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDownClick(object sender, EventArgs e)
		{
			MapFile file = MainViewOverlay.that.MapFile;
			if (file != null)
			{
				file.ChangeLevel(MapFile.LEVEL_Dn);
				EnableLevelers(file.Level, file.MapSize.Levs);
			}
		}

		/// <summary>
		/// Handles a level-up click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnUpClick(object sender, EventArgs e)
		{
			MapFile file = MainViewOverlay.that.MapFile;
			if (file != null)
			{
				file.ChangeLevel(MapFile.LEVEL_Up);
				EnableLevelers(file.Level, file.MapSize.Levs);
			}
		}
		#endregion Events (level)


		#region Events (search)
		/// <summary>
		/// Handler for pressing the Enter-key when the search-textbox is
		/// focused.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnSearchKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter
				&& Control.ModifierKeys == Keys.None)
			{
				e.Handled = true;
				MainViewF.that.Search(_tstbSearch.Text);
			}
		}

		/// <summary>
		/// Clears the searched, found, and highlighted Treenode.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnClearHighlightClick(object sender, EventArgs e)
		{
			MainViewF.that.ClearSearched();
			_tstbSearch.Focus();
		}
		#endregion Events (search)
	}
}
