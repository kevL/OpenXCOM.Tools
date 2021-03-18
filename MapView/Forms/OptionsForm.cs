using System;
using System.Reflection;
using System.Windows.Forms;

using DSShared;

using MapView.Forms.MainView;
using MapView.Forms.Observers;


namespace MapView
{
	/// <summary>
	/// An Options form.
	/// </summary>
	internal sealed class OptionsForm
		:
			Form
	{
		internal enum OptionableType
		{
			MainView,
			TileView,
			TopView,
			RouteView
		}


		#region Fields (static)
		private const string GridViewEdit = "GridViewEdit";	// the currently edited field
		private const string DocComment   = "DocComment";	// the Description area
		private const string userSized    = "userSized";	// tells .net that the Description area has been/can be resized
		#endregion Fields (static)


		#region Fields
		/// <summary>
		/// The viewer that this belongs to as an OptionableType.
		/// </summary>
		private OptionableType _oType;

		/// <summary>
		/// The Description area control - used to get/set each viewers'
		/// 'DescriptionHeight' option.
		/// @note .net appears to handle heights that are too large etc okay.
		/// </summary>
		private Control _desc;

		/// <summary>
		/// True bypasses eventhandlers during instantiation.
		/// </summary>
		private bool _init;
		#endregion Fields


		#region cTor
		/// <summary>
		/// cTor. Constructs an OptionsForm.
		/// </summary>
		/// <param name="o">a class-object w/ Properties that are optionable</param>
		/// <param name="options">its Options</param>
		/// <param name="oType">its optionable type</param>
		internal OptionsForm(
				object o,
				Options options,
				OptionableType oType)
		{
			_init = true;
			InitializeComponent();

			_desc = null;
			foreach (Control control in propertyGrid.Controls)
			if (control.GetType().Name == DocComment)
			{
				_desc = control;
				break;
			}
			_desc.SizeChanged += OnDescriptionSizeChanged;

			_desc.GetType().BaseType.GetField(userSized, BindingFlags.Instance
													   | BindingFlags.NonPublic).SetValue(_desc, true);

			propertyGrid.Options = options;

			switch (_oType = oType)
			{
				case OptionableType.MainView:
					propertyGrid.SelectedObject = o as MainViewOptionables;
					_desc.Height = MainViewF.Optionables.DescriptionHeight;
					break;
				case OptionableType.TileView:
					propertyGrid.SelectedObject = o as TileViewOptionables;
					_desc.Height = TileView.Optionables.DescriptionHeight;
					break;
				case OptionableType.TopView:
					propertyGrid.SelectedObject = o as TopViewOptionables;
					_desc.Height = TopView.Optionables.DescriptionHeight;
					break;
				case OptionableType.RouteView:
					propertyGrid.SelectedObject = o as RouteViewOptionables;
					_desc.Height = RouteView.Optionables.DescriptionHeight;
					break;
			}

			RegistryInfo.RegisterProperties(this); // NOTE: 1 metric for all four types
			_init = false;
		}
		#endregion cTor


		#region Events (override)
		/// <summary>
		/// Handles command-key processing. Closes this form when either of
		/// [Esc] or [Ctrl+o] is pressed.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Control | Keys.O: // non-whiteman code-page users beware ... Mista Kurtz, he dead. GLORY TO THE LOST CAUSE!!! yeah whatever.
				case Keys.Escape:
					if (!FindFocusedControl().GetType().ToString().Contains(GridViewEdit))
					{
						Close();
						return true;
					}
					break;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}
		#endregion (override)


		#region Events
		/// <summary>
		/// Handles the SizeChanged event of the Description area.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnDescriptionSizeChanged(object sender, EventArgs e)
		{
			if (!_init && WindowState == FormWindowState.Normal)
			{
				switch (_oType)
				{
					case OptionableType.MainView:
						MainViewF.Optionables.DescriptionHeight = _desc.Height;
						break;
					case OptionableType.TileView:
						TileView .Optionables.DescriptionHeight = _desc.Height;
						break;
					case OptionableType.TopView:
						TopView  .Optionables.DescriptionHeight = _desc.Height;
						break;
					case OptionableType.RouteView:
						RouteView.Optionables.DescriptionHeight = _desc.Height;
						break;
				}
			}
		}

		/// <summary>
		/// Handles this form's VisibleChanged event.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>The cached metric of an OptionsForm is updated every time
		/// the form is hidden, unlike other viewers that update their metrics
		/// only when MapView quits.</remarks>
		private void OnVisibleChanged(object sender, EventArgs e)
		{
			if (!Visible)
				RegistryInfo.UpdateRegistry(this, true); // NOTE: 1 metric for all four types
		}
		#endregion Events


		#region Methods
		/// <summary>
		/// Finds the focused control in this container.
		/// </summary>
		/// <returns></returns>
		private Control FindFocusedControl()
		{
			Control control = null;

			var container = this as ContainerControl;
			while (container != null)
			{
				control = container.ActiveControl;
				container = control as ContainerControl;
			}
			return control;
		}
		#endregion Methods



		#region Designer
		internal OptionsPropertyGrid propertyGrid;

		private void InitializeComponent()
		{
			this.propertyGrid = new MapView.OptionsPropertyGrid();
			this.SuspendLayout();
			// 
			// propertyGrid
			// 
			this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.propertyGrid.Location = new System.Drawing.Point(0, 0);
			this.propertyGrid.Margin = new System.Windows.Forms.Padding(0);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(592, 374);
			this.propertyGrid.TabIndex = 0;
			// 
			// OptionsForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
			this.ClientSize = new System.Drawing.Size(592, 374);
			this.Controls.Add(this.propertyGrid);
			this.Font = new System.Drawing.Font("Verdana", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MinimumSize = new System.Drawing.Size(500, 300);
			this.Name = "OptionsForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Custom PropertyGrid";
			this.VisibleChanged += new System.EventHandler(this.OnVisibleChanged);
			this.ResumeLayout(false);

		}
		#endregion Designer
	}
}
